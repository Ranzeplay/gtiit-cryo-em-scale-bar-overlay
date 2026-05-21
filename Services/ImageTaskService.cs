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
            AppLogger.Info(nameof(ImageTaskService), $"Creating tasks for {files.Count} file(s).");
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
                        AppLogger.Warn(nameof(ImageTaskService),
                            $"Failed to detect magnification for '{file.Path.LocalPath}'. Falling back to 11K.");
                        detectedMagnification = MagnificationOption.TemplateOptions[1];
                    }
                    
                    magnificationOptionValue = detectedMagnification;
                }

                var task = new ImageTask(file.Path.LocalPath, magnificationOptionValue, outputPath, alignmentOption);
                newTasks.Add(task);
            }

            AppLogger.Info(nameof(ImageTaskService), $"Created {newTasks.Count} task(s).");
            return newTasks;
        }

        public static async Task ProcessAllTasksAsync(
            ObservableCollection<ImageTask> imageTasks,
            ScaleBarLocation location)
        {
            AppLogger.Info(nameof(ImageTaskService), $"Processing {imageTasks.Count} task(s).");
            foreach (var imageTask in imageTasks)
            {
                var result = await ImageProcessorService.ProcessImageAsync(imageTask, location);
                await ImageProcessorService.SaveImageAsync(result, imageTask.OutputPath);
            }
            AppLogger.Info(nameof(ImageTaskService), "Finished processing all tasks.");
        }
    }
}