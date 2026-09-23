using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class TransferService(FinanceDbContext db, AuditService audit)
{
    public async Task<IReadOnlyList<TransferResponse>> ListAsync()
    {
        // SQLite não traduz OrderBy diretamente sobre DateTimeOffset; a base é local,
        // então materializamos antes de ordenar para manter a listagem estável.
        var transfers = await db.Transferencias.AsNoTracking().Include(x => x.Transacoes).ToListAsync();
        transfers = [.. transfers.OrderByDescending(x => x.Data)];
        var accounts = await db.Contas.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nome);
        var members = await db.Membros.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nome);
        return transfers.Select(x => new TransferResponse(x.Id, x.ContaOrigemId, accounts.GetValueOrDefault(x.ContaOrigemId, ""), x.ContaDestinoId, accounts.GetValueOrDefault(x.ContaDestinoId, ""), x.MembroId, members.GetValueOrDefault(x.MembroId, ""), x.ValorCentavos, x.Data, x.Descricao, x.Status, x.Transacoes.Select(t => t.Id).ToList())).ToList();
    }

    public async Task<TransferResponse> GetAsync(Guid id) => await Map(await FindAsync(id));

    public async Task<TransferResponse> CreateAsync(TransferRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var transfer = new Transferencia { ContaOrigemId = request.ContaOrigemId, ContaDestinoId = request.ContaDestinoId, MembroId = request.MembroId, ValorCentavos = request.ValorCentavos, Data = request.Data, Descricao = request.Descricao.Trim(), Status = request.Status };
        db.Transferencias.Add(transfer);
        await db.SaveChangesAsync();
        var outgoing = new Transacao { MembroId = request.MembroId, ContaId = request.ContaOrigemId, Tipo = TipoTransacao.TRANSFERENCIA_SAIDA, Descricao = request.Descricao.Trim(), ValorCentavos = request.ValorCentavos, DataCompetencia = request.Data, DataMovimentacao = request.Data, Status = request.Status, Observacao = request.Observacao, TransferenciaId = transfer.Id };
        var incoming = new Transacao { MembroId = request.MembroId, ContaId = request.ContaDestinoId, Tipo = TipoTransacao.TRANSFERENCIA_ENTRADA, Descricao = request.Descricao.Trim(), ValorCentavos = request.ValorCentavos, DataCompetencia = request.Data, DataMovimentacao = request.Data, Status = request.Status, Observacao = request.Observacao, TransferenciaId = transfer.Id };
        db.Transacoes.AddRange(outgoing, incoming);
        db.Lancamentos.AddRange(ToLedger(outgoing, transfer.Id), ToLedger(incoming, transfer.Id));
        await audit.RecordAsync("TRANSFERENCIA", transfer.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(transfer), usuarioId);
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return await Map(await FindAsync(transfer.Id));
    }

    public async Task<TransferResponse> UpdateAsync(Guid id, TransferRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var transfer = await FindAsync(id); var before = Snapshot(transfer);
        transfer.ContaOrigemId = request.ContaOrigemId; transfer.ContaDestinoId = request.ContaDestinoId; transfer.MembroId = request.MembroId; transfer.ValorCentavos = request.ValorCentavos; transfer.Data = request.Data; transfer.Descricao = request.Descricao.Trim(); transfer.Status = request.Status;
        foreach (var movement in transfer.Transacoes)
        {
            movement.MembroId = request.MembroId; movement.ContaId = movement.Tipo == TipoTransacao.TRANSFERENCIA_SAIDA ? request.ContaOrigemId : request.ContaDestinoId; movement.ValorCentavos = request.ValorCentavos; movement.Descricao = request.Descricao.Trim(); movement.DataCompetencia = request.Data; movement.DataMovimentacao = request.Data; movement.Status = request.Status; movement.Observacao = request.Observacao;
            var canonical = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == movement.Id); if (canonical is not null) { canonical.MembroId = movement.MembroId; canonical.ContaId = movement.ContaId; canonical.ValorCentavos = movement.ValorCentavos; canonical.Descricao = movement.Descricao; canonical.DataCompetencia = movement.DataCompetencia; canonical.Status = MapStatus(movement.Status); canonical.DataEfetivacao = movement.Status == StatusTransacao.EFETIVADA ? movement.DataMovimentacao : null; canonical.Observacao = movement.Observacao; }
        }
        await audit.RecordAsync("TRANSFERENCIA", transfer.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(transfer), usuarioId);
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return await Map(await FindAsync(id));
    }

    public async Task<TransferResponse> CancelAsync(Guid id, Guid? usuarioId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var transfer = await FindAsync(id); if (transfer.Status != StatusTransacao.CANCELADA)
        {
            var before = Snapshot(transfer); transfer.Status = StatusTransacao.CANCELADA;
            foreach (var movement in transfer.Transacoes) { movement.Status = StatusTransacao.CANCELADA; var canonical = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == movement.Id); if (canonical is not null) canonical.Status = StatusLancamento.CANCELADO; }
            await audit.RecordAsync("TRANSFERENCIA", transfer.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(transfer), usuarioId);
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        return await Map(transfer);
    }

    private async Task ValidateAsync(TransferRequest request)
    {
        var errors = new List<string>();
        if (request.ContaOrigemId == Guid.Empty || request.ContaDestinoId == Guid.Empty) errors.Add("As contas de origem e destino são obrigatórias.");
        if (request.ContaOrigemId == request.ContaDestinoId) errors.Add("A conta de origem deve ser diferente da conta de destino.");
        if (request.ValorCentavos <= 0) errors.Add("O valor deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(request.Descricao)) errors.Add("A descrição é obrigatória.");
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar a transferência.", [.. errors]);
        if (!await db.Contas.AnyAsync(x => x.Id == request.ContaOrigemId && x.Ativo) || !await db.Contas.AnyAsync(x => x.Id == request.ContaDestinoId && x.Ativo)) throw new DomainException("Não foi possível salvar a transferência.", "As duas contas devem existir e estar ativas.");
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("Não foi possível salvar a transferência.", "O membro selecionado está desativado ou não existe.");
    }

    private async Task<Transferencia> FindAsync(Guid id) => await db.Transferencias.Include(x => x.Transacoes).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Transferência não encontrada.");
    private static object Snapshot(Transferencia x) => new { x.Id, x.ContaOrigemId, x.ContaDestinoId, x.MembroId, x.ValorCentavos, x.Data, x.Descricao, x.Status };
    private static Lancamento ToLedger(Transacao transaction, Guid transferId) => new()
    {
        Id = transaction.Id, FamiliaId = transaction.FamiliaId, MembroId = transaction.MembroId, ContaId = transaction.ContaId, Tipo = transaction.Tipo == TipoTransacao.TRANSFERENCIA_ENTRADA ? TipoLancamento.TRANSFERENCIA_ENTRADA : TipoLancamento.TRANSFERENCIA_SAIDA, Natureza = NaturezaLancamento.TRANSFERENCIA, Status = MapStatus(transaction.Status), OrigemTipo = OrigemLancamento.TRANSFERENCIA, OrigemId = transferId, Descricao = transaction.Descricao, ValorCentavos = transaction.ValorCentavos, DataCompetencia = transaction.DataCompetencia, DataEfetivacao = transaction.Status == StatusTransacao.EFETIVADA ? transaction.DataMovimentacao : null, Observacao = transaction.Observacao
    };
    private static StatusLancamento MapStatus(StatusTransacao status) => status switch { StatusTransacao.EFETIVADA => StatusLancamento.EFETIVADO, StatusTransacao.CANCELADA => StatusLancamento.CANCELADO, _ => StatusLancamento.PREVISTO };
    private async Task<TransferResponse> Map(Transferencia x)
    {
        var names = await db.Contas.AsNoTracking().Where(c => c.Id == x.ContaOrigemId || c.Id == x.ContaDestinoId).ToDictionaryAsync(c => c.Id, c => c.Nome);
        var member = await db.Membros.AsNoTracking().Where(m => m.Id == x.MembroId).Select(m => m.Nome).SingleOrDefaultAsync() ?? "";
        return new TransferResponse(x.Id, x.ContaOrigemId, names.GetValueOrDefault(x.ContaOrigemId, ""), x.ContaDestinoId, names.GetValueOrDefault(x.ContaDestinoId, ""), x.MembroId, member, x.ValorCentavos, x.Data, x.Descricao, x.Status, x.Transacoes.Select(t => t.Id).ToList());
    }
}
