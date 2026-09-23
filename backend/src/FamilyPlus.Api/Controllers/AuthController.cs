using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpGet("setup-status")]
    public async Task<ActionResult<ApiResponse<object>>> SetupStatus() => Ok(ApiResponse<object>.Ok(new { configured = await authService.HasAdministratorAsync() }));

    [HttpPost("setup")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Setup(SetupRequest request) => Ok(ApiResponse<AuthResult>.Ok(await authService.SetupAsync(request), "Administrador local criado."));

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResult>>> Login(LoginRequest request) => Ok(ApiResponse<AuthResult>.Ok(await authService.LoginAsync(request), "Login efetuado."));

    [HttpPost("logout")]
    [ServiceFilter<RequireSessionFilter>]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var token = Request.Headers["X-Session-Token"].FirstOrDefault()!;
        await authService.LogoutAsync(token);
        return Ok(ApiResponse<object>.Ok(new { }, "Sessão encerrada."));
    }

    [HttpGet("me")]
    [ServiceFilter<RequireSessionFilter>]
    public ActionResult<ApiResponse<SessionUser>> Me() => Ok(ApiResponse<SessionUser>.Ok((SessionUser)HttpContext.Items["currentUser"]!));
}
