using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace ScaleBarOverlay;

public partial class MagnificationSelectionDialog : Window
{
    public List<MagnificationOption> Options { get; } =
    [
        new(11, 13.3, 500),
        new(45, 3.1, 100),
        new(57, 2.5, 100),
        new(73, 1.9, 100),
        new(92, 1.5, 100),
        new(120, 1.2, 100),
        new(150, 0.95, 50),
        new(190, 0.74, 50),
        new(240, 0.58, 50)
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
    
    private void OnOkClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SelectedOption = OptionsComboBox.SelectedItem as MagnificationOption ?? throw new InvalidOperationException("No option selected.");
        Close(SelectedOption);
    }

    private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}

