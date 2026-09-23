using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/membros"), ServiceFilter<RequireSessionFilter>]
public sealed class MembersController(FamilyAdministrationService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<MemberResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<MemberResponse>>.Ok(await service.ListMembersAsync()));
    [HttpPost] public async Task<ActionResult<ApiResponse<MemberResponse>>> Create(MemberRequest request) => Ok(ApiResponse<MemberResponse>.Ok(await service.CreateMemberAsync(request, CurrentUserId()), "Membro cadastrado."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<MemberResponse>>> Update(Guid id, MemberRequest request) => Ok(ApiResponse<MemberResponse>.Ok(await service.UpdateMemberAsync(id, request, CurrentUserId()), "Membro atualizado."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<MemberResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<MemberResponse>.Ok(await service.SetMemberStatusAsync(id, ativo, CurrentUserId()), "Situação atualizada."));
    [HttpGet("{id:guid}/perfil-financeiro")] public async Task<ActionResult<ApiResponse<FinancialProfileResponse>>> Profile(Guid id) => Ok(ApiResponse<FinancialProfileResponse>.Ok((await service.FinancialProfilesAsync()).SingleOrDefault(x => x.MembroId == id) ?? throw new DomainException("Membro não encontrado.")));
    private Guid CurrentUserId() => ((SessionUser)HttpContext.Items["currentUser"]!).Id;
}
