using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ScaleBarOverlay.Services
{
    public class ImageTaskService
    {
        private readonly ImageProcessorService _imageProcessorService;
        
        public ImageTaskService(ImageProcessorService imageProcessorService)
        {
            _imageProcessorService = imageProcessorService;
        }
        
        public List<ImageTask> CreateImageTasks(
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
        
        public async Task ProcessAllTasksAsync(ObservableCollection<ImageTask> imageTasks)
        {
            foreach (var imageTask in imageTasks)
            {
                var result = await _imageProcessorService.ProcessImageAsync(imageTask);
                await _imageProcessorService.SaveImageAsync(result, imageTask.OutputPath);
            }
        }
    }
}
