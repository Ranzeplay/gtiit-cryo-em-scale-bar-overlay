using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using SixLabors.ImageSharp;
using ScaleBarOverlay.Services;
using System.Threading;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DeadlockDetection;
using Image = SixLabors.ImageSharp.Image;

namespace ScaleBarOverlay
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ImageTask> _imageTasks = new();
        private readonly FileDialogService _fileDialogService;
        private ImageTask? _selectedImageTask;
        private bool _isOriginalPreview = true;
        private CancellationTokenSource? _previewCancellationTokenSource;
        private bool _isPreviewLoading;
        private int _scaleBarLeftMargin = 100;
        private int _scaleBarBottomMargin = 100;
        private bool _isUpdatingPreview;
        
        private static readonly string[] AllowedFileExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };

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
        public int ScaleBarLeftMargin
        {
            get => _scaleBarLeftMargin;
            set
            {
                if (_scaleBarLeftMargin == value) return;

                _scaleBarLeftMargin = value;
                OnPropertyChanged();

                // When the margin changes, if there is a selected task, update its margin and refresh the preview
                if (_selectedImageTask == null) return;

                _ = UpdatePreviewImage();
            }
        }

        public int ScaleBarBottomMargin
        {
            get => _scaleBarBottomMargin;
            set
            {
                if (_scaleBarBottomMargin == value) return;

                _scaleBarBottomMargin = value;
                OnPropertyChanged();

                // When the margin changes, if there is a selected task, update its margin and refresh the preview
                if (_selectedImageTask == null) return;

                _ = UpdatePreviewImage();
            }
        }

        public ImageTask? SelectedImageTask
        {
            get => _selectedImageTask;
            private set
            {
                if (_selectedImageTask == value) return;

                _selectedImageTask = value;
                OnPropertyChanged();

                // When selecting a new task, update the margin controller value
                if (value == null) return;

                OnPropertyChanged(nameof(ScaleBarLeftMargin));
            }
        }

        public bool IsOriginalPreview
        {
            get => _isOriginalPreview;
            set
            {
                if (_isOriginalPreview == value) return;

                _isOriginalPreview = value;
                OnPropertyChanged();
                _ = UpdatePreviewImage();
            }
        }

        public bool IsPreviewLoading
        {
            get => _isPreviewLoading;
            set
            {
                if (_isPreviewLoading == value) return;
                _isPreviewLoading = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initialize services
            _fileDialogService = new FileDialogService(this);
            
            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragEnterEvent, Drag);
            AddHandler(DragDrop.DragOverEvent, Drag);
        }
        
        private bool IsValidFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedFileExtensions.Contains(extension);
        }

        private void Drag(object? sender, DragEventArgs e)
        {
            var items = e.Data.GetFiles();
            if (items == null || !items.All(item => item is IStorageFile sf && IsValidFileExtension(sf.Name)))
            {
                // If the items are not files, we don't handle the drop
                e.DragEffects = DragDropEffects.None;
                e.Handled = false;
            }
            else
            {
                e.DragEffects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private async void Drop(object? sender, DragEventArgs e)
        {
            var items = e.Data.GetFiles();
            if (items == null || !items.All(item => item is IStorageFile sf && IsValidFileExtension(sf.Name)))
            {
                // If the items are not files, we don't handle the drop
                e.DragEffects = DragDropEffects.None;
                e.Handled = false;
            }
            else
            {
                e.DragEffects = DragDropEffects.Copy;
                e.Handled = true;

                await AddFiles(items.OfType<IStorageFile>().ToList());
            }
        }

        private async void OnAddClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
                {
                    var files = await _fileDialogService.OpenImageFilesAsync();
                    await AddFiles(files);
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error adding images: {ex.Message}")
                    .ShowAsPopupAsync(this);
            }
        }

        private async Task AddFiles(IReadOnlyList<IStorageFile> files)
        {
            try
            {
                using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
                {
                    if (files.Count == 0) return;

                    var magnificationSelectionDialog = new MagnificationSelectionDialog();
                    var magnificationChoice = await magnificationSelectionDialog.ShowDialog<(MagnificationOption, bool)?>(this);
                    if (magnificationChoice == null)
                    {
                        await MessageBoxManager
                            .GetMessageBoxStandard("Error", "No magnification option selected.")
                            .ShowAsPopupAsync(this);
                        return;
                    }

                    List<ImageTask> newTasks;
                    if (magnificationChoice.Value.Item2)
                    {
                        var destinationFolder = await _fileDialogService.OpenFolderAsync();
                        if (destinationFolder.Count == 0) return;

                        // Use the service to create tasks
                        newTasks = ImageTaskService.CreateImageTasks(files, magnificationChoice.Value.Item1, destinationFolder[0]);
                    }
                    else
                    {
                        // Use the service to create tasks without a destination folder
                        newTasks = ImageTaskService.CreateImageTasks(files, magnificationChoice.Value.Item1);
                    }
                    
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var task in newTasks)
                        {
                            ImageTasks.Add(task);
                        }
                    }).Wait();
                    
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error adding images: {ex.Message}")
                    .ShowAsPopupAsync(this);
            }
        }

        private async void OnProcessClicked(object? sender, RoutedEventArgs e)
        {
            // Process images in the background thread to avoid blocking the UI
            await Task.Run(async () =>
            {
                await ImageTaskService.ProcessAllTasksAsync(ImageTasks, _scaleBarLeftMargin, _scaleBarBottomMargin);
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

        private Bitmap? _previewImageSource;

        public Bitmap? PreviewImageSource
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
            if (sender is not DataGrid { SelectedItem: ImageTask selectedTask }) return;

            SelectedImageTask = selectedTask;
            // Update the margin to the selected task's margin value
            await UpdatePreviewImage();
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

                memoryStream.Seek(0, SeekOrigin.Begin);

                // Create bitmap and update UI on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        var bitmap = Bitmap.DecodeToWidth(memoryStream, 512, BitmapInterpolationMode.MediumQuality);
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
                    processedImage =
                        await ImageProcessorService.ProcessImageAsync(task, _scaleBarLeftMargin, _scaleBarBottomMargin, 512);

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
                            var bitmap = Bitmap.DecodeToWidth(memStream, 512, BitmapInterpolationMode.MediumQuality);
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

        private void OnClearClicked(object? sender, RoutedEventArgs e)
        {
            _imageTasks.Clear();
        }

        private async void OnResetOutputDirectoryClicked(object? sender, RoutedEventArgs e)
        {
            var destinationFolders = await _fileDialogService.OpenFolderAsync();
            if (destinationFolders.Count == 0) return;
            var destinationFolder = destinationFolders[0];

            var tasks = ImageTasks.Select(task =>
            {
                var fileName = Path.GetFileName(task.ImagePath);

                var outputName = $"{Path.GetFileNameWithoutExtension(fileName)}_scaleBar{Path.GetExtension(fileName)}";
                var outputPath = Path.Combine(destinationFolder.Path.AbsolutePath, outputName);

                task.OutputPath = outputPath;

                return task;
            });
            
            // Reset the output directory for all tasks
            ImageTasks = new ObservableCollection<ImageTask>(tasks);
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
        }
    }
}