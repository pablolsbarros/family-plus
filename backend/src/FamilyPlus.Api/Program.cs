using FamilyPlus.Api.Config;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Middleware;
using FamilyPlus.Api.Repositories;
using FamilyPlus.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var storageRoot = Environment.GetEnvironmentVariable("FAMILYPLUS_STORAGE_ROOT");
var paths = AppPaths.Create(string.IsNullOrWhiteSpace(storageRoot) ? builder.Environment.ContentRootPath : storageRoot);
paths.EnsureDirectories();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new LocalFileLoggerProvider(paths.LogsDirectory));

builder.Services.AddSingleton(paths);
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite($"Data Source={paths.DatabasePath}")
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FamilyAccessService>();
builder.Services.AddScoped<FamilyAdministrationService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<DatabaseHealthService>();
builder.Services.AddScoped<ExpenseSplitService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<AccountBalanceService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<RecurrenceService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<SaudeFinanceiraService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<RequireSessionFilter>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .SetIsOriginAllowed(IsLocalFrontendOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FamilyPlus.Api v1"));
app.UseCors("frontend");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { success = true, data = new { status = "ok", offline = true } }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await db.Database.MigrateAsync();
    db.SetFamilyContext(null);
    var access = scope.ServiceProvider.GetRequiredService<FamilyAccessService>();
    foreach (var legacyUser in await db.Usuarios.IgnoreQueryFilters().ToListAsync())
    {
        db.SetFamilyContext(null);
        await access.ProvisionAsync(legacyUser);
    }
    await scope.ServiceProvider.GetRequiredService<CardService>().AutoCloseAsync(DateTimeOffset.UtcNow);
    await scope.ServiceProvider.GetRequiredService<RecurrenceService>().GenerateAllAsync(DateTimeOffset.UtcNow);
    await scope.ServiceProvider.GetRequiredService<AlertService>().RefreshAsync();
    app.Logger.LogInformation("Banco SQLite pronto em {DatabasePath}", paths.DatabasePath);
}

app.Logger.LogInformation("Family+ API iniciada em modo local/offline.");
app.Run();

static bool IsLocalFrontendOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
    var isLoopbackHost = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1";
    return uri.Scheme == Uri.UriSchemeHttp && isLoopbackHost && uri.Port is >= 5173 and <= 5199;
}

public partial class Program;
