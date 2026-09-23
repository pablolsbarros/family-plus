using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/categorias"), ServiceFilter<RequireSessionFilter>]
public sealed class CategoriesController(CategoryService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<CategoryResponse>>.Ok(await service.ListAsync()));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<CategoryResponse>>> Get(Guid id) => Ok(ApiResponse<CategoryResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(CategoryRequest request) => Ok(ApiResponse<CategoryResponse>.Ok(await service.CreateAsync(request), "Categoria cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(Guid id, CategoryRequest request) => Ok(ApiResponse<CategoryResponse>.Ok(await service.UpdateAsync(id, request), "Categoria atualizada."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<CategoryResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<CategoryResponse>.Ok(await service.SetStatusAsync(id, ativo), "Situação atualizada."));
}
