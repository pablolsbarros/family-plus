using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/movimentacoes"), ServiceFilter<RequireSessionFilter>]
public sealed class SummaryController(TransactionService service) : ControllerBase
{
    [HttpGet("resumo")] public async Task<ActionResult<ApiResponse<FinanceSummary>>> Summary([FromQuery] string? dataInicio = null, [FromQuery] string? dataFim = null) => Ok(ApiResponse<FinanceSummary>.Ok(await service.SummaryAsync(dataInicio, dataFim)));
}
