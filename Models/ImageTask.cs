using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScaleBarOverlay.Models;

public class ImageTask : INotifyPropertyChanged
{
    public ImageTask(string imagePath, MagnificationOption magnification, string outputPath, AlignmentOption alignmentOption)
    {
        ImagePath = Uri.UnescapeDataString(imagePath);
        Magnification = magnification;
        OutputPath = Uri.UnescapeDataString(outputPath);
        AlignmentOption = alignmentOption;

        Services.AppLogger.Info(nameof(ImageTask), $"Created task for '{ImagePath}' -> '{OutputPath}'.");
    }

    public string ImagePath { get; }
    
    public string ImageName => System.IO.Path.GetFileName(ImagePath);

    public MagnificationOption Magnification
    {
        get;
        set => SetField(ref field, value);
    }

    public AlignmentOption AlignmentOption
    {
        get;
        set => SetField(ref field, value);
    }

    public string OutputPath
    {
        get;
        set => SetField(ref field, Uri.UnescapeDataString(value));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Services.AppLogger.Info(nameof(ImageTask), $"Property '{propertyName}' updated for '{ImagePath}'.");
        OnPropertyChanged(propertyName);
        return true;
    }
}

