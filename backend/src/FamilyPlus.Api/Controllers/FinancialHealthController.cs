using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/saude-financeira"), ServiceFilter<RequireSessionFilter>]
public sealed class FinancialHealthController(SaudeFinanceiraService service) : ControllerBase
{
    [HttpGet("perfil")]
    public async Task<ActionResult<ApiResponse<FinancialHealthProfileResponse>>> Profile([FromQuery] Guid? membroId = null) =>
        Ok(ApiResponse<FinancialHealthProfileResponse>.Ok(await service.GetProfileAsync(membroId)));

    [HttpPut("perfil")]
    public async Task<ActionResult<ApiResponse<FinancialHealthProfileResponse>>> SaveProfile(FinancialHealthProfileRequest request) =>
        Ok(ApiResponse<FinancialHealthProfileResponse>.Ok(await service.SaveProfileAsync(request), "Perfil de saúde financeira salvo."));

    [HttpGet("indicadores")]
    public async Task<ActionResult<ApiResponse<FinancialHealthResponse>>> Indicators([FromQuery] Guid? membroId = null, [FromQuery] string? dataInicio = null, [FromQuery] string? dataFim = null)
    {
        var start = Parse(dataInicio) ?? new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 12, 0, 0, TimeSpan.Zero);
        var end = Parse(dataFim) ?? DateTimeOffset.UtcNow;
        if (end < start) return BadRequest(ApiResponse<FinancialHealthResponse>.Fail("O fim do período deve ser igual ou posterior ao início."));
        return Ok(ApiResponse<FinancialHealthResponse>.Ok(await service.CalculateAsync(membroId, start, end)));
    }

    private static DateTimeOffset? Parse(string? value) => DateTimeOffset.TryParse(value, out var parsed)
        ? new DateTimeOffset(parsed.Year, parsed.Month, parsed.Day, 12, 0, 0, TimeSpan.Zero)
        : null;
}
