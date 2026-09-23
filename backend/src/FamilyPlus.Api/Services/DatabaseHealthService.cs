using System.Data;
using FamilyPlus.Api.Config;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class DatabaseHealthService(FinanceDbContext db, AppPaths paths)
{
    public async Task<SystemHealthResponse> CheckAsync()
    {
        var connection = db.Database.GetDbConnection(); var wasOpen = connection.State == ConnectionState.Open; if (!wasOpen) await connection.OpenAsync();
        string integrity; await using (var command = connection.CreateCommand()) { command.CommandText = "PRAGMA integrity_check;"; integrity = Convert.ToString(await command.ExecuteScalarAsync()) ?? "unknown"; }
        if (!wasOpen) await connection.CloseAsync();
        var latestBackup = Directory.Exists(paths.BackupsDirectory) ? Directory.GetFiles(paths.BackupsDirectory, "*.zip").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() : null;
        var migrations = await db.Database.GetAppliedMigrationsAsync();
        return new SystemHealthResponse("Family+", "0.7.0", "local-offline", "SQLite", migrations.Count(), paths.DataDirectory, latestBackup is null ? null : Path.GetFileName(latestBackup), integrity.Equals("ok", StringComparison.OrdinalIgnoreCase), integrity);
    }
}
