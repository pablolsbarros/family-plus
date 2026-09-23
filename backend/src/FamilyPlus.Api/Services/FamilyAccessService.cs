using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class FamilyAccessService(FinanceDbContext db)
{
    public static readonly IReadOnlyList<(string Code, string Module, string Description)> PermissionCatalog =
    [
        ("FAMILIA_VISUALIZAR", "familia", "Visualizar a família"),
        ("FAMILIA_ADMINISTRAR", "familia", "Administrar família e membros"),
        ("USUARIO_ADMINISTRAR", "familia", "Administrar usuários e acessos"),
        ("PERMISSAO_ADMINISTRAR", "familia", "Alterar permissões"),
        ("MOVIMENTACAO_VISUALIZAR", "movimentacao", "Visualizar movimentações"),
        ("MOVIMENTACAO_CRIAR", "movimentacao", "Criar movimentações"),
        ("MOVIMENTACAO_EDITAR", "movimentacao", "Editar movimentações"),
        ("MOVIMENTACAO_CANCELAR", "movimentacao", "Cancelar movimentações"),
        ("TRANSFERENCIA_ADMINISTRAR", "movimentacao", "Administrar transferências"),
        ("CARTAO_VISUALIZAR", "cartao", "Visualizar cartões"),
        ("CARTAO_CRIAR", "cartao", "Criar cartões"),
        ("CARTAO_EDITAR", "cartao", "Editar cartões"),
        ("CARTAO_ADMINISTRAR", "cartao", "Administrar cartões e faturas"),
        ("RECORRENCIA_VISUALIZAR", "recorrencia", "Visualizar recorrências"),
        ("RECORRENCIA_ADMINISTRAR", "recorrencia", "Administrar recorrências"),
        ("ORCAMENTO_VISUALIZAR", "orcamento", "Visualizar orçamentos"),
        ("ORCAMENTO_EDITAR", "orcamento", "Editar orçamentos"),
        ("CONFIGURACAO_VISUALIZAR", "configuracao", "Visualizar configurações"),
        ("CONFIGURACAO_ADMINISTRAR", "configuracao", "Alterar configurações"),
        ("BACKUP_RESTAURAR", "configuracao", "Criar e restaurar backups")
    ];

    public async Task<Usuario> ProvisionAsync(Usuario user)
    {
        var family = user.FamiliaId.HasValue
            ? await db.Familias.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == user.FamiliaId.Value)
            : null;
        if (family is null)
        {
            family = new Familia { Nome = "Minha Família" };
            db.Familias.Add(family);
            user.FamiliaId = family.Id;
        }

        db.SetFamilyContext(family.Id);
        var member = user.MembroId.HasValue
            ? await db.Membros.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == user.MembroId.Value)
            : await db.Membros.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.FamiliaId == family.Id && x.Tipo == TipoMembro.TITULAR);
        if (member is null)
        {
            member = new Membro { FamiliaId = family.Id, Nome = user.Nome, Tipo = TipoMembro.TITULAR };
            db.Membros.Add(member);
            user.MembroId = member.Id;
        }
        else if (user.MembroId != member.Id) user.MembroId = member.Id;

        await EnsureCatalogAsync(family.Id);
        if (!await db.ConfiguracoesFamilia.AnyAsync(x => x.FamiliaId == family.Id)) db.ConfiguracoesFamilia.Add(new ConfiguracaoFamilia { FamiliaId = family.Id, Moeda = family.MoedaPadrao, Timezone = family.Timezone });
        if (!await db.PreferenciasUsuarios.AnyAsync(x => x.UsuarioId == user.Id)) db.PreferenciasUsuarios.Add(new PreferenciaUsuario { UsuarioId = user.Id });
        if (!await db.ConfiguracoesNotificacao.AnyAsync(x => x.UsuarioId == user.Id)) db.ConfiguracoesNotificacao.Add(new ConfiguracaoNotificacao { UsuarioId = user.Id });

        var admin = await db.PerfisAcesso.SingleAsync(x => x.Nome == PerfilAcessoNome.ADMINISTRADOR.ToString());
        if (!await db.UsuariosPerfis.AnyAsync(x => x.UsuarioId == user.Id)) db.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = user.Id, PerfilAcessoId = admin.Id });

        await AssociateLegacyDataAsync(family.Id);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<SessionUser> CreateSessionUserAsync(Usuario user)
    {
        if (!user.FamiliaId.HasValue) user = await ProvisionAsync(user);
        db.SetFamilyContext(user.FamiliaId);
        var profiles = await db.UsuariosPerfis.Include(x => x.PerfilAcesso).Where(x => x.UsuarioId == user.Id).Select(x => x.PerfilAcesso!.Nome).ToListAsync();
        var permissions = await db.UsuariosPerfis.Where(x => x.UsuarioId == user.Id).SelectMany(x => x.PerfilAcesso!.Permissoes).Select(x => x.Permissao!.Codigo).Distinct().ToListAsync();
        return new SessionUser(user.Id, user.Nome, user.Login, user.FamiliaId!.Value, user.MembroId, profiles.FirstOrDefault() ?? PerfilAcessoNome.MEMBRO.ToString(), permissions);
    }

    public async Task EnsurePermissionAsync(Guid userId, string permission)
    {
        var allowed = await db.UsuariosPerfis.Where(x => x.UsuarioId == userId).SelectMany(x => x.PerfilAcesso!.Permissoes).AnyAsync(x => x.Permissao!.Codigo == permission);
        if (!allowed) throw new ForbiddenException("Seu perfil não possui a permissão necessária para esta operação.");
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission) => await db.UsuariosPerfis.Where(x => x.UsuarioId == userId).SelectMany(x => x.PerfilAcesso!.Permissoes).AnyAsync(x => x.Permissao!.Codigo == permission);

    public async Task<bool> IsAdministratorAsync(Guid userId) => await db.UsuariosPerfis.Where(x => x.UsuarioId == userId).AnyAsync(x => x.PerfilAcesso!.Nome == PerfilAcessoNome.ADMINISTRADOR.ToString());

    private async Task EnsureCatalogAsync(Guid familyId)
    {
        foreach (var item in PermissionCatalog)
        {
            if (!await db.Permissoes.AnyAsync(x => x.Codigo == item.Code)) db.Permissoes.Add(new Permissao { FamiliaId = familyId, Codigo = item.Code, Modulo = item.Module, Descricao = item.Description });
        }
        foreach (var name in Enum.GetNames<PerfilAcessoNome>())
        {
            if (!await db.PerfisAcesso.AnyAsync(x => x.Nome == name)) db.PerfisAcesso.Add(new PerfilAcesso { FamiliaId = familyId, Nome = name });
        }
        await db.SaveChangesAsync();
        var permissions = await db.Permissoes.ToListAsync();
        var profiles = await db.PerfisAcesso.ToListAsync();
        var admin = profiles.Single(x => x.Nome == PerfilAcessoNome.ADMINISTRADOR.ToString());
        var member = profiles.Single(x => x.Nome == PerfilAcessoNome.MEMBRO.ToString());
        var readOnly = profiles.Single(x => x.Nome == PerfilAcessoNome.SOMENTE_LEITURA.ToString());
        foreach (var permission in permissions)
        {
            if (!await db.PerfisPermissoes.AnyAsync(x => x.PerfilAcessoId == admin.Id && x.PermissaoId == permission.Id)) db.PerfisPermissoes.Add(new PerfilPermissao { PerfilAcessoId = admin.Id, PermissaoId = permission.Id });
            var memberAllowed = permission.Codigo.EndsWith("_VISUALIZAR", StringComparison.Ordinal) || permission.Codigo is "MOVIMENTACAO_CRIAR" or "MOVIMENTACAO_EDITAR" or "MOVIMENTACAO_CANCELAR" or "TRANSFERENCIA_ADMINISTRAR" or "CARTAO_CRIAR" or "CARTAO_EDITAR" or "CARTAO_ADMINISTRAR" or "RECORRENCIA_ADMINISTRAR" or "ORCAMENTO_EDITAR";
            if (memberAllowed && !await db.PerfisPermissoes.AnyAsync(x => x.PerfilAcessoId == member.Id && x.PermissaoId == permission.Id)) db.PerfisPermissoes.Add(new PerfilPermissao { PerfilAcessoId = member.Id, PermissaoId = permission.Id });
            if (permission.Codigo.EndsWith("_VISUALIZAR", StringComparison.Ordinal) && !await db.PerfisPermissoes.AnyAsync(x => x.PerfilAcessoId == readOnly.Id && x.PermissaoId == permission.Id)) db.PerfisPermissoes.Add(new PerfilPermissao { PerfilAcessoId = readOnly.Id, PermissaoId = permission.Id });
        }
    }

    private async Task AssociateLegacyDataAsync(Guid familyId)
    {
        await AssociateLegacyDataAsync<Conta>(familyId); await AssociateLegacyDataAsync<Categoria>(familyId); await AssociateLegacyDataAsync<Lancamento>(familyId); await AssociateLegacyDataAsync<Transacao>(familyId);
        await AssociateLegacyDataAsync<Transferencia>(familyId); await AssociateLegacyDataAsync<Cartao>(familyId); await AssociateLegacyDataAsync<CompraCartao>(familyId);
        await AssociateLegacyDataAsync<Fatura>(familyId); await AssociateLegacyDataAsync<ParcelaCartao>(familyId); await AssociateLegacyDataAsync<EstornoCartao>(familyId);
        await AssociateLegacyDataAsync<Recorrencia>(familyId); await AssociateLegacyDataAsync<OcorrenciaRecorrencia>(familyId); await AssociateLegacyDataAsync<Assinatura>(familyId);
        await AssociateLegacyDataAsync<Alerta>(familyId); await AssociateLegacyDataAsync<Orcamento>(familyId); await AssociateLegacyDataAsync<ItemOrcamento>(familyId);
    }

    private async Task AssociateLegacyDataAsync<TEntity>(Guid familyId) where TEntity : EntityBase
    {
        foreach (var entity in await db.Set<TEntity>().IgnoreQueryFilters().Where(x => !x.FamiliaId.HasValue).ToListAsync()) entity.FamiliaId = familyId;
    }
}

public sealed class ForbiddenException(string message) : Exception(message);
