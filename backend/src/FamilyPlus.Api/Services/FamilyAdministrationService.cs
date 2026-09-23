using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class FamilyAdministrationService(FinanceDbContext db, FamilyAccessService access, AuditService audit)
{
    public async Task<FamilyResponse> GetFamilyAsync(Guid familyId)
    {
        var family = await db.Familias.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == familyId) ?? throw new DomainException("Família não encontrada.");
        return Map(family);
    }

    public async Task<FamilyResponse> UpdateFamilyAsync(Guid familyId, FamilyRequest request, Guid userId)
    {
        await access.EnsurePermissionAsync(userId, "FAMILIA_ADMINISTRAR");
        if (string.IsNullOrWhiteSpace(request.Nome) || request.MoedaPadrao.Trim().Length != 3) throw new DomainException("Configuração de família inválida.", "Informe o nome e uma moeda ISO de três letras.");
        var family = await db.Familias.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == familyId) ?? throw new DomainException("Família não encontrada.");
        family.Nome = request.Nome.Trim(); family.MoedaPadrao = request.MoedaPadrao.Trim().ToUpperInvariant(); family.Timezone = request.Timezone.Trim();
        var settings = await db.ConfiguracoesFamilia.SingleOrDefaultAsync(x => x.FamiliaId == familyId) ?? new ConfiguracaoFamilia { FamiliaId = familyId };
        settings.Moeda = family.MoedaPadrao; settings.Timezone = family.Timezone; if (settings.FamiliaId == familyId && db.Entry(settings).State == EntityState.Detached) db.ConfiguracoesFamilia.Add(settings);
        await audit.RecordAsync("FAMILIA", family.Id, OperacaoAuditoria.ALTERACAO, null, new { family.Nome, family.MoedaPadrao, family.Timezone }, userId);
        await db.SaveChangesAsync(); return Map(family);
    }

    public async Task<IReadOnlyList<MemberResponse>> ListMembersAsync()
    {
        var rows = await db.Membros.AsNoTracking().OrderBy(x => x.Nome).ToListAsync();
        var userIds = await db.Usuarios.AsNoTracking().Where(x => x.MembroId.HasValue).Select(x => x.MembroId!.Value).ToListAsync();
        return rows.Select(x => new MemberResponse(x.Id, x.Nome, x.Descricao, x.Tipo, x.DataNascimento, x.Ativo, x.CriadoEm, userIds.Contains(x.Id))).ToList();
    }

    public async Task<MemberResponse> CreateMemberAsync(MemberRequest request, Guid userId)
    {
        await access.EnsurePermissionAsync(userId, "FAMILIA_ADMINISTRAR");
        if (string.IsNullOrWhiteSpace(request.Nome)) throw new DomainException("O nome do membro é obrigatório.");
        var session = await CurrentUserAsync(userId);
        var member = new Membro { FamiliaId = session.FamiliaId, Nome = request.Nome.Trim(), Descricao = Trim(request.Descricao), Tipo = request.Tipo, DataNascimento = request.DataNascimento };
        db.Membros.Add(member); await audit.RecordAsync("MEMBRO", member.Id, OperacaoAuditoria.ALTERACAO_MEMBRO, null, new { member.Nome, member.Tipo }, userId); await db.SaveChangesAsync();
        return new MemberResponse(member.Id, member.Nome, member.Descricao, member.Tipo, member.DataNascimento, member.Ativo, member.CriadoEm);
    }

    public async Task<MemberResponse> UpdateMemberAsync(Guid id, MemberRequest request, Guid userId)
    {
        await access.EnsurePermissionAsync(userId, "FAMILIA_ADMINISTRAR");
        var member = await db.Membros.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Membro não encontrado.");
        member.Nome = request.Nome.Trim(); member.Descricao = Trim(request.Descricao); member.Tipo = request.Tipo; member.DataNascimento = request.DataNascimento;
        await audit.RecordAsync("MEMBRO", member.Id, OperacaoAuditoria.ALTERACAO_MEMBRO, null, new { member.Nome, member.Tipo }, userId); await db.SaveChangesAsync();
        var hasUser = await db.Usuarios.AnyAsync(x => x.MembroId == member.Id);
        return new MemberResponse(member.Id, member.Nome, member.Descricao, member.Tipo, member.DataNascimento, member.Ativo, member.CriadoEm, hasUser);
    }

    public async Task<MemberResponse> SetMemberStatusAsync(Guid id, bool ativo, Guid userId)
    {
        await access.EnsurePermissionAsync(userId, "FAMILIA_ADMINISTRAR");
        var member = await db.Membros.SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Membro não encontrado.");
        if (!ativo && await HasFinancialHistoryAsync(id)) { member.Ativo = false; } else member.Ativo = ativo;
        await audit.RecordAsync("MEMBRO", id, OperacaoAuditoria.ALTERACAO_MEMBRO, null, new { ativo }, userId); await db.SaveChangesAsync();
        return new MemberResponse(member.Id, member.Nome, member.Descricao, member.Tipo, member.DataNascimento, member.Ativo, member.CriadoEm, await db.Usuarios.AnyAsync(x => x.MembroId == id));
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(Guid currentUserId)
    {
        await access.EnsurePermissionAsync(currentUserId, "USUARIO_ADMINISTRAR");
        var users = await db.Usuarios.Include(x => x.Membro).OrderBy(x => x.Nome).ToListAsync();
        var profiles = await db.UsuariosPerfis.Include(x => x.PerfilAcesso).ToListAsync();
        return users.Select(user => MapUser(user, profiles.FirstOrDefault(x => x.UsuarioId == user.Id)?.PerfilAcesso?.Nome ?? PerfilAcessoNome.MEMBRO.ToString())).ToList();
    }

    public async Task<UserResponse> CreateUserAsync(UserRequest request, Guid currentUserId)
    {
        await access.EnsurePermissionAsync(currentUserId, "USUARIO_ADMINISTRAR");
        if (request.Senha.Length < 8) throw new DomainException("A senha deve ter pelo menos 8 caracteres.");
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Login == request.Login.Trim().ToLowerInvariant())) throw new DomainException("O login já está em uso.");
        var member = await db.Membros.SingleOrDefaultAsync(x => x.Id == request.MembroId) ?? throw new DomainException("Membro não encontrado na família atual.");
        var session = await CurrentUserAsync(currentUserId);
        var user = new Usuario { FamiliaId = session.FamiliaId, MembroId = member.Id, Nome = member.Nome, Login = request.Login.Trim().ToLowerInvariant(), SenhaHash = PasswordService.Hash(request.Senha), Ativo = request.Ativo };
        db.Usuarios.Add(user); await db.SaveChangesAsync(); await SetProfileAsync(user.Id, request.Perfil, currentUserId); await audit.RecordAsync("USUARIO", user.Id, OperacaoAuditoria.CRIACAO_USUARIO, null, new { user.Login, user.MembroId }, currentUserId); await db.SaveChangesAsync(); return MapUser(user, request.Perfil);
    }

    public async Task<UserResponse> SetUserStatusAsync(Guid id, bool ativo, Guid currentUserId)
    {
        await access.EnsurePermissionAsync(currentUserId, "USUARIO_ADMINISTRAR");
        var user = await db.Usuarios.Include(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Usuário não encontrado.");
        user.Ativo = ativo; await audit.RecordAsync("USUARIO", id, ativo ? OperacaoAuditoria.ALTERACAO : OperacaoAuditoria.DESATIVACAO_USUARIO, null, new { ativo }, currentUserId); await db.SaveChangesAsync();
        var profile = await db.UsuariosPerfis.Where(x => x.UsuarioId == id).Select(x => x.PerfilAcesso!.Nome).FirstOrDefaultAsync() ?? PerfilAcessoNome.MEMBRO.ToString(); return MapUser(user, profile);
    }

    public async Task<IReadOnlyList<PermissionResponse>> ListPermissionsAsync(Guid currentUserId) { await access.EnsurePermissionAsync(currentUserId, "PERMISSAO_ADMINISTRAR"); return await db.Permissoes.AsNoTracking().OrderBy(x => x.Modulo).ThenBy(x => x.Codigo).Select(x => new PermissionResponse(x.Id, x.Codigo, x.Modulo, x.Descricao)).ToListAsync(); }

    public async Task<UserPermissionsResponse> GetUserPermissionsAsync(Guid userId, Guid currentUserId)
    {
        await access.EnsurePermissionAsync(currentUserId, "PERMISSAO_ADMINISTRAR");
        if (!await db.Usuarios.AnyAsync(x => x.Id == userId)) throw new DomainException("Usuário não encontrado.");
        var permissions = await db.UsuariosPerfis.Where(x => x.UsuarioId == userId).SelectMany(x => x.PerfilAcesso!.Permissoes).Select(x => new PermissionResponse(x.Permissao!.Id, x.Permissao.Codigo, x.Permissao.Modulo, x.Permissao.Descricao)).ToListAsync();
        var scopes = await db.PermissoesEscopo.Include(x => x.Membros).Where(x => x.UsuarioId == userId).ToListAsync();
        return new UserPermissionsResponse(userId, permissions, scopes.Select(x => new ScopeRequest(x.Recurso, x.TipoEscopo, x.Membros.Select(m => m.MembroId).ToList())).ToList());
    }

    public async Task SetUserPermissionsAsync(Guid userId, IReadOnlyList<ScopeRequest> scopes, string profile, Guid currentUserId)
    {
        await access.EnsurePermissionAsync(currentUserId, "PERMISSAO_ADMINISTRAR"); await SetProfileAsync(userId, profile, currentUserId);
        db.PermissoesEscopo.RemoveRange(db.PermissoesEscopo.Where(x => x.UsuarioId == userId));
        foreach (var scope in scopes) { var entity = new PermissaoEscopo { UsuarioId = userId, Recurso = scope.Recurso.Trim().ToUpperInvariant(), TipoEscopo = scope.TipoEscopo }; db.PermissoesEscopo.Add(entity); if (scope.MembroIds is not null) foreach (var memberId in scope.MembroIds) db.PermissoesMembro.Add(new PermissaoMembro { PermissaoEscopoId = entity.Id, MembroId = memberId }); }
        await audit.RecordAsync("USUARIO_PERMISSOES", userId, OperacaoAuditoria.ALTERACAO_PERMISSAO, null, new { profile, scopes }, currentUserId); await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<FinancialProfileResponse>> FinancialProfilesAsync()
    {
        var members = await db.Membros.AsNoTracking().Where(x => x.Ativo).ToListAsync(); var accounts = await db.Contas.AsNoTracking().ToListAsync(); var transactions = await db.Transacoes.AsNoTracking().Where(x => x.Status == StatusTransacao.EFETIVADA).ToListAsync(); var cards = await db.Cartoes.AsNoTracking().ToListAsync();
        var from = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero); var until = from.AddMonths(1);
        return members.Select(member => { var memberTransactions = transactions.Where(x => x.MembroId == member.Id); var income = memberTransactions.Where(x => x.Tipo == TipoTransacao.RECEITA && x.DataMovimentacao >= from && x.DataMovimentacao < until).Sum(x => x.ValorCentavos); var expense = memberTransactions.Where(x => x.Tipo == TipoTransacao.DESPESA && x.DataMovimentacao >= from && x.DataMovimentacao < until).Sum(x => x.ValorCentavos); return new FinancialProfileResponse(member.Id, member.Nome, accounts.Where(x => x.MembroId == member.Id).Sum(x => x.SaldoInicialCentavos), income, expense, cards.Where(x => x.MembroId == member.Id).Sum(x => x.LimiteTotalCentavos), 0); }).ToList();
    }

    private async Task SetProfileAsync(Guid userId, string profile, Guid currentUserId)
    {
        if (!Enum.TryParse<PerfilAcessoNome>(profile, true, out var parsed)) throw new DomainException("Perfil de acesso inválido.");
        var entity = await db.PerfisAcesso.SingleOrDefaultAsync(x => x.Nome == parsed.ToString()) ?? throw new DomainException("Perfil de acesso não encontrado.");
        db.UsuariosPerfis.RemoveRange(db.UsuariosPerfis.Where(x => x.UsuarioId == userId)); db.UsuariosPerfis.Add(new UsuarioPerfil { UsuarioId = userId, PerfilAcessoId = entity.Id });
        await audit.RecordAsync("USUARIO_PERFIL", userId, OperacaoAuditoria.ALTERACAO_PERMISSAO, null, new { profile = parsed.ToString() }, currentUserId);
    }

    private async Task<Usuario> CurrentUserAsync(Guid id) => await db.Usuarios.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
    private async Task<bool> HasFinancialHistoryAsync(Guid memberId) => await db.Contas.AnyAsync(x => x.MembroId == memberId) || await db.Transacoes.AnyAsync(x => x.MembroId == memberId) || await db.Cartoes.AnyAsync(x => x.MembroId == memberId) || await db.Orcamentos.AnyAsync(x => x.MembroId == memberId);
    private static FamilyResponse Map(Familia x) => new(x.Id, x.Nome, x.MoedaPadrao, x.Timezone, x.Ativo);
    private static UserResponse MapUser(Usuario x, string profile) => new(x.Id, x.MembroId, x.Membro?.Nome, x.Nome, x.Login, x.Ativo, profile, x.UltimoAcessoEm);
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
