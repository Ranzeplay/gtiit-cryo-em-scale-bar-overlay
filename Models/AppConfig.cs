namespace ScaleBarOverlay.Models;

public class AppConfig
{
    public AppConfig()
    {
        Services.AppLogger.Info(nameof(AppConfig), "AppConfig instance created.");
    }

    public ImportConfig ImportConfig { get; set; }
    
    public ScaleBarLocation ScaleBarLocation { get; set; }
}