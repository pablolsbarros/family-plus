using Microsoft.Extensions.Logging;

namespace FamilyPlus.Api.Config;

public sealed class LocalFileLoggerProvider(string logsDirectory) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new LocalFileLogger(logsDirectory, categoryName);
    public void Dispose() { }

    private sealed class LocalFileLogger(string logsDirectory, string categoryName) : ILogger
    {
        private static readonly Lock FileLock = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {categoryName}: {message}";
            if (exception is not null) line += $"{Environment.NewLine}{exception}";

            lock (FileLock)
            {
                File.AppendAllText(Path.Combine(logsDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log"),
                    line + Environment.NewLine);
            }
        }
    }
}
