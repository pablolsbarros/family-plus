using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/lancamentos"), ServiceFilter<RequireSessionFilter>]
public sealed class LedgerController(LedgerService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<LedgerPage>>> List([FromQuery] LedgerQueryRequest request) => Ok(ApiResponse<LedgerPage>.Ok(await service.ListAsync(request, UserId())));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Get(Guid id) => Ok(ApiResponse<LedgerResponse>.Ok(await service.GetAsync(id, UserId())));
    [HttpPost] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Create(LedgerRequest request) => Ok(ApiResponse<LedgerResponse>.Ok(await service.CreateAsync(request, UserId()), "Lançamento criado."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Update(Guid id, LedgerRequest request) => Ok(ApiResponse<LedgerResponse>.Ok(await service.UpdateAsync(id, request, UserId()), "Lançamento atualizado."));
    [HttpPost("{id:guid}/efetivar")] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Effect(Guid id, LedgerEffectRequest? request = null) => Ok(ApiResponse<LedgerResponse>.Ok(await service.EfetivarAsync(id, request?.ValorCentavos, request?.DataEfetivacao, UserId()), "Lançamento efetivado."));
    [HttpPost("{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Cancel(Guid id) => Ok(ApiResponse<LedgerResponse>.Ok(await service.CancelAsync(id, UserId()), "Lançamento cancelado."));
    [HttpPost("ajustes")] public async Task<ActionResult<ApiResponse<LedgerResponse>>> Adjustment(AdjustmentRequest request) => Ok(ApiResponse<LedgerResponse>.Ok(await service.CreateAdjustmentAsync(request, UserId()), "Ajuste registrado."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
