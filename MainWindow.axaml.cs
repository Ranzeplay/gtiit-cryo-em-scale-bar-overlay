using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using SixLabors.ImageSharp;
using ScaleBarOverlay.Services;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using DeadlockDetection;
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
        private int _scaleBarMargin = 50;  // Newly added margin property, default is 50
        private bool _isUpdatingPreview = false; // Added flag to prevent concurrent updates

        public ObservableCollection<ImageTask> ImageTasks
        {
            get => _imageTasks;
            set
            {
                _imageTasks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
            }
        }
        
        // Newly added ScaleBarMargin property
        public int ScaleBarMargin
        {
            get => _scaleBarMargin;
            set
            {
                if (_scaleBarMargin != value)
                {
                    _scaleBarMargin = value;
                    OnPropertyChanged();
                    
                    // When the margin changes, if there is a selected task, update its margin and refresh the preview
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
                    
                    // When selecting a new task, update the margin controller value
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
            
            // Initialize services
            _fileDialogService = new FileDialogService(this);
            _imageProcessorService = new ImageProcessorService();
            _imageTaskService = new ImageTaskService(_imageProcessorService);
        }

        private async void OnAddClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
                {
                    var files = await _fileDialogService.OpenImageFilesAsync();
                    if (files.Count == 0) return;

                    var magnificationSelectionDialog = new MagnificationSelectionDialog();
                    var magnificationChoice = await magnificationSelectionDialog.ShowDialog<MagnificationOption?>(this);
                    if (magnificationChoice == null)
                    {
                        await MessageBoxManager
                            .GetMessageBoxStandard("Error", "No magnification option selected.")
                            .ShowAsPopupAsync(this);
                        return;
                    }

                    var destinationFolder = await _fileDialogService.OpenFolderAsync();
                    if (destinationFolder.Count == 0) return;

                    // Use the service to create tasks
                    var newTasks = _imageTaskService.CreateImageTasks(files, magnificationChoice, destinationFolder[0]);
                    foreach (var task in newTasks)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ImageTasks.Add(task);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error adding images: {ex.Message}")
                    .ShowAsPopupAsync(this);
            }
        }

        private async void OnRunClicked(object? sender, RoutedEventArgs e)
        {
            // Process images in the background thread to avoid blocking the UI
            await Task.Run(async () =>
            {
                await _imageTaskService.ProcessAllTasksAsync(ImageTasks);
            });

            await MessageBoxManager
                .GetMessageBoxStandard("Complete", "All image processing tasks have been completed.")
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
                    _previewImageSource?.Dispose(); // Release old bitmap resources
                    _previewImageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid { SelectedItem: ImageTask selectedTask })
            {
                SelectedImageTask = selectedTask;
                // Update the margin to the selected task's margin value
                ScaleBarMargin = selectedTask.ScaleBarMargin;
                await UpdatePreviewImage();
            }
        }
        
        private async Task UpdatePreviewImage()
        {
            // If already updating the preview image, skip this call
            if (_isUpdatingPreview || _selectedImageTask == null)
            {
                if (_selectedImageTask == null)
                    PreviewImageSource = null;
                return;
            }

            try
            {
                _isUpdatingPreview = true; // Set flag indicating preview is being updated
                
                // Cancel any ongoing preview operations
                _previewCancellationTokenSource?.Cancel();
                _previewCancellationTokenSource?.Dispose();
                _previewCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _previewCancellationTokenSource.Token;
                
                // Set loading flag
                IsPreviewLoading = true;
                
                // Use await to handle delay instead of Task.ContinueWith
                try
                {
                    // Short delay to avoid frequent updates
                    await Task.Delay(150, cancellationToken);
                    
                    // Check if file exists
                    if (!File.Exists(_selectedImageTask.ImagePath))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => 
                        {
                            PreviewImageSource = null;
                            IsPreviewLoading = false;
                        });
                        return;
                    }

                    // Process image based on preview mode
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
                    // Preview was canceled, do nothing
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
                                .GetMessageBoxStandard("Error", $"Failed to load image preview: {ex.Message}")
                                .ShowAsPopupAsync(this);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error processing preview image: {ex.Message}")
                    .ShowAsPopupAsync(this);
                
                IsPreviewLoading = false;
            }
            finally
            {
                _isUpdatingPreview = false; // Reset flag regardless of success or failure
            }
        }
        
        private async Task LoadOriginalImageSafe(string imagePath, CancellationToken cancellationToken)
        {
            // Perform file operations on a true background thread
            await Task.Run(async () =>
            {
                await using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                memoryStream.Position = 0;
                
                // Create bitmap and update UI on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = new Bitmap(memoryStream).CreateScaledBitmap(new PixelSize(256, 256));
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
            // Perform all processing operations on a true background thread
            await Task.Run(async () =>
            {
                // Process image
                Image? processedImage = null;
                try
                {
                    processedImage = await _imageProcessorService.ProcessImageAsync(task);
                    
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    // Convert processed image to memory stream
                    using var memStream = new MemoryStream();
                    await processedImage.SaveAsPngAsync(memStream, cancellationToken);
                    
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    memStream.Position = 0;
                    
                    // Create bitmap and update UI on the UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try 
                        {
                            var bitmap = new Bitmap(memStream).CreateScaledBitmap(new PixelSize(256, 256));
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
                    // Ensure resources are released
                    processedImage?.Dispose();
                }
            }, cancellationToken);
        }
    }
}
