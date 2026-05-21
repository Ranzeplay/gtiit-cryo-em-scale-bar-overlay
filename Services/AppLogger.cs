using System;
using System.IO;

namespace ScaleBarOverlay.Services;

internal static class AppLogger
{
    private static readonly object SyncLock = new();

    public static string LogPath => Path.Combine(AppContext.BaseDirectory, "latest.log");

    public static TextWriter CreateAvaloniaLogWriter() =>
        new StreamWriter(new FileStream(LogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };

    public static void Info(string source, string message) => Write("INF", source, message);

    public static void Warn(string source, string message) => Write("WRN", source, message);

    public static void Error(string source, string message, Exception? ex = null)
    {
        var fullMessage = ex == null ? message : $"{message} | {ex}";
        Write("ERR", source, fullMessage);
    }

    private static void Write(string level, string source, string message)
    {
        var line = $"{DateTime.UtcNow:O} [{level}] {source}: {message}";

        try
        {
            lock (SyncLock)
            {
                using var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(line);
            }
        }
        catch
        {
            // Logging should not break the app if disk or file sharing fails.
        }
    }
}

