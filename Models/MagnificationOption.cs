namespace ScaleBarOverlay.Models;

public class MagnificationOption(int ratio, float pixels, int scaleBarNanometers)
{
    public int Ratio { get; } = ratio;

    public float Pixels { get; } = pixels;

    public float PixelLength => 100f / (0.1f * Pixels);
    
    public int ScaleBarNanometers { get; } = scaleBarNanometers;

    public string DisplayText => $"{Ratio}K";
    
    public static MagnificationOption[] TemplateOptions =>
    [
        new(11, 13.3f, 500),
        new(36, 3.96f, 100),
        new(45, 3.17f, 100),
        new(57, 2.5f, 100),
        new(73, 1.9f, 100),
        new(92, 1.5f, 50),
        new(120, 1.2f, 50),
        new(150, 0.95f, 50),
        new(190, 0.74f, 50),
        new(240, 0.58f, 50)
    ];
}

