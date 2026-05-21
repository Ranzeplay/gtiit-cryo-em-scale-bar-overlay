using System;
using Avalonia;
using ScaleBarOverlay.Services;

namespace ScaleBarOverlay;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        GlobalExceptionHandler.RegisterGlobalHandlers();
        AppLogger.Info(nameof(Program), $"Application starting with {args.Length} argument(s).");
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            AppLogger.Info(nameof(Program), "Application shutdown completed.");
        }
        catch (Exception ex)
        {
            GlobalExceptionHandler.HandleException(ex, "Program.Main");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTextWriter(AppLogger.CreateAvaloniaLogWriter());
}