using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class BudgetOperationsTests
{
    [Fact]
    public async Task Orcamento_calcula_disponivel_e_percentual_a_partir_do_realizado()
    {
        await using var fixture = await BudgetFixture.CreateAsync();
        var budget = await fixture.CreateBudgetAsync(150_000);
        await fixture.AddBudgetItemAsync(budget.Id, fixture.ExpenseCategory.Id, 150_000);
        await fixture.AddExpenseAsync(90_000, fixture.ExpenseCategory.Id);

        var summary = await fixture.Budgets.SummaryAsync(budget.Id);
        var line = Assert.Single(summary.Linhas);

        Assert.Equal(150_000, summary.TotalPlanejadoCentavos);
        Assert.Equal(90_000, summary.TotalRealizadoCentavos);
        Assert.Equal(60_000, line.DisponivelCentavos);
        Assert.Equal(60, line.PercentualUtilizado);
        Assert.Equal("SAUDAVEL", line.Situacao);
    }

    [Fact]
    public async Task Cartao_entra_no_consumo_e_pagamento_de_fatura_nao_duplica()
    {
        await using var fixture = await BudgetFixture.CreateAsync();
        var budget = await fixture.CreateBudgetAsync(200_000);
        await fixture.AddBudgetItemAsync(budget.Id, fixture.ExpenseCategory.Id, 50_000);
        await fixture.Cards.CreatePurchaseAsync(new CardPurchaseRequest(fixture.Card.Id, fixture.Member.Id, fixture.ExpenseCategory.Id, "Supermercado", 50_000, 1, fixture.Today, null), null);
        fixture.Db.Transacoes.Add(new Transacao { MembroId = fixture.Member.Id, ContaId = fixture.Account.Id, CategoriaId = fixture.ExpenseCategory.Id, Tipo = TipoTransacao.DESPESA, Descricao = "Pagamento fatura", ValorCentavos = 50_000, DataCompetencia = fixture.Today, DataMovimentacao = fixture.Today, Status = StatusTransacao.EFETIVADA, Origem = OrigemTransacao.PAGAMENTO_FATURA });
        await fixture.Db.SaveChangesAsync();

        var summary = await fixture.Budgets.SummaryAsync(budget.Id);
        Assert.Equal(50_000, summary.TotalRealizadoCentavos);
    }

    [Fact]
    public async Task Transferencia_fica_fora_do_orcamento_e_categoria_pai_soma_as_filhas()
    {
        await using var fixture = await BudgetFixture.CreateAsync();
        var market = new Categoria { Nome = "Mercado", Tipo = TipoCategoria.Despesa, CategoriaPaiId = fixture.ExpenseCategory.Id };
        var restaurant = new Categoria { Nome = "Restaurante", Tipo = TipoCategoria.Despesa, CategoriaPaiId = fixture.ExpenseCategory.Id };
        fixture.Db.AddRange(market, restaurant);
        await fixture.Db.SaveChangesAsync();
        var budget = await fixture.CreateBudgetAsync(200_000);
        await fixture.AddBudgetItemAsync(budget.Id, fixture.ExpenseCategory.Id, 150_000);
        await fixture.AddExpenseAsync(50_000, market.Id);
        await fixture.AddExpenseAsync(30_000, restaurant.Id);
        fixture.Db.Transacoes.Add(new Transacao { MembroId = fixture.Member.Id, ContaId = fixture.Account.Id, CategoriaId = market.Id, Tipo = TipoTransacao.TRANSFERENCIA_SAIDA, Descricao = "Transferência", ValorCentavos = 100_000, DataCompetencia = fixture.Today, DataMovimentacao = fixture.Today, Status = StatusTransacao.EFETIVADA });
        await fixture.Db.SaveChangesAsync();

        var summary = await fixture.Budgets.SummaryAsync(budget.Id);
        var line = Assert.Single(summary.Linhas);
        Assert.Equal(80_000, line.RealizadoCentavos);
    }

    [Fact]
    public async Task Estouro_projetado_e_alerta_sao_identificados()
    {
        await using var fixture = await BudgetFixture.CreateAsync();
        var budget = await fixture.CreateBudgetAsync(200_000);
        await fixture.AddBudgetItemAsync(budget.Id, fixture.ExpenseCategory.Id, 80_000);
        await fixture.AddExpenseAsync(60_000, fixture.ExpenseCategory.Id);
        var recurrence = new Recorrencia { MembroId = fixture.Member.Id, ContaId = fixture.Account.Id, CategoriaId = fixture.ExpenseCategory.Id, Tipo = TipoTransacao.DESPESA, Descricao = "Conta futura", ValorCentavos = 30_000, Frequencia = FrequenciaRecorrencia.MENSAL, DataInicio = fixture.Today, ProximaOcorrencia = fixture.Today.AddDays(5), DiaReferencia = fixture.Today.Day, Status = StatusRecorrencia.ATIVA };
        fixture.Db.Recorrencias.Add(recurrence);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.OcorrenciasRecorrentes.Add(new OcorrenciaRecorrencia { RecorrenciaId = recurrence.Id, DataPrevista = fixture.Today.AddDays(5), ValorPrevistoCentavos = 30_000, Status = StatusOcorrenciaRecorrencia.PENDENTE });
        await fixture.Db.SaveChangesAsync();

        var projection = await fixture.Budgets.ProjectionAsync(budget.Id);
        Assert.Equal(90_000, projection.TotalProjetadoCentavos);
        Assert.Equal(10_000, projection.EstouroProjetadoCentavos);
        var keys = await fixture.Budgets.RefreshAlertsAsync();
        Assert.Contains(keys, key => key.StartsWith("ORCAMENTO_ESTOURO_PROJETADO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Pai_e_filha_nao_podem_ser_orcados_no_mesmo_ramo()
    {
        await using var fixture = await BudgetFixture.CreateAsync();
        var child = new Categoria { Nome = "Mercado", Tipo = TipoCategoria.Despesa, CategoriaPaiId = fixture.ExpenseCategory.Id };
        fixture.Db.Add(child);
        await fixture.Db.SaveChangesAsync();
        var budget = await fixture.CreateBudgetAsync(200_000);
        await fixture.AddBudgetItemAsync(budget.Id, fixture.ExpenseCategory.Id, 100_000);

        var exception = await Assert.ThrowsAsync<DomainException>(() => fixture.Budgets.AddItemAsync(budget.Id, new BudgetItemRequest(child.Id, null, 50_000, null), null));
        Assert.Contains("pai e uma filha", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BudgetFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private BudgetFixture(SqliteConnection connection, FinanceDbContext db)
        {
            this.connection = connection; Db = db; Today = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero); var audit = new AuditService(db); Transactions = new TransactionService(db, audit); Cards = new CardService(db, audit); Recurrences = new RecurrenceService(db, audit, Transactions, Cards); Budgets = new BudgetService(db, audit, Recurrences, () => Today);
        }
        public FinanceDbContext Db { get; }
        public TransactionService Transactions { get; }
        public CardService Cards { get; }
        public RecurrenceService Recurrences { get; }
        public BudgetService Budgets { get; }
        public DateTimeOffset Today { get; }
        public Membro Member { get; private set; } = null!;
        public Conta Account { get; private set; } = null!;
        public Categoria ExpenseCategory { get; private set; } = null!;
        public CardResponse Card { get; private set; } = null!;

        public static async Task<BudgetFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var db = new FinanceDbContext(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync(); var fixture = new BudgetFixture(connection, db);
            fixture.Member = new Membro { Nome = "Pablo" }; fixture.Account = new Conta { MembroId = fixture.Member.Id, Nome = "Conta", Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = 200_000 }; fixture.ExpenseCategory = new Categoria { Nome = "Alimentação", Tipo = TipoCategoria.Despesa }; db.AddRange(fixture.Member, fixture.Account, fixture.ExpenseCategory); await db.SaveChangesAsync(); fixture.Card = await fixture.Cards.CreateCardAsync(new CardRequest(fixture.Member.Id, "Nubank", "Mastercard", "1234", 300_000, 10, 20, fixture.Account.Id, null), null); return fixture;
        }
        public async Task<BudgetResponse> CreateBudgetAsync(long revenue) => await Budgets.CreateAsync(new BudgetRequest(2026, 9, null, "Setembro/2026", revenue, ModoReceitaPrevista.MANUAL, StatusOrcamento.ATIVO, null), null);
        public async Task AddBudgetItemAsync(Guid budgetId, Guid categoryId, long value) => await Budgets.AddItemAsync(budgetId, new BudgetItemRequest(categoryId, null, value, null), null);
        public async Task AddExpenseAsync(long value, Guid categoryId) { Db.Transacoes.Add(new Transacao { MembroId = Member.Id, ContaId = Account.Id, CategoriaId = categoryId, Tipo = TipoTransacao.DESPESA, Descricao = "Despesa", ValorCentavos = value, DataCompetencia = Today, DataMovimentacao = Today, Status = StatusTransacao.EFETIVADA }); await Db.SaveChangesAsync(); }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
