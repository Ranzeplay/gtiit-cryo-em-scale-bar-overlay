using System.Linq;
using System.Text.Json.Serialization;

namespace ScaleBarOverlay.Models;

public class ImportConfig
{
    [JsonIgnore]
    public MagnificationOption MagnificationOption
    {
        get => MagnificationOption.TemplateOptions.FirstOrDefault(r => r.Ratio == MagnificationRatio) ?? MagnificationOption.TemplateOptions[0];
        set => MagnificationRatio = value.Ratio;
    }

    public int MagnificationRatio { get; set; }

    public string DestinationDirectory { get; set; } = string.Empty;

    public AlignmentOption Alignment { get; set; }
}