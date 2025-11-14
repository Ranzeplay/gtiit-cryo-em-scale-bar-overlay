using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScaleBarOverlay.Models;

public class ImageTask(string imagePath, MagnificationOption magnification, string outputPath, AlignmentOption alignmentOption) : INotifyPropertyChanged
{
    private string _outputPath = outputPath;
    private MagnificationOption _magnification = magnification;
    private AlignmentOption _alignmentOption = alignmentOption;
    public string ImagePath { get; set; } = imagePath;
    
    public string ImageName => System.IO.Path.GetFileName(ImagePath);

    public MagnificationOption Magnification
    {
        get => _magnification;
        set => SetField(ref _magnification, value);
    }

    public AlignmentOption AlignmentOption
    {
        get => _alignmentOption;
        set => SetField(ref _alignmentOption, value);
    }

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

