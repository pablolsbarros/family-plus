using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class MemberService(IRepository<Membro> repository)
{
    public async Task<IReadOnlyList<MemberResponse>> ListAsync() => await repository.Query().OrderBy(x => x.Nome).Select(Projection).ToListAsync();
    public async Task<MemberResponse> GetAsync(Guid id) => Map(await FindAsync(id));
    public async Task<MemberResponse> CreateAsync(MemberRequest request)
    {
        Validate(request);
        var member = new Membro { Nome = request.Nome.Trim(), Descricao = TrimOrNull(request.Descricao), Tipo = request.Tipo, DataNascimento = request.DataNascimento };
        await repository.AddAsync(member);
        await repository.SaveAsync();
        return Map(member);
    }
    public async Task<MemberResponse> UpdateAsync(Guid id, MemberRequest request)
    {
        Validate(request);
        var member = await FindAsync(id);
        member.Nome = request.Nome.Trim(); member.Descricao = TrimOrNull(request.Descricao); member.Tipo = request.Tipo; member.DataNascimento = request.DataNascimento;
        await repository.SaveAsync(); return Map(member);
    }
    public async Task<MemberResponse> SetStatusAsync(Guid id, bool ativo)
    {
        var member = await FindAsync(id); member.Ativo = ativo; await repository.SaveAsync(); return Map(member);
    }
    private async Task<Membro> FindAsync(Guid id) => await repository.GetAsync(id) ?? throw new DomainException("Membro não encontrado.");
    private static void Validate(MemberRequest request) { if (string.IsNullOrWhiteSpace(request.Nome)) throw new DomainException("Não foi possível cadastrar o membro.", "O nome do membro é obrigatório."); }
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static readonly System.Linq.Expressions.Expression<Func<Membro, MemberResponse>> Projection = entity => new(entity.Id, entity.Nome, entity.Descricao, entity.Tipo, entity.DataNascimento, entity.Ativo, entity.CriadoEm, false);
    private static MemberResponse Map(Membro entity) => new(entity.Id, entity.Nome, entity.Descricao, entity.Tipo, entity.DataNascimento, entity.Ativo, entity.CriadoEm);
}
