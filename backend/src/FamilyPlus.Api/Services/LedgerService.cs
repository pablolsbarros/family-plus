using System.Globalization;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

/// <summary>
/// Fonte única dos fatos financeiros. Transacao continua existindo apenas como
/// espelho de compatibilidade da API legada durante a migração para Lancamento.
/// </summary>
public sealed class LedgerService(FinanceDbContext db, AuditService audit)
{
    public async Task<LedgerPage> ListAsync(LedgerQueryRequest request, Guid? usuarioId = null)
    {
        var rows = await db.Lancamentos.AsNoTracking().Include(x => x.Conta).Include(x => x.Categoria).Include(x => x.Membro).ToListAsync();
        rows = await ApplyScopeAsync(rows, usuarioId);
        rows = [.. rows.Where(x => MatchesDate(x.DataCompetencia, request.DataInicio, request.DataFim))];
        if (request.ContaId.HasValue) rows = [.. rows.Where(x => x.ContaId == request.ContaId)];
        if (request.MembroId.HasValue) rows = [.. rows.Where(x => x.MembroId == request.MembroId)];
        if (request.CategoriaId.HasValue) rows = [.. rows.Where(x => x.CategoriaId == request.CategoriaId)];
        if (request.Tipo.HasValue) rows = [.. rows.Where(x => x.Tipo == request.Tipo)];
        if (request.Natureza.HasValue) rows = [.. rows.Where(x => x.Natureza == request.Natureza)];
        if (request.Status.HasValue) rows = [.. rows.Where(x => x.Status == request.Status)];
        if (request.OrigemTipo.HasValue) rows = [.. rows.Where(x => x.OrigemTipo == request.OrigemTipo)];
        if (!string.IsNullOrWhiteSpace(request.Texto)) rows = [.. rows.Where(x => x.Descricao.Contains(request.Texto.Trim(), StringComparison.OrdinalIgnoreCase))];
        var page = Math.Max(request.Pagina, 1); var size = Math.Clamp(request.TamanhoPagina, 1, 200); var total = rows.Count;
        var items = rows.OrderByDescending(x => x.DataCompetencia).ThenByDescending(x => x.CriadoEm).Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new LedgerPage(items, page, size, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)size));
    }

    public async Task<LedgerResponse> GetAsync(Guid id, Guid? usuarioId = null)
    {
        var row = await db.Lancamentos.Include(x => x.Conta).Include(x => x.Categoria).Include(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Lançamento não encontrado.");
        if (!(await ApplyScopeAsync([row], usuarioId)).Any()) throw new DomainException("Lançamento não encontrado.");
        return Map(row);
    }

    public async Task<LedgerResponse> CreateAsync(LedgerRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        if (request.OrigemTipo != OrigemLancamento.MANUAL && request.OrigemId.HasValue && await db.Lancamentos.AnyAsync(x => x.OrigemTipo == request.OrigemTipo && x.OrigemId == request.OrigemId && x.Status != StatusLancamento.CANCELADO))
            throw new DomainException("Este fato financeiro já foi lançado.");
        var entity = new Lancamento
        {
            FamiliaId = await FamilyIdAsync(request.MembroId), MembroId = request.MembroId, ContaId = request.ContaId, CategoriaId = request.CategoriaId,
            Tipo = request.Tipo, Natureza = request.Natureza, Status = request.Status, OrigemTipo = request.OrigemTipo, OrigemId = request.OrigemId,
            Descricao = request.Descricao.Trim(), ValorCentavos = request.ValorCentavos, ValorPrevistoCentavos = request.ValorPrevistoCentavos,
            DataCompetencia = request.DataCompetencia, DataVencimento = request.DataVencimento, DataEfetivacao = request.DataEfetivacao ?? (request.Status == StatusLancamento.EFETIVADO ? request.DataCompetencia : null),
            CriadoPorUsuarioId = usuarioId, Observacao = Trim(request.Observacao)
        };
        db.Lancamentos.Add(entity);
        AddLegacyMirror(entity, request.OrigemId);
        await audit.RecordAsync("LANCAMENTO", entity.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(entity), usuarioId);
        await db.SaveChangesAsync();
        return await GetAsync(entity.Id, usuarioId);
    }

    public async Task<LedgerResponse> UpdateAsync(Guid id, LedgerRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        var entity = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Lançamento não encontrado.");
        if (entity.Status == StatusLancamento.CANCELADO) throw new DomainException("Lançamento cancelado não pode ser editado.");
        var before = Snapshot(entity); var wasEffective = entity.Status == StatusLancamento.EFETIVADO;
        entity.MembroId = request.MembroId; entity.ContaId = request.ContaId; entity.CategoriaId = request.CategoriaId; entity.Tipo = request.Tipo; entity.Natureza = request.Natureza;
        entity.Status = request.Status; entity.Descricao = request.Descricao.Trim(); entity.ValorCentavos = request.ValorCentavos; entity.ValorPrevistoCentavos = request.ValorPrevistoCentavos;
        entity.DataCompetencia = request.DataCompetencia; entity.DataVencimento = request.DataVencimento; entity.DataEfetivacao = request.DataEfetivacao ?? (request.Status == StatusLancamento.EFETIVADO ? entity.DataEfetivacao ?? request.DataCompetencia : null); entity.Observacao = Trim(request.Observacao); SyncLegacyMirror(entity);
        await audit.RecordAsync("LANCAMENTO", id, request.Status == StatusLancamento.EFETIVADO && !wasEffective ? OperacaoAuditoria.EFETIVACAO : OperacaoAuditoria.ALTERACAO, before, Snapshot(entity), usuarioId);
        await db.SaveChangesAsync();
        return await GetAsync(id, usuarioId);
    }

    public async Task<LedgerResponse> EfetivarAsync(Guid id, long? valorRealizadoCentavos, DateTimeOffset? dataEfetivacao, Guid? usuarioId)
    {
        var entity = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Lançamento não encontrado.");
        if (entity.Status == StatusLancamento.CANCELADO) throw new DomainException("Lançamento cancelado não pode ser efetivado.");
        if (entity.Status == StatusLancamento.EFETIVADO && !valorRealizadoCentavos.HasValue) return await GetAsync(id, usuarioId);
        var before = Snapshot(entity); entity.Status = StatusLancamento.EFETIVADO; entity.ValorCentavos = valorRealizadoCentavos ?? entity.ValorCentavos; entity.DataEfetivacao = dataEfetivacao ?? entity.DataEfetivacao ?? DateTimeOffset.UtcNow; SyncLegacyMirror(entity);
        await audit.RecordAsync("LANCAMENTO", id, OperacaoAuditoria.EFETIVACAO, before, Snapshot(entity), usuarioId); await db.SaveChangesAsync(); return await GetAsync(id, usuarioId);
    }

    public Task<LedgerResponse> EfetivarAsync(Guid id, Guid? usuarioId = null) => EfetivarAsync(id, null, null, usuarioId);

    public async Task<LedgerResponse> CancelAsync(Guid id, Guid? usuarioId)
    {
        var entity = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Lançamento não encontrado.");
        if (entity.Status != StatusLancamento.CANCELADO) { var before = Snapshot(entity); entity.Status = StatusLancamento.CANCELADO; SyncLegacyMirror(entity); await audit.RecordAsync("LANCAMENTO", id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(entity), usuarioId); await db.SaveChangesAsync(); }
        return await GetAsync(id, usuarioId);
    }

    public async Task<LedgerResponse> CreateAdjustmentAsync(AdjustmentRequest request, Guid? usuarioId)
    {
        if (string.IsNullOrWhiteSpace(request.Justificativa)) throw new DomainException("A justificativa do ajuste é obrigatória.");
        if (request.ValorCentavos == 0) throw new DomainException("O ajuste não pode ser zero.");
        var member = await db.Membros.SingleOrDefaultAsync(x => x.Id == request.MembroId && x.Ativo) ?? throw new DomainException("O membro deve existir e estar ativo.");
        _ = await db.Contas.SingleOrDefaultAsync(x => x.Id == request.ContaId && x.Ativo) ?? throw new DomainException("A conta deve existir e estar ativa.");
        return await CreateAsync(new LedgerRequest(TipoLancamento.AJUSTE, NaturezaLancamento.AJUSTE, request.Status, OrigemLancamento.AJUSTE, null, request.Justificativa.Trim(), request.ValorCentavos, request.Data, null, request.Status == StatusLancamento.EFETIVADO ? request.Data : null, request.ContaId, null, member.Id, null, request.Justificativa), usuarioId);
    }

    public async Task<LedgerBalanceResponse> BalanceAsync(Guid accountId, DateTimeOffset? data = null)
    {
        var account = await db.Contas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId) ?? throw new DomainException("Conta não encontrada.");
        var rows = await db.Lancamentos.AsNoTracking().Where(x => x.ContaId == accountId && x.Status != StatusLancamento.CANCELADO).ToListAsync();
        var actual = rows.Where(x => x.Status == StatusLancamento.EFETIVADO && (!data.HasValue || EffectiveDate(x) <= data.Value));
        var projected = rows.Where(x => (!data.HasValue || EffectiveDate(x) <= data.Value) && (x.Status == StatusLancamento.EFETIVADO || x.Status == StatusLancamento.PREVISTO));
        var entries = actual.Where(x => Delta(x) > 0).Sum(x => Delta(x)); var exits = actual.Where(x => Delta(x) < 0).Sum(x => -Delta(x));
        var current = account.SaldoInicialCentavos + actual.Sum(Delta); var forecast = account.SaldoInicialCentavos + projected.Sum(Delta);
        return new LedgerBalanceResponse(account.Id, account.Nome, account.SaldoInicialCentavos, entries, exits, current, forecast, data);
    }

    public async Task<LedgerStatementResponse> StatementAsync(Guid accountId, DateTimeOffset? dataInicio = null, DateTimeOffset? dataFim = null, Guid? usuarioId = null)
    {
        var balance = await BalanceAsync(accountId, dataFim); var page = await ListAsync(new LedgerQueryRequest(dataInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), dataFim?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), accountId, TamanhoPagina: 2000), usuarioId);
        return new LedgerStatementResponse(balance, page.Items);
    }

    public async Task<FinanceSummary> SummaryAsync(DateTimeOffset? dataInicio = null, DateTimeOffset? dataFim = null, Guid? usuarioId = null)
    {
        var rows = await db.Lancamentos.AsNoTracking().Where(x => x.Status == StatusLancamento.EFETIVADO).ToListAsync();
        rows = [.. rows.Where(x => MatchesDate(x.DataCompetencia, dataInicio, dataFim))];
        rows = await ApplyScopeAsync(rows, usuarioId);
        var income = rows.Where(x => x.Tipo == TipoLancamento.RECEITA && x.Natureza == NaturezaLancamento.RENDA).Sum(x => x.ValorCentavos);
        var expense = rows.Where(x => x.Tipo == TipoLancamento.DESPESA && x.Natureza == NaturezaLancamento.CONSUMO).Sum(x => x.ValorCentavos);
        expense += rows.Where(x => x.Tipo == TipoLancamento.AJUSTE && x.Natureza == NaturezaLancamento.CONSUMO).Sum(x => x.ValorCentavos);
        return new FinanceSummary(income, expense, income - expense);
    }

    public async Task<Lancamento> CreateCardConsumptionAsync(CompraCartao purchase, Cartao card, Guid? usuarioId)
    {
        var existing = await db.Lancamentos.SingleOrDefaultAsync(x => x.OrigemTipo == OrigemLancamento.COMPRA_CARTAO && x.OrigemId == purchase.Id);
        if (existing is not null) return existing;
        var entity = new Lancamento { FamiliaId = purchase.FamiliaId, MembroId = purchase.MembroId, ContaId = card.ContaPagamentoPadraoId, CategoriaId = purchase.CategoriaId, Tipo = TipoLancamento.DESPESA, Natureza = NaturezaLancamento.CONSUMO, Status = StatusLancamento.EFETIVADO, OrigemTipo = OrigemLancamento.COMPRA_CARTAO, OrigemId = purchase.Id, Descricao = purchase.Descricao, ValorCentavos = purchase.ValorTotalCentavos, DataCompetencia = purchase.DataCompra, DataEfetivacao = purchase.DataCompra, CriadoPorUsuarioId = usuarioId, Observacao = purchase.Observacao };
        db.Lancamentos.Add(entity); return entity;
    }

    private async Task ValidateAsync(LedgerRequest request)
    {
        var errors = new List<string>();
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório."); if (request.ContaId == Guid.Empty) errors.Add("A conta é obrigatória."); if (string.IsNullOrWhiteSpace(request.Descricao)) errors.Add("A descrição é obrigatória.");
        if (request.Tipo is not TipoLancamento.AJUSTE && request.ValorCentavos <= 0) errors.Add("O valor deve ser maior que zero.");
        if (request.Tipo is TipoLancamento.RECEITA or TipoLancamento.DESPESA && request.CategoriaId is null) errors.Add("A categoria é obrigatória.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar o lançamento.", [.. errors]);
        var member = await db.Membros.SingleOrDefaultAsync(x => x.Id == request.MembroId && x.Ativo) ?? throw new DomainException("O membro deve existir e estar ativo.");
        var account = await db.Contas.SingleOrDefaultAsync(x => x.Id == request.ContaId && x.Ativo) ?? throw new DomainException("A conta deve existir e estar ativa.");
        if (account.MembroId != member.Id && account.FamiliaId != member.FamiliaId) throw new DomainException("A conta e o membro devem pertencer à mesma família.");
        if (request.CategoriaId.HasValue)
        {
            var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaId && x.Ativo) ?? throw new DomainException("A categoria deve existir e estar ativa.");
            if (request.Tipo == TipoLancamento.RECEITA && category.Tipo != TipoCategoria.Receita || request.Tipo == TipoLancamento.DESPESA && category.Tipo != TipoCategoria.Despesa) throw new DomainException("A categoria não é compatível com o tipo do lançamento.");
        }
    }

    private async Task<List<Lancamento>> ApplyScopeAsync(List<Lancamento> rows, Guid? usuarioId)
    {
        if (!usuarioId.HasValue) return rows;
        var scope = await db.PermissoesEscopo.Include(x => x.Membros).SingleOrDefaultAsync(x => x.UsuarioId == usuarioId && (x.Recurso == "MOVIMENTACAO" || x.Recurso == "LANCAMENTO"));
        if (scope is null || scope.TipoEscopo == TipoEscopo.FAMILIA) return rows;
        if (scope.TipoEscopo == TipoEscopo.PROPRIOS)
        {
            var memberId = await db.Usuarios.Where(x => x.Id == usuarioId).Select(x => x.MembroId).SingleOrDefaultAsync();
            return memberId.HasValue ? [.. rows.Where(x => x.MembroId == memberId)] : [];
        }
        var selected = scope.Membros.Select(x => x.MembroId).ToHashSet(); return [.. rows.Where(x => selected.Contains(x.MembroId))];
    }

    private static DateTimeOffset EffectiveDate(Lancamento x) => x.DataEfetivacao ?? x.DataCompetencia;
    private static long Delta(Lancamento x)
    {
        if (x.OrigemTipo == OrigemLancamento.COMPRA_CARTAO) return 0;
        return x.Tipo switch { TipoLancamento.RECEITA or TipoLancamento.TRANSFERENCIA_ENTRADA => Math.Abs(x.ValorCentavos), TipoLancamento.DESPESA or TipoLancamento.TRANSFERENCIA_SAIDA or TipoLancamento.PAGAMENTO_FATURA => -Math.Abs(x.ValorCentavos), TipoLancamento.AJUSTE => x.ValorCentavos, _ => 0 };
    }
    private static bool MatchesDate(DateTimeOffset date, string? start, string? end) => MatchesDate(date, ParseDate(start), ParseDate(end));
    private static bool MatchesDate(DateTimeOffset date, DateTimeOffset? start, DateTimeOffset? end) => (!start.HasValue || date >= start.Value) && (!end.HasValue || date < end.Value.AddDays(1));
    private static DateTimeOffset? ParseDate(string? value) => string.IsNullOrWhiteSpace(value) ? null : DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? new DateTimeOffset(date, TimeSpan.Zero) : null;
    private async Task<Guid?> FamilyIdAsync(Guid memberId) => await db.Membros.AsNoTracking().Where(x => x.Id == memberId).Select(x => x.FamiliaId).SingleOrDefaultAsync();
    private static LedgerResponse Map(Lancamento x) => new(x.Id, x.FamiliaId ?? Guid.Empty, x.MembroId, x.Membro?.Nome ?? "", x.ContaId, x.Conta?.Nome ?? "", x.CategoriaId, x.Categoria?.Nome, x.Tipo, x.Natureza, x.Status, x.OrigemTipo, x.OrigemId, x.Descricao, x.ValorCentavos, x.ValorPrevistoCentavos, x.DataCompetencia, x.DataVencimento, x.DataEfetivacao, x.CriadoPorUsuarioId, x.Observacao);
    private static object Snapshot(Lancamento x) => new { x.Id, x.Tipo, x.Natureza, x.Status, x.OrigemTipo, x.OrigemId, x.ValorCentavos, x.ValorPrevistoCentavos, x.DataCompetencia, x.DataEfetivacao, x.ContaId, x.CategoriaId, x.MembroId };
    private void AddLegacyMirror(Lancamento entity, Guid? transferId)
    {
        if (entity.Tipo is not (TipoLancamento.RECEITA or TipoLancamento.DESPESA) || db.Transacoes.Local.Any(x => x.Id == entity.Id)) return;
        db.Transacoes.Add(new Transacao { Id = entity.Id, FamiliaId = entity.FamiliaId, MembroId = entity.MembroId, ContaId = entity.ContaId, CategoriaId = entity.CategoriaId, Tipo = entity.Tipo switch { TipoLancamento.RECEITA => TipoTransacao.RECEITA, TipoLancamento.DESPESA => TipoTransacao.DESPESA, TipoLancamento.TRANSFERENCIA_ENTRADA => TipoTransacao.TRANSFERENCIA_ENTRADA, _ => TipoTransacao.TRANSFERENCIA_SAIDA }, Descricao = entity.Descricao, ValorCentavos = entity.ValorCentavos, DataCompetencia = entity.DataCompetencia, DataMovimentacao = entity.DataEfetivacao ?? entity.DataCompetencia, Status = entity.Status switch { StatusLancamento.EFETIVADO => StatusTransacao.EFETIVADA, StatusLancamento.CANCELADO => StatusTransacao.CANCELADA, _ => StatusTransacao.PREVISTA }, TransferenciaId = transferId, LancamentoId = entity.Id, Observacao = entity.Observacao });
    }
    private void SyncLegacyMirror(Lancamento entity)
    {
        var mirror = db.Transacoes.Local.SingleOrDefault(x => x.Id == entity.Id) ?? db.Transacoes.SingleOrDefault(x => x.Id == entity.Id);
        if (mirror is null || entity.Tipo is not (TipoLancamento.RECEITA or TipoLancamento.DESPESA)) return;
        mirror.MembroId = entity.MembroId; mirror.ContaId = entity.ContaId; mirror.CategoriaId = entity.CategoriaId; mirror.Tipo = entity.Tipo switch { TipoLancamento.RECEITA => TipoTransacao.RECEITA, TipoLancamento.DESPESA => TipoTransacao.DESPESA, TipoLancamento.TRANSFERENCIA_ENTRADA => TipoTransacao.TRANSFERENCIA_ENTRADA, _ => TipoTransacao.TRANSFERENCIA_SAIDA }; mirror.Descricao = entity.Descricao; mirror.ValorCentavos = entity.ValorCentavos; mirror.DataCompetencia = entity.DataCompetencia; mirror.DataMovimentacao = entity.DataEfetivacao ?? entity.DataCompetencia; mirror.Status = entity.Status switch { StatusLancamento.EFETIVADO => StatusTransacao.EFETIVADA, StatusLancamento.CANCELADO => StatusTransacao.CANCELADA, _ => StatusTransacao.PREVISTA }; mirror.LancamentoId = entity.Id; mirror.Observacao = entity.Observacao;
    }
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AccountBalanceService(LedgerService ledger)
{
    public Task<LedgerBalanceResponse> GetAsync(Guid accountId, DateTimeOffset? data = null) => ledger.BalanceAsync(accountId, data);
    public Task<LedgerStatementResponse> StatementAsync(Guid accountId, DateTimeOffset? start = null, DateTimeOffset? end = null, Guid? usuarioId = null) => ledger.StatementAsync(accountId, start, end, usuarioId);
    public Task<LedgerBalanceResponse> ProjectionAsync(Guid accountId, DateTimeOffset? data = null) => ledger.BalanceAsync(accountId, data);
}
