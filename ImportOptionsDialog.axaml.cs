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
    
    public class AlignmentViewModel(AlignmentOption alignment, string displayName)
    {
        public AlignmentOption AlignmentOption { get; set; } = alignment;
        
        public string DisplayName { get; set; } = displayName;
    }
    
    public ImportOptionsDialog()
    {
        InitializeComponent();
        DataContext = this;
        
        var initialConfig = ConfigService.LoadConfig().ImportConfig;
        DestinationPathTextBox.Text = initialConfig.DestinationDirectory;
        
        OptionsComboBox.ItemsSource = Options;
        OptionsComboBox.SelectedIndex = Options.ToList().FindIndex(o => o.Ratio.Equals(initialConfig.MagnificationOption.Ratio));
        SelectedOption = Options.ElementAt(OptionsComboBox.SelectedIndex);
        
        ScaleTextAlignmentComboBox.SelectedIndex = AlignmentViewModels.ToList().FindIndex(o => o.AlignmentOption.Equals(initialConfig.Alignment));
    }
    
    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        
        var state = new ImportConfig
        {
            MagnificationOption = SelectedOption,
            DestinationDirectory = DestinationPathTextBox.Text,
            Alignment = ScaleTextAlignmentComboBox.SelectionBoxItem as AlignmentViewModel is { } vm
                ? vm.AlignmentOption
                : AlignmentOption.Left
        };

        var config = ConfigService.LoadConfig();
        config.ImportConfig = state;
        ConfigService.SaveConfig(config);
        
        Close(state);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private async void SelectDestinationDirectoryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var destinationFolder = await new FileDialogService(this).OpenFolderAsync();

        if (destinationFolder.Count > 0)
        {
            DestinationPathTextBox.Text = destinationFolder[0].Path.LocalPath;
        }
    }
}

