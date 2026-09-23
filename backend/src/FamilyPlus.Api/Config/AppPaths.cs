namespace FamilyPlus.Api.Config;

public sealed class AppPaths
{
    private AppPaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        DataDirectory = Path.Combine(rootDirectory, "data");
        LogsDirectory = Path.Combine(rootDirectory, "logs");
        BackupsDirectory = Path.Combine(rootDirectory, "backups");
        DatabasePath = Path.Combine(DataDirectory, "familyplus.db");
    }

    public string RootDirectory { get; }
    public string DataDirectory { get; }
    public string LogsDirectory { get; }
    public string BackupsDirectory { get; }
    public string DatabasePath { get; }

    public static AppPaths Create(string rootDirectory) => new(rootDirectory);

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(BackupsDirectory);
    }
}
