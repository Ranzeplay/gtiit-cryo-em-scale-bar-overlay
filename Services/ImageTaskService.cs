using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ScaleBarOverlay.Services
{
    public static class ImageTaskService
    {
        public static List<ImageTask> CreateImageTasks(
            IReadOnlyList<IStorageFile> files, 
            MagnificationOption magnificationOption, 
            IStorageFolder destinationFolder)
        {
            var newTasks = new List<ImageTask>();
            
            foreach (var file in files)
            {
                var outputName = $"{Path.GetFileNameWithoutExtension(file.Name)}_scaleBar{Path.GetExtension(file.Name)}";
                var outputPath = Path.Combine(destinationFolder.Path.LocalPath, outputName);
                var task = new ImageTask(file.Path.AbsolutePath, magnificationOption, outputPath);
                newTasks.Add(task);
            }
            
            return newTasks;
        }
        
        public static async Task ProcessAllTasksAsync(ObservableCollection<ImageTask> imageTasks)
        {
            foreach (var imageTask in imageTasks)
            {
                var result = await ImageProcessorService.ProcessImageAsync(imageTask);
                await ImageProcessorService.SaveImageAsync(result, imageTask.OutputPath);
            }
        }
    }
}
