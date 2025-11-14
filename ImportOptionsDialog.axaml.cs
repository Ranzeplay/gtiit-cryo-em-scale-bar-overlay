using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScaleBarOverlay.Services;

namespace ScaleBarOverlay;

public partial class ImportOptionsDialog : Window
{
    public IEnumerable<MagnificationOption> Options => MagnificationOption.TemplateOptions;
    
    public MagnificationOption SelectedOption { get; set; }

    public IEnumerable<AlignmentViewModel> AlignmentViewModels =>
    [
        new(ImportConfig.AlignmentOption.Left, "Left"),
        new(ImportConfig.AlignmentOption.Center, "Center"),
        new(ImportConfig.AlignmentOption.Right, "Right")
    ];
    
    public class AlignmentViewModel(ImportConfig.AlignmentOption alignment, string displayName)
    {
        public ImportConfig.AlignmentOption AlignmentOption { get; set; } = alignment;
        
        public string DisplayName { get; set; } = displayName;
    }
    
    public ImportOptionsDialog()
    {
        InitializeComponent();
        DataContext = this;
        OptionsComboBox.ItemsSource = Options;
        OptionsComboBox.SelectedIndex = 0;
        SelectedOption = Options.ElementAt(0);
    }
    
    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        Close(new ImportConfig
        {
            MagnificationOption = SelectedOption,
            DestinationDirectory = DestinationPathTextBox.Text,
            Alignment = ScaleTextAlignmentComboBox.SelectionBoxItem as AlignmentViewModel is AlignmentViewModel vm
                ? vm.AlignmentOption
                : ImportConfig.AlignmentOption.Left
        });
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

