using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using ScaleBarOverlay.Models;

namespace ScaleBarOverlay.Services
{
    public static class ImageTaskService
    {
        public static List<ImageTask> CreateImageTasks(
            IReadOnlyList<IStorageFile> files,
            MagnificationOption magnificationOption,
            AlignmentOption alignmentOption,
            string? destinationFolder = null)
        {
            var newTasks = new List<ImageTask>();

            foreach (var file in files)
            {
                var outputName =
                    $"{Path.GetFileNameWithoutExtension(file.Name)}_ScaleBar{Path.GetExtension(file.Name)}";
                var outputPath = Path.Combine(destinationFolder ?? Path.GetDirectoryName(file.Path.AbsolutePath)!,
                    outputName);

                var magnificationOptionValue = magnificationOption;
                if (magnificationOption is MagnificationOption.AutoMagnificationOption)
                {
                    var detectedMagnification = ImageProcessorService.DetectMagnificationOption(file.Path.LocalPath);
                    if (detectedMagnification is null)
                    {
                        detectedMagnification = MagnificationOption.TemplateOptions[1];
                    }
                    
                    magnificationOptionValue = detectedMagnification;
                }

                var task = new ImageTask(file.Path.LocalPath, magnificationOptionValue, outputPath, alignmentOption);
                newTasks.Add(task);
            }

            return newTasks;
        }

        public static async Task ProcessAllTasksAsync(
            ObservableCollection<ImageTask> imageTasks,
            int marginLeft,
            int marginBottom)
        {
            foreach (var imageTask in imageTasks)
            {
                var result = await ImageProcessorService.ProcessImageAsync(imageTask, marginLeft, marginBottom);
                await ImageProcessorService.SaveImageAsync(result, imageTask.OutputPath);
            }
        }
    }
}