using System.Linq;
using System.Text.Json.Serialization;

namespace ScaleBarOverlay.Models;

public class ImportConfig
{
    public ImportConfig()
    {
        Services.AppLogger.Info(nameof(ImportConfig), "ImportConfig instance created.");
    }

    [JsonIgnore]
    public MagnificationOption MagnificationOption
    {
        get => MagnificationOption.TemplateOptions.FirstOrDefault(r => r.Ratio == MagnificationRatio) ?? MagnificationOption.TemplateOptions[0];
        set => MagnificationRatio = value.Ratio;
    }

    public int MagnificationRatio { get; set; }

    [JsonIgnore]
    public string DestinationDirectory { get; set; } = string.Empty;

    public AlignmentOption Alignment { get; set; }
}