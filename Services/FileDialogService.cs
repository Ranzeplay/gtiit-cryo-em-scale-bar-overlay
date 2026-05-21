using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Controls;

namespace ScaleBarOverlay.Services
{
    public class FileDialogService(Window parentWindow)
    {
        public async Task<IReadOnlyList<IStorageFile>> OpenImageFilesAsync()
        {
            AppLogger.Info(nameof(FileDialogService), "Opening image file picker.");
            var files = await parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                Title = "Choose Images",
                FileTypeFilter = 
                [
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"]
                    }
                ]
            });
            AppLogger.Info(nameof(FileDialogService), $"Image picker returned {files.Count} file(s).");
            return files;
        }

        public async Task<IReadOnlyList<IStorageFolder>> OpenFolderAsync(string title = "Select Output Folder")
        {
            AppLogger.Info(nameof(FileDialogService), $"Opening folder picker with title '{title}'.");
            var folders = await parentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
            });
            AppLogger.Info(nameof(FileDialogService), $"Folder picker returned {folders.Count} folder(s).");
            return folders;
        }
        
        public async Task<IStorageFile?> SaveFile(string title = "Choose Output File", string defaultFilePath = "output.png")
        {
            AppLogger.Info(nameof(FileDialogService), $"Opening save file picker with suggested name '{defaultFilePath}'.");
            var file = await parentWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFilePath,
                FileTypeChoices =
                [
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"]
                    }
                ]
            });
            AppLogger.Info(nameof(FileDialogService), file == null ? "Save file picker canceled." : "Save file picker selected a file.");
            return file;
        }
    }
}
