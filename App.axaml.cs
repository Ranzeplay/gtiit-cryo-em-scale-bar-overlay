using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScaleBarOverlay.Services;

namespace ScaleBarOverlay;

public class App : Application
{
    public override void Initialize()
    {
        AppLogger.Info(nameof(App), "Initializing application resources.");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppLogger.Info(nameof(App), "Framework initialization completed.");
        GlobalExceptionHandler.RegisterUiThreadHandler();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            AppLogger.Info(nameof(App), "Main window created.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}