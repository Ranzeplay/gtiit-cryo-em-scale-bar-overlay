namespace ScaleBarOverlay;

public class MagnificationOption(int ratio, float pixels, int scaleBarNanometers)
{
    public int Ratio { get; } = ratio;

    public float Pixels { get; } = pixels;

    public float PixelLength => 100f / (0.1f * Pixels);
    
    public int ScaleBarNanometers { get; } = scaleBarNanometers;

    public string DisplayText => $"{Ratio}K";
}

