using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FamilyPlus.Api.Filters;

public sealed class RequireSessionFilter(AuthService authService, FinanceDbContext db, FamilyAccessService access) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var token = context.HttpContext.Request.Headers["X-Session-Token"].FirstOrDefault();
        var user = await authService.GetByTokenAsync(token);
        if (user is null)
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("Sessão local inválida ou expirada.", "Faça login novamente."));
            return;
        }
        context.HttpContext.Items["currentUser"] = user;
        db.SetFamilyContext(user.FamiliaId);
        if (user.Perfil == nameof(PerfilAcessoNome.SOMENTE_LEITURA) && context.HttpContext.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("O perfil somente leitura não pode alterar dados.")) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
        var permission = RequiredPermission(context.HttpContext.Request.Path, context.HttpContext.Request.Method);
        if (permission is not null && !await access.HasPermissionAsync(user.Id, permission))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Fail("Seu perfil não possui a permissão necessária para esta operação.")) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
        await next();
    }

    private static string? RequiredPermission(PathString path, string method)
    {
        var value = path.Value ?? string.Empty;
        var isWrite = method is "POST" or "PUT" or "PATCH" or "DELETE";
        if (!isWrite)
        {
            if (value.StartsWith("/api/transacoes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/lancamentos", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/transferencias", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/contas", StringComparison.OrdinalIgnoreCase)) return "MOVIMENTACAO_VISUALIZAR";
            if (value.StartsWith("/api/cartoes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/compras-cartao", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/faturas", StringComparison.OrdinalIgnoreCase)) return "CARTAO_VISUALIZAR";
            if (value.StartsWith("/api/recorrencias", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/ocorrencias-recorrentes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/assinaturas", StringComparison.OrdinalIgnoreCase)) return "RECORRENCIA_VISUALIZAR";
            return null;
        }
        if (value.StartsWith("/api/transacoes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/lancamentos", StringComparison.OrdinalIgnoreCase)) return value.Contains("/cancelar", StringComparison.OrdinalIgnoreCase) ? "MOVIMENTACAO_CANCELAR" : value.Contains("/efetivar", StringComparison.OrdinalIgnoreCase) ? "MOVIMENTACAO_EDITAR" : method == "POST" ? "MOVIMENTACAO_CRIAR" : "MOVIMENTACAO_EDITAR";
        if (value.StartsWith("/api/transferencias", StringComparison.OrdinalIgnoreCase)) return "TRANSFERENCIA_ADMINISTRAR";
        if (value.StartsWith("/api/cartoes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/compras-cartao", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/faturas", StringComparison.OrdinalIgnoreCase)) return method == "POST" ? "CARTAO_CRIAR" : "CARTAO_ADMINISTRAR";
        if (value.StartsWith("/api/recorrencias", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/ocorrencias-recorrentes", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/assinaturas", StringComparison.OrdinalIgnoreCase)) return "RECORRENCIA_ADMINISTRAR";
        if (value.StartsWith("/api/orcamentos", StringComparison.OrdinalIgnoreCase)) return "ORCAMENTO_EDITAR";
        if (value.StartsWith("/api/seguranca", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/configuracoes/preferencias", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/api/configuracoes/notificacoes", StringComparison.OrdinalIgnoreCase)) return null;
        if (value.StartsWith("/api/configuracoes/backup", StringComparison.OrdinalIgnoreCase)) return "BACKUP_RESTAURAR";
        if (value.StartsWith("/api/configuracoes", StringComparison.OrdinalIgnoreCase)) return "CONFIGURACAO_ADMINISTRAR";
        if (value.StartsWith("/api/membros", StringComparison.OrdinalIgnoreCase)) return "FAMILIA_ADMINISTRAR";
        if (value.StartsWith("/api/usuarios", StringComparison.OrdinalIgnoreCase)) return "USUARIO_ADMINISTRAR";
        return null;
    }
}
