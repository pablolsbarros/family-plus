using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class AlertService(FinanceDbContext db, DashboardService dashboard, CardService cards, BudgetService budgets)
{
    public async Task<IReadOnlyList<AlertResponse>> ListAsync(AlertQueryRequest request)
    {
        await RefreshAsync();
        var query = db.Alertas.AsNoTracking().AsQueryable();
        if (request.Tipo.HasValue) query = query.Where(x => x.Tipo == request.Tipo);
        if (request.Severidade.HasValue) query = query.Where(x => x.Severidade == request.Severidade);
        if (request.Resolvido.HasValue) query = query.Where(x => x.Resolvido == request.Resolvido);
        return (await query.ToListAsync()).OrderByDescending(x => x.Severidade).ThenByDescending(x => x.DataGeracao).Select(Map).ToList();
    }

    public async Task<AlertResponse> MarkReadAsync(Guid id)
    {
        var alert = await FindAsync(id);
        alert.DataLeitura ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Map(alert);
    }

    public async Task<AlertResponse> ResolveAsync(Guid id)
    {
        var alert = await FindAsync(id);
        alert.Resolvido = true;
        alert.DataLeitura ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Map(alert);
    }

    public async Task RefreshAsync()
    {
        var today = Normalize(DateTimeOffset.UtcNow);
        var projection = await dashboard.GetProjectionAsync(new DashboardQueryRequest(HorizonteDias: 30));
        var currentKeys = new HashSet<string>();
        currentKeys.UnionWith(await budgets.RefreshAlertsAsync());
        var accounts = await db.Contas.AsNoTracking().Where(x => x.Ativo).ToListAsync();
        var transactions = await db.Transacoes.AsNoTracking().ToListAsync();
        foreach (var account in accounts)
        {
            var balance = FinanceCalculator.CalculateBalance(account.SaldoInicialCentavos, transactions.Where(x => x.ContaId == account.Id).Select(x => (x.Tipo, x.ValorCentavos, x.Status)));
            if (balance < 0)
            {
                var key = $"SALDO_NEGATIVO:{account.Id}"; currentKeys.Add(key);
                await EnsureAsync(key, TipoAlerta.SALDO_NEGATIVO, SeveridadeAlerta.CRITICO, "Saldo negativo", $"A conta {account.Nome} está com saldo de {FormatMoney(balance)}.", "CONTA", account.Id);
            }
        }

        if (projection.SaldoProjetadoCentavos < 0)
        {
            var key = $"SALDO_PROJETADO_NEGATIVO:{projection.DataFim:yyyy-MM-dd}"; currentKeys.Add(key);
            await EnsureAsync(key, TipoAlerta.SALDO_PROJETADO_NEGATIVO, SeveridadeAlerta.CRITICO, "Saldo projetado negativo", $"O saldo projetado ficará negativo até {projection.DataFim:dd/MM/yyyy}: {FormatMoney(projection.SaldoProjetadoCentavos)}.", "DASHBOARD", null);
        }

        var predicted = transactions.Where(x => x.Status == StatusTransacao.PREVISTA && (x.Tipo == TipoTransacao.RECEITA || x.Tipo == TipoTransacao.DESPESA)).ToList();
        foreach (var item in predicted)
        {
            var days = (item.DataMovimentacao.Date - today.Date).Days;
            if (days < 0 || days <= 7)
            {
                var type = days < 0 ? TipoAlerta.CONTA_VENCIDA : TipoAlerta.CONTA_PROXIMA;
                var severity = days < 0 ? SeveridadeAlerta.CRITICO : days <= 1 ? SeveridadeAlerta.ATENCAO : SeveridadeAlerta.INFORMACAO;
                var key = $"{type}:{item.Id}"; currentKeys.Add(key);
                await EnsureAsync(key, type, severity, days < 0 ? "Conta vencida" : "Conta próxima", $"{item.Descricao} está {(days < 0 ? "vencida" : $"prevista para {item.DataMovimentacao:dd/MM/yyyy}")} no valor de {FormatMoney(item.ValorCentavos)}.", "TRANSACAO", item.Id);
            }
        }

        var occurrences = await db.OcorrenciasRecorrentes.AsNoTracking().Include(x => x.Recorrencia).Where(x => x.Status == StatusOcorrenciaRecorrencia.ATRASADA).ToListAsync();
        foreach (var item in occurrences)
        {
            var key = $"RECORRENCIA_ATRASADA:{item.Id}"; currentKeys.Add(key);
            await EnsureAsync(key, TipoAlerta.RECORRENCIA_ATRASADA, SeveridadeAlerta.ATENCAO, "Recorrência atrasada", $"A ocorrência {item.Recorrencia?.Descricao ?? "recorrente"} de {item.DataPrevista:dd/MM/yyyy} ainda está pendente.", "OCORRENCIA_RECORRENCIA", item.Id);
        }

        var invoices = await db.Faturas.AsNoTracking().Include(x => x.Cartao).Where(x => x.Status != StatusFatura.PAGA && x.Status != StatusFatura.CANCELADA).ToListAsync();
        foreach (var invoice in invoices)
        {
            var days = (invoice.DataVencimento.Date - today.Date).Days;
            if (days < 0 || days <= 7)
            {
                var type = days < 0 ? TipoAlerta.FATURA_VENCIDA : TipoAlerta.FATURA_PROXIMA;
                var severity = days < 0 ? SeveridadeAlerta.CRITICO : days <= 1 ? SeveridadeAlerta.ATENCAO : SeveridadeAlerta.INFORMACAO;
                var key = $"{type}:{invoice.Id}"; currentKeys.Add(key);
                await EnsureAsync(key, type, severity, days < 0 ? "Fatura vencida" : "Fatura próxima", $"A fatura {invoice.Cartao?.Nome ?? "do cartão"} vence em {invoice.DataVencimento:dd/MM/yyyy} no valor de {FormatMoney(invoice.ValorTotalCentavos)}.", "FATURA", invoice.Id);
            }
        }

        foreach (var card in await db.Cartoes.AsNoTracking().Where(x => x.Ativo).ToListAsync())
        {
            var limit = await cards.GetLimitAsync(card.Id);
            if (limit.PercentualUtilizado >= 70)
            {
                var severity = limit.PercentualUtilizado >= 90 ? SeveridadeAlerta.CRITICO : SeveridadeAlerta.ATENCAO;
                var key = $"LIMITE_CARTAO_ALTO:{card.Id}"; currentKeys.Add(key);
                await EnsureAsync(key, TipoAlerta.LIMITE_CARTAO_ALTO, severity, "Limite do cartão comprometido", $"O cartão {card.Nome} está com {limit.PercentualUtilizado:0.##}% do limite utilizado.", "CARTAO", card.Id);
            }
        }

        var existing = await db.Alertas.Where(x => !x.Resolvido).ToListAsync();
        foreach (var alert in existing.Where(x => IsRefreshKey(x.ChaveUnica) && !currentKeys.Contains(x.ChaveUnica))) { alert.Resolvido = true; alert.DataLeitura ??= DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync();
    }

    private async Task EnsureAsync(string key, TipoAlerta type, SeveridadeAlerta severity, string title, string message, string source, Guid? sourceId)
    {
        var alert = await db.Alertas.SingleOrDefaultAsync(x => x.ChaveUnica == key);
        if (alert is null)
        {
            db.Alertas.Add(new Alerta { ChaveUnica = key, Tipo = type, Severidade = severity, Titulo = title, Mensagem = message, EntidadeOrigem = source, EntidadeId = sourceId });
        }
        else if (!alert.Resolvido)
        {
            alert.Severidade = severity; alert.Titulo = title; alert.Mensagem = message; alert.DataGeracao = DateTimeOffset.UtcNow;
        }
    }

    private async Task<Alerta> FindAsync(Guid id) => await db.Alertas.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Alerta não encontrado.");
    private static AlertResponse Map(Alerta x) => new(x.Id, x.Tipo, x.Severidade, x.Titulo, x.Mensagem, x.EntidadeOrigem, x.EntidadeId, x.DataGeracao, x.DataLeitura, x.Resolvido);
    private static bool IsRefreshKey(string key) => key.StartsWith("SALDO_", StringComparison.Ordinal) || key.StartsWith("CONTA_", StringComparison.Ordinal) || key.StartsWith("FATURA_", StringComparison.Ordinal) || key.StartsWith("RECORRENCIA_", StringComparison.Ordinal) || key.StartsWith("LIMITE_", StringComparison.Ordinal) || key.StartsWith("ORCAMENTO_", StringComparison.Ordinal) || key.StartsWith("CATEGORIA_SEM_ORCAMENTO", StringComparison.Ordinal);
    private static DateTimeOffset Normalize(DateTimeOffset value) => new(value.Year, value.Month, value.Day, 12, 0, 0, TimeSpan.Zero);
    private static string FormatMoney(long cents) => $"R$ {(cents / 100m):N2}";
}
