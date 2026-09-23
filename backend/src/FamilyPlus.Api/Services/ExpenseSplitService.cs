using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class ExpenseSplitService(FinanceDbContext db, FamilyAccessService access, AuditService audit)
{
    public async Task<SplitResponse?> GetAsync(Guid transactionId)
    {
        var split = await db.RateiosDespesa.Include(x => x.Itens).ThenInclude(x => x.Membro).SingleOrDefaultAsync(x => x.TransacaoId == transactionId); return split is null ? null : Map(split);
    }

    public async Task<SplitResponse> SaveAsync(Guid transactionId, SplitRequest request, Guid userId)
    {
        await access.EnsurePermissionAsync(userId, "MOVIMENTACAO_EDITAR");
        var transaction = await db.Transacoes.SingleOrDefaultAsync(x => x.Id == transactionId) ?? throw new DomainException("Transação não encontrada.");
        if (transaction.Tipo != TipoTransacao.DESPESA) throw new DomainException("Rateio só pode ser aplicado a despesas.");
        if (request.Itens.Count == 0) throw new DomainException("Informe ao menos um membro no rateio.");
        var memberIds = request.Itens.Select(x => x.MembroId).Distinct().ToList(); var members = await db.Membros.Where(x => memberIds.Contains(x.Id) && x.Ativo).ToListAsync(); if (members.Count != memberIds.Count) throw new DomainException("Um ou mais membros não pertencem à família atual.");
        var values = new List<(Guid Id, decimal Percent, long Value)>();
        if (request.Tipo == TipoRateio.PERCENTUAL)
        {
            var total = request.Itens.Sum(x => x.Percentual); if (Math.Abs(total - 100m) > 0.0001m) throw new DomainException("O rateio percentual precisa totalizar exatamente 100%.");
            long accumulated = 0; foreach (var (item, index) in request.Itens.Select((x, i) => (x, i))) { var value = index == request.Itens.Count - 1 ? transaction.ValorCentavos - accumulated : (long)Math.Round(transaction.ValorCentavos * item.Percentual / 100m, MidpointRounding.AwayFromZero); values.Add((item.MembroId, item.Percentual, value)); accumulated += value; }
        }
        else if (request.Tipo == TipoRateio.VALOR_FIXO)
        {
            var total = request.Itens.Sum(x => x.ValorCentavos); if (total != transaction.ValorCentavos) throw new DomainException("O rateio por valor precisa fechar exatamente o valor da despesa."); values.AddRange(request.Itens.Select(x => (x.MembroId, transaction.ValorCentavos == 0 ? 0 : x.ValorCentavos * 100m / transaction.ValorCentavos, x.ValorCentavos)));
        }
        else
        {
            var baseValue = transaction.ValorCentavos / request.Itens.Count; var remainder = transaction.ValorCentavos % request.Itens.Count; values.AddRange(request.Itens.Select((x, i) => (x.MembroId, 100m / request.Itens.Count, baseValue + (i < remainder ? 1 : 0))));
        }
        var current = await db.RateiosDespesa.Include(x => x.Itens).SingleOrDefaultAsync(x => x.TransacaoId == transactionId); if (current is not null) db.RateiosDespesa.Remove(current);
        var split = new RateioDespesa { FamiliaId = transaction.FamiliaId, TransacaoId = transaction.Id, LancamentoId = await db.Lancamentos.Where(x => x.Id == transaction.Id).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(), Tipo = request.Tipo, ValorDespesaCentavos = transaction.ValorCentavos, PadraoAplicado = request.AplicarComoPadrao, Itens = values.Select(x => new RateioDespesaItem { FamiliaId = transaction.FamiliaId, MembroId = x.Id, Percentual = x.Percent, ValorCentavos = x.Value }).ToList() }; db.RateiosDespesa.Add(split); await audit.RecordAsync("RATEIO_DESPESA", split.Id, OperacaoAuditoria.CRIACAO, null, new { split.Tipo, split.ValorDespesaCentavos, itens = values }, userId); await db.SaveChangesAsync(); return Map(await db.RateiosDespesa.Include(x => x.Itens).ThenInclude(x => x.Membro).SingleAsync(x => x.Id == split.Id));
    }

    public async Task DeleteAsync(Guid transactionId, Guid userId) { await access.EnsurePermissionAsync(userId, "MOVIMENTACAO_EDITAR"); var split = await db.RateiosDespesa.SingleOrDefaultAsync(x => x.TransacaoId == transactionId) ?? throw new DomainException("A despesa não possui rateio."); db.RateiosDespesa.Remove(split); await audit.RecordAsync("RATEIO_DESPESA", split.Id, OperacaoAuditoria.CANCELAMENTO, null, null, userId); await db.SaveChangesAsync(); }
    private static SplitResponse Map(RateioDespesa x) => new(x.Id, x.TransacaoId, x.Tipo, x.ValorDespesaCentavos, x.PadraoAplicado, x.Itens.Select(i => new SplitItemResponse(i.MembroId, i.Membro?.Nome ?? "", i.Percentual, i.ValorCentavos)).ToList());
}
