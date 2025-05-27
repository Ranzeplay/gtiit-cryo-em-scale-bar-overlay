using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using SixLabors.ImageSharp;

namespace ScaleBarOverlay
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ImageTask> _imageTasks = new();

        public ObservableCollection<ImageTask> ImageTasks
        {
            get => _imageTasks;
            set
            {
                _imageTasks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private async void OnAddClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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

                if (files.Count == 0) return;

                var magnificationSelectionDialog = new MagnificationSelectionDialog();
                var magnificationChoice = await magnificationSelectionDialog.ShowDialog<MagnificationOption>(this);

                var destinationFolder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Output Folder",
                });

                if (destinationFolder.Count == 0) return;

                // 批量创建任务，最后一次性添加，避免频繁UI更新
                var newTasks = new List<ImageTask>();
                foreach (var file in files)
                {
                    var outputName =
                        $"{Path.GetFileNameWithoutExtension(file.Name)}_scaleBar{Path.GetExtension(file.Name)}";
                    var outputPath = Path.Combine(destinationFolder[0].Path.LocalPath, outputName);
                    var task = new ImageTask(file.Path.AbsolutePath, magnificationChoice, outputPath);
                    newTasks.Add(task);
                }
                foreach (var task in newTasks)
                {
                    ImageTasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("错误", $"添加图片时出错: {ex.Message}")
                    .ShowAsPopupAsync(this);
            }
        }

        private async void OnRunClicked(object? sender, RoutedEventArgs e)
        {
            // 在后台线程处理图片，避免阻塞UI
            await Task.Run(async () =>
            {
                foreach (var imageTask in ImageTasks)
                {
                    var result = await ImageProcessor.ProcessImageAsync(imageTask, 50, 50);
                    var ext = Path.GetExtension(imageTask.OutputPath).ToLowerInvariant();
                    switch (ext)
                    {
                        case ".jpg" or ".jpeg":
                            await result.SaveAsJpegAsync(imageTask.OutputPath);
                            break;
                        case ".png":
                            await result.SaveAsPngAsync(imageTask.OutputPath);
                            break;
                        case ".bmp":
                            await result.SaveAsBmpAsync(imageTask.OutputPath);
                            break;
                        default:
                            await result.SaveAsPngAsync(imageTask.OutputPath);
                            break;
                    };
                }
            });

            await MessageBoxManager
                .GetMessageBoxStandard("完成", "所有图像处理任务已完成。")
                .ShowAsPopupAsync(this);

            _imageTasks.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Avalonia.Media.Imaging.Bitmap? _selectedImageSource;

        public Avalonia.Media.Imaging.Bitmap? SelectedImageSource
        {
            get => _selectedImageSource;
            set
            {
                _selectedImageSource = value;
                OnPropertyChanged();
            }
        }

        private async void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is ImageTask selectedTask)
            {
                try
                {
                    // 在后台线程加载图像，避免阻塞 UI
                    await Task.Run(async () =>
                    {
                        try
                        {
                            // 检查文件是否存在
                            if (File.Exists(selectedTask.ImagePath))
                            {
                                // 在 UI 线程上更新图像源
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    using var fileStream = File.OpenRead(selectedTask.ImagePath);
                                    var bitmap = new Avalonia.Media.Imaging.Bitmap(fileStream);
                                    SelectedImageSource = bitmap;
                                });
                            }
                            else
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => { SelectedImageSource = null; });
                            }
                        }
                        catch (Exception ex)
                        {
                            await Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                SelectedImageSource = null;
                                await MessageBoxManager
                                    .GetMessageBoxStandard("错误", $"加载图像预览失败: {ex.Message}")
                                    .ShowAsPopupAsync(this);
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    await MessageBoxManager
                        .GetMessageBoxStandard("错误", $"处理选择变更时出错: {ex.Message}")
                        .ShowAsPopupAsync(this);
                }
            }
        }
    }
}
