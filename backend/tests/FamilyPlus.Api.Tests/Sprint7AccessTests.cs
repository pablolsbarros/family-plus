using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;

namespace FamilyPlus.Api.Tests;

public sealed class Sprint7AccessTests
{
    [Fact]
    public async Task Contexto_de_familia_isola_membros_de_outra_familia()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = CreateDb(connection); await db.Database.EnsureCreatedAsync();
        var familyA = new Familia { Nome = "A" }; var familyB = new Familia { Nome = "B" };
        db.Familias.AddRange(familyA, familyB); db.Membros.AddRange(new Membro { FamiliaId = familyA.Id, Nome = "Pablo" }, new Membro { FamiliaId = familyB.Id, Nome = "Luar" }); await db.SaveChangesAsync();
        db.SetFamilyContext(familyA.Id); Assert.Equal(["Pablo"], await db.Membros.Select(x => x.Nome).ToListAsync());
        db.SetFamilyContext(familyB.Id); Assert.Equal(["Luar"], await db.Membros.Select(x => x.Nome).ToListAsync());
    }

    [Fact]
    public async Task Rateio_percentual_fecha_exatamente_e_rejeita_total_invalido()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = CreateDb(connection); await db.Database.EnsureCreatedAsync();
        var family = new Familia { Nome = "Família" }; var pablo = new Membro { FamiliaId = family.Id, Nome = "Pablo" }; var luar = new Membro { FamiliaId = family.Id, Nome = "Luar" }; var account = new Conta { FamiliaId = family.Id, MembroId = pablo.Id, Nome = "Conta", Tipo = TipoConta.ContaCorrente }; var category = new Categoria { FamiliaId = family.Id, Nome = "Casa", Tipo = TipoCategoria.Despesa }; var user = new Usuario { FamiliaId = family.Id, MembroId = pablo.Id, Nome = "Pablo", Login = "pablo", SenhaHash = "hash" }; db.AddRange(family, pablo, luar, account, category, user); var transaction = new Transacao { FamiliaId = family.Id, MembroId = pablo.Id, ContaId = account.Id, CategoriaId = category.Id, Tipo = TipoTransacao.DESPESA, Descricao = "Energia", ValorCentavos = 100_000, DataCompetencia = DateTimeOffset.UtcNow, DataMovimentacao = DateTimeOffset.UtcNow, Status = StatusTransacao.EFETIVADA }; db.Transacoes.Add(transaction); await db.SaveChangesAsync();
        var access = new FamilyAccessService(db); await access.ProvisionAsync(user); var service = new ExpenseSplitService(db, access, new AuditService(db));
        var split = await service.SaveAsync(transaction.Id, new SplitRequest(TipoRateio.PERCENTUAL, [new SplitItemRequest(pablo.Id, 60), new SplitItemRequest(luar.Id, 40)]), user.Id); Assert.Equal(100_000, split.Itens.Sum(x => x.ValorCentavos)); Assert.Equal(60_000, split.Itens.Single(x => x.MembroId == pablo.Id).ValorCentavos);
        await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(transaction.Id, new SplitRequest(TipoRateio.PERCENTUAL, [new SplitItemRequest(pablo.Id, 60), new SplitItemRequest(luar.Id, 30)]), user.Id));
    }

    private static FinanceDbContext CreateDb(SqliteConnection connection) => new(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(connection).Options);
}
