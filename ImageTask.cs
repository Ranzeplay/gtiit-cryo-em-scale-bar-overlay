namespace ScaleBarOverlay;

public class ImageTask
{
    public string ImagePath { get; set; }
    
    public MagnificationOption Magnification { get; set; }
    
    public string OutputPath { get; set; }
    
    public int ScaleBarMargin { get; set; } = 50;

    public ImageTask(string imagePath, MagnificationOption magnification, string outputPath)
    {
        ImagePath = imagePath;
        Magnification = magnification;
        OutputPath = outputPath;
        ScaleBarMargin = 50; // Default value
    }
}

