using System.Globalization;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class TransactionService(FinanceDbContext db, AuditService audit, LedgerService? ledger = null)
{
    public async Task<TransactionPage> ListAsync(TransactionQueryRequest request)
    {
        var query = ApplyFilters(db.Transacoes.AsNoTracking().Include(x => x.Conta).Include(x => x.Categoria).Include(x => x.Membro), request);
        var pageSize = Math.Clamp(request.TamanhoPagina, 1, 100);
        var page = Math.Max(request.Pagina, 1);
        // SQLite não traduz OrderBy diretamente sobre DateTimeOffset. Como o Family+ é local,
        // materializamos o conjunto filtrado e ordenamos em memória sem perder a paginação da resposta.
        var rows = await query.ToListAsync();
        rows = [.. ApplyDateFilters(rows, request.DataInicio, request.DataFim)];
        var ordered = (request.OrdenarPor ?? "data").ToLowerInvariant() switch
        {
            "valor" => request.Desc ? rows.OrderByDescending(x => x.ValorCentavos) : rows.OrderBy(x => x.ValorCentavos),
            "descricao" => request.Desc ? rows.OrderByDescending(x => x.Descricao) : rows.OrderBy(x => x.Descricao),
            _ => request.Desc ? rows.OrderByDescending(x => x.DataMovimentacao) : rows.OrderBy(x => x.DataMovimentacao)
        };
        var total = rows.Count;
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToList();
        return new TransactionPage(items, page, pageSize, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<TransactionResponse> GetAsync(Guid id, TipoTransacao? expectedType = null)
    {
        var entity = await FindAsync(id);
        EnsureExpectedType(entity, expectedType);
        return Map(entity);
    }

    public async Task<TransactionResponse> CreateAsync(TransactionRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request, null);
        var entity = new Transacao
        {
            Tipo = request.Tipo, Descricao = request.Descricao.Trim(), ValorCentavos = request.ValorCentavos,
            DataCompetencia = request.DataCompetencia, DataMovimentacao = request.DataMovimentacao, ContaId = request.ContaId,
            CategoriaId = request.CategoriaId, MembroId = request.MembroId, Status = request.Status, Observacao = Trim(request.Observacao)
        };
        db.Transacoes.Add(entity);
        db.Lancamentos.Add(ToLedger(entity, request, usuarioId));
        await audit.RecordAsync("TRANSACAO", entity.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(entity), usuarioId);
        await audit.RecordAsync("LANCAMENTO", entity.Id, OperacaoAuditoria.CRIACAO, null, new { entity.Tipo, Natureza = NaturezaFor(entity.Tipo), entity.ValorCentavos }, usuarioId);
        await db.SaveChangesAsync();
        return Map(await FindAsync(entity.Id));
    }

    public async Task<TransactionResponse> UpdateAsync(Guid id, TransactionRequest request, Guid? usuarioId, TipoTransacao? expectedType = null)
    {
        var entity = await FindAsync(id);
        EnsureExpectedType(entity, expectedType);
        if (entity.TransferenciaId.HasValue) throw new DomainException("Transferências devem ser editadas pela operação de transferência.");
        await ValidateAsync(request, id);
        var before = Snapshot(entity);
        entity.Tipo = request.Tipo; entity.Descricao = request.Descricao.Trim(); entity.ValorCentavos = request.ValorCentavos;
        entity.DataCompetencia = request.DataCompetencia; entity.DataMovimentacao = request.DataMovimentacao; entity.ContaId = request.ContaId;
        entity.CategoriaId = request.CategoriaId; entity.MembroId = request.MembroId; entity.Status = request.Status; entity.Observacao = Trim(request.Observacao);
        var canonical = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == entity.Id);
        if (canonical is not null) ApplyLedger(canonical, entity, request);
        await audit.RecordAsync("TRANSACAO", entity.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(entity), usuarioId);
        await db.SaveChangesAsync();
        return Map(await FindAsync(id));
    }

    public async Task<TransactionResponse> CancelAsync(Guid id, Guid? usuarioId, TipoTransacao? expectedType = null)
    {
        var entity = await FindAsync(id);
        EnsureExpectedType(entity, expectedType);
        if (entity.TransferenciaId.HasValue)
        {
            await CancelTransferTransactionsAsync(entity.TransferenciaId.Value, usuarioId);
            return Map(await FindAsync(id));
        }
        if (entity.Status != StatusTransacao.CANCELADA)
        {
            var before = Snapshot(entity); entity.Status = StatusTransacao.CANCELADA;
            var canonical = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == entity.Id); if (canonical is not null) canonical.Status = StatusLancamento.CANCELADO;
            await audit.RecordAsync("TRANSACAO", entity.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(entity), usuarioId);
            await db.SaveChangesAsync();
        }
        return Map(await FindAsync(id));
    }

    public async Task<FinanceSummary> SummaryAsync(string? dataInicio, string? dataFim)
    {
        return await (ledger ?? new LedgerService(db, audit)).SummaryAsync(ParseDate(dataInicio), ParseDate(dataFim));
        /*var query = db.Transacoes.AsNoTracking().Where(x => x.Status == StatusTransacao.EFETIVADA);
        // O provedor SQLite pode falhar ao traduzir a combinação de enum, GroupBy e Sum
        // em algumas versões locais. Como o resumo é um agregado pequeno da base local,
        // materializamos apenas os campos necessários e calculamos em memória.
        var values = await query.Where(x => (x.Tipo == TipoTransacao.RECEITA || x.Tipo == TipoTransacao.DESPESA) && x.Origem != OrigemTransacao.PAGAMENTO_FATURA)
            .Select(x => new { x.Tipo, x.ValorCentavos, x.DataMovimentacao }).ToListAsync();
        var from = ParseDate(dataInicio);
        var until = ParseDate(dataFim)?.AddDays(1);
        values = [.. values.Where(x => (!from.HasValue || x.DataMovimentacao >= from.Value) && (!until.HasValue || x.DataMovimentacao < until.Value))];
        var income = values.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => (long)x.ValorCentavos);
        var expense = values.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => (long)x.ValorCentavos);
        var purchases = await db.ComprasCartao.AsNoTracking().Where(x => x.Status != StatusCompraCartao.CANCELADA)
            .Select(x => new { x.Id, x.ValorTotalCentavos, x.DataCompra }).ToListAsync();
        var refunds = await db.EstornosCartao.AsNoTracking().GroupBy(x => x.CompraCartaoId)
            .Select(group => new { CompraId = group.Key, ValorCentavos = group.Sum(x => x.ValorCentavos) }).ToDictionaryAsync(x => x.CompraId, x => x.ValorCentavos);
        expense += purchases.Where(x => (!from.HasValue || x.DataCompra >= from.Value) && (!until.HasValue || x.DataCompra < until.Value))
            .Sum(x => Math.Max(0, x.ValorTotalCentavos - refunds.GetValueOrDefault(x.Id)));
        return new FinanceSummary(income, expense, income - expense);*/
    }

    public async Task<AccountBalanceResponse> BalanceAsync(Guid accountId, DateTimeOffset? data = null)
    {
        var balance = await (ledger ?? new LedgerService(db, audit)).BalanceAsync(accountId, data);
        return new AccountBalanceResponse(balance.ContaId, balance.ContaNome, balance.SaldoInicialCentavos, balance.EntradasCentavos, balance.SaidasCentavos, balance.SaldoAtualCentavos);
    }

    public async Task<AccountStatementResponse> StatementAsync(Guid accountId, string? dataInicio, string? dataFim, string? tipo, string? categoria, string? status)
    {
        _ = await db.Contas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId) ?? throw new DomainException("Conta não encontrada.");
        var query = new TransactionQueryRequest(dataInicio, dataFim, accountId, null, ParseGuid(categoria), tipo, status, null, "data", true, 1, 1000);
        var page = await ListAsync(query);
        return new AccountStatementResponse(await BalanceAsync(accountId), page.Items);
    }

    private async Task CancelTransferTransactionsAsync(Guid transferId, Guid? usuarioId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var transfer = await db.Transferencias.SingleOrDefaultAsync(x => x.Id == transferId);
        var linked = await db.Transacoes.Where(x => x.TransferenciaId == transferId).ToListAsync();
        if (transfer is not null && transfer.Status != StatusTransacao.CANCELADA)
        {
            var before = Snapshot(transfer); transfer.Status = StatusTransacao.CANCELADA;
            await audit.RecordAsync("TRANSFERENCIA", transfer.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(transfer), usuarioId);
        }
        foreach (var item in linked.Where(x => x.Status != StatusTransacao.CANCELADA))
        {
            var before = Snapshot(item); item.Status = StatusTransacao.CANCELADA;
            var canonical = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == item.Id); if (canonical is not null) canonical.Status = StatusLancamento.CANCELADO;
            await audit.RecordAsync("TRANSACAO", item.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(item), usuarioId);
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    private async Task ValidateAsync(TransactionRequest request, Guid? editingId)
    {
        var errors = new List<string>();
        if (request.Tipo is not (TipoTransacao.RECEITA or TipoTransacao.DESPESA)) errors.Add("Use a operação de receita ou despesa para este endpoint.");
        if (string.IsNullOrWhiteSpace(request.Descricao)) errors.Add("A descrição é obrigatória.");
        if (request.ValorCentavos <= 0) errors.Add("O valor deve ser maior que zero.");
        if (request.ContaId == Guid.Empty) errors.Add("A conta é obrigatória.");
        if (request.CategoriaId == Guid.Empty) errors.Add("A categoria é obrigatória.");
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar a movimentação.", [.. errors]);
        var account = await db.Contas.SingleOrDefaultAsync(x => x.Id == request.ContaId);
        if (account is null) throw new DomainException("Não foi possível salvar a movimentação.", "A conta informada não existe.");
        if (!account.Ativo) throw new DomainException("Não foi possível salvar a movimentação.", "A conta selecionada está desativada e não pode receber movimentações.");
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("Não foi possível salvar a movimentação.", "O membro selecionado está desativado ou não existe.");
        var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaId);
        if (category is null) throw new DomainException("Não foi possível salvar a movimentação.", "A categoria informada não existe.");
        if (!category.Ativo) throw new DomainException("Não foi possível salvar a movimentação.", "A categoria selecionada está desativada.");
        var expected = request.Tipo == TipoTransacao.RECEITA ? TipoCategoria.Receita : TipoCategoria.Despesa;
        if (category.Tipo != expected) throw new DomainException("Não foi possível salvar a movimentação.", $"A categoria selecionada não pode ser utilizada em uma {(request.Tipo == TipoTransacao.RECEITA ? "receita" : "despesa")}.");
    }

    private async Task<Transacao> FindAsync(Guid id) => await db.Transacoes.Include(x => x.Conta).Include(x => x.Categoria).Include(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Movimentação não encontrada.");

    private static void EnsureExpectedType(Transacao transaction, TipoTransacao? expectedType)
    {
        if (!expectedType.HasValue || transaction.Tipo == expectedType.Value) return;
        var entity = expectedType == TipoTransacao.RECEITA ? "Receita" : "Despesa";
        throw new DomainException($"{entity} não encontrada.");
    }

    private static IQueryable<Transacao> ApplyFilters(IQueryable<Transacao> query, TransactionQueryRequest request)
    {
        if (request.ContaId.HasValue) query = query.Where(x => x.ContaId == request.ContaId);
        if (request.MembroId.HasValue) query = query.Where(x => x.MembroId == request.MembroId);
        if (request.CategoriaId.HasValue) query = query.Where(x => x.CategoriaId == request.CategoriaId);
        if (TryParseEnum(request.Tipo, out TipoTransacao type)) query = query.Where(x => x.Tipo == type);
        if (TryParseEnum(request.Status, out StatusTransacao status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Texto)) query = query.Where(x => x.Descricao.Contains(request.Texto.Trim()));
        return query;
    }

    private static IEnumerable<Transacao> ApplyDateFilters(IEnumerable<Transacao> rows, string? start, string? end)
    {
        var from = ParseDate(start); var until = ParseDate(end);
        return rows.Where(x => (!from.HasValue || x.DataMovimentacao >= from.Value) && (!until.HasValue || x.DataMovimentacao < until.Value.AddDays(1)));
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc));
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result) ? result : null;
    }

    private static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum => Enum.TryParse(value?.Replace("-", "_"), true, out result);
    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var id) ? id : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static object Snapshot(Transacao x) => new { x.Id, x.Tipo, x.Descricao, x.ValorCentavos, x.DataCompetencia, x.DataMovimentacao, x.ContaId, x.CategoriaId, x.MembroId, x.Status, x.Observacao };
    private static object Snapshot(Transferencia x) => new { x.Id, x.ContaOrigemId, x.ContaDestinoId, x.MembroId, x.ValorCentavos, x.Data, x.Descricao, x.Status };
    private static Lancamento ToLedger(Transacao x, TransactionRequest request, Guid? usuarioId) => new()
    {
        Id = x.Id, FamiliaId = x.FamiliaId, MembroId = x.MembroId, ContaId = x.ContaId, CategoriaId = x.CategoriaId, Tipo = MapType(x.Tipo), Natureza = NaturezaFor(x.Tipo), Status = MapStatus(x.Status), OrigemTipo = OrigemLancamento.MANUAL, Descricao = x.Descricao, ValorCentavos = x.ValorCentavos, DataCompetencia = x.DataCompetencia, DataEfetivacao = x.Status == StatusTransacao.EFETIVADA ? x.DataMovimentacao : null, CriadoPorUsuarioId = usuarioId, Observacao = x.Observacao
    };
    private static void ApplyLedger(Lancamento target, Transacao source, TransactionRequest request) { target.MembroId = source.MembroId; target.ContaId = source.ContaId; target.CategoriaId = source.CategoriaId; target.Tipo = MapType(source.Tipo); target.Natureza = NaturezaFor(source.Tipo); target.Status = MapStatus(source.Status); target.Descricao = source.Descricao; target.ValorCentavos = source.ValorCentavos; target.DataCompetencia = source.DataCompetencia; target.DataEfetivacao = source.Status == StatusTransacao.EFETIVADA ? source.DataMovimentacao : null; target.Observacao = source.Observacao; }
    private static TipoLancamento MapType(TipoTransacao type) => type switch { TipoTransacao.RECEITA => TipoLancamento.RECEITA, TipoTransacao.DESPESA => TipoLancamento.DESPESA, TipoTransacao.TRANSFERENCIA_ENTRADA => TipoLancamento.TRANSFERENCIA_ENTRADA, TipoTransacao.TRANSFERENCIA_SAIDA => TipoLancamento.TRANSFERENCIA_SAIDA, _ => TipoLancamento.AJUSTE };
    private static StatusLancamento MapStatus(StatusTransacao status) => status switch { StatusTransacao.EFETIVADA => StatusLancamento.EFETIVADO, StatusTransacao.CANCELADA => StatusLancamento.CANCELADO, _ => StatusLancamento.PREVISTO };
    private static NaturezaLancamento NaturezaFor(TipoTransacao type) => type switch { TipoTransacao.RECEITA => NaturezaLancamento.RENDA, TipoTransacao.DESPESA => NaturezaLancamento.CONSUMO, _ => NaturezaLancamento.TRANSFERENCIA };
    private static NaturezaLancamento NaturezaFor(TipoLancamento type) => type switch { TipoLancamento.RECEITA => NaturezaLancamento.RENDA, TipoLancamento.DESPESA => NaturezaLancamento.CONSUMO, TipoLancamento.PAGAMENTO_FATURA => NaturezaLancamento.PAGAMENTO_FATURA, TipoLancamento.AJUSTE => NaturezaLancamento.AJUSTE, _ => NaturezaLancamento.TRANSFERENCIA };
    private static readonly System.Linq.Expressions.Expression<Func<Transacao, TransactionResponse>> MapProjection = x => new(x.Id, x.MembroId, x.Membro!.Nome, x.ContaId, x.Conta!.Nome, x.CategoriaId, x.Categoria == null ? null : x.Categoria.Nome, x.Tipo, x.Descricao, x.ValorCentavos, x.DataCompetencia, x.DataMovimentacao, x.Status, x.Observacao, x.TransferenciaId);
    private static TransactionResponse Map(Transacao x) => new(x.Id, x.MembroId, x.Membro?.Nome ?? "", x.ContaId, x.Conta?.Nome ?? "", x.CategoriaId, x.Categoria?.Nome, x.Tipo, x.Descricao, x.ValorCentavos, x.DataCompetencia, x.DataMovimentacao, x.Status, x.Observacao, x.TransferenciaId);
}
