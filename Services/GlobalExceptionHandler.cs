using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;

namespace ScaleBarOverlay.Services;

internal static class GlobalExceptionHandler
{
    private static int _showingExceptionDialog;

    public static void RegisterGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                HandleException(ex, "AppDomain.CurrentDomain.UnhandledException");
            }
            else
            {
                HandleException(new Exception($"Unhandled non-exception object: {args.ExceptionObject}"),
                    "AppDomain.CurrentDomain.UnhandledException");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            HandleException(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };
    }

    public static void RegisterUiThreadHandler()
    {
        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            HandleException(args.Exception, "Dispatcher.UIThread.UnhandledException");
            args.Handled = true;
        };
    }

    public static void HandleException(Exception exception, string source)
    {
        AppLogger.Error(nameof(GlobalExceptionHandler), $"Unhandled exception from {source}.", exception);

        if (Interlocked.CompareExchange(ref _showingExceptionDialog, 1, 0) != 0)
        {
            return;
        }

        var details =
            $"Unhandled exception ({source})\n\nException:\n{exception.Message}\n\nStackTrace:\n{exception.StackTrace}\n\nFull:\n{exception}";

        try
        {
            if (Application.Current == null)
            {
                Console.Error.WriteLine(details);
                Interlocked.Exchange(ref _showingExceptionDialog, 0);
                return;
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                _ = ShowExceptionDialogAsync(details);
                return;
            }

            Dispatcher.UIThread.Post(async () => await ShowExceptionDialogAsync(details));
        }
        catch (Exception dialogException)
        {
            AppLogger.Error(nameof(GlobalExceptionHandler), "Failed to show unhandled exception dialog.", dialogException);
            Console.Error.WriteLine(details);
            Interlocked.Exchange(ref _showingExceptionDialog, 0);
        }
    }

    private static async Task ShowExceptionDialogAsync(string details)
    {
        try
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Unhandled Exception", details);

            if (TryGetMainWindow(out var mainWindow) && mainWindow is { } owner)
            {
                await box.ShowWindowDialogAsync(owner);
                return;
            }

            await box.ShowWindowAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _showingExceptionDialog, 0);
        }
    }

    private static bool TryGetMainWindow(out Window? mainWindow)
    {
        mainWindow = null;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return false;
        }

        mainWindow = desktop.MainWindow;
        return mainWindow != null;
    }
}


