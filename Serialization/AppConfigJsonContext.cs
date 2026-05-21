using System.Text.Json.Serialization;
using ScaleBarOverlay.Models;

namespace ScaleBarOverlay.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ImportConfig))]
[JsonSerializable(typeof(ScaleBarLocation))]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
    static AppConfigJsonContext()
    {
        Services.AppLogger.Info(nameof(AppConfigJsonContext), "JSON serialization context initialized.");
    }

    // Source-generated implementation will be provided by System.Text.Json at compile time.
}
