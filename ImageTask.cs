namespace ScaleBarOverlay;

public class ImageTask
{
    public string ImagePath { get; set; }
    
    public MagnificationOption Magnification { get; set; }
    
    public string OutputPath { get; set; }

    public ImageTask(string imagePath, MagnificationOption magnification, string outputPath)
    {
        ImagePath = imagePath;
        Magnification = magnification;
        OutputPath = outputPath;
    }
}