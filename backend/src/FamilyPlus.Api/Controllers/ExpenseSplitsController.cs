using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/transacoes/{transactionId:guid}/rateio"), ServiceFilter<RequireSessionFilter>]
public sealed class ExpenseSplitsController(ExpenseSplitService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<SplitResponse?>>> Get(Guid transactionId) => Ok(ApiResponse<SplitResponse?>.Ok(await service.GetAsync(transactionId)));
    [HttpPost, HttpPut] public async Task<ActionResult<ApiResponse<SplitResponse>>> Save(Guid transactionId, SplitRequest request) => Ok(ApiResponse<SplitResponse>.Ok(await service.SaveAsync(transactionId, request, Current().Id), "Rateio salvo sem duplicar a despesa original."));
    [HttpDelete] public async Task<ActionResult<ApiResponse<object>>> Delete(Guid transactionId) { await service.DeleteAsync(transactionId, Current().Id); return Ok(ApiResponse<object>.Ok(new { }, "Rateio removido.")); }
    private SessionUser Current() => (SessionUser)HttpContext.Items["currentUser"]!;
}
