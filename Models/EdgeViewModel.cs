namespace ScaleBarOverlay.Models;

public class EdgeViewModel
{
    public EdgeViewModel()
    {
        Services.AppLogger.Info(nameof(EdgeViewModel), "EdgeViewModel instance created.");
    }

    public CornerOption CornerOption { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}