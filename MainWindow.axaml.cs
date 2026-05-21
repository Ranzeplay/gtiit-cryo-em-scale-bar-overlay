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
using ScaleBarOverlay.Models;
using Image = SixLabors.ImageSharp.Image;

namespace ScaleBarOverlay
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<ImageTask> _imageTasks = new();
        private readonly FileDialogService _fileDialogService;
        private ImageTask? _selectedImageTask;
        private CancellationTokenSource? _previewCancellationTokenSource;
        private bool _isUpdatingPreview;

        private static readonly string[] AllowedFileExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };

        public IEnumerable<EdgeViewModel> EdgeOptions { get; } =
        [
            new() { CornerOption = CornerOption.TopLeft, DisplayName = "Top Left" },
            new() { CornerOption = CornerOption.TopRight, DisplayName = "Top Right" },
            new() { CornerOption = CornerOption.BottomLeft, DisplayName = "Bottom Left" },
            new() { CornerOption = CornerOption.BottomRight, DisplayName = "Bottom Right" }
        ];

        public ObservableCollection<ImageTask> ImageTasks
        {
            get => _imageTasks;
            set
            {
                _imageTasks = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
            }
        }

        public ScaleBarLocation? ScaleBarLocation { get; } = new(CornerOption.BottomLeft, 100, 100);

        public EdgeViewModel? SelectedEdge
        {
            get => ScaleBarLocation == null
                ? null
                : EdgeOptions.FirstOrDefault(e => e.CornerOption == ScaleBarLocation.CornerOption);
            set
            {
                if (value == null) return;
                if (ScaleBarLocation == null) return;

                ScaleBarLocation.CornerOption = value.CornerOption;
                
                _ = UpdatePreviewImage();
            }
        }

        public int ScaleBarVerticalOffset
        {
            get => ScaleBarLocation?.VerticalOffset ?? 0;
            set
            {
                if (ScaleBarLocation == null) return;

                ScaleBarLocation.VerticalOffset = value;
                OnPropertyChanged();
                
                _ = UpdatePreviewImage();
            }
        }

        public int ScaleBarHorizontalOffset
        {
            get => ScaleBarLocation?.HorizontalOffset ?? 0;
            set
            {
                if (ScaleBarLocation == null) return;

                ScaleBarLocation.HorizontalOffset = value;
                OnPropertyChanged();
                
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
            }
        }

        public bool IsOriginalPreview
        {
            get;
            set
            {
                if (field == value) return;

                field = value;
                OnPropertyChanged();
                _ = UpdatePreviewImage();
            }
        } = false;

        public bool IsPreviewLoading
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            AppLogger.Info(nameof(MainWindow), "Main window initialized.");

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
            var items = e.Data.GetFiles()?.ToList();
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
                AppLogger.Info(nameof(MainWindow), $"Drop event accepted with {items.Count} file(s).");

                await AddFiles(items.OfType<IStorageFile>().ToList());
            }
        }

        private async void OnAddClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                AppLogger.Info(nameof(MainWindow), "Add images button clicked.");
                using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
                {
                    var files = await _fileDialogService.OpenImageFilesAsync();
                    await AddFiles(files);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(nameof(MainWindow), "Failed while adding images.", ex);
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error adding images: {ex.Message}")
                    .ShowWindowDialogAsync(this);
            }
        }

        private async Task AddFiles(IReadOnlyList<IStorageFile> files)
        {
            try
            {
                using (Enable.DeadlockDetection(DeadlockDetectionMode.AlsoPotentialDeadlocks))
                {
                    if (files.Count == 0) return;
                    AppLogger.Info(nameof(MainWindow), $"Starting add workflow for {files.Count} file(s).");

                    var importOptionsDialog = new ImportOptionsDialog();
                    var importConfig = await importOptionsDialog.ShowDialog<ImportConfig?>(this);
                    if (importConfig == null)
                    {
                        AppLogger.Warn(nameof(MainWindow), "Import canceled because no magnification option was selected.");
                        await MessageBoxManager
                            .GetMessageBoxStandard("Error", "No magnification option selected.")
                            .ShowWindowDialogAsync(this);
                        return;
                    }

                    List<ImageTask> newTasks;
                    if (string.IsNullOrWhiteSpace(importConfig.DestinationDirectory))
                    {
                        // Use the service to create tasks without a destination folder
                        newTasks = ImageTaskService.CreateImageTasks(files, importConfig.MagnificationOption,
                            importConfig.Alignment);
                    }
                    else
                    {
                        // Use the service to create tasks
                        newTasks = ImageTaskService.CreateImageTasks(files, importConfig.MagnificationOption,
                            importConfig.Alignment, importConfig.DestinationDirectory);
                    }

                    int currentImageCount = ImageTasks.Count;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var task in newTasks)
                        {
                            ImageTasks.Add(task);
                        }

                        // Auto select the first newly added image
                        ImagesDataGrid.SelectedIndex = currentImageCount;
                    });

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
                    AppLogger.Info(nameof(MainWindow), $"Image task list now contains {ImageTasks.Count} item(s).");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(nameof(MainWindow), "Error while creating image tasks.", ex);
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error adding images: {ex.Message}")
                    .ShowAsPopupAsync(this);
            }
        }

        private async void OnProcessClicked(object? sender, RoutedEventArgs e)
        {
            AppLogger.Info(nameof(MainWindow), $"Processing requested for {ImageTasks.Count} task(s).");
            // Process images in the background thread to avoid blocking the UI
            await Task.Run(async () =>
            {
                var config = ConfigService.LoadConfig();
                config.ScaleBarLocation = ScaleBarLocation;
                ConfigService.SaveConfig(config);

                await ImageTaskService.ProcessAllTasksAsync(ImageTasks, ScaleBarLocation);
            });

            await MessageBoxManager
                .GetMessageBoxStandard("Complete", "All image processing tasks have been completed.")
                .ShowWindowAsync();

            var outputDirectories =
                ImageTasks.Select(task => Path.GetDirectoryName(task.OutputPath)).Distinct().ToList();
            if (outputDirectories.Count == 1)
            {
                // Open directory
                var outputDirectory = outputDirectories[0];
                if (outputDirectory != null)
                {
                    await Launcher.LaunchUriAsync(new Uri(outputDirectory));
                }
            }

            _imageTasks.Clear();
            AppLogger.Info(nameof(MainWindow), "Processing completed and task list cleared.");
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Bitmap? PreviewImageSource
        {
            get;
            set
            {
                if (field != value)
                {
                    field?.Dispose(); // Release old bitmap resources
                    field = value;
                    OnPropertyChanged();
                }
            }
        }

        private async void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid { SelectedItem: ImageTask selectedTask }) return;

            SelectedImageTask = selectedTask;
            AppLogger.Info(nameof(MainWindow), $"Selected image task changed to '{selectedTask.ImagePath}'.");
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
                AppLogger.Info(nameof(MainWindow), $"Updating preview for '{_selectedImageTask.ImagePath}'.");
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
                    AppLogger.Info(nameof(MainWindow), "Preview update canceled.");
                    IsPreviewLoading = false;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        AppLogger.Error(nameof(MainWindow), "Failed while generating preview image.", ex);
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            PreviewImageSource = null;
                            IsPreviewLoading = false;
                            await MessageBoxManager
                                .GetMessageBoxStandard("Error", $"Failed to load image preview: {ex.Message}")
                                .ShowWindowDialogAsync(this);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(nameof(MainWindow), "Unexpected error while updating preview image.", ex);
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Error processing preview image: {ex.Message}")
                    .ShowWindowDialogAsync(this);

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
                        await ImageProcessorService.ProcessImageAsync(task, ScaleBarLocation, 512);

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
            AppLogger.Info(nameof(MainWindow), "Clear image list clicked.");
            _imageTasks.Clear();
        }

        private async void OnResetOutputDirectoryClicked(object? sender, RoutedEventArgs e)
        {
            AppLogger.Info(nameof(MainWindow), "Reset output directory clicked.");
            var destinationFolders = await _fileDialogService.OpenFolderAsync();
            if (destinationFolders.Count == 0) return;
            var destinationFolder = destinationFolders[0];

            var tasks = ImageTasks.Select(task =>
            {
                var fileName = Path.GetFileName(task.ImagePath);

                var outputName = $"{Path.GetFileNameWithoutExtension(fileName)}_ScaleBar{Path.GetExtension(fileName)}";
                var outputPath = Path.Combine(destinationFolder.Path.AbsolutePath, outputName);

                task.OutputPath = outputPath;

                return task;
            });

            // Reset the output directory for all tasks
            ImageTasks = new ObservableCollection<ImageTask>(tasks);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageTasks)));
            AppLogger.Info(nameof(MainWindow), "Output directory updated for all current tasks.");
        }

        private async void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is ImageTask selectedTask)
            {
                var selectedColumn = dataGrid.CurrentColumn;
                if (selectedColumn != null)
                {
                    if (selectedColumn.Header.ToString() == "Output Path")
                    {
                        // Open file save dialog
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var saveFile = await _fileDialogService.SaveFile(
                                "Choose Output File",
                                Path.GetFileName(selectedTask.OutputPath));

                            if (saveFile != null)
                            {
                                selectedTask.OutputPath = saveFile.Path.LocalPath;
                            }
                        });
                    }
                    else if (selectedColumn.Header.ToString() == "Image Path")
                    {
                        // Open image
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            if (File.Exists(selectedTask.ImagePath))
                            {
                                await Launcher.LaunchUriAsync(new Uri(selectedTask.ImagePath));
                            }
                            else
                            {
                                await MessageBoxManager
                                    .GetMessageBoxStandard("Error", "Image file does not exist.")
                                    .ShowWindowDialogAsync(this);
                            }
                        });
                    }
                    else
                    {
                        var importOptionsDialog = new ImportOptionsDialog(new ImportConfig
                        {
                            MagnificationOption = selectedTask.Magnification,
                            Alignment = selectedTask.AlignmentOption,
                            DestinationDirectory = Path.GetDirectoryName(selectedTask.OutputPath) ?? ""
                        });
                        var importConfig = await importOptionsDialog.ShowDialog<ImportConfig?>(this);
                        if (importConfig == null) return;

                        selectedTask.Magnification = importConfig.MagnificationOption;
                        selectedTask.AlignmentOption = importConfig.Alignment;

                        await UpdatePreviewImage();
                    }
                }
            }
        }

        private async void ScaleBarEdgeLocation_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            await UpdatePreviewImage();
        }
    }
}