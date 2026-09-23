using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/alertas"), ServiceFilter<RequireSessionFilter>]
public sealed class AlertsController(AlertService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<AlertResponse>>>> List([FromQuery] AlertQueryRequest request) => Ok(ApiResponse<IReadOnlyList<AlertResponse>>.Ok(await service.ListAsync(request)));
    [HttpPost("{id:guid}/lido")] public async Task<ActionResult<ApiResponse<AlertResponse>>> MarkRead(Guid id) => Ok(ApiResponse<AlertResponse>.Ok(await service.MarkReadAsync(id), "Alerta marcado como lido."));
    [HttpPost("{id:guid}/resolver")] public async Task<ActionResult<ApiResponse<AlertResponse>>> Resolve(Guid id) => Ok(ApiResponse<AlertResponse>.Ok(await service.ResolveAsync(id), "Alerta resolvido."));
}
