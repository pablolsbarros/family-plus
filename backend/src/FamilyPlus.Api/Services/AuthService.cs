using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FamilyPlus.Api.Services;

public sealed class AuthService(FinanceDbContext db, ILogger<AuthService> logger, FamilyAccessService access)
{
    public Task<bool> HasAdministratorAsync() => db.Usuarios.AnyAsync();

    public async Task<AuthResult> SetupAsync(SetupRequest request)
    {
        if (await HasAdministratorAsync()) throw new DomainException("O administrador local já foi configurado.");
        Validate(request.Nome, request.Login, request.Senha);
        var user = new Usuario { Nome = request.Nome.Trim(), Login = request.Login.Trim().ToLowerInvariant(), SenhaHash = PasswordService.Hash(request.Senha) };
        db.Usuarios.Add(user);
        await access.ProvisionAsync(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Administrador local criado: {Login}", user.Login);
        return await CreateSessionAsync(user);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await db.Usuarios.SingleOrDefaultAsync(x => x.Login == login && x.Ativo);
        if (user is null || !PasswordService.Verify(request.Senha, user.SenhaHash))
        {
            logger.LogWarning("Tentativa de login recusada para {Login}", login);
            throw new DomainException("Login ou senha inválidos.");
        }
        await access.ProvisionAsync(user);
        user.UltimoAcessoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return await CreateSessionAsync(user);
    }

    public async Task<SessionUser?> GetByTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = PasswordService.HashToken(token);
        var session = await db.SessoesLocais.Include(x => x.Usuario)
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.EncerradaEm == null);
        if (session?.Usuario is null || session.ExpiraEm <= DateTimeOffset.UtcNow || !session.Usuario.Ativo) return null;
        return await access.CreateSessionUserAsync(session.Usuario);
    }

    public async Task LogoutAsync(string token)
    {
        var hash = PasswordService.HashToken(token);
        var session = await db.SessoesLocais.SingleOrDefaultAsync(x => x.TokenHash == hash && x.EncerradaEm == null);
        if (session is null) return;
        session.EncerradaEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task<AuthResult> CreateSessionAsync(Usuario user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expires = DateTimeOffset.UtcNow.AddHours(8);
        db.SessoesLocais.Add(new SessaoLocal { UsuarioId = user.Id, TokenHash = PasswordService.HashToken(token), ExpiraEm = expires });
        await db.SaveChangesAsync();
        return new AuthResult(user.Id, user.Nome, token, expires);
    }

    private static void Validate(string name, string login, string password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(login)) errors.Add("O login é obrigatório.");
        if (password?.Length < 8) errors.Add("A senha deve ter pelo menos 8 caracteres.");
        if (errors.Count > 0) throw new DomainException("Não foi possível criar o usuário local.", [.. errors]);
    }
}
