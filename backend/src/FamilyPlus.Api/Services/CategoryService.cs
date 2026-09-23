using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class CategoryService(IRepository<Categoria> repository)
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync() => await repository.Query().Include(x => x.CategoriaPai).OrderBy(x => x.Tipo).ThenBy(x => x.Nome).Select(Projection).ToListAsync();
    public async Task<CategoryResponse> GetAsync(Guid id) => Map(await FindAsync(id));
    public async Task<CategoryResponse> CreateAsync(CategoryRequest request)
    {
        await ValidateAsync(request, null);
        var category = new Categoria { Nome = request.Nome.Trim(), Tipo = request.Tipo, CategoriaPaiId = request.CategoriaPaiId };
        await repository.AddAsync(category); await repository.SaveAsync(); return Map(category);
    }
    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request)
    {
        await ValidateAsync(request, id);
        var category = await FindAsync(id); category.Nome = request.Nome.Trim(); category.Tipo = request.Tipo; category.CategoriaPaiId = request.CategoriaPaiId;
        await repository.SaveAsync(); return Map(await FindAsync(id));
    }
    public async Task<CategoryResponse> SetStatusAsync(Guid id, bool ativo) { var category = await FindAsync(id); category.Ativo = ativo; await repository.SaveAsync(); return Map(category); }
    private async Task ValidateAsync(CategoryRequest request, Guid? editingId)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Nome)) errors.Add("O nome da categoria é obrigatório.");
        if (editingId.HasValue && request.CategoriaPaiId == editingId) errors.Add("Uma categoria não pode ser pai dela mesma.");
        if (errors.Count > 0) throw new DomainException("Não foi possível cadastrar a categoria.", [.. errors]);
        if (!request.CategoriaPaiId.HasValue) return;
        var parent = await repository.Query().SingleOrDefaultAsync(x => x.Id == request.CategoriaPaiId.Value) ?? throw new DomainException("A categoria pai informada não existe.");
        if (parent.Tipo != request.Tipo) throw new DomainException("A categoria pai deve possuir o mesmo tipo da categoria filha.");
        var cursor = parent;
        while (cursor.CategoriaPaiId.HasValue)
        {
            if (cursor.CategoriaPaiId == editingId) throw new DomainException("Não é permitido criar ciclos na hierarquia de categorias.");
            var next = await repository.GetAsync(cursor.CategoriaPaiId.Value);
            if (next is null) break;
            cursor = next;
        }
    }
    private async Task<Categoria> FindAsync(Guid id) => await repository.Query().Include(x => x.CategoriaPai).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Categoria não encontrada.");
    private static readonly System.Linq.Expressions.Expression<Func<Categoria, CategoryResponse>> Projection = x => new(x.Id, x.Nome, x.Tipo, x.CategoriaPaiId, x.CategoriaPai == null ? null : x.CategoriaPai.Nome, x.Ativo);
    private static CategoryResponse Map(Categoria x) => new(x.Id, x.Nome, x.Tipo, x.CategoriaPaiId, x.CategoriaPai?.Nome, x.Ativo);
}
