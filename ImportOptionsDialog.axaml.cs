using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScaleBarOverlay.Models;
using ScaleBarOverlay.Services;

namespace ScaleBarOverlay;

public partial class ImportOptionsDialog : Window
{
    public IEnumerable<MagnificationOption> Options => MagnificationOption.TemplateOptions;
    
    public MagnificationOption SelectedOption { get; set; }

    public static IEnumerable<AlignmentViewModel> AlignmentViewModels =>
    [
        new(AlignmentOption.Left, "Left"),
        new(AlignmentOption.Center, "Center"),
        new(AlignmentOption.Right, "Right")
    ];
    
    public class AlignmentViewModel
    {
        public AlignmentViewModel(AlignmentOption alignment, string displayName)
        {
            AlignmentOption = alignment;
            DisplayName = displayName;
            Services.AppLogger.Info(nameof(AlignmentViewModel),
                $"Alignment option created: '{DisplayName}'.");
        }

        public AlignmentOption AlignmentOption { get; set; }
        
        public string DisplayName { get; set; }
    }
    
    public ImportOptionsDialog(ImportConfig? config = null)
    {
        Services.AppLogger.Info(nameof(ImportOptionsDialog), "Import options dialog opening.");
        InitializeComponent();
        DataContext = this;
        
        var initialConfig = config ?? ConfigService.LoadConfig().ImportConfig;
        DestinationPathTextBox.Text = initialConfig.DestinationDirectory;
        
        OptionsComboBox.ItemsSource = Options;
        OptionsComboBox.SelectedIndex = Options.ToList().FindIndex(o => o.Ratio.Equals(initialConfig.MagnificationOption.Ratio));
        SelectedOption = Options.ElementAt(OptionsComboBox.SelectedIndex);
        
        ScaleTextAlignmentComboBox.SelectedIndex = AlignmentViewModels.ToList().FindIndex(o => o.AlignmentOption.Equals(initialConfig.Alignment));
    }
    
    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        Services.AppLogger.Info(nameof(ImportOptionsDialog), "Import options confirmed by user.");
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        
        var state = new ImportConfig
        {
            MagnificationOption = SelectedOption,
            DestinationDirectory = DestinationPathTextBox.Text ?? "",
            Alignment = ScaleTextAlignmentComboBox.SelectionBoxItem as AlignmentViewModel is { } vm
                ? vm.AlignmentOption
                : AlignmentOption.Center
        };

        var config = ConfigService.LoadConfig();
        config.ImportConfig = state;
        ConfigService.SaveConfig(config);
        
        Close(state);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Services.AppLogger.Info(nameof(ImportOptionsDialog), "Import options dialog canceled by user.");
        Close(null);
    }

    private async void SelectDestinationDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Services.AppLogger.Info(nameof(ImportOptionsDialog), "Selecting destination folder.");
        var destinationFolder = await new FileDialogService(this).OpenFolderAsync();

        if (destinationFolder.Count > 0)
        {
            DestinationPathTextBox.Text = destinationFolder[0].Path.LocalPath;
            Services.AppLogger.Info(nameof(ImportOptionsDialog),
                $"Destination folder selected: '{DestinationPathTextBox.Text}'.");
        }
    }
}

