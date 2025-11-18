using System.Text.Json.Serialization;
using ScaleBarOverlay.Models;

namespace ScaleBarOverlay.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
    // Source-generated implementation will be provided by System.Text.Json at compile time.
}

