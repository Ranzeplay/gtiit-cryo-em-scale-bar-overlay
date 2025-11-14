namespace ScaleBarOverlay;

public class ImportConfig
{
    public MagnificationOption MagnificationOption { get; set; }
    
    public string DestinationDirectory { get; set; }
    
    public enum AlignmentOption
    {
        Left,
        Center,
        Right
    }
    
    public AlignmentOption Alignment { get; set; }
}