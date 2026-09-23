using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/usuarios"), ServiceFilter<RequireSessionFilter>]
public sealed class UsersController(FamilyAdministrationService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<UserResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<UserResponse>>.Ok(await service.ListUsersAsync(Current().Id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<UserResponse>>> Create(UserRequest request) => Ok(ApiResponse<UserResponse>.Ok(await service.CreateUserAsync(request, Current().Id), "Acesso local criado."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<UserResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<UserResponse>.Ok(await service.SetUserStatusAsync(id, ativo, Current().Id), "Situação do usuário atualizada."));
    [HttpGet("{id:guid}/permissoes")] public async Task<ActionResult<ApiResponse<UserPermissionsResponse>>> Permissions(Guid id) => Ok(ApiResponse<UserPermissionsResponse>.Ok(await service.GetUserPermissionsAsync(id, Current().Id)));
    [HttpPut("{id:guid}/permissoes")] public async Task<ActionResult<ApiResponse<object>>> UpdatePermissions(Guid id, UserPermissionsUpdateRequest request) { await service.SetUserPermissionsAsync(id, request.Escopos, request.Perfil, Current().Id); return Ok(ApiResponse<object>.Ok(new { }, "Permissões atualizadas.")); }
    [HttpGet("/api/perfis")] public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> Profiles() => Ok(ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(await service.ListPermissionsAsync(Current().Id)));
    private SessionUser Current() => (SessionUser)HttpContext.Items["currentUser"]!;
}

public record UserPermissionsUpdateRequest(string Perfil, IReadOnlyList<ScopeRequest> Escopos);
