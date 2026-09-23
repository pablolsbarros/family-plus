using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/transacoes"), ServiceFilter<RequireSessionFilter>]
public sealed class TransactionsController(TransactionService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<TransactionPage>>> List([FromQuery] TransactionQueryRequest request) => Ok(ApiResponse<TransactionPage>.Ok(await service.ListAsync(request)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Get(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CreateAsync(request, CurrentUserId()), "Movimentação cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Update(Guid id, TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.UpdateAsync(id, request, CurrentUserId()), "Movimentação atualizada."));
    [HttpPatch("{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Cancel(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CancelAsync(id, CurrentUserId()), "Movimentação cancelada."));
    private Guid? CurrentUserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
