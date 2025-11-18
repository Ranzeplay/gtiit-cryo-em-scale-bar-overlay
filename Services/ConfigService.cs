using System;
using System.IO;
using System.Text.Json;
using ScaleBarOverlay.Models;
using ScaleBarOverlay.Serialization;

namespace ScaleBarOverlay.Services;

public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
    
    public static void SaveConfig(AppConfig config)
    {
        // Use the JsonSerializer overload that accepts a JsonTypeInfo to ensure the source-generated metadata is used.
        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        File.WriteAllText(ConfigPath, json);
    }

    public static AppConfig LoadConfig()
    {
        var defaultConfig = new AppConfig
        {
            ImportConfig = new ImportConfig()
            {
                MagnificationOption = MagnificationOption.TemplateOptions[0],
                DestinationDirectory = "",
                Alignment = AlignmentOption.Center
            },
            ScaleBarLeftMargin = 100,
            ScaleBarBottomMargin = 100
        };
        
        if (!File.Exists(ConfigPath))
        {
            return defaultConfig;
        }

        var json = File.ReadAllText(ConfigPath);
        // Use the JsonSerializer overload that accepts a JsonTypeInfo to ensure the source-generated metadata is used.
        return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? defaultConfig;
    }
}