namespace ScaleBarOverlay;

public class MagnificationOption(int ratio, double pixels, int scaleBarNanometers)
{
    public int Ratio { get; } = ratio;

    public double Pixels { get; } = pixels;

    public double PixelLength => 100.0 / (0.1 * Pixels);
    
    public int ScaleBarNanometers { get; } = scaleBarNanometers;

    public string DisplayText => $"{Ratio}x";
}

