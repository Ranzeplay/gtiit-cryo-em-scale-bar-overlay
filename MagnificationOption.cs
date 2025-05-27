namespace ScaleBarOverlay;

public class MagnificationOption(int ratio, double pixels, int scaleBarNanometers)
{
    public int Ratio { get; } = ratio;

    public double Pixels { get; } = pixels;

    public int ScaleBarNanometers { get; } = scaleBarNanometers;

    public string DisplayText => $"{Ratio}x";
}