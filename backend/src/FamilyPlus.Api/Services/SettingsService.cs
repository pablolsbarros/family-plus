using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class SettingsService(FinanceDbContext db, FamilyAdministrationService family, FamilyAccessService access, DatabaseHealthService health, AuditService audit)
{
    public async Task<FamilySettingsResponse> GetAsync(Guid userId)
    {
        var user = await db.Usuarios.IgnoreQueryFilters().SingleAsync(x => x.Id == userId); db.SetFamilyContext(user.FamiliaId);
        var familyResponse = await family.GetFamilyAsync(user.FamiliaId!.Value);
        var config = await db.ConfiguracoesFamilia.SingleAsync(x => x.FamiliaId == user.FamiliaId);
        var preferences = await db.PreferenciasUsuarios.SingleAsync(x => x.UsuarioId == userId);
        var notifications = await db.ConfiguracoesNotificacao.SingleAsync(x => x.UsuarioId == userId);
        return new FamilySettingsResponse(familyResponse,
            new FamilyConfigurationResponse(config.FamiliaId, config.Moeda, config.Timezone, config.Locale, config.PrimeiroDiaSemana, config.InicioMesFinanceiro),
            new UserPreferencesResponse(preferences.UsuarioId, preferences.Idioma, preferences.FormatoData, preferences.PaginaInicial, preferences.ItensPorPagina, preferences.ConfirmarOperacoes, preferences.PeriodoPadraoDashboard, preferences.Tema),
            new NotificationSettingsResponse(notifications.UsuarioId, notifications.AvisarContasProximas, notifications.AntecedenciaDias, notifications.AvisarFatura, notifications.AvisarSaldoProjetadoNegativo, notifications.AvisarOrcamento));
    }

    public async Task<FamilySettingsResponse> UpdateFamilyAsync(Guid userId, FamilyRequest request)
    {
        await access.EnsurePermissionAsync(userId, "CONFIGURACAO_ADMINISTRAR"); var user = await db.Usuarios.IgnoreQueryFilters().SingleAsync(x => x.Id == userId); await family.UpdateFamilyAsync(user.FamiliaId!.Value, request, userId); return await GetAsync(userId);
    }

    public async Task<UserPreferencesResponse> UpdatePreferencesAsync(Guid userId, PreferenciaUsuario request)
    {
        var current = await db.PreferenciasUsuarios.SingleAsync(x => x.UsuarioId == userId); current.Idioma = request.Idioma; current.FormatoData = request.FormatoData; current.PaginaInicial = request.PaginaInicial; current.ItensPorPagina = Math.Clamp(request.ItensPorPagina, 5, 100); current.ConfirmarOperacoes = request.ConfirmarOperacoes; current.PeriodoPadraoDashboard = request.PeriodoPadraoDashboard; current.Tema = request.Tema; await audit.RecordAsync("PREFERENCIA_USUARIO", userId, OperacaoAuditoria.ALTERACAO_CONFIGURACAO, null, new { current.Idioma, current.PaginaInicial, current.Tema }, userId); await db.SaveChangesAsync(); return new UserPreferencesResponse(current.UsuarioId, current.Idioma, current.FormatoData, current.PaginaInicial, current.ItensPorPagina, current.ConfirmarOperacoes, current.PeriodoPadraoDashboard, current.Tema);
    }

    public async Task<NotificationSettingsResponse> UpdateNotificationsAsync(Guid userId, ConfiguracaoNotificacao request)
    {
        var current = await db.ConfiguracoesNotificacao.SingleAsync(x => x.UsuarioId == userId); current.AvisarContasProximas = request.AvisarContasProximas; current.AntecedenciaDias = Math.Clamp(request.AntecedenciaDias, 0, 60); current.AvisarFatura = request.AvisarFatura; current.AvisarSaldoProjetadoNegativo = request.AvisarSaldoProjetadoNegativo; current.AvisarOrcamento = request.AvisarOrcamento; await audit.RecordAsync("CONFIGURACAO_NOTIFICACAO", userId, OperacaoAuditoria.ALTERACAO_CONFIGURACAO, null, new { current.AvisarContasProximas, current.AvisarFatura, current.AvisarOrcamento }, userId); await db.SaveChangesAsync(); return new NotificationSettingsResponse(current.UsuarioId, current.AvisarContasProximas, current.AntecedenciaDias, current.AvisarFatura, current.AvisarSaldoProjetadoNegativo, current.AvisarOrcamento);
    }

    public async Task ChangePasswordAsync(Guid userId, SecurityPasswordRequest request)
    {
        if (request.NovaSenha.Length < 8) throw new DomainException("A nova senha deve ter pelo menos 8 caracteres.");
        var user = await db.Usuarios.IgnoreQueryFilters().SingleAsync(x => x.Id == userId); if (!PasswordService.Verify(request.SenhaAtual, user.SenhaHash)) throw new DomainException("A senha atual não confere."); user.SenhaHash = PasswordService.Hash(request.NovaSenha); await audit.RecordAsync("SEGURANCA", userId, OperacaoAuditoria.ALTERACAO_CONFIGURACAO, null, new { alterouSenha = true }, userId); await db.SaveChangesAsync();
    }

    public Task<SystemHealthResponse> HealthAsync() => health.CheckAsync();

    public async Task<IReadOnlyList<ConnectionResponse>> ConnectionsAsync() => await db.ConexoesFinanceiras.AsNoTracking().OrderBy(x => x.Instituicao).Select(x => new ConnectionResponse(x.Id, x.Instituicao, x.Tipo, x.Status, x.Configuracao)).ToListAsync();
    public async Task<ConnectionResponse> AddConnectionAsync(ConnectionRequest request, Guid userId) { await access.EnsurePermissionAsync(userId, "CONFIGURACAO_ADMINISTRAR"); var item = new ConexaoFinanceira { Instituicao = request.Instituicao.Trim(), Tipo = request.Tipo, Status = StatusConexaoFinanceira.INATIVA, Configuracao = request.Configuracao }; db.ConexoesFinanceiras.Add(item); await db.SaveChangesAsync(); return new ConnectionResponse(item.Id, item.Instituicao, item.Tipo, item.Status, item.Configuracao); }
}
