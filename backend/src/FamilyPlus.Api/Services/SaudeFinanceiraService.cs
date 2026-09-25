using System.Text.Json;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class SaudeFinanceiraService(FinanceDbContext db, BudgetService budgets, Func<DateTimeOffset>? utcNow = null)
{
    private DateTimeOffset UtcNow() => (utcNow ?? (() => DateTimeOffset.UtcNow))();

    public async Task<FinancialHealthProfileResponse> GetProfileAsync(Guid? memberId)
    {
        var row = await FindProfileAsync(memberId);
        if (row is null) return new FinancialHealthProfileResponse(memberId, 6m, 30m, 20m, null, [], false);
        return ToResponse(row, true);
    }

    public async Task<FinancialHealthProfileResponse> SaveProfileAsync(FinancialHealthProfileRequest request)
    {
        var familiaId = db.CurrentFamiliaId ?? throw new InvalidOperationException("O contexto da família ativa é obrigatório para salvar o perfil.");
        if (request.MetaReservaMeses is < 1 or > 60) throw new DomainException("A meta de reserva deve ficar entre 1 e 60 meses.");
        if (request.TetoComprometimentoPercentual is < 0 or > 100) throw new DomainException("O teto de comprometimento deve ficar entre 0 e 100%.");
        if (request.MetaPoupancaPercentual is < 0 or > 100) throw new DomainException("A meta de poupança deve ficar entre 0 e 100%.");
        if (request.Observacao?.Length > 1000) throw new DomainException("A observação deve ter no máximo 1000 caracteres.");

        if (request.MembroId.HasValue && !await db.Membros.AnyAsync(x => x.FamiliaId == familiaId && x.Id == request.MembroId.Value && x.Ativo))
            throw new DomainException("O membro selecionado não pertence à família ativa ou está arquivado.");

        var categoryIds = request.CategoriasEssenciais.Distinct().ToArray();
        var validCategories = await db.Categorias.Where(x => x.FamiliaId == familiaId && categoryIds.Contains(x.Id) && x.Ativo && x.Tipo == TipoCategoria.Despesa).Select(x => x.Id).ToListAsync();
        if (validCategories.Count != categoryIds.Length) throw new DomainException("Selecione apenas categorias de despesa ativas da família.");

        var row = await db.PerfisSaudeFinanceira.SingleOrDefaultAsync(x => x.FamiliaId == familiaId && x.MembroId == request.MembroId);
        if (row is null)
        {
            row = new PerfilSaudeFinanceira { FamiliaId = familiaId, MembroId = request.MembroId };
            db.PerfisSaudeFinanceira.Add(row);
        }
        row.MetaReservaMeses = request.MetaReservaMeses;
        row.TetoComprometimentoPercentual = request.TetoComprometimentoPercentual;
        row.MetaPoupancaPercentual = request.MetaPoupancaPercentual;
        row.Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim();
        row.CategoriasEssenciaisJson = JsonSerializer.Serialize(categoryIds);
        row.AtualizadoEm = UtcNow();
        await db.SaveChangesAsync();
        return ToResponse(row, true);
    }

    public async Task<FinancialHealthResponse> CalculateAsync(Guid? memberId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        var familiaId = db.CurrentFamiliaId ?? throw new InvalidOperationException("O contexto da família ativa é obrigatório para calcular os indicadores.");
        var profileRow = await FindProfileAsync(memberId);
        var profile = profileRow is null
            ? new FinancialHealthProfileResponse(memberId, 6m, 30m, 20m, null, [], false)
            : ToResponse(profileRow, true);

        var selectedStart = NormalizeBoundary(periodStart);
        var referenceEnd = NormalizeBoundary(periodEnd);
        if (referenceEnd < selectedStart) throw new DomainException("O fim do período deve ser igual ou posterior ao início.");
        var historicalEnd = new DateTimeOffset(referenceEnd.Year, referenceEnd.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var historicalStart = historicalEnd.AddMonths(-6);
        var periodEndExclusive = referenceEnd.AddDays(1);
        var analysisStart = selectedStart < historicalStart ? selectedStart : historicalStart;
        var familyAccounts = await db.Contas.AsNoTracking().Where(x => x.FamiliaId == familiaId && x.Ativo).ToListAsync();
        var memberAccounts = familyAccounts.Where(x => !memberId.HasValue || x.MembroId == memberId).ToList();
        var accounts = memberAccounts.Where(x => x.Tipo is TipoConta.ContaCorrente or TipoConta.ContaPoupanca or TipoConta.ContaDigital).ToList();
        var accountIds = memberAccounts.Select(x => x.Id).ToArray();
        var eligibleAccountIds = accounts.Select(x => x.Id).ToArray();
        // O provedor SQLite usado pelo aplicativo não traduz DateTimeOffset em LINQ.
        // A consulta parametrizada mantém o recorte temporal no banco sem perder compatibilidade.
        var transactions = await db.Transacoes
            .FromSqlInterpolated($"SELECT * FROM transacao WHERE FamiliaId = {familiaId} AND Status = {(int)StatusTransacao.EFETIVADA} AND DataCompetencia >= {analysisStart}")
            .AsNoTracking()
            .ToListAsync();
        var periodTransactions = transactions
            .Where(x => accountIds.Contains(x.ContaId)
                && (!memberId.HasValue || x.MembroId == memberId)
                && x.Status == StatusTransacao.EFETIVADA
                && x.Origem != OrigemTransacao.PAGAMENTO_FATURA
                && x.TransferenciaId == null
                && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA)
                && x.DataMovimentacao >= selectedStart
                && x.DataMovimentacao < periodEndExclusive)
            .ToList();
        var periodIncome = periodTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorCentavos);
        var purchases = await db.ComprasCartao
            .FromSqlInterpolated($"SELECT * FROM compra_cartao WHERE FamiliaId = {familiaId} AND Status = {(int)StatusCompraCartao.ATIVA} AND DataCompra >= {analysisStart}")
            .AsNoTracking()
            .ToListAsync();
        var periodPurchases = purchases
            .Where(x => x.Status == StatusCompraCartao.ATIVA
                && (!memberId.HasValue || x.MembroId == memberId)
                && x.DataCompra >= selectedStart
                && x.DataCompra < periodEndExclusive)
            .ToList();
        var historicalTransactions = transactions
            .Where(x => accountIds.Contains(x.ContaId)
                && (!memberId.HasValue || x.MembroId == memberId)
                && x.Status == StatusTransacao.EFETIVADA
                && x.Origem != OrigemTransacao.PAGAMENTO_FATURA
                && x.TransferenciaId == null
                && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA)
                && x.DataCompetencia >= historicalStart
                && x.DataCompetencia < historicalEnd)
            .ToList();
        var historicalPurchases = purchases
            .Where(x => x.Status == StatusCompraCartao.ATIVA
                && (!memberId.HasValue || x.MembroId == memberId)
                && x.DataCompra >= historicalStart
                && x.DataCompra < historicalEnd)
            .ToList();
        var purchaseIds = purchases.Select(x => x.Id).ToArray();
        var refunds = purchaseIds.Length == 0
            ? new List<EstornoCartao>()
            : await db.EstornosCartao.AsNoTracking()
                .Where(x => x.FamiliaId == familiaId && purchaseIds.Contains(x.CompraCartaoId))
                .ToListAsync();
        var refundsByPurchase = refunds.GroupBy(x => x.CompraCartaoId).ToDictionary(x => x.Key, x => x.Sum(y => y.ValorCentavos));
        var periodExpense = periodTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => x.ValorCentavos)
            + periodPurchases.Sum(x => Math.Max(0, x.ValorTotalCentavos - refundsByPurchase.GetValueOrDefault(x.Id)));
        var savingRate = periodIncome > 0 ? Metric((decimal)(periodIncome - periodExpense) * 100 / periodIncome) : Insufficient();

        var monthsWithHistory = historicalTransactions.Select(x => (x.DataCompetencia.Year, x.DataCompetencia.Month))
            .Concat(historicalPurchases.Select(x => (x.DataCompra.Year, x.DataCompra.Month))).Distinct().Count();
        var enoughHistory = monthsWithHistory >= 3;
        var averageIncome = enoughHistory ? historicalTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorCentavos) / 6m : 0m;
        var essentialPurchases = historicalPurchases.Where(x => profile.CategoriasEssenciais.Contains(x.CategoriaId)).Sum(x => Math.Max(0, x.ValorTotalCentavos - refundsByPurchase.GetValueOrDefault(x.Id)));
        var averageEssentialExpenses = enoughHistory && profile.CategoriasEssenciais.Count > 0
            ? (historicalTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA && x.CategoriaId.HasValue && profile.CategoriasEssenciais.Contains(x.CategoriaId.Value)).Sum(x => x.ValorCentavos) + essentialPurchases) / 6m
            : 0m;

        var recurringCandidates = await db.Recorrencias.AsNoTracking()
            .Where(x => x.FamiliaId == familiaId && x.Status == StatusRecorrencia.ATIVA && x.Tipo == TipoTransacao.DESPESA)
            .ToListAsync();
        var recurring = recurringCandidates
            .Where(x => x.DataInicio <= referenceEnd
                && (!x.DataFim.HasValue || x.DataFim.Value >= selectedStart)
                && (!memberId.HasValue || x.MembroId == memberId))
            .ToList();
        var recurringExpense = recurring.Where(x => x.Classificacao == TipoRecorrencia.NORMAL || x.Classificacao == TipoRecorrencia.CONTA_FIXA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var fixedExpense = recurring.Where(x => x.Classificacao == TipoRecorrencia.CONTA_FIXA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var commitment = enoughHistory && averageIncome > 0 ? Metric((decimal)recurringExpense * 100 / averageIncome) : Insufficient();
        var fixedShare = enoughHistory && averageIncome > 0 ? Metric((decimal)fixedExpense * 100 / averageIncome) : Insufficient();

        var balanceDeltas = await db.Transacoes.AsNoTracking()
            .Where(x => x.FamiliaId == familiaId && x.Status == StatusTransacao.EFETIVADA)
            .GroupBy(x => x.ContaId)
            .Select(group => new
            {
                ContaId = group.Key,
                DeltaCentavos = group.Sum(x => x.Tipo == TipoTransacao.RECEITA || x.Tipo == TipoTransacao.TRANSFERENCIA_ENTRADA
                    ? x.ValorCentavos
                    : x.Tipo == TipoTransacao.DESPESA || x.Tipo == TipoTransacao.TRANSFERENCIA_SAIDA
                        ? -x.ValorCentavos
                        : 0)
            })
            .ToDictionaryAsync(x => x.ContaId, x => x.DeltaCentavos);
        var available = accounts.Sum(account => Math.Max(0, FinanceCalculator.CalculateBalance(
            account.SaldoInicialCentavos,
            balanceDeltas.TryGetValue(account.Id, out var delta)
                ? [(TipoTransacao.RECEITA, delta, StatusTransacao.EFETIVADA)]
                : [])));
        var reserve = profile.CategoriasEssenciais.Count == 0
            ? new FinancialHealthMetric(null, "categorias_nao_configuradas")
            : enoughHistory && averageEssentialExpenses > 0 ? Metric((decimal)available / averageEssentialExpenses) : Insufficient();

        var budget = await budgets.DashboardAsync(referenceEnd.Year, referenceEnd.Month, memberId);
        var projectedExpenses = budget is null ? 0 : (await budgets.SummaryAsync(budget.OrcamentoId)).TotalProjetadoCentavos;
        var coverage = budget is not null && projectedExpenses > 0 ? Metric((decimal)budget.SaldoDisponivelCentavos * 100 / projectedExpenses) : Insufficient();

        return new FinancialHealthResponse(profile, savingRate, reserve, commitment, fixedShare, coverage);
    }

    private Task<PerfilSaudeFinanceira?> FindProfileAsync(Guid? memberId)
    {
        var familiaId = db.CurrentFamiliaId ?? throw new InvalidOperationException("O contexto da família ativa é obrigatório para consultar o perfil.");
        return db.PerfisSaudeFinanceira.AsNoTracking()
            .Where(x => x.FamiliaId == familiaId && (x.MembroId == memberId || (memberId.HasValue && x.MembroId == null)))
            .OrderByDescending(x => x.MembroId == memberId)
            .FirstOrDefaultAsync();
    }

    private static FinancialHealthProfileResponse ToResponse(PerfilSaudeFinanceira row, bool configured) => new(
        row.MembroId, row.MetaReservaMeses, row.TetoComprometimentoPercentual, row.MetaPoupancaPercentual, row.Observacao,
        JsonSerializer.Deserialize<Guid[]>(row.CategoriasEssenciaisJson) ?? [], configured);

    private static FinancialHealthMetric Metric(decimal value) => new(Math.Round(value, 2, MidpointRounding.AwayFromZero), "calculado");
    private static FinancialHealthMetric Insufficient() => new(null, "dados_insuficientes");
    private static DateTimeOffset NormalizeBoundary(DateTimeOffset value) => new(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
}
