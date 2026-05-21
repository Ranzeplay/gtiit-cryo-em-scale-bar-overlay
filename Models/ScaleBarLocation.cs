namespace ScaleBarOverlay.Models;

public class ScaleBarLocation
{
    public ScaleBarLocation(CornerOption cornerOption, int verticalOffset, int horizontalOffset)
    {
        CornerOption = cornerOption;
        VerticalOffset = verticalOffset;
        HorizontalOffset = horizontalOffset;

        Services.AppLogger.Info(nameof(ScaleBarLocation),
            $"ScaleBarLocation created ({CornerOption}, V={VerticalOffset}, H={HorizontalOffset}).");
    }

    public CornerOption CornerOption { get; set; }
    public int VerticalOffset { get; set; }
    public int HorizontalOffset { get; set; }
}
