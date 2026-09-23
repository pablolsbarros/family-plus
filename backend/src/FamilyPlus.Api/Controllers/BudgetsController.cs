using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/orcamentos"), ServiceFilter<RequireSessionFilter>]
public sealed class BudgetsController(BudgetService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<BudgetResponse>>>> List([FromQuery] int? ano = null, [FromQuery] int? mes = null, [FromQuery] Guid? membroId = null, [FromQuery] StatusOrcamento? status = null) => Ok(ApiResponse<IReadOnlyList<BudgetResponse>>.Ok(await service.ListAsync(ano, mes, membroId, status)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Get(Guid id) => Ok(ApiResponse<BudgetResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Create(BudgetRequest request) => Ok(ApiResponse<BudgetResponse>.Ok(await service.CreateAsync(request, UserId()), "Orçamento criado."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Update(Guid id, BudgetRequest request) => Ok(ApiResponse<BudgetResponse>.Ok(await service.UpdateAsync(id, request, UserId()), "Orçamento atualizado."));
    [HttpPost("{id:guid}/ativar")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Activate(Guid id) => Ok(ApiResponse<BudgetResponse>.Ok(await service.ActivateAsync(id, UserId()), "Orçamento ativado."));
    [HttpPost("{id:guid}/encerrar")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Close(Guid id) => Ok(ApiResponse<BudgetResponse>.Ok(await service.CloseAsync(id, UserId()), "Orçamento encerrado e preservado no histórico."));
    [HttpPost("{id:guid}/reabrir")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Reopen(Guid id) => Ok(ApiResponse<BudgetResponse>.Ok(await service.ReopenAsync(id, UserId()), "Orçamento reaberto."));
    [HttpGet("{id:guid}/itens")] public async Task<ActionResult<ApiResponse<IReadOnlyList<BudgetItemResponse>>>> Items(Guid id) => Ok(ApiResponse<IReadOnlyList<BudgetItemResponse>>.Ok(await service.ListItemsAsync(id)));
    [HttpPost("{id:guid}/itens")] public async Task<ActionResult<ApiResponse<BudgetItemResponse>>> AddItem(Guid id, BudgetItemRequest request) => Ok(ApiResponse<BudgetItemResponse>.Ok(await service.AddItemAsync(id, request, UserId()), "Item de orçamento adicionado."));
    [HttpPut("{id:guid}/itens/{itemId:guid}")] public async Task<ActionResult<ApiResponse<BudgetItemResponse>>> UpdateItem(Guid id, Guid itemId, BudgetItemRequest request) => Ok(ApiResponse<BudgetItemResponse>.Ok(await service.UpdateItemAsync(id, itemId, request, UserId()), "Item de orçamento atualizado."));
    [HttpDelete("{id:guid}/itens/{itemId:guid}")] public async Task<ActionResult<ApiResponse<object>>> DeleteItem(Guid id, Guid itemId) { await service.DeleteItemAsync(id, itemId, UserId()); return Ok(ApiResponse<object>.Ok(new { }, "Item de orçamento removido.")); }
    [HttpPost("{id:guid}/copiar")] public async Task<ActionResult<ApiResponse<BudgetResponse>>> Copy(Guid id, BudgetCopyRequest request) => Ok(ApiResponse<BudgetResponse>.Ok(await service.CopyAsync(id, request, UserId()), "Orçamento copiado para o novo mês."));
    [HttpGet("{id:guid}/realizado")] public async Task<ActionResult<ApiResponse<BudgetSummaryResponse>>> Realized(Guid id) => Ok(ApiResponse<BudgetSummaryResponse>.Ok(await service.SummaryAsync(id)));
    [HttpGet("{id:guid}/comparativo")] public async Task<ActionResult<ApiResponse<BudgetComparisonResponse>>> Comparison(Guid id) => Ok(ApiResponse<BudgetComparisonResponse>.Ok(await service.ComparisonAsync(id)));
    [HttpGet("{id:guid}/projecao")] public async Task<ActionResult<ApiResponse<BudgetProjectionResponse>>> Projection(Guid id) => Ok(ApiResponse<BudgetProjectionResponse>.Ok(await service.ProjectionAsync(id)));
    [HttpGet("{id:guid}/resumo")] public async Task<ActionResult<ApiResponse<BudgetSummaryResponse>>> Summary(Guid id) => Ok(ApiResponse<BudgetSummaryResponse>.Ok(await service.SummaryAsync(id)));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
