using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/configuracoes"), ServiceFilter<RequireSessionFilter>]
public sealed class SettingsController(BackupService backupService, SettingsService service) : ControllerBase
{
    [HttpGet("sistema")]
    public async Task<ActionResult<ApiResponse<SystemHealthResponse>>> SystemInfo() => Ok(ApiResponse<SystemHealthResponse>.Ok(await service.HealthAsync()));

    [HttpGet("familia")] public async Task<ActionResult<ApiResponse<FamilySettingsResponse>>> Family() => Ok(ApiResponse<FamilySettingsResponse>.Ok(await service.GetAsync(Current().Id)));
    [HttpPut("familia")] public async Task<ActionResult<ApiResponse<FamilySettingsResponse>>> UpdateFamily(FamilyRequest request) => Ok(ApiResponse<FamilySettingsResponse>.Ok(await service.UpdateFamilyAsync(Current().Id, request)));
    [HttpGet("preferencias")] public async Task<ActionResult<ApiResponse<UserPreferencesResponse>>> Preferences() => Ok(ApiResponse<UserPreferencesResponse>.Ok((await service.GetAsync(Current().Id)).Preferencias));
    [HttpPut("preferencias")] public async Task<ActionResult<ApiResponse<UserPreferencesResponse>>> Preferences(PreferenciaUsuario request) => Ok(ApiResponse<UserPreferencesResponse>.Ok(await service.UpdatePreferencesAsync(Current().Id, request), "Preferências atualizadas."));
    [HttpGet("notificacoes")] public async Task<ActionResult<ApiResponse<NotificationSettingsResponse>>> Notifications() => Ok(ApiResponse<NotificationSettingsResponse>.Ok((await service.GetAsync(Current().Id)).Notificacoes));
    [HttpPut("notificacoes")] public async Task<ActionResult<ApiResponse<NotificationSettingsResponse>>> Notifications(ConfiguracaoNotificacao request) => Ok(ApiResponse<NotificationSettingsResponse>.Ok(await service.UpdateNotificationsAsync(Current().Id, request), "Notificações atualizadas."));
    [HttpGet("importacao-exportacao")] public ActionResult<ApiResponse<object>> ImportExport() => Ok(ApiResponse<object>.Ok(new { formatos = new[] { "CSV", "OFX" }, offline = true, infraestrutura = new[] { "IMPORTACAO", "IMPORTACAO_ARQUIVO", "IMPORTACAO_ITEM" } }));
    [HttpGet("conexoes-financeiras")] public async Task<ActionResult<ApiResponse<IReadOnlyList<ConnectionResponse>>>> Connections() => Ok(ApiResponse<IReadOnlyList<ConnectionResponse>>.Ok(await service.ConnectionsAsync()));
    [HttpPost("conexoes-financeiras")] public async Task<ActionResult<ApiResponse<ConnectionResponse>>> AddConnection(ConnectionRequest request) => Ok(ApiResponse<ConnectionResponse>.Ok(await service.AddConnectionAsync(request, Current().Id), "Estrutura de integração criada."));

    [HttpPost("backup")]
    public async Task<ActionResult<ApiResponse<BackupResponse>>> Backup() => Ok(ApiResponse<BackupResponse>.Ok(await backupService.CreateAsync(Current().Id), "Backup local criado."));
    [HttpGet("backup/historico")] public async Task<ActionResult<ApiResponse<IReadOnlyList<BackupResponse>>>> BackupHistory() => Ok(ApiResponse<IReadOnlyList<BackupResponse>>.Ok(await backupService.HistoryAsync()));
    [HttpPost("backup/restaurar")] public async Task<ActionResult<ApiResponse<object>>> Restore(RestoreRequest request) { await backupService.RestoreAsync(request.NomeArquivo, Current().Id); return Ok(ApiResponse<object>.Ok(new { }, "Backup restaurado. Reinicie a aplicação local para recarregar a conexão SQLite.")); }
    private SessionUser Current() => (SessionUser)HttpContext.Items["currentUser"]!;
}
