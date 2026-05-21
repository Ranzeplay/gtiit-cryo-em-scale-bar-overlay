using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ScaleBarOverlay.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats;

namespace ScaleBarOverlay.Services
{
    public static class ImageProcessorService
    {
        public static async Task<Image> ProcessImageAsync(ImageTask task, ScaleBarLocation location, int? targetSize = null)
        {
            AppLogger.Info(nameof(ImageProcessorService), $"Processing image '{task.ImagePath}' with output '{task.OutputPath}'.");
            // Load image
            await using var stream = new FileStream(task.ImagePath, FileMode.Open, FileAccess.Read);
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            
            // Get image resolution
            var info = await Image.IdentifyAsync(memoryStream);
            int originalWidth = info?.Width ?? 0;
            int originalHeight = info?.Height ?? 0;
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

            float scale = (targetSize ?? originalWidth) / (originalWidth * 1f);

            float actualSize = originalHeight * scale;

            const float baseFontSize = 96f;
            const float baseSpacing = 30f;
            const float baseBarHeight = 15f;
            float fontSize = baseFontSize * scale;
            float spacing = baseSpacing * scale;
            float barHeight = baseBarHeight * scale;

            var fontFamily = SystemFonts.Get("Arial");
            var font = fontFamily.CreateFont(fontSize, FontStyle.Regular);
            var text = $"{task.Magnification.ScaleBarNanometers} nm";
            var textMeasure = TextMeasurer.MeasureSize(text, new RichTextOptions(font));
            
            var textHeight = textMeasure.Height;
            var textWidth = textMeasure.Width;
            
            float totalHeight = textHeight + spacing + barHeight;
            float totalWidth = task.Magnification.PixelLength * task.Magnification.ScaleBarNanometers / 100f * scale;
            
            float actualHorizontalOffset = location.HorizontalOffset * scale;
            float actualVerticalOffset = location.VerticalOffset * scale;

            float anchorX;
            float anchorY;
            switch (location.CornerOption)
            {
                case CornerOption.TopLeft:
                    anchorX = actualHorizontalOffset;
                    anchorY = actualVerticalOffset;
                    break;
                case CornerOption.BottomRight:
                    anchorX = actualSize - actualHorizontalOffset - totalWidth;
                    anchorY = actualSize - actualVerticalOffset - totalHeight;
                    break;
                case CornerOption.BottomLeft:
                    anchorX = actualHorizontalOffset;
                    anchorY = actualSize - actualVerticalOffset - totalHeight;
                    break;
                case CornerOption.TopRight:
                    anchorX = actualSize - actualHorizontalOffset - totalWidth;
                    anchorY = actualVerticalOffset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location.CornerOption));
            }
            
            // Locate text and bar
            var textX = task.AlignmentOption switch 
            {
                AlignmentOption.Left => anchorX,
                AlignmentOption.Center => anchorX + (totalWidth - textWidth) / 2,
                AlignmentOption.Right => anchorX + totalWidth - textWidth,
                _ => throw new ArgumentOutOfRangeException(nameof(task.AlignmentOption))
            };
            var textY = anchorY;

            var barX = anchorX;
            var barY = anchorY + textHeight + spacing;
            
            // Draw text and bar
            image.Mutate(ctx =>
            {
                ctx.DrawText(new RichTextOptions(font){ Origin = new Vector2(textX, textY)}, text, new SolidBrush(Color.White));
                ctx.Fill(Color.White, new RectangleF(barX, barY, totalWidth, barHeight));
            });

            AppLogger.Info(nameof(ImageProcessorService), $"Image processing complete for '{task.ImagePath}'.");
            return image;
        }

        public static async Task SaveImageAsync(Image image, string outputPath)
        {
            AppLogger.Info(nameof(ImageProcessorService), $"Saving processed image to '{outputPath}'.");
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
            AppLogger.Info(nameof(ImageProcessorService), $"Saved image to '{outputPath}'.");
        }

        public static MagnificationOption? DetectMagnificationOption(string imagePath) =>
            MagnificationOption.TemplateOptions.FirstOrDefault(option =>
            {
                var isMatch = imagePath.Contains(option.DisplayText, StringComparison.CurrentCultureIgnoreCase);
                if (isMatch)
                {
                    AppLogger.Info(nameof(ImageProcessorService),
                        $"Detected magnification '{option.DisplayText}' from '{imagePath}'.");
                }

                return isMatch;
            });
    }
}