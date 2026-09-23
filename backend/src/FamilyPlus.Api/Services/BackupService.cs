using System.IO.Compression;
using System.Text.Json;
using FamilyPlus.Api.Config;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class BackupService(AppPaths paths, FinanceDbContext db, ILogger<BackupService> logger, AuditService audit)
{
    public async Task<BackupResponse> CreateAsync(Guid? userId = null)
    {
        await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
        var now = DateTimeOffset.Now; var fileName = $"familyplus-backup-{now:yyyy-MM-dd_HHmmss}.zip"; var target = Path.Combine(paths.BackupsDirectory, fileName); var staging = Path.Combine(paths.RootDirectory, $"backup-staging-{Guid.NewGuid():N}"); Directory.CreateDirectory(staging);
        try
        {
            File.Copy(paths.DatabasePath, Path.Combine(staging, "familyplus.db"), true);
            var manifest = new { application = "Family+", version = "0.7.0", databaseVersion = 7, createdAt = now, formatVersion = 1, includes = new[] { "familyplus.db", "documentos-locais" } };
            await File.WriteAllTextAsync(Path.Combine(staging, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            if (Directory.Exists(Path.Combine(paths.RootDirectory, "documents"))) CopyDirectory(Path.Combine(paths.RootDirectory, "documents"), Path.Combine(staging, "documents"));
            ZipFile.CreateFromDirectory(staging, target, CompressionLevel.Optimal, false);
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        await audit.RecordAsync("BACKUP", Guid.NewGuid(), OperacaoAuditoria.BACKUP, null, new { fileName }, userId); await db.SaveChangesAsync(); logger.LogInformation("Backup Family+ v0.7 criado: {FileName}", fileName); return new BackupResponse(fileName, now);
    }

    public Task<IReadOnlyList<BackupResponse>> HistoryAsync()
    {
        var result = Directory.Exists(paths.BackupsDirectory) ? Directory.GetFiles(paths.BackupsDirectory, "*.zip").OrderByDescending(File.GetLastWriteTimeUtc).Select(x => new BackupResponse(Path.GetFileName(x), File.GetLastWriteTimeUtc(x))).ToList() : [];
        return Task.FromResult<IReadOnlyList<BackupResponse>>(result);
    }

    public async Task RestoreAsync(string fileName, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName) throw new DomainException("Arquivo de backup inválido.");
        var source = Path.Combine(paths.BackupsDirectory, fileName); if (!File.Exists(source)) throw new DomainException("Backup não encontrado.");
        using var archive = ZipFile.OpenRead(source); var manifestEntry = archive.GetEntry("manifest.json") ?? throw new DomainException("Manifesto ausente no backup."); using var manifestStream = manifestEntry.Open(); using var reader = new StreamReader(manifestStream); var manifest = JsonSerializer.Deserialize<BackupManifest>(await reader.ReadToEndAsync()) ?? throw new DomainException("Manifesto inválido."); if (manifest.Application != "Family+" || manifest.FormatVersion != 1 || manifest.DatabaseVersion < 7) throw new DomainException("Versão de backup incompatível com a instalação atual.");
        var databaseEntry = archive.GetEntry("familyplus.db") ?? throw new DomainException("Banco SQLite ausente no backup."); var safety = await CreateAsync(userId); var temp = Path.Combine(paths.DataDirectory, $"restore-{Guid.NewGuid():N}.db"); try { await using (var input = databaseEntry.Open()) await using (var output = File.Create(temp)) await input.CopyToAsync(output); File.Copy(temp, paths.DatabasePath, true); await audit.RecordAsync("RESTAURACAO", Guid.NewGuid(), OperacaoAuditoria.RESTAURACAO, new { safety.NomeArquivo }, new { fileName }, userId); await db.SaveChangesAsync(); } finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void CopyDirectory(string source, string target) { Directory.CreateDirectory(target); foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true); foreach (var directory in Directory.GetDirectories(source)) CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory))); }
    private sealed record BackupManifest(string Application, int DatabaseVersion, int FormatVersion);
}
