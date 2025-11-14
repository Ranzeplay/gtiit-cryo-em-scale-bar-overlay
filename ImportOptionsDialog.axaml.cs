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
            DestinationDirectory = DestinationPathTextBox.Text
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

