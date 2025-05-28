namespace ScaleBarOverlay;

public class ImageTask(string imagePath, MagnificationOption magnification, string outputPath)
{
    public string ImagePath { get; set; } = imagePath;

    public MagnificationOption Magnification { get; } = magnification;

    public string OutputPath { get; set; } = outputPath;

    public int ScaleBarMargin { get; set; } = 50;
}

