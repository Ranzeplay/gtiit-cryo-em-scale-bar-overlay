using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScaleBarOverlay;

public partial class MagnificationSelectionDialog : Window
{
    public List<MagnificationOption> Options { get; } =
    [
        new(11, 13.3f, 500),
        new(36, 3.96f, 100),
        new(45, 3.17f, 100),
        new(57, 2.5f, 100),
        new(73, 1.9f, 100),
        new(92, 1.5f, 50),
        new(120, 1.2f, 50),
        new(150, 0.95f, 50),
        new(190, 0.74f, 50),
        new(240, 0.58f, 50)
    ];
    
    public MagnificationOption SelectedOption { get; set; }
    
    public MagnificationSelectionDialog()
    {
        InitializeComponent();
        DataContext = this;
        OptionsComboBox.ItemsSource = Options;
        OptionsComboBox.SelectedIndex = 0;
        SelectedOption = Options[0];
    }
    
    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        Close((SelectedOption, true));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnOkWithSameFolderClicked(object? sender, RoutedEventArgs e)
    {
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        Close((SelectedOption, false));
    }
}

