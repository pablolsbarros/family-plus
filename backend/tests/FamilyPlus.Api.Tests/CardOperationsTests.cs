using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class CardOperationsTests
{
    [Fact]
    public async Task Compra_simples_deve_comprometer_limite_sem_alterar_saldo_bancario()
    {
        await using var fixture = await CardFixture.CreateAsync();
        var seeded = await fixture.SeedAsync(500_000, 500_000);

        await fixture.Cards.CreatePurchaseAsync(new CardPurchaseRequest(seeded.Card.Id, seeded.Member.Id, seeded.Category.Id, "Compra simples", 100_000, 1, fixture.September4, null), null);

        var limit = await fixture.Cards.GetLimitAsync(seeded.Card.Id);
        var balance = await fixture.Transactions.BalanceAsync(seeded.Account.Id);
        Assert.Equal(100_000, limit.LimiteUtilizadoCentavos);
        Assert.Equal(400_000, limit.LimiteDisponivelCentavos);
        Assert.Equal(500_000, balance.SaldoAtualCentavos);
    }

    [Fact]
    public async Task Parcelamento_deve_preservar_todos_os_centavos_e_projetar_competencias()
    {
        await using var fixture = await CardFixture.CreateAsync();
        var seeded = await fixture.SeedAsync(500_000, 500_000);

        var purchase = await fixture.Cards.CreatePurchaseAsync(new CardPurchaseRequest(seeded.Card.Id, seeded.Member.Id, seeded.Category.Id, "Compra em três vezes", 10_000, 3, fixture.September4, null), null);

        Assert.Equal([3_333L, 3_333L, 3_334L], purchase.Parcelas.Select(x => x.ValorCentavos));
        Assert.Equal(10_000, purchase.Parcelas.Sum(x => x.ValorCentavos));
        Assert.Equal([9, 10, 11], purchase.Parcelas.Select(x => x.DataCompetencia.Month));
    }

    [Fact]
    public void Competencia_deve_mudar_apos_o_dia_de_fechamento()
    {
        var beforeClosing = InvoiceCycle.CompetenceFor(new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero), 5);
        var afterClosing = InvoiceCycle.CompetenceFor(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero), 5);

        Assert.Equal(9, beforeClosing.Month);
        Assert.Equal(10, afterClosing.Month);
    }

    [Fact]
    public async Task Pagamento_de_fatura_deve_reduzir_conta_sem_duplicar_consumo()
    {
        await using var fixture = await CardFixture.CreateAsync();
        var seeded = await fixture.SeedAsync(500_000, 500_000);
        var purchase = await fixture.Cards.CreatePurchaseAsync(new CardPurchaseRequest(seeded.Card.Id, seeded.Member.Id, seeded.Category.Id, "Mercado", 150_000, 1, fixture.September4, null), null);
        var invoiceId = purchase.Parcelas.Single().FaturaId;

        await fixture.Cards.CloseInvoiceAsync(invoiceId, null);
        await fixture.Cards.PayInvoiceAsync(invoiceId, new InvoicePaymentRequest(seeded.Account.Id, fixture.September12, 150_000), null);

        var balance = await fixture.Transactions.BalanceAsync(seeded.Account.Id);
        var summary = await fixture.Transactions.SummaryAsync("2026-09-01", "2026-09-30");
        var limit = await fixture.Cards.GetLimitAsync(seeded.Card.Id);
        Assert.Equal(350_000, balance.SaldoAtualCentavos);
        Assert.Equal(150_000, summary.DespesasCentavos);
        Assert.Equal(0, limit.LimiteUtilizadoCentavos);
    }

    private sealed class CardFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private CardFixture(SqliteConnection connection, FinanceDbContext db)
        {
            this.connection = connection; Db = db;
            var audit = new AuditService(db);
            Cards = new CardService(db, audit);
            Transactions = new TransactionService(db, audit);
        }
        public FinanceDbContext Db { get; }
        public CardService Cards { get; }
        public TransactionService Transactions { get; }
        public DateTimeOffset September4 => new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset September12 => new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

        public static async Task<CardFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new FinanceDbContext(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new CardFixture(connection, db);
        }

        public async Task<(Membro Member, Conta Account, Categoria Category, Cartao Card)> SeedAsync(long initialBalance, long limit)
        {
            var member = new Membro { Nome = "Pablo" };
            var account = new Conta { MembroId = member.Id, Nome = "Conta", Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = initialBalance };
            var category = new Categoria { Nome = "Mercado", Tipo = TipoCategoria.Despesa };
            var card = new Cartao { MembroId = member.Id, Nome = "Cartão", Bandeira = "Visa", UltimosDigitos = "1234", LimiteTotalCentavos = limit, DiaFechamento = 5, DiaVencimento = 12, ContaPagamentoPadraoId = account.Id };
            Db.AddRange(member, account, category, card);
            await Db.SaveChangesAsync();
            return (member, account, category, card);
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
