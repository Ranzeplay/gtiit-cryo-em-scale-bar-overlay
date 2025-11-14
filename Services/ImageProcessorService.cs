using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats;

namespace ScaleBarOverlay.Services
{
    public static class ImageProcessorService
    {
        public static async Task<Image> ProcessImageAsync(ImageTask task, int marginLeft, int marginBottom, int? targetSize = null)
        {
            // Load image
            await using var stream = new FileStream(task.ImagePath, FileMode.Open, FileAccess.Read);
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            Image image;
            if (targetSize.HasValue)
            {
                var decoderOptions = new DecoderOptions
                {
                    TargetSize = new Size(targetSize.Value)
                };
                image = await Image.LoadAsync(decoderOptions, memoryStream);
            }
            else
            {
                image = await Image.LoadAsync(memoryStream);
            }
            float scale = image.Width / 4096f;

            // Bar related
            float pixelLength = task.Magnification.PixelLength * task.Magnification.ScaleBarNanometers / 100f * scale;
            float barHeight = 15f * scale;
            float rectY1 = image.Height - marginBottom * scale - barHeight;
            var rectangle = new RectangleF(marginLeft * scale, rectY1, pixelLength, barHeight);
            
            // Text related
            float fontSize = 72f * scale;
            float textOffsetY = 90f * scale;

            var fontFamily = SystemFonts.Get("Arial");
            var font = fontFamily.CreateFont(fontSize, FontStyle.Regular);

            var text = $"{task.Magnification.ScaleBarNanometers} nm";
            var textX = marginLeft * scale;
            var textY = rectY1 - textOffsetY;
            
            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(textX, textY)
            };

            image.Mutate(ctx =>
            {
                ctx.DrawText(textOptions, text, new SolidBrush(Color.White));
                ctx.Fill(Color.White, rectangle);
            });

            return image;
        }

        public static async Task SaveImageAsync(Image image, string outputPath)
        {
            // Save image according to file extension
            string ext = Path.GetExtension(outputPath).ToLowerInvariant();
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    await image.SaveAsJpegAsync(outputPath);
                    break;
                case ".png":
                    await image.SaveAsPngAsync(outputPath);
                    break;
                case ".bmp":
                    await image.SaveAsBmpAsync(outputPath);
                    break;
                default:
                    await image.SaveAsPngAsync(outputPath);
                    break;
            }
        }
    }
}
