using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScaleBarOverlay;

public class ImageTask(string imagePath, MagnificationOption magnification, string outputPath) : INotifyPropertyChanged
{
    private string _outputPath = outputPath;
    public string ImagePath { get; set; } = imagePath;

    public MagnificationOption Magnification { get; } = magnification;

    public string OutputPath
    {
        get => _outputPath;
        set => SetField(ref _outputPath, value);
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
        OnPropertyChanged(propertyName);
        return true;
    }
}

