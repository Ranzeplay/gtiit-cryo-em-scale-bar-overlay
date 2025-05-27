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
using ScaleBarOverlay.Services;

namespace ScaleBarOverlay
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ImageTask> _imageTasks = new();
        private readonly FileDialogService _fileDialogService;
        private readonly ImageProcessorService _imageProcessorService;
        private readonly ImageTaskService _imageTaskService;

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
            
            // 初始化服务
            _fileDialogService = new FileDialogService(this);
            _imageProcessorService = new ImageProcessorService();
            _imageTaskService = new ImageTaskService(_imageProcessorService);
        }

        private async void OnAddClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                var files = await _fileDialogService.OpenImageFilesAsync();
                if (files.Count == 0) return;

                var magnificationSelectionDialog = new MagnificationSelectionDialog();
                var magnificationChoice = await magnificationSelectionDialog.ShowDialog<MagnificationOption>(this);

                var destinationFolder = await _fileDialogService.OpenFolderAsync();
                if (destinationFolder.Count == 0) return;

                // 使用服务创建任务
                var newTasks = _imageTaskService.CreateImageTasks(files, magnificationChoice, destinationFolder[0]);
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
                await _imageTaskService.ProcessAllTasksAsync(ImageTasks);
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
