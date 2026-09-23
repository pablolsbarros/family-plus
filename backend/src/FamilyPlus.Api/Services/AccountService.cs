using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class AccountService(IRepository<Conta> repository, FinanceDbContext db)
{
    public async Task<IReadOnlyList<AccountResponse>> ListAsync() => await repository.Query().Include(x => x.Membro).OrderBy(x => x.Nome).Select(Projection).ToListAsync();
    public async Task<AccountResponse> GetAsync(Guid id) => Map(await FindAsync(id));
    public async Task<AccountResponse> CreateAsync(AccountRequest request)
    {
        await ValidateAsync(request);
        var account = new Conta { MembroId = request.MembroId, Nome = request.Nome.Trim(), Tipo = request.Tipo, SaldoInicialCentavos = request.SaldoInicialCentavos, Observacao = TrimOrNull(request.Observacao) };
        await repository.AddAsync(account); await repository.SaveAsync();
        return Map(await FindAsync(account.Id));
    }
    public async Task<AccountResponse> UpdateAsync(Guid id, AccountRequest request)
    {
        await ValidateAsync(request);
        var account = await FindAsync(id);
        account.MembroId = request.MembroId; account.Nome = request.Nome.Trim(); account.Tipo = request.Tipo; account.SaldoInicialCentavos = request.SaldoInicialCentavos; account.Observacao = TrimOrNull(request.Observacao);
        await repository.SaveAsync(); return Map(await FindAsync(id));
    }
    public async Task<AccountResponse> SetStatusAsync(Guid id, bool ativo) { var account = await FindAsync(id); account.Ativo = ativo; await repository.SaveAsync(); return Map(account); }
    private async Task ValidateAsync(AccountRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Nome)) errors.Add("O nome da conta é obrigatório.");
        if (request.MembroId == Guid.Empty) errors.Add("O membro da conta é obrigatório.");
        if (errors.Count > 0) throw new DomainException("Não foi possível cadastrar a conta.", [.. errors]);
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId)) throw new DomainException("Não foi possível cadastrar a conta.", "O membro informado não existe.");
    }
    private async Task<Conta> FindAsync(Guid id) => await repository.Query().Include(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Conta não encontrada.");
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static readonly System.Linq.Expressions.Expression<Func<Conta, AccountResponse>> Projection = x => new(x.Id, x.MembroId, x.Membro!.Nome, x.Nome, x.Tipo, x.SaldoInicialCentavos, x.Observacao, x.Ativo);
    private static AccountResponse Map(Conta x) => new(x.Id, x.MembroId, x.Membro?.Nome ?? "", x.Nome, x.Tipo, x.SaldoInicialCentavos, x.Observacao, x.Ativo);
}
