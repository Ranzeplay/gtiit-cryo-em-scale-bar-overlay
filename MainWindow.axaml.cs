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
using SixLabors.ImageSharp.Formats;
using System.Diagnostics;
using System.Threading;
using Image = SixLabors.ImageSharp.Image;

namespace ScaleBarOverlay
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ImageTask> _imageTasks = new();
        private readonly FileDialogService _fileDialogService;
        private readonly ImageProcessorService _imageProcessorService;
        private readonly ImageTaskService _imageTaskService;
        private ImageTask? _selectedImageTask;
        private bool _isOriginalPreview = true;
        private CancellationTokenSource? _previewCancellationTokenSource;
        private bool _isPreviewLoading;
        private int _scaleBarMargin = 50;  // 新添加的边距属性，默认50
        private bool _isUpdatingPreview = false; // 添加标志以防止并发更新

        public ObservableCollection<ImageTask> ImageTasks
        {
            get => _imageTasks;
            set
            {
                _imageTasks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
            }
        }
        
        // 新添加的ScaleBarMargin属性
        public int ScaleBarMargin
        {
            get => _scaleBarMargin;
            set
            {
                if (_scaleBarMargin != value)
                {
                    _scaleBarMargin = value;
                    OnPropertyChanged();
                    
                    // 当边距改变时，如果有选中的任务，更新其边距并刷新预览
                    if (_selectedImageTask != null)
                    {
                        _selectedImageTask.ScaleBarMargin = value;
                        _ = UpdatePreviewImage();
                    }
                }
            }
        }
        
        public ImageTask? SelectedImageTask
        {
            get => _selectedImageTask;
            set
            {
                if (_selectedImageTask != value)
                {
                    _selectedImageTask = value;
                    OnPropertyChanged();
                    
                    // 当选择新任务时，更新边距控制器的值
                    if (value != null)
                    {
                        _scaleBarMargin = value.ScaleBarMargin;
                        OnPropertyChanged(nameof(ScaleBarMargin));
                    }
                }
            }
        }
        
        public bool IsOriginalPreview
        {
            get => _isOriginalPreview;
            set
            {
                if (_isOriginalPreview != value)
                {
                    _isOriginalPreview = value;
                    OnPropertyChanged();
                    _ = UpdatePreviewImage();
                }
            }
        }
        
        public bool IsPreviewLoading
        {
            get => _isPreviewLoading;
            set
            {
                if (_isPreviewLoading != value)
                {
                    _isPreviewLoading = value;
                    OnPropertyChanged();
                }
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

        private Avalonia.Media.Imaging.Bitmap? _previewImageSource;

        public Avalonia.Media.Imaging.Bitmap? PreviewImageSource
        {
            get => _previewImageSource;
            set
            {
                if (_previewImageSource != value)
                {
                    _previewImageSource?.Dispose(); // 释放旧的位图资源
                    _previewImageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is ImageTask selectedTask)
            {
                SelectedImageTask = selectedTask;
                // 更新边距为选中任务的边距值
                ScaleBarMargin = selectedTask.ScaleBarMargin;
                await UpdatePreviewImage();
            }
        }
        
        private async void OnRefreshPreviewClicked(object? sender, RoutedEventArgs e)
        {
            await UpdatePreviewImage();
        }
        
        private async Task UpdatePreviewImage()
        {
            // 如果已经在更新预览图像，则跳过此次调用
            if (_isUpdatingPreview || _selectedImageTask == null)
            {
                if (_selectedImageTask == null)
                    PreviewImageSource = null;
                return;
            }

            try
            {
                _isUpdatingPreview = true; // 设置标志，表示正在更新预览
                
                // 取消任何正在进行的预览操作
                _previewCancellationTokenSource?.Cancel();
                _previewCancellationTokenSource?.Dispose();
                _previewCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _previewCancellationTokenSource.Token;
                
                // 设置加载中标志
                IsPreviewLoading = true;
                
                // 使用await处理延迟，而不是Task.ContinueWith
                try
                {
                    // 短暂延迟，避免频繁更新
                    await Task.Delay(150, cancellationToken);
                    
                    // 检查文件是否存在
                    if (!File.Exists(_selectedImageTask.ImagePath))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => 
                        {
                            PreviewImageSource = null;
                            IsPreviewLoading = false;
                        });
                        return;
                    }

                    // 根据预览模式处理图像
                    if (IsOriginalPreview)
                    {
                        await LoadOriginalImageSafe(_selectedImageTask.ImagePath, cancellationToken);
                    }
                    else
                    {
                        await GenerateProcessedPreviewSafe(_selectedImageTask, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 预览被取消，不做任何处理
                    IsPreviewLoading = false;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            PreviewImageSource = null;
                            IsPreviewLoading = false;
                            await MessageBoxManager
                                .GetMessageBoxStandard("错误", $"加载图像预览失败: {ex.Message}")
                                .ShowAsPopupAsync(this);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("错误", $"处理预览图像时出错: {ex.Message}")
                    .ShowAsPopupAsync(this);
                
                IsPreviewLoading = false;
            }
            finally
            {
                _isUpdatingPreview = false; // 无论成功还是失败，都重置标志
            }
        }
        
        private async Task LoadOriginalImageSafe(string imagePath, CancellationToken cancellationToken)
        {
            // 在真正的后台线程中执行文件操作
            await Task.Run(async () =>
            {
                using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                memoryStream.Position = 0;
                
                // 在UI线程创建位图并更新UI
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = new Avalonia.Media.Imaging.Bitmap(memoryStream);
                        PreviewImageSource = bitmap;
                    }
                    finally
                    {
                        IsPreviewLoading = false;
                    }
                });
            }, cancellationToken);
        }
        
        private async Task GenerateProcessedPreviewSafe(ImageTask task, CancellationToken cancellationToken)
        {
            // 在真正的后台线程中执行所有处理操作
            await Task.Run(async () =>
            {
                // 处理图像
                Image? processedImage = null;
                try
                {
                    processedImage = await _imageProcessorService.ProcessImageAsync(task);
                    
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    // 将处理后的图像转换为内存流
                    using var memStream = new MemoryStream();
                    await processedImage.SaveAsPngAsync(memStream, cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    memStream.Position = 0;
                    
                    // 在UI线程创建位图并更新UI
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try 
                        {
                            var bitmap = new Avalonia.Media.Imaging.Bitmap(memStream);
                            PreviewImageSource = bitmap;
                        }
                        finally
                        {
                            IsPreviewLoading = false;
                        }
                    });
                }
                finally
                {
                    // 确保释放资源
                    processedImage?.Dispose();
                }
            }, cancellationToken);
        }
    }
}
