using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class CardService(FinanceDbContext db, AuditService audit, LedgerService? ledger = null)
{
    public async Task<IReadOnlyList<CardResponse>> ListCardsAsync()
    {
        await AutoCloseAsync(DateTimeOffset.UtcNow);
        var cards = await db.Cartoes.AsNoTracking().Include(x => x.Membro).Include(x => x.ContaPagamentoPadrao).ToListAsync();
        return cards.OrderBy(x => x.Nome).Select(MapCard).ToList();
    }

    public async Task<CardResponse> GetCardAsync(Guid id) => MapCard(await FindCardAsync(id));

    public async Task<CardResponse> CreateCardAsync(CardRequest request, Guid? usuarioId)
    {
        await ValidateCardAsync(request);
        var card = new Cartao { MembroId = request.MembroId, Nome = request.Nome.Trim(), Bandeira = request.Bandeira.Trim(), UltimosDigitos = request.UltimosDigitos.Trim(), LimiteTotalCentavos = request.LimiteTotalCentavos, DiaFechamento = request.DiaFechamento, DiaVencimento = request.DiaVencimento, ContaPagamentoPadraoId = request.ContaPagamentoPadraoId, Observacao = Trim(request.Observacao) };
        db.Cartoes.Add(card);
        await audit.RecordAsync("CARTAO", card.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(card), usuarioId);
        await db.SaveChangesAsync();
        return await GetCardAsync(card.Id);
    }

    public async Task<CardResponse> UpdateCardAsync(Guid id, CardRequest request, Guid? usuarioId)
    {
        await ValidateCardAsync(request);
        var card = await FindCardAsync(id); var before = Snapshot(card);
        card.MembroId = request.MembroId; card.Nome = request.Nome.Trim(); card.Bandeira = request.Bandeira.Trim(); card.UltimosDigitos = request.UltimosDigitos.Trim(); card.LimiteTotalCentavos = request.LimiteTotalCentavos; card.DiaFechamento = request.DiaFechamento; card.DiaVencimento = request.DiaVencimento; card.ContaPagamentoPadraoId = request.ContaPagamentoPadraoId; card.Observacao = Trim(request.Observacao);
        await audit.RecordAsync("CARTAO", card.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(card), usuarioId);
        await db.SaveChangesAsync();
        return await GetCardAsync(id);
    }

    public async Task<CardResponse> SetCardStatusAsync(Guid id, bool ativo, Guid? usuarioId)
    {
        var card = await FindCardAsync(id);
        if (card.Ativo == ativo) return MapCard(card);
        var before = Snapshot(card); card.Ativo = ativo;
        await audit.RecordAsync("CARTAO", card.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(card), usuarioId);
        await db.SaveChangesAsync();
        return MapCard(card);
    }

    public async Task<CardLimitResponse> GetLimitAsync(Guid cardId)
    {
        var card = await db.Cartoes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == cardId) ?? throw new DomainException("Cartão não encontrado.");
        var installments = await db.ParcelasCartao.AsNoTracking().Include(x => x.Fatura).Where(x => x.CompraCartao!.CartaoId == cardId && x.Status == StatusCompraCartao.ATIVA).ToListAsync();
        var refunds = await db.EstornosCartao.AsNoTracking().Where(x => x.CompraCartao!.CartaoId == cardId).SumAsync(x => (long?)x.ValorCentavos) ?? 0;
        var used = Math.Max(0, installments.Where(x => x.Fatura?.Status != StatusFatura.PAGA).Sum(x => x.ValorCentavos) - refunds);
        var available = Math.Max(0, card.LimiteTotalCentavos - used);
        var percentage = card.LimiteTotalCentavos == 0 ? 0 : Math.Round((decimal)used * 100 / card.LimiteTotalCentavos, 2);
        return new CardLimitResponse(card.Id, card.LimiteTotalCentavos, used, available, percentage);
    }

    public async Task<IReadOnlyList<CardPurchaseResponse>> ListPurchasesAsync(Guid? cardId = null)
    {
        var query = db.ComprasCartao.AsNoTracking().Include(x => x.Cartao).Include(x => x.Membro).Include(x => x.Categoria).Include(x => x.Parcelas).AsQueryable();
        if (cardId.HasValue) query = query.Where(x => x.CartaoId == cardId);
        return (await query.ToListAsync()).OrderByDescending(x => x.DataCompra).Select(MapPurchase).ToList();
    }

    public async Task<CardPurchaseResponse> GetPurchaseAsync(Guid id) => MapPurchase(await FindPurchaseAsync(id));

    public async Task<CardPurchaseResponse> CreatePurchaseAsync(CardPurchaseRequest request, Guid? usuarioId)
    {
        var card = await ValidatePurchaseAsync(request);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var purchase = new CompraCartao { CartaoId = request.CartaoId, MembroId = request.MembroId, CategoriaId = request.CategoriaId, Descricao = request.Descricao.Trim(), ValorTotalCentavos = request.ValorTotalCentavos, QuantidadeParcelas = request.QuantidadeParcelas, DataCompra = request.DataCompra, Observacao = Trim(request.Observacao) };
        db.ComprasCartao.Add(purchase);
        await db.SaveChangesAsync();
        await (ledger ?? new LedgerService(db, audit)).CreateCardConsumptionAsync(purchase, card, usuarioId);
        var invoices = new HashSet<Guid>();
        var values = SplitInstallments(request.ValorTotalCentavos, request.QuantidadeParcelas);
        for (var index = 0; index < values.Count; index++)
        {
            var referenceDate = request.DataCompra.AddMonths(index);
            var competence = InvoiceCycle.CompetenceFor(referenceDate, card.DiaFechamento);
            var invoice = await GetOrCreateInvoiceAsync(card, competence);
            invoices.Add(invoice.Id);
            db.ParcelasCartao.Add(new ParcelaCartao { CompraCartaoId = purchase.Id, NumeroParcela = index + 1, QuantidadeTotal = values.Count, ValorCentavos = values[index], DataCompetencia = competence, FaturaId = invoice.Id });
        }
        await audit.RecordAsync("COMPRA_CARTAO", purchase.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(purchase), usuarioId);
        await db.SaveChangesAsync();
        await RefreshInvoicesAsync(invoices);
        await transaction.CommitAsync();
        return await GetPurchaseAsync(purchase.Id);
    }

    public async Task<CardPurchaseResponse> CancelPurchaseAsync(Guid id, Guid? usuarioId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var purchase = await FindPurchaseAsync(id);
        if (purchase.Status != StatusCompraCartao.ATIVA) throw new DomainException("Somente compras ativas podem ser canceladas.");
        if (purchase.Parcelas.Any(x => x.Fatura?.Status is StatusFatura.FECHADA or StatusFatura.PAGA or StatusFatura.VENCIDA)) throw new DomainException("A compra possui fatura fechada ou paga; registre um estorno para preservar o histórico.");
        var before = Snapshot(purchase); purchase.Status = StatusCompraCartao.CANCELADA;
        foreach (var installment in purchase.Parcelas) installment.Status = StatusCompraCartao.CANCELADA;
        var consumption = await db.Lancamentos.SingleOrDefaultAsync(x => x.OrigemTipo == OrigemLancamento.COMPRA_CARTAO && x.OrigemId == purchase.Id); if (consumption is not null) consumption.Status = StatusLancamento.CANCELADO;
        await audit.RecordAsync("COMPRA_CARTAO", purchase.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(purchase), usuarioId);
        await db.SaveChangesAsync();
        await RefreshInvoicesAsync(purchase.Parcelas.Select(x => x.FaturaId));
        await transaction.CommitAsync();
        return await GetPurchaseAsync(id);
    }

    public async Task<CardPurchaseResponse> RefundPurchaseAsync(Guid id, RefundRequest request, Guid? usuarioId)
    {
        if (request.ValorCentavos <= 0 || string.IsNullOrWhiteSpace(request.Descricao)) throw new DomainException("Não foi possível registrar o estorno.", "Informe valor positivo e descrição.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var purchase = await FindPurchaseAsync(id);
        if (purchase.Status == StatusCompraCartao.CANCELADA) throw new DomainException("Compra cancelada não pode receber estorno.");
        var refunded = purchase.Estornos.Sum(x => x.ValorCentavos);
        if (request.ValorCentavos > purchase.ValorTotalCentavos - refunded) throw new DomainException("O estorno não pode exceder o saldo da compra.");
        var invoiceId = request.FaturaId ?? purchase.Parcelas.Where(x => x.Fatura?.Status != StatusFatura.PAGA).OrderBy(x => x.DataCompetencia).Select(x => (Guid?)x.FaturaId).FirstOrDefault();
        if (!invoiceId.HasValue || !purchase.Parcelas.Any(x => x.FaturaId == invoiceId.Value)) throw new DomainException("Selecione uma fatura aberta da própria compra para o estorno.");
        var refund = new EstornoCartao { CompraCartaoId = purchase.Id, FaturaId = invoiceId.Value, ValorCentavos = request.ValorCentavos, Data = request.Data, Descricao = request.Descricao.Trim() };
        db.EstornosCartao.Add(refund);
        var refundLedger = new Lancamento { FamiliaId = purchase.FamiliaId, MembroId = purchase.MembroId, ContaId = purchase.Cartao!.ContaPagamentoPadraoId, CategoriaId = purchase.CategoriaId, Tipo = TipoLancamento.AJUSTE, Natureza = NaturezaLancamento.CONSUMO, Status = StatusLancamento.EFETIVADO, OrigemTipo = OrigemLancamento.AJUSTE, OrigemId = refund.Id, Descricao = refund.Descricao, ValorCentavos = -request.ValorCentavos, DataCompetencia = request.Data, DataEfetivacao = request.Data, CriadoPorUsuarioId = usuarioId, Observacao = refund.Descricao };
        db.Lancamentos.Add(refundLedger);
        if (refunded + request.ValorCentavos == purchase.ValorTotalCentavos) purchase.Status = StatusCompraCartao.ESTORNADA;
        await audit.RecordAsync("ESTORNO_CARTAO", refund.Id, OperacaoAuditoria.ESTORNO, null, new { refund.CompraCartaoId, refund.FaturaId, refund.ValorCentavos, refund.Data }, usuarioId);
        await db.SaveChangesAsync();
        await RefreshInvoicesAsync([invoiceId.Value]);
        await transaction.CommitAsync();
        return await GetPurchaseAsync(id);
    }

    public async Task<IReadOnlyList<InvoiceResponse>> ListInvoicesAsync(Guid? cardId = null, StatusFatura? status = null)
    {
        await AutoCloseAsync(DateTimeOffset.UtcNow);
        var query = db.Faturas.AsNoTracking().Include(x => x.Cartao).Include(x => x.ContaPagamento).AsQueryable();
        if (cardId.HasValue) query = query.Where(x => x.CartaoId == cardId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        return (await query.ToListAsync()).OrderByDescending(x => x.Competencia).Select(MapInvoice).ToList();
    }

    public async Task<InvoiceDetailResponse> GetInvoiceAsync(Guid id)
    {
        var invoice = await FindInvoiceAsync(id);
        var items = invoice.Parcelas.OrderBy(x => x.DataCompetencia).ThenBy(x => x.NumeroParcela).Select(x => new InvoiceItemResponse(x.Id, x.CompraCartaoId, x.CompraCartao?.Descricao ?? "", x.CompraCartao?.Categoria?.Nome ?? "", x.NumeroParcela, x.QuantidadeTotal, x.ValorCentavos, x.Status)).ToList();
        return new InvoiceDetailResponse(MapInvoice(invoice), items);
    }

    public async Task<InvoiceResponse> CloseInvoiceAsync(Guid id, Guid? usuarioId)
    {
        var invoice = await FindInvoiceAsync(id);
        if (invoice.Status == StatusFatura.PAGA) throw new DomainException("Fatura já foi paga.");
        if (invoice.Status == StatusFatura.CANCELADA) throw new DomainException("Fatura cancelada não pode ser fechada.");
        if (invoice.Status == StatusFatura.ABERTA)
        {
            var before = Snapshot(invoice); invoice.Status = StatusFatura.FECHADA;
            await audit.RecordAsync("FATURA", invoice.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(invoice), usuarioId);
            await db.SaveChangesAsync();
        }
        return MapInvoice(invoice);
    }

    public async Task<InvoiceResponse> PayInvoiceAsync(Guid id, InvoicePaymentRequest request, Guid? usuarioId)
    {
        if (request.ValorCentavos <= 0) throw new DomainException("O valor do pagamento deve ser maior que zero.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var invoice = await FindInvoiceAsync(id);
        if (invoice.Status == StatusFatura.PAGA) return MapInvoice(invoice);
        if (invoice.Status is not (StatusFatura.FECHADA or StatusFatura.VENCIDA)) throw new DomainException("Feche a fatura antes de registrar o pagamento.");
        if (request.ValorCentavos != invoice.ValorTotalCentavos) throw new DomainException("Nesta sprint, o pagamento deve corresponder ao valor total da fatura.");
        var account = await db.Contas.SingleOrDefaultAsync(x => x.Id == request.ContaPagamentoId && x.Ativo) ?? throw new DomainException("A conta de pagamento deve existir e estar ativa.");
        var before = Snapshot(invoice);
        invoice.Status = StatusFatura.PAGA; invoice.DataPagamento = request.DataPagamento; invoice.ContaPagamentoId = account.Id;
        var payment = new Transacao { MembroId = invoice.Cartao!.MembroId, ContaId = account.Id, Tipo = TipoTransacao.DESPESA, Descricao = $"Pagamento fatura {invoice.Cartao.Nome} {invoice.Competencia:MM/yyyy}", ValorCentavos = request.ValorCentavos, DataCompetencia = request.DataPagamento, DataMovimentacao = request.DataPagamento, Status = StatusTransacao.EFETIVADA, Origem = OrigemTransacao.PAGAMENTO_FATURA };
        db.Transacoes.Add(payment);
        var paymentLedger = new Lancamento { Id = payment.Id, FamiliaId = invoice.FamiliaId, MembroId = payment.MembroId, ContaId = payment.ContaId, Tipo = TipoLancamento.PAGAMENTO_FATURA, Natureza = NaturezaLancamento.PAGAMENTO_FATURA, Status = StatusLancamento.EFETIVADO, OrigemTipo = OrigemLancamento.FATURA, OrigemId = invoice.Id, Descricao = payment.Descricao, ValorCentavos = payment.ValorCentavos, DataCompetencia = payment.DataCompetencia, DataEfetivacao = payment.DataMovimentacao, CriadoPorUsuarioId = usuarioId, Observacao = "Pagamento da fatura" };
        db.Lancamentos.Add(paymentLedger); invoice.LancamentoPagamentoId = paymentLedger.Id;
        await audit.RecordAsync("FATURA", invoice.Id, OperacaoAuditoria.PAGAMENTO, before, Snapshot(invoice), usuarioId);
        await audit.RecordAsync("TRANSACAO", payment.Id, OperacaoAuditoria.CRIACAO, null, new { payment.Tipo, payment.ValorCentavos, payment.ContaId, payment.Origem }, usuarioId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return MapInvoice(invoice);
    }

    public async Task<IReadOnlyList<InvoiceResponse>> ListCardInvoicesAsync(Guid cardId) => await ListInvoicesAsync(cardId);

    public async Task<IReadOnlyList<InvoiceProjectionResponse>> GetProjectionAsync(Guid cardId)
    {
        _ = await db.Cartoes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == cardId) ?? throw new DomainException("Cartão não encontrado.");
        var current = InvoiceCycle.MonthStart(DateTimeOffset.UtcNow);
        var invoices = await db.Faturas.AsNoTracking().Where(x => x.CartaoId == cardId).ToListAsync();
        return invoices.Where(x => x.Competencia >= current && x.Status != StatusFatura.PAGA && x.Status != StatusFatura.CANCELADA).OrderBy(x => x.Competencia).Select(x => new InvoiceProjectionResponse(x.Competencia, x.ValorTotalCentavos, x.Status)).ToList();
    }

    public async Task AutoCloseAsync(DateTimeOffset now)
    {
        var invoices = await db.Faturas.Where(x => x.Status == StatusFatura.ABERTA || x.Status == StatusFatura.FECHADA).ToListAsync();
        var changed = false;
        foreach (var invoice in invoices)
        {
            if (invoice.Status == StatusFatura.ABERTA && invoice.DataFechamento.Date <= now.Date) { invoice.Status = StatusFatura.FECHADA; changed = true; }
            if (invoice.Status == StatusFatura.FECHADA && invoice.DataVencimento.Date < now.Date) { invoice.Status = StatusFatura.VENCIDA; changed = true; }
        }
        if (changed) await db.SaveChangesAsync();
    }

    private async Task<Cartao> ValidatePurchaseAsync(CardPurchaseRequest request)
    {
        var errors = new List<string>();
        if (request.CartaoId == Guid.Empty) errors.Add("O cartão é obrigatório.");
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório.");
        if (request.CategoriaId == Guid.Empty) errors.Add("A categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(request.Descricao)) errors.Add("A descrição é obrigatória.");
        if (request.ValorTotalCentavos <= 0) errors.Add("O valor deve ser maior que zero.");
        if (request.QuantidadeParcelas < 1 || request.QuantidadeParcelas > 360) errors.Add("Informe entre 1 e 360 parcelas.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar a compra no cartão.", [.. errors]);
        var card = await db.Cartoes.SingleOrDefaultAsync(x => x.Id == request.CartaoId) ?? throw new DomainException("Cartão não encontrado.");
        if (!card.Ativo) throw new DomainException("O cartão selecionado está desativado e não pode receber compras.");
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("O membro selecionado está desativado ou não existe.");
        var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaId);
        if (category is null || !category.Ativo || category.Tipo != TipoCategoria.Despesa) throw new DomainException("A categoria selecionada deve estar ativa e ser do tipo Despesa.");
        var limit = await GetLimitAsync(card.Id);
        if (request.ValorTotalCentavos > limit.LimiteDisponivelCentavos) throw new DomainException("A compra excede o limite disponível do cartão.");
        return card;
    }

    private async Task ValidateCardAsync(CardRequest request)
    {
        var errors = new List<string>();
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório.");
        if (request.ContaPagamentoPadraoId == Guid.Empty) errors.Add("A conta padrão de pagamento é obrigatória.");
        if (string.IsNullOrWhiteSpace(request.Nome)) errors.Add("O nome do cartão é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Bandeira)) errors.Add("A bandeira é obrigatória.");
        if (request.UltimosDigitos?.Length != 4 || !request.UltimosDigitos.All(char.IsDigit)) errors.Add("Informe exatamente os quatro últimos dígitos do cartão.");
        if (request.LimiteTotalCentavos <= 0) errors.Add("O limite total deve ser maior que zero.");
        if (request.DiaFechamento is < 1 or > 31) errors.Add("O dia de fechamento deve estar entre 1 e 31.");
        if (request.DiaVencimento is < 1 or > 31) errors.Add("O dia de vencimento deve estar entre 1 e 31.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar o cartão.", [.. errors]);
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("O membro selecionado está desativado ou não existe.");
        if (!await db.Contas.AnyAsync(x => x.Id == request.ContaPagamentoPadraoId && x.Ativo)) throw new DomainException("A conta padrão de pagamento deve existir e estar ativa.");
    }

    private async Task<Fatura> GetOrCreateInvoiceAsync(Cartao card, DateTimeOffset competence)
    {
        var existing = (await db.Faturas.Where(x => x.CartaoId == card.Id).ToListAsync()).SingleOrDefault(x => x.Competencia.Year == competence.Year && x.Competencia.Month == competence.Month);
        if (existing is not null) return existing;
        var invoice = new Fatura { CartaoId = card.Id, Competencia = competence, DataFechamento = InvoiceCycle.DayOfMonth(competence, card.DiaFechamento), DataVencimento = InvoiceCycle.DayOfMonth(competence, card.DiaVencimento), ContaPagamentoId = card.ContaPagamentoPadraoId };
        db.Faturas.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private async Task RefreshInvoicesAsync(IEnumerable<Guid> invoiceIds)
    {
        foreach (var invoiceId in invoiceIds.Distinct())
        {
            var invoice = await db.Faturas.SingleAsync(x => x.Id == invoiceId);
            var installments = await db.ParcelasCartao.Where(x => x.FaturaId == invoiceId && x.Status == StatusCompraCartao.ATIVA).SumAsync(x => (long?)x.ValorCentavos) ?? 0;
            var refunds = await db.EstornosCartao.Where(x => x.FaturaId == invoiceId).SumAsync(x => (long?)x.ValorCentavos) ?? 0;
            invoice.ValorTotalCentavos = Math.Max(0, installments - refunds);
        }
        await db.SaveChangesAsync();
    }

    private async Task<Cartao> FindCardAsync(Guid id) => await db.Cartoes.Include(x => x.Membro).Include(x => x.ContaPagamentoPadrao).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Cartão não encontrado.");
    private async Task<CompraCartao> FindPurchaseAsync(Guid id) => await db.ComprasCartao.Include(x => x.Cartao).Include(x => x.Membro).Include(x => x.Categoria).Include(x => x.Estornos).Include(x => x.Parcelas).ThenInclude(x => x.Fatura).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Compra no cartão não encontrada.");
    private async Task<Fatura> FindInvoiceAsync(Guid id) => await db.Faturas.Include(x => x.Cartao).Include(x => x.ContaPagamento).Include(x => x.Parcelas).ThenInclude(x => x.CompraCartao).ThenInclude(x => x!.Categoria).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Fatura não encontrada.");

    private static IReadOnlyList<long> SplitInstallments(long total, int count)
    {
        var baseValue = total / count; var remainder = total % count;
        return Enumerable.Range(0, count).Select(index => baseValue + (index >= count - remainder ? 1 : 0)).ToList();
    }
    private static CardResponse MapCard(Cartao x) => new(x.Id, x.MembroId, x.Membro?.Nome ?? "", x.Nome, x.Bandeira, x.UltimosDigitos, x.LimiteTotalCentavos, x.DiaFechamento, x.DiaVencimento, x.ContaPagamentoPadraoId, x.ContaPagamentoPadrao?.Nome ?? "", x.Observacao, x.Ativo);
    private static CardPurchaseResponse MapPurchase(CompraCartao x) => new(x.Id, x.CartaoId, x.Cartao?.Nome ?? "", x.MembroId, x.Membro?.Nome ?? "", x.CategoriaId, x.Categoria?.Nome ?? "", x.Descricao, x.ValorTotalCentavos, x.QuantidadeParcelas, x.DataCompra, x.Observacao, x.Status, x.Parcelas.OrderBy(p => p.NumeroParcela).Select(p => new InstallmentResponse(p.Id, p.NumeroParcela, p.QuantidadeTotal, p.ValorCentavos, p.DataCompetencia, p.FaturaId, p.Status)).ToList());
    private static InvoiceResponse MapInvoice(Fatura x) => new(x.Id, x.CartaoId, x.Cartao?.Nome ?? "", x.Competencia, x.DataFechamento, x.DataVencimento, x.ValorTotalCentavos, x.Status, x.DataPagamento, x.ContaPagamentoId, x.ContaPagamento?.Nome);
    private static object Snapshot(Cartao x) => new { x.Id, x.Nome, x.LimiteTotalCentavos, x.DiaFechamento, x.DiaVencimento, x.Ativo };
    private static object Snapshot(CompraCartao x) => new { x.Id, x.CartaoId, x.Descricao, x.ValorTotalCentavos, x.QuantidadeParcelas, x.DataCompra, x.Status };
    private static object Snapshot(Fatura x) => new { x.Id, x.CartaoId, x.Competencia, x.ValorTotalCentavos, x.Status, x.DataPagamento, x.ContaPagamentoId };
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public static class InvoiceCycle
{
    public static DateTimeOffset CompetenceFor(DateTimeOffset purchaseDate, int closingDay)
    {
        var closing = Math.Min(closingDay, DateTime.DaysInMonth(purchaseDate.Year, purchaseDate.Month));
        var reference = purchaseDate.Day <= closing ? purchaseDate : purchaseDate.AddMonths(1);
        return MonthStart(reference);
    }

    // Meio-dia UTC evita que uma competência mensal seja renderizada como o último dia do mês anterior em fusos negativos.
    public static DateTimeOffset MonthStart(DateTimeOffset date) => new(date.Year, date.Month, 1, 12, 0, 0, TimeSpan.Zero);
    public static DateTimeOffset DayOfMonth(DateTimeOffset competence, int requestedDay) => new(competence.Year, competence.Month, Math.Min(requestedDay, DateTime.DaysInMonth(competence.Year, competence.Month)), 12, 0, 0, TimeSpan.Zero);
}
