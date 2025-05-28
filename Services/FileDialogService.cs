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
            return await parentWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
        }

        public async Task<IReadOnlyList<IStorageFolder>> OpenFolderAsync(string title = "Select Output Folder")
        {
            return await parentWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
            });
        }
    }
}
