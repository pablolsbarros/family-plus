using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class RecurrenceOperationsTests
{
    [Fact]
    public async Task Recorrencia_mensal_gera_horizonte_sem_duplicar_ocorrencias()
    {
        await using var fixture = await RecurrenceFixture.CreateAsync(); var seed = await fixture.SeedAsync();
        var start = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow).AddDays(2);
        var recurrence = await fixture.Recurrences.CreateAsync(fixture.ExpenseRequest(seed, "Internet", 12_000, start), null);
        var first = await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: recurrence.Id));
        await fixture.Recurrences.GenerateAllAsync(DateTimeOffset.UtcNow);
        var second = await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: recurrence.Id));

        Assert.True(first.Count >= 12);
        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first.Count, second.Select(x => x.DataPrevista.Date).Distinct().Count());
        Assert.All(second, x => Assert.Equal(12_000, x.ValorPrevistoCentavos));
    }

    [Fact]
    public async Task Alteracao_da_recorrencia_preserva_ocorrencia_ja_efetivada_e_recalcula_futuras()
    {
        await using var fixture = await RecurrenceFixture.CreateAsync(); var seed = await fixture.SeedAsync();
        var start = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow).AddDays(1);
        var created = await fixture.Recurrences.CreateAsync(fixture.ExpenseRequest(seed, "Energia", 12_000, start), null);
        var occurrences = await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: created.Id));
        var first = occurrences.OrderBy(x => x.DataPrevista).First();
        await fixture.Recurrences.EffectAsync(first.Id, new EffectOccurrenceRequest(12_000, first.DataPrevista, null), null);
        var update = fixture.ExpenseRequest(seed, "Energia", 14_000, start);
        await fixture.Recurrences.UpdateAsync(created.Id, update, null);
        var after = await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: created.Id));

        Assert.Equal(12_000, after.Single(x => x.Id == first.Id).ValorRealizadoCentavos);
        Assert.All(after.Where(x => x.Id != first.Id && x.Status != StatusOcorrenciaRecorrencia.EFETIVADA), x => Assert.Equal(14_000, x.ValorPrevistoCentavos));
    }

    [Fact]
    public async Task Previsao_nao_muda_saldo_e_efetivacao_cria_transacao_real()
    {
        await using var fixture = await RecurrenceFixture.CreateAsync(); var seed = await fixture.SeedAsync();
        var start = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow).AddDays(1);
        var recurrence = await fixture.Recurrences.CreateAsync(fixture.ExpenseRequest(seed, "Academia", 50_000, start), null);
        var balanceBefore = await fixture.Transactions.BalanceAsync(seed.Account.Id);
        var occurrence = (await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: recurrence.Id))).OrderBy(x => x.DataPrevista).First();
        var effected = await fixture.Recurrences.EffectAsync(occurrence.Id, new EffectOccurrenceRequest(54_000, occurrence.DataPrevista, "Valor de dezembro"), null);
        var balanceAfter = await fixture.Transactions.BalanceAsync(seed.Account.Id);

        Assert.Equal(200_000, balanceBefore.SaldoAtualCentavos);
        Assert.Equal(146_000, balanceAfter.SaldoAtualCentavos);
        Assert.Equal(StatusOcorrenciaRecorrencia.EFETIVADA, effected.Status);
        Assert.Equal(54_000, effected.ValorRealizadoCentavos);
        Assert.NotNull(effected.TransacaoId);
    }

    [Fact]
    public async Task Assinatura_anual_normaliza_custo_e_cobranca_no_cartao_gera_compra()
    {
        await using var fixture = await RecurrenceFixture.CreateAsync(); var seed = await fixture.SeedAsync();
        var today = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow);
        var subscription = await fixture.Recurrences.CreateSubscriptionAsync(new SubscriptionRequest(seed.Member.Id, "Microsoft 365", seed.ExpenseCategory.Id, seed.Account.Id, 60_000, PeriodicidadeAssinatura.ANUAL, today, today, MetodoPagamentoAssinatura.CARTAO, seed.Card.Id, null), null);
        var summary = await fixture.Recurrences.SubscriptionSummaryAsync();
        var purchases = await fixture.Cards.ListPurchasesAsync(seed.Card.Id);

        Assert.Equal(5_000, subscription.CustoMensalEquivalenteCentavos);
        Assert.Equal(60_000, subscription.CustoAnualEquivalenteCentavos);
        Assert.Equal(5_000, summary.CustoMensalCentavos);
        Assert.Equal(60_000, summary.CustoAnualCentavos);
        Assert.Contains(purchases, x => x.Descricao == "Microsoft 365");
    }

    private sealed class RecurrenceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private RecurrenceFixture(SqliteConnection connection, FinanceDbContext db)
        {
            this.connection = connection; Db = db;
            var audit = new AuditService(db); Transactions = new TransactionService(db, audit); Cards = new CardService(db, audit); Recurrences = new RecurrenceService(db, audit, Transactions, Cards);
        }
        public FinanceDbContext Db { get; }
        public TransactionService Transactions { get; }
        public CardService Cards { get; }
        public RecurrenceService Recurrences { get; }
        public static async Task<RecurrenceFixture> CreateAsync() { var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var db = new FinanceDbContext(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync(); return new RecurrenceFixture(connection, db); }
        public async Task<(Membro Member, Conta Account, Categoria ExpenseCategory, Categoria IncomeCategory, Cartao Card)> SeedAsync()
        {
            var member = new Membro { Nome = "Pablo" }; var account = new Conta { MembroId = member.Id, Nome = "Conta", Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = 200_000 };
            var expense = new Categoria { Nome = "Serviços", Tipo = TipoCategoria.Despesa }; var income = new Categoria { Nome = "Salário", Tipo = TipoCategoria.Receita };
            var card = new Cartao { MembroId = member.Id, Nome = "Cartão", Bandeira = "Visa", UltimosDigitos = "1234", LimiteTotalCentavos = 500_000, DiaFechamento = 5, DiaVencimento = 12, ContaPagamentoPadraoId = account.Id };
            Db.AddRange(member, account, expense, income, card); await Db.SaveChangesAsync(); return (member, account, expense, income, card);
        }
        public RecurrenceRequest ExpenseRequest((Membro Member, Conta Account, Categoria ExpenseCategory, Categoria IncomeCategory, Cartao Card) seed, string description, long cents, DateTimeOffset start) => new(seed.Member.Id, seed.Account.Id, seed.ExpenseCategory.Id, TipoTransacao.DESPESA, description, cents, FrequenciaRecorrencia.MENSAL, start, null, start.Day, true, false, TipoRecorrencia.CONTA_FIXA, null);
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
