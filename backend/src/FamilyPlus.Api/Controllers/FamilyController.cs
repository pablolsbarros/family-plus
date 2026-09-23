using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/familia"), ServiceFilter<RequireSessionFilter>]
public sealed class FamilyController(FamilyAdministrationService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<FamilyResponse>>> Get() => Ok(ApiResponse<FamilyResponse>.Ok(await service.GetFamilyAsync(Current().FamiliaId)));
    [HttpPut] public async Task<ActionResult<ApiResponse<FamilyResponse>>> Update(FamilyRequest request) => Ok(ApiResponse<FamilyResponse>.Ok(await service.UpdateFamilyAsync(Current().FamiliaId, request, Current().Id), "Família atualizada."));
    [HttpGet("membros")] public async Task<ActionResult<ApiResponse<IReadOnlyList<MemberResponse>>>> Members() => Ok(ApiResponse<IReadOnlyList<MemberResponse>>.Ok(await service.ListMembersAsync()));
    [HttpGet("perfil-financeiro")] public async Task<ActionResult<ApiResponse<IReadOnlyList<FinancialProfileResponse>>>> Profiles() => Ok(ApiResponse<IReadOnlyList<FinancialProfileResponse>>.Ok(await service.FinancialProfilesAsync()));
    private SessionUser Current() => (SessionUser)HttpContext.Items["currentUser"]!;
}
