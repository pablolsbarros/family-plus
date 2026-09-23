using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Tests;

public sealed class LedgerOperationsTests
{
    [Fact]
    public async Task Lancamento_manual_e_ajuste_compoem_saldo_sem_campo_mutavel()
    {
        await using var fixture = await Fixture.CreateAsync();
        var seed = await fixture.SeedAsync(100_000);
        await fixture.Ledger.CreateAsync(new LedgerRequest(TipoLancamento.RECEITA, NaturezaLancamento.RENDA, StatusLancamento.EFETIVADO, OrigemLancamento.MANUAL, null, "Salário", 50_000, fixture.Today, null, fixture.Today, seed.Account.Id, seed.Income.Id, seed.Member.Id, null, null), null);
        await fixture.Ledger.CreateAdjustmentAsync(new AdjustmentRequest(seed.Account.Id, seed.Member.Id, -10_000, fixture.Today, "Conferência bancária"), null);

        var balance = await fixture.Ledger.BalanceAsync(seed.Account.Id);

        Assert.Equal(140_000, balance.SaldoAtualCentavos);
        Assert.Equal(50_000, balance.EntradasCentavos);
        Assert.Equal(10_000, balance.SaidasCentavos);
        Assert.Equal(2, await fixture.Db.Lancamentos.CountAsync());
    }

    [Fact]
    public async Task Transferencia_cria_dois_lancamentos_de_transferencia_e_nao_altera_indicadores()
    {
        await using var fixture = await Fixture.CreateAsync();
        var seed = await fixture.SeedAsync(0);
        var destination = new Conta { Nome = "Reserva", MembroId = seed.Member.Id, Tipo = TipoConta.ContaPoupanca };
        fixture.Db.Contas.Add(destination); await fixture.Db.SaveChangesAsync();

        await fixture.Transfers.CreateAsync(new TransferRequest(seed.Account.Id, destination.Id, seed.Member.Id, 25_000, fixture.Today, "Reserva", StatusTransacao.EFETIVADA, null), null);

        var ledgerRows = await fixture.Db.Lancamentos.ToListAsync();
        var summary = await fixture.Ledger.SummaryAsync();
        Assert.Equal(2, ledgerRows.Count);
        Assert.All(ledgerRows, row => Assert.Equal(NaturezaLancamento.TRANSFERENCIA, row.Natureza));
        Assert.Equal(0, summary.ReceitasCentavos);
        Assert.Equal(0, summary.DespesasCentavos);
    }

    [Fact]
    public async Task Compra_e_pagamento_de_cartao_separam_consumo_de_saida_de_caixa()
    {
        await using var fixture = await Fixture.CreateAsync();
        var seed = await fixture.SeedAsync(200_000);
        var purchase = await fixture.Cards.CreatePurchaseAsync(new CardPurchaseRequest(seed.Card.Id, seed.Member.Id, seed.Expense.Id, "Mercado", 30_000, 1, fixture.Today, null), null);
        var invoice = purchase.Parcelas.Single().FaturaId;
        await fixture.Cards.CloseInvoiceAsync(invoice, null);
        await fixture.Cards.PayInvoiceAsync(invoice, new InvoicePaymentRequest(seed.Account.Id, fixture.Today, 30_000), null);

        var balance = await fixture.Ledger.BalanceAsync(seed.Account.Id);
        var summary = await fixture.Ledger.SummaryAsync();
        Assert.Equal(170_000, balance.SaldoAtualCentavos);
        Assert.Equal(30_000, summary.DespesasCentavos);
        Assert.Equal(1, await fixture.Db.Lancamentos.CountAsync(x => x.Natureza == NaturezaLancamento.CONSUMO));
        Assert.Equal(1, await fixture.Db.Lancamentos.CountAsync(x => x.Natureza == NaturezaLancamento.PAGAMENTO_FATURA));
    }

    [Fact]
    public async Task Ocorrencia_recorrente_tem_previsao_unica_e_efetivacao_no_mesmo_lancamento()
    {
        await using var fixture = await Fixture.CreateAsync();
        var seed = await fixture.SeedAsync(100_000);
        var recurrence = await fixture.Recurrences.CreateAsync(new RecurrenceRequest(seed.Member.Id, seed.Account.Id, seed.Expense.Id, TipoTransacao.DESPESA, "Internet", 12_000, FrequenciaRecorrencia.MENSAL, fixture.Today.AddDays(1), null, fixture.Today.Day, true, false, TipoRecorrencia.CONTA_FIXA, null), null);
        var occurrence = (await fixture.Recurrences.ListOccurrencesAsync(new OccurrenceQueryRequest(RecorrenciaId: recurrence.Id))).First();
        await fixture.Recurrences.GenerateAllAsync(fixture.Today);
        Assert.Equal(1, await fixture.Db.Lancamentos.CountAsync(x => x.OrigemTipo == OrigemLancamento.RECORRENCIA && x.OrigemId == occurrence.Id));

        await fixture.Recurrences.EffectAsync(occurrence.Id, new EffectOccurrenceRequest(13_000, occurrence.DataPrevista, null), null);
        Assert.Equal(1, await fixture.Db.Lancamentos.CountAsync(x => x.Id == occurrence.Id));
        Assert.Equal(StatusLancamento.EFETIVADO, await fixture.Db.Lancamentos.Where(x => x.Id == occurrence.Id).Select(x => x.Status).SingleAsync());
        Assert.Equal(13_000, await fixture.Db.Lancamentos.Where(x => x.Id == occurrence.Id).Select(x => x.ValorCentavos).SingleAsync());
    }

    private sealed class Fixture(SqliteConnection connection, FinanceDbContext db) : IAsyncDisposable
    {
        private readonly AuditService audit = new(db);
        public FinanceDbContext Db { get; } = db;
        public LedgerService Ledger { get; } = new(db, new AuditService(db));
        public TransactionService Transactions { get; } = new(db, new AuditService(db));
        public TransferService Transfers { get; } = new(db, new AuditService(db));
        public CardService Cards { get; } = new(db, new AuditService(db));
        public RecurrenceService Recurrences { get; } = new(db, new AuditService(db), new TransactionService(db, new AuditService(db)), new CardService(db, new AuditService(db)));
        public DateTimeOffset Today { get; } = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new FinanceDbContext(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }

        public async Task<(Membro Member, Conta Account, Categoria Income, Categoria Expense, Cartao Card)> SeedAsync(long initial)
        {
            var member = new Membro { Nome = "Pablo" }; var account = new Conta { Nome = "Conta", MembroId = member.Id, Tipo = TipoConta.ContaDigital, SaldoInicialCentavos = initial };
            var income = new Categoria { Nome = "Salário", Tipo = TipoCategoria.Receita }; var expense = new Categoria { Nome = "Mercado", Tipo = TipoCategoria.Despesa };
            var card = new Cartao { MembroId = member.Id, Nome = "Cartão", Bandeira = "Visa", UltimosDigitos = "1234", LimiteTotalCentavos = 100_000, DiaFechamento = 5, DiaVencimento = 12, ContaPagamentoPadraoId = account.Id };
            Db.AddRange(member, account, income, expense, card); await Db.SaveChangesAsync(); return (member, account, income, expense, card);
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
