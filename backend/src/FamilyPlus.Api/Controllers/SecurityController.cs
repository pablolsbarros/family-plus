using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/seguranca"), ServiceFilter<RequireSessionFilter>]
public sealed class SecurityController(SettingsService settings, AuthService auth) : ControllerBase
{
    [HttpPost("alterar-senha")] public async Task<ActionResult<ApiResponse<object>>> ChangePassword(SecurityPasswordRequest request) { await settings.ChangePasswordAsync(Current().Id, request); return Ok(ApiResponse<object>.Ok(new { }, "Senha alterada. As próximas sessões usarão a nova senha.")); }
    [HttpPost("bloquear")] public async Task<ActionResult<ApiResponse<object>>> Lock() { var token = Request.Headers["X-Session-Token"].FirstOrDefault(); if (!string.IsNullOrWhiteSpace(token)) await auth.LogoutAsync(token); return Ok(ApiResponse<object>.Ok(new { locked = true }, "Aplicação bloqueada. Faça login novamente para continuar.")); }
    [HttpPost("desbloquear")] public ActionResult<ApiResponse<object>> Unlock() => Ok(ApiResponse<object>.Ok(new { locked = false }, "A sessão atual já está desbloqueada."));
    private SessionUser Current() => (SessionUser)HttpContext.Items["currentUser"]!;
}
