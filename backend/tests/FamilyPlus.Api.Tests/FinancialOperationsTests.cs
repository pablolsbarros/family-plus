using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class FinancialOperationsTests
{
    [Fact]
    public async Task Receita_e_despesa_efetivadas_devem_compor_o_saldo_derivado()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var (member, account, incomeCategory, expenseCategory) = await fixture.SeedMemberAccountAndCategoriesAsync(100_000);

        await fixture.Transactions.CreateAsync(new TransactionRequest(
            TipoTransacao.RECEITA, "Salário", 200_000, fixture.Today, fixture.Today,
            account.Id, incomeCategory.Id, member.Id, StatusTransacao.EFETIVADA, null), null);
        await fixture.Transactions.CreateAsync(new TransactionRequest(
            TipoTransacao.DESPESA, "Mercado", 50_000, fixture.Today, fixture.Today,
            account.Id, expenseCategory.Id, member.Id, StatusTransacao.EFETIVADA, null), null);

        var balance = await fixture.Transactions.BalanceAsync(account.Id);

        Assert.Equal(100_000, balance.SaldoInicialCentavos);
        Assert.Equal(200_000, balance.EntradasCentavos);
        Assert.Equal(50_000, balance.SaidasCentavos);
        Assert.Equal(250_000, balance.SaldoAtualCentavos);
    }

    [Fact]
    public async Task Transferencia_efetivada_deve_movimentar_duas_contas_sem_alterar_o_total()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var member = new Membro { Nome = "Pablo" };
        var origin = new Conta { Nome = "Nubank", MembroId = member.Id, Tipo = TipoConta.ContaDigital, SaldoInicialCentavos = 250_000 };
        var destination = new Conta { Nome = "Itaú", MembroId = member.Id, Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = 100_000 };
        fixture.Db.AddRange(member, origin, destination);
        await fixture.Db.SaveChangesAsync();

        var transfer = await fixture.Transfers.CreateAsync(new TransferRequest(
            origin.Id, destination.Id, member.Id, 50_000, fixture.Today, "Reserva", StatusTransacao.EFETIVADA, null), null);

        var originBalance = await fixture.Transactions.BalanceAsync(origin.Id);
        var destinationBalance = await fixture.Transactions.BalanceAsync(destination.Id);

        Assert.Equal(200_000, originBalance.SaldoAtualCentavos);
        Assert.Equal(150_000, destinationBalance.SaldoAtualCentavos);
        Assert.Equal(350_000, originBalance.SaldoAtualCentavos + destinationBalance.SaldoAtualCentavos);
        Assert.Equal(2, transfer.TransacaoIds.Count);

        await fixture.Transfers.CancelAsync(transfer.Id, null);
        originBalance = await fixture.Transactions.BalanceAsync(origin.Id);
        destinationBalance = await fixture.Transactions.BalanceAsync(destination.Id);

        Assert.Equal(250_000, originBalance.SaldoAtualCentavos);
        Assert.Equal(100_000, destinationBalance.SaldoAtualCentavos);
    }

    [Fact]
    public async Task Despesa_prevista_so_deve_afetar_saldo_quando_efetivada()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var (member, account, _, expenseCategory) = await fixture.SeedMemberAccountAndCategoriesAsync(100_000);
        var request = new TransactionRequest(
            TipoTransacao.DESPESA, "Conta de luz", 30_000, fixture.Today, fixture.Today,
            account.Id, expenseCategory.Id, member.Id, StatusTransacao.PREVISTA, null);

        var planned = await fixture.Transactions.CreateAsync(request, null);
        var before = await fixture.Transactions.BalanceAsync(account.Id);
        Assert.Equal(100_000, before.SaldoAtualCentavos);

        await fixture.Transactions.UpdateAsync(planned.Id, request with { Status = StatusTransacao.EFETIVADA }, null);
        var after = await fixture.Transactions.BalanceAsync(account.Id);

        Assert.Equal(70_000, after.SaldoAtualCentavos);
    }

    [Fact]
    public async Task Edicao_de_movimentacao_efetivada_deve_recalcular_o_saldo()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var (member, account, _, expenseCategory) = await fixture.SeedMemberAccountAndCategoriesAsync(100_000);
        var request = new TransactionRequest(
            TipoTransacao.DESPESA, "Mercado", 10_000, fixture.Today, fixture.Today,
            account.Id, expenseCategory.Id, member.Id, StatusTransacao.EFETIVADA, null);

        var expense = await fixture.Transactions.CreateAsync(request, null);
        await fixture.Transactions.UpdateAsync(expense.Id, request with { ValorCentavos = 15_000 }, null);

        var balance = await fixture.Transactions.BalanceAsync(account.Id);
        Assert.Equal(85_000, balance.SaldoAtualCentavos);
    }

    [Fact]
    public async Task Categoria_incompativel_deve_ser_rejeitada()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var (member, account, incomeCategory, _) = await fixture.SeedMemberAccountAndCategoriesAsync(0);

        var exception = await Assert.ThrowsAsync<DomainException>(() => fixture.Transactions.CreateAsync(new TransactionRequest(
            TipoTransacao.DESPESA, "Tentativa inválida", 1_000, fixture.Today, fixture.Today,
            account.Id, incomeCategory.Id, member.Id, StatusTransacao.EFETIVADA, null), null));

        Assert.Contains(exception.Errors, error => error.Contains("não pode ser utilizada em uma despesa", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Rota_tipificada_nao_deve_permitir_acesso_a_transacao_de_outro_tipo()
    {
        await using var fixture = await FinancialFixture.CreateAsync();
        var (member, account, _, expenseCategory) = await fixture.SeedMemberAccountAndCategoriesAsync(0);
        var expense = await fixture.Transactions.CreateAsync(new TransactionRequest(
            TipoTransacao.DESPESA, "Despesa", 1_000, fixture.Today, fixture.Today,
            account.Id, expenseCategory.Id, member.Id, StatusTransacao.EFETIVADA, null), null);

        var exception = await Assert.ThrowsAsync<DomainException>(() => fixture.Transactions.GetAsync(expense.Id, TipoTransacao.RECEITA));

        Assert.Equal("Receita não encontrada.", exception.Message);
    }

    private sealed class FinancialFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private FinancialFixture(SqliteConnection connection, FinanceDbContext db)
        {
            this.connection = connection;
            Db = db;
            var audit = new AuditService(db);
            Transactions = new TransactionService(db, audit);
            Transfers = new TransferService(db, audit);
            Today = new DateTimeOffset(2026, 9, 13, 0, 0, 0, TimeSpan.Zero);
        }

        public FinanceDbContext Db { get; }
        public TransactionService Transactions { get; }
        public TransferService Transfers { get; }
        public DateTimeOffset Today { get; }

        public static async Task<FinancialFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options;
            var db = new FinanceDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new FinancialFixture(connection, db);
        }

        public async Task<(Membro Member, Conta Account, Categoria IncomeCategory, Categoria ExpenseCategory)> SeedMemberAccountAndCategoriesAsync(long initialBalance)
        {
            var member = new Membro { Nome = "Pablo" };
            var account = new Conta { Nome = "Conta principal", MembroId = member.Id, Tipo = TipoConta.ContaCorrente, SaldoInicialCentavos = initialBalance };
            var income = new Categoria { Nome = "Salário", Tipo = TipoCategoria.Receita };
            var expense = new Categoria { Nome = "Alimentação", Tipo = TipoCategoria.Despesa };
            Db.AddRange(member, account, income, expense);
            await Db.SaveChangesAsync();
            return (member, account, income, expense);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
