using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class DashboardOperationsTests
{
    [Fact]
    public async Task Resumo_calcula_resultado_taxa_e_ignora_transferencia_e_pagamento_de_fatura()
    {
        await using var fixture = await DashboardFixture.CreateAsync();
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 1_000_000, StatusTransacao.EFETIVADA);
        await fixture.AddTransactionAsync(TipoTransacao.DESPESA, 700_000, StatusTransacao.EFETIVADA);
        await fixture.AddTransactionAsync(TipoTransacao.TRANSFERENCIA_SAIDA, 200_000, StatusTransacao.EFETIVADA);
        await fixture.AddTransactionAsync(TipoTransacao.DESPESA, 800_000, StatusTransacao.EFETIVADA, OrigemTransacao.PAGAMENTO_FATURA);

        var summary = await fixture.Dashboard.GetSummaryAsync(new DashboardQueryRequest("2026-09-01", "2026-09-30", HorizonteDias: 30));

        Assert.Equal(1_000_000, summary.Periodo.ReceitasCentavos);
        Assert.Equal(700_000, summary.Periodo.DespesasCentavos);
        Assert.Equal(300_000, summary.Periodo.ResultadoCentavos);
        Assert.Equal(30, summary.Kpis.TaxaPoupancaPercentual);
    }

    [Fact]
    public async Task Evolucao_mensal_respeita_o_intervalo_customizado()
    {
        await using var fixture = await DashboardFixture.CreateAsync();
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 10_000, StatusTransacao.EFETIVADA, data: new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 15_000, StatusTransacao.EFETIVADA, data: new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 20_000, StatusTransacao.EFETIVADA, data: new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 5_000, StatusTransacao.EFETIVADA, data: new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        await fixture.AddTransactionAsync(TipoTransacao.RECEITA, 30_000, StatusTransacao.EFETIVADA, data: new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

        var summary = await fixture.Dashboard.GetSummaryAsync(new DashboardQueryRequest("2026-08-15", "2026-09-20", HorizonteDias: 30));

        Assert.Equal(2, summary.Evolucao.Count);
        Assert.Equal(15_000, summary.Evolucao[0].ReceitasCentavos);
        Assert.Equal(20_000, summary.Evolucao[1].ReceitasCentavos);
        Assert.Equal(35_000, summary.Periodo.ReceitasCentavos);
    }

    [Fact]
    public async Task Projecao_soma_previsto_recorrencia_e_fatura_sem_alterar_saldo_real()
    {
        await using var fixture = await DashboardFixture.CreateAsync();
        await fixture.AddTransactionAsync(TipoTransacao.DESPESA, 50_000, StatusTransacao.PREVISTA, data: fixture.Today.AddDays(5));
        var recurrence = new Recorrencia { MembroId = fixture.Member.Id, ContaId = fixture.Account.Id, CategoriaId = fixture.ExpenseCategory.Id, Tipo = TipoTransacao.DESPESA, Descricao = "Energia", ValorCentavos = 25_000, Frequencia = FrequenciaRecorrencia.MENSAL, DataInicio = fixture.Today.AddDays(6), ProximaOcorrencia = fixture.Today.AddDays(6), DiaReferencia = fixture.Today.AddDays(6).Day };
        fixture.Db.Recorrencias.Add(recurrence);
        var card = new Cartao { MembroId = fixture.Member.Id, Nome = "Cartão", Bandeira = "Visa", UltimosDigitos = "1234", LimiteTotalCentavos = 500_000, DiaFechamento = 25, DiaVencimento = 10, ContaPagamentoPadraoId = fixture.Account.Id };
        fixture.Db.Cartoes.Add(card);
        fixture.Db.Faturas.Add(new Fatura { CartaoId = card.Id, Competencia = fixture.Today, DataFechamento = fixture.Today.AddDays(10), DataVencimento = fixture.Today.AddDays(8), ValorTotalCentavos = 100_000 });
        await fixture.Db.SaveChangesAsync();

        var summary = await fixture.Dashboard.GetSummaryAsync(new DashboardQueryRequest(HorizonteDias: 15));

        Assert.Equal(200_000, summary.Saldo.SaldoConsolidadoCentavos);
        Assert.Equal(25_000, summary.Projecao.SaldoProjetadoCentavos);
        Assert.Equal(75_000, summary.Projecao.DespesasPrevistasCentavos);
        Assert.Equal(100_000, summary.Projecao.FaturasPrevistasCentavos);
    }

    [Fact]
    public async Task Alertas_sao_gerados_sem_duplicidade_e_podem_ser_resolvidos()
    {
        await using var fixture = await DashboardFixture.CreateAsync(initialBalance: -10_000);
        var first = await fixture.Alerts.ListAsync(new AlertQueryRequest(Resolvido: false));
        var second = await fixture.Alerts.ListAsync(new AlertQueryRequest(Resolvido: false));

        var balanceAlerts = second.Where(x => x.Tipo == TipoAlerta.SALDO_NEGATIVO).ToList();
        Assert.Single(balanceAlerts);
        Assert.Equal(first.Count, second.Count);
        await fixture.Alerts.ResolveAsync(balanceAlerts[0].Id);
        Assert.DoesNotContain(await fixture.Alerts.ListAsync(new AlertQueryRequest(Resolvido: false)), x => x.Id == balanceAlerts[0].Id);
    }

    private sealed class DashboardFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private DashboardFixture(SqliteConnection connection, FinanceDbContext db)
        {
            this.connection = connection; Db = db; Today = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero); var audit = new AuditService(db); Transactions = new TransactionService(db, audit); Cards = new CardService(db, audit); Recurrences = new RecurrenceService(db, audit, Transactions, Cards); Budgets = new BudgetService(db, audit, Recurrences, () => Today); Dashboard = new DashboardService(db, Recurrences, Budgets, () => Today); Alerts = new AlertService(db, Dashboard, Cards, Budgets);
        }
        public FinanceDbContext Db { get; }
        public TransactionService Transactions { get; }
        public CardService Cards { get; }
        public RecurrenceService Recurrences { get; }
        public BudgetService Budgets { get; }
        public DashboardService Dashboard { get; }
        public AlertService Alerts { get; }
        public DateTimeOffset Today { get; }
        public Membro Member { get; private set; } = null!;
        public Conta Account { get; private set; } = null!;
        public Categoria IncomeCategory { get; private set; } = null!;
        public Categoria ExpenseCategory { get; private set; } = null!;

        public static async Task<DashboardFixture> CreateAsync(long initialBalance = 200_000)
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var db = new FinanceDbContext(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync(); var fixture = new DashboardFixture(connection, db); fixture.Member = new Membro { Nome = "Pablo" }; fixture.Account = new Conta { MembroId = fixture.Member.Id, Nome = "Conta", Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = initialBalance }; fixture.IncomeCategory = new Categoria { Nome = "Salário", Tipo = TipoCategoria.Receita }; fixture.ExpenseCategory = new Categoria { Nome = "Casa", Tipo = TipoCategoria.Despesa }; db.AddRange(fixture.Member, fixture.Account, fixture.IncomeCategory, fixture.ExpenseCategory); await db.SaveChangesAsync(); return fixture;
        }
        public async Task AddTransactionAsync(TipoTransacao type, long value, StatusTransacao status, OrigemTransacao origin = OrigemTransacao.NORMAL, DateTimeOffset? data = null) { Db.Transacoes.Add(new Transacao { MembroId = Member.Id, ContaId = Account.Id, CategoriaId = type == TipoTransacao.RECEITA ? IncomeCategory.Id : ExpenseCategory.Id, Tipo = type, Descricao = "Teste", ValorCentavos = value, DataCompetencia = data ?? Today, DataMovimentacao = data ?? Today, Status = status, Origem = origin }); await Db.SaveChangesAsync(); }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
