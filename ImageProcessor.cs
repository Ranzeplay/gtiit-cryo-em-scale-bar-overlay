using SixLabors.ImageSharp;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;

namespace ScaleBarOverlay;

public class ImageProcessor
{
    public static async Task<Image> ProcessImageAsync(ImageTask task, int marginLeft, int marginBottom)
    {
        // Read the image
        var image = await Image.LoadAsync(task.ImagePath);

        // Calculate the scale bar length (in pixels)
        double pixelLength = task.Magnification.PixelLength * task.Magnification.ScaleBarNanometers / 100.0;
        
        // Scale bar parameters
        int offset = marginLeft;
        int height = image.Height;
        int rectX1 = offset;
        int rectY1 = height - marginBottom - 15;
        int rectX2 = offset + (int)pixelLength;
        int rectY2 = height - marginBottom;
        
        // Draw the scale bar rectangle
        var rectangle = new RectangleF(rectX1, rectY1, rectX2 - rectX1, rectY2 - rectY1);
        image.Mutate(ctx => ctx.Fill(Color.White, rectangle));
        
        // Scale bar text
        var fontFamily = SystemFonts.Get("Arial");
        var font = fontFamily.CreateFont(72, FontStyle.Bold);
        
        string text = $"{task.Magnification.ScaleBarNanometers} nm";
        int textX = rectX1;
        int textY = rectY1 - 90;
        
        // Draw the text
        var textOptions = new RichTextOptions(font)
        {
            Origin = new PointF(textX, textY)
        };

        image.Mutate(ctx => ctx.DrawText(textOptions, text, new SolidBrush(Color.White)));

        return image;
    }

    // Retain the original synchronous method for compatibility
    public static void ProcessImage(ImageTask task, int marginLeft, int marginBottom)
    {
        ProcessImageAsync(task, marginLeft, marginBottom).GetAwaiter().GetResult();
    }
}
