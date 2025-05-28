using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;

namespace ScaleBarOverlay.Services
{
    public static class ImageProcessorService
    {
        public static async Task<Image> ProcessImageAsync(ImageTask task)
        {
            // 使用任务中的边距
            return await ProcessImageAsync(task, task.ScaleBarMargin, task.ScaleBarMargin);
        }

        public static async Task<Image> ProcessImageAsync(ImageTask task, int marginLeft, int marginBottom)
        {
            // 读取图像
            var image = await Image.LoadAsync(task.ImagePath);

            // 计算比例尺长度（像素）
            double pixelLength = task.Magnification.PixelLength * task.Magnification.ScaleBarNanometers / 100.0;
            
            // 比例尺参数
            var rectY1 = image.Height - marginBottom - 15;
            var rectX2 = marginLeft + (int)pixelLength;
            var rectY2 = image.Height - marginBottom;
            
            // 绘制比例尺矩形
            var rectangle = new RectangleF(marginLeft, rectY1, rectX2 - marginLeft, rectY2 - rectY1);
            image.Mutate(ctx => ctx.Fill(Color.White, rectangle));
            
            // 比例尺文字
            var fontFamily = SystemFonts.Get("Arial");
            var font = fontFamily.CreateFont(72, FontStyle.Bold);
            
            var text = $"{task.Magnification.ScaleBarNanometers} nm";
            var textX = marginLeft;
            var textY = rectY1 - 90;
            
            // 绘制文字
            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(textX, textY)
            };

            image.Mutate(ctx => ctx.DrawText(textOptions, text, new SolidBrush(Color.White)));

            return image;
        }

        public static async Task SaveImageAsync(Image image, string outputPath)
        {
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
