namespace ScaleBarOverlay.Models;

public class ScaleBarLocation(CornerOption cornerOption, int verticalOffset, int horizontalOffset)
{
    public CornerOption CornerOption { get; set; } = cornerOption;
    public int VerticalOffset { get; set; } = verticalOffset;
    public int HorizontalOffset { get; set; } = horizontalOffset;
}
