using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/transferencias"), ServiceFilter<RequireSessionFilter>]
public sealed class TransfersController(TransferService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<TransferResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<TransferResponse>>.Ok(await service.ListAsync()));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<TransferResponse>>> Get(Guid id) => Ok(ApiResponse<TransferResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<TransferResponse>>> Create(TransferRequest request) => Ok(ApiResponse<TransferResponse>.Ok(await service.CreateAsync(request, CurrentUserId()), "Transferência cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<TransferResponse>>> Update(Guid id, TransferRequest request) => Ok(ApiResponse<TransferResponse>.Ok(await service.UpdateAsync(id, request, CurrentUserId()), "Transferência atualizada."));
    [HttpPatch("{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<TransferResponse>>> Cancel(Guid id) => Ok(ApiResponse<TransferResponse>.Ok(await service.CancelAsync(id, CurrentUserId()), "Transferência cancelada."));
    private Guid? CurrentUserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
