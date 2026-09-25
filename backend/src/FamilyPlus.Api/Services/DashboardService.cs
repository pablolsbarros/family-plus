using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class DashboardService(FinanceDbContext db, RecurrenceService recurrences, BudgetService budgets, Func<DateTimeOffset>? utcNow = null, SaudeFinanceiraService? financialHealth = null)
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(DashboardQueryRequest request)
    {
        var scope = await LoadScopeAsync(request);
        var today = Normalize(UtcNow());
        var horizonDays = NormalizeHorizon(request.HorizonteDias);
        var projectionEnd = today.AddDays(horizonDays);
        var periodStart = ParseDate(request.DataInicio) ?? new DateTimeOffset(today.Year, today.Month, 1, 12, 0, 0, TimeSpan.Zero);
        var periodEnd = ParseDate(request.DataFim) ?? today;
        if (periodEnd < periodStart) (periodStart, periodEnd) = (periodEnd, periodStart);

        var accounts = scope.Accounts.Where(x => x.Ativo && (!request.MembroId.HasValue || x.MembroId == request.MembroId)).ToList();
        var accountIds = accounts.Select(x => x.Id).ToHashSet();
        var memberTransactions = scope.Transactions.Where(x => accountIds.Contains(x.ContaId) && (!request.MembroId.HasValue || x.MembroId == request.MembroId)).ToList();
        var realTransactions = memberTransactions.Where(x => x.Status == StatusTransacao.EFETIVADA && x.Origem != OrigemTransacao.PAGAMENTO_FATURA).ToList();
        var periodTransactions = realTransactions.Where(x => InRange(x.DataMovimentacao, periodStart, periodEnd)).ToList();
        var purchases = scope.Purchases.Where(x => x.Status != StatusCompraCartao.CANCELADA && (!request.MembroId.HasValue || x.MembroId == request.MembroId) && InRange(x.DataCompra, periodStart, periodEnd)).ToList();
        var purchaseRefunds = scope.Refunds.GroupBy(x => x.CompraCartaoId).ToDictionary(x => x.Key, x => x.Sum(y => y.ValorCentavos));
        var cardConsumption = purchases.Sum(x => Math.Max(0, x.ValorTotalCentavos - purchaseRefunds.GetValueOrDefault(x.Id)));
        var income = periodTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorCentavos);
        var expense = periodTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => x.ValorCentavos) + cardConsumption;
        var period = new DashboardPeriodResponse(income, expense, income - expense);

        var accountBalances = accounts.Select(account =>
        {
            var balance = FinanceCalculator.CalculateBalance(account.SaldoInicialCentavos, memberTransactions.Where(x => x.ContaId == account.Id).Select(x => (x.Tipo, x.ValorCentavos, x.Status)));
            return new DashboardAccountResponse(account.Id, account.Nome, account.Membro?.Nome ?? "", balance);
        }).ToList();
        var balanceResponse = new DashboardBalanceResponse(accountBalances.Sum(x => x.SaldoAtualCentavos), accountBalances);

        var occurrenceRows = await LoadOccurrencesAsync(today, projectionEnd, request.MembroId);
        var plannedTransactions = memberTransactions.Where(x => x.Status == StatusTransacao.PREVISTA && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA) && InRange(x.DataMovimentacao, today, projectionEnd)).ToList();
        var futureInvoices = scope.Invoices.Where(x => x.Status is not (StatusFatura.PAGA or StatusFatura.CANCELADA) && (!request.MembroId.HasValue || x.Cartao?.MembroId == request.MembroId) && InRange(x.DataVencimento, today, projectionEnd)).ToList();
        var plannedIncome = plannedTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorCentavos) + occurrenceRows.Where(x => x.Recorrencia!.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorPrevistoCentavos);
        var plannedExpense = plannedTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => x.ValorCentavos) + occurrenceRows.Where(x => x.Recorrencia!.Tipo == TipoTransacao.DESPESA).Sum(x => x.ValorPrevistoCentavos);
        var invoiceTotal = futureInvoices.Sum(x => x.ValorTotalCentavos);
        var projection = new DashboardProjectionResponse(balanceResponse.SaldoConsolidadoCentavos, plannedIncome, plannedExpense, invoiceTotal, balanceResponse.SaldoConsolidadoCentavos + plannedIncome - plannedExpense - invoiceTotal, horizonDays, today, projectionEnd);

        var flow = BuildCashFlow(today, projectionEnd, balanceResponse.SaldoConsolidadoCentavos, memberTransactions, occurrenceRows, futureInvoices);
        var upcoming = BuildUpcoming(today, projectionEnd, plannedTransactions, occurrenceRows, futureInvoices);
        var cards = BuildCards(scope.Cards.Where(x => x.Ativo && (!request.MembroId.HasValue || x.MembroId == request.MembroId)).ToList(), scope.Invoices, scope.Installments, purchaseRefunds);
        var expenseCategories = BuildCategories(periodTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA), purchases, purchaseRefunds, false, expense);
        var incomeCategories = BuildCategories(periodTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA), [], purchaseRefunds, true, income);
        var evolution = BuildEvolution(memberTransactions, scope.Purchases.Where(x => !request.MembroId.HasValue || x.MembroId == request.MembroId).ToList(), purchaseRefunds, accounts, periodStart, periodEnd);
        var activeRecurrences = scope.Recurrences.Where(x => x.Status == StatusRecorrencia.ATIVA && (!request.MembroId.HasValue || x.MembroId == request.MembroId)).ToList();
        var recurringIncome = activeRecurrences.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var recurringExpense = activeRecurrences.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var openAlerts = await db.Alertas.CountAsync(x => !x.Resolvido);
        var kpis = new DashboardKpiResponse(
            Percentage(period.ResultadoCentavos, period.ReceitasCentavos),
            Percentage(recurringExpense, recurringIncome),
            Percentage(activeRecurrences.Where(x => x.Tipo == TipoTransacao.DESPESA && x.Classificacao == TipoRecorrencia.CONTA_FIXA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia)), period.DespesasCentavos),
            upcoming.Expenses.Sum(x => x.ValorCentavos), upcoming.Incomes.Sum(x => x.ValorCentavos), openAlerts);
        var budget = await budgets.DashboardAsync(today.Year, today.Month, request.MembroId);
        var financialHealthResponse = await (financialHealth ?? new SaudeFinanceiraService(db, budgets, utcNow)).CalculateAsync(request.MembroId, periodStart, periodEnd);
        return new DashboardSummaryResponse(balanceResponse, period, projection, flow, upcoming.Incomes, upcoming.Expenses, cards, expenseCategories, incomeCategories, evolution, kpis, budget, financialHealthResponse);
    }

    public async Task<DashboardBalanceResponse> GetBalanceAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).Saldo;
    public async Task<IReadOnlyList<DashboardAccountResponse>> GetAccountBalancesAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).Saldo.Contas;
    public async Task<DashboardPeriodResponse> GetPeriodAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).Periodo;
    public async Task<DashboardCashFlowResponse> GetCashFlowAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).FluxoCaixa;
    public async Task<DashboardProjectionResponse> GetProjectionAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).Projecao;
    public async Task<DashboardKpiResponse> GetKpisAsync(DashboardQueryRequest request) => (await GetSummaryAsync(request)).Kpis;
    public async Task<BudgetDashboardResponse?> GetBudgetAsync(DashboardQueryRequest request)
    {
        var today = Normalize(UtcNow());
        return await budgets.DashboardAsync(today.Year, today.Month, request.MembroId);
    }

    private async Task<DashboardScope> LoadScopeAsync(DashboardQueryRequest request)
    {
        await recurrences.GenerateAllAsync(UtcNow());
        var accounts = await db.Contas.AsNoTracking().Include(x => x.Membro).Where(x => x.Ativo).ToListAsync();
        var transactions = await db.Transacoes.AsNoTracking().Include(x => x.Categoria).ToListAsync();
        var purchases = await db.ComprasCartao.AsNoTracking().Include(x => x.Categoria).ToListAsync();
        var refunds = await db.EstornosCartao.AsNoTracking().ToListAsync();
        var cards = await db.Cartoes.AsNoTracking().Include(x => x.Membro).ToListAsync();
        var invoices = await db.Faturas.AsNoTracking().Include(x => x.Cartao).ToListAsync();
        var installments = await db.ParcelasCartao.AsNoTracking().Include(x => x.Fatura).Include(x => x.CompraCartao).ToListAsync();
        var recurrenceList = await db.Recorrencias.AsNoTracking().ToListAsync();
        return new DashboardScope(accounts, transactions, purchases, refunds, cards, invoices, installments, recurrenceList);
    }

    private async Task<List<OcorrenciaRecorrencia>> LoadOccurrencesAsync(DateTimeOffset start, DateTimeOffset end, Guid? memberId)
    {
        var rows = await db.OcorrenciasRecorrentes.AsNoTracking().Include(x => x.Recorrencia).ToListAsync();
        return rows.Where(x => x.Status is StatusOcorrenciaRecorrencia.PENDENTE or StatusOcorrenciaRecorrencia.ATRASADA && InRange(x.DataPrevista, start, end) && (!memberId.HasValue || x.Recorrencia!.MembroId == memberId)).ToList();
    }

    private static DashboardCashFlowResponse BuildCashFlow(DateTimeOffset start, DateTimeOffset end, long openingBalance, IReadOnlyList<Transacao> transactions, IReadOnlyList<OcorrenciaRecorrencia> occurrences, IReadOnlyList<Fatura> invoices)
    {
        var events = new Dictionary<DateTime, FlowValues>();
        void Add(DateTimeOffset date, Action<FlowValues> change) { var key = date.Date; if (!events.TryGetValue(key, out var item)) events[key] = item = new FlowValues(); change(item); }
        foreach (var item in transactions.Where(x => x.Status == StatusTransacao.EFETIVADA && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA) && InRange(x.DataMovimentacao, start, end))) Add(item.DataMovimentacao, values => { if (item.Tipo == TipoTransacao.RECEITA) values.RealIncome += item.ValorCentavos; else values.RealExpense += item.ValorCentavos; });
        foreach (var item in transactions.Where(x => x.Status == StatusTransacao.PREVISTA && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA) && InRange(x.DataMovimentacao, start, end))) Add(item.DataMovimentacao, values => { if (item.Tipo == TipoTransacao.RECEITA) values.PlannedIncome += item.ValorCentavos; else values.PlannedExpense += item.ValorCentavos; });
        foreach (var item in occurrences) Add(item.DataPrevista, values => { if (item.Recorrencia!.Tipo == TipoTransacao.RECEITA) values.PlannedIncome += item.ValorPrevistoCentavos; else values.PlannedExpense += item.ValorPrevistoCentavos; });
        foreach (var item in invoices) Add(item.DataVencimento, values => values.Invoices += item.ValorTotalCentavos);
        var running = openingBalance; var rows = new List<DashboardCashFlowRow>();
        foreach (var pair in events.OrderBy(x => x.Key)) { var value = pair.Value; running += value.RealIncome + value.PlannedIncome - value.RealExpense - value.PlannedExpense - value.Invoices; rows.Add(new DashboardCashFlowRow(new DateTimeOffset(pair.Key, TimeSpan.Zero).AddHours(12), value.RealIncome, value.RealExpense, value.PlannedIncome, value.PlannedExpense, value.Invoices, running)); }
        return new DashboardCashFlowResponse(start, end, openingBalance, rows);
    }

    private static (IReadOnlyList<DashboardUpcomingResponse> Incomes, IReadOnlyList<DashboardUpcomingResponse> Expenses) BuildUpcoming(DateTimeOffset start, DateTimeOffset end, IReadOnlyList<Transacao> transactions, IReadOnlyList<OcorrenciaRecorrencia> occurrences, IReadOnlyList<Fatura> invoices)
    {
        var items = new List<DashboardUpcomingResponse>();
        items.AddRange(transactions.Select(x => new DashboardUpcomingResponse(x.Id, "TRANSACAO", x.Descricao, x.DataMovimentacao, x.ValorCentavos, x.Tipo, true, x.ContaId.ToString())));
        items.AddRange(occurrences.Select(x => new DashboardUpcomingResponse(x.Id, "RECORRENCIA", x.Recorrencia!.Descricao, x.DataPrevista, x.ValorPrevistoCentavos, x.Recorrencia.Tipo, true, x.RecorrenciaId.ToString())));
        items.AddRange(invoices.Select(x => new DashboardUpcomingResponse(x.Id, "FATURA", $"Fatura {x.Cartao?.Nome ?? "Cartão"}", x.DataVencimento, x.ValorTotalCentavos, TipoTransacao.DESPESA, true, x.CartaoId.ToString())));
        var ordered = items.Where(x => InRange(x.Data, start, end)).OrderBy(x => x.Data).ThenBy(x => x.Descricao).ToList();
        return (ordered.Where(x => x.Tipo == TipoTransacao.RECEITA).Take(12).ToList(), ordered.Where(x => x.Tipo == TipoTransacao.DESPESA).Take(12).ToList());
    }

    private static IReadOnlyList<DashboardCardResponse> BuildCards(IReadOnlyList<Cartao> cards, IReadOnlyList<Fatura> invoices, IReadOnlyList<ParcelaCartao> installments, IReadOnlyDictionary<Guid, long> refunds)
    {
        return cards.Select(card =>
        {
            var cardInvoices = invoices.Where(x => x.CartaoId == card.Id).ToList();
            var currentInvoice = cardInvoices.Where(x => x.Status is not (StatusFatura.PAGA or StatusFatura.CANCELADA)).OrderBy(x => x.DataVencimento).FirstOrDefault();
            var cardInstallments = installments.Where(x => x.Fatura?.CartaoId == card.Id && x.Status == StatusCompraCartao.ATIVA && x.Fatura.Status is not (StatusFatura.PAGA or StatusFatura.CANCELADA)).ToList();
            var used = cardInstallments.Sum(x => x.ValorCentavos);
            used = Math.Max(0, used - cardInstallments.Select(x => x.CompraCartaoId).Distinct().Sum(id => refunds.GetValueOrDefault(id)));
            var percentage = card.LimiteTotalCentavos == 0 ? 0 : Math.Round((decimal)used * 100 / card.LimiteTotalCentavos, 2);
            return new DashboardCardResponse(card.Id, card.Nome, card.Bandeira, currentInvoice?.ValorTotalCentavos ?? 0, currentInvoice?.DataVencimento, card.LimiteTotalCentavos, used, Math.Max(0, card.LimiteTotalCentavos - used), percentage);
        }).ToList();
    }

    private static IReadOnlyList<DashboardCategoryResponse> BuildCategories(IEnumerable<Transacao> transactions, IReadOnlyList<CompraCartao> purchases, IReadOnlyDictionary<Guid, long> refunds, bool income, long total)
    {
        var values = new Dictionary<string, (Guid? Id, string Name, long Value)>();
        foreach (var item in transactions) { var key = item.CategoriaId?.ToString() ?? ""; var name = item.Categoria?.Nome ?? "Sem categoria"; var current = values.GetValueOrDefault(key); values[key] = (item.CategoriaId, name, current.Value + item.ValorCentavos); }
        if (!income) foreach (var item in purchases) { var key = item.CategoriaId.ToString(); var name = item.Categoria?.Nome ?? "Sem categoria"; var current = values.GetValueOrDefault(key); values[key] = (item.CategoriaId, name, current.Value + Math.Max(0, item.ValorTotalCentavos - refunds.GetValueOrDefault(item.Id))); }
        return values.Values.OrderByDescending(x => x.Value).Select(x => new DashboardCategoryResponse(x.Id, x.Name, x.Value, total == 0 ? 0 : Math.Round((decimal)x.Value * 100 / total, 2))).ToList();
    }

    private static IReadOnlyList<DashboardMonthlyResponse> BuildEvolution(IReadOnlyList<Transacao> memberTransactions, IReadOnlyList<CompraCartao> purchasesForEvolution, IReadOnlyDictionary<Guid, long> refunds, IReadOnlyList<Conta> accounts, DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        var firstMonth = new DateTimeOffset(periodStart.Year, periodStart.Month, 1, 12, 0, 0, TimeSpan.Zero);
        var lastMonth = new DateTimeOffset(periodEnd.Year, periodEnd.Month, 1, 12, 0, 0, TimeSpan.Zero);
        var result = new List<DashboardMonthlyResponse>();
        for (var month = firstMonth; month <= lastMonth; month = month.AddMonths(1))
        {
            var next = month.AddMonths(1);
            var monthStartUtc = month.UtcDateTime.Date;
            var nextMonthStartUtc = next.UtcDateTime.Date;
            var rows = memberTransactions.Where(x => x.Status == StatusTransacao.EFETIVADA && x.Origem != OrigemTransacao.PAGAMENTO_FATURA && (x.Tipo is TipoTransacao.RECEITA or TipoTransacao.DESPESA) && x.DataMovimentacao.UtcDateTime.Date >= monthStartUtc && x.DataMovimentacao.UtcDateTime.Date < nextMonthStartUtc && InRange(x.DataMovimentacao, periodStart, periodEnd)).ToList();
            var monthPurchases = purchasesForEvolution.Where(x => x.Status != StatusCompraCartao.CANCELADA && x.DataCompra.UtcDateTime.Date >= monthStartUtc && x.DataCompra.UtcDateTime.Date < nextMonthStartUtc && InRange(x.DataCompra, periodStart, periodEnd)).Sum(x => Math.Max(0, x.ValorTotalCentavos - refunds.GetValueOrDefault(x.Id)));
            var income = rows.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => x.ValorCentavos);
            var expense = rows.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => x.ValorCentavos) + monthPurchases;
            var balanceCutoffUtc = periodEnd.UtcDateTime.Date.AddDays(1) < nextMonthStartUtc ? periodEnd.UtcDateTime.Date.AddDays(1) : nextMonthStartUtc;
            var balance = accounts.Sum(x => x.SaldoInicialCentavos) + memberTransactions.Where(x => x.Status == StatusTransacao.EFETIVADA && x.DataMovimentacao.UtcDateTime < balanceCutoffUtc).Sum(x => FinanceCalculator.Delta(x.Tipo, x.ValorCentavos));
            result.Add(new DashboardMonthlyResponse(month, income, expense, income - expense, balance));
        }
        return result;
    }

    private static decimal Percentage(long numerator, long denominator) => denominator == 0 ? 0 : Math.Round((decimal)numerator * 100 / denominator, 2);
    private DateTimeOffset UtcNow() => utcNow?.Invoke() ?? DateTimeOffset.UtcNow;
    private static int NormalizeHorizon(int value) => value is 7 or 15 or 30 or 60 or 90 ? value : 30;
    private static bool InRange(DateTimeOffset value, DateTimeOffset start, DateTimeOffset end) => value.Date >= start.Date && value.Date <= end.Date;
    private static DateTimeOffset Normalize(DateTimeOffset value) => new(value.Year, value.Month, value.Day, 12, 0, 0, TimeSpan.Zero);
    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, out var result) ? Normalize(result) : null;

    private sealed class FlowValues { public long RealIncome; public long RealExpense; public long PlannedIncome; public long PlannedExpense; public long Invoices; }
    private sealed record DashboardScope(IReadOnlyList<Conta> Accounts, IReadOnlyList<Transacao> Transactions, IReadOnlyList<CompraCartao> Purchases, IReadOnlyList<EstornoCartao> Refunds, IReadOnlyList<Cartao> Cards, IReadOnlyList<Fatura> Invoices, IReadOnlyList<ParcelaCartao> Installments, IReadOnlyList<Recorrencia> Recurrences);
}
