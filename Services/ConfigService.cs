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
        AppLogger.Info(nameof(ConfigService), $"Saving config to '{ConfigPath}'.");
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
            ScaleBarLocation = new ScaleBarLocation(CornerOption.BottomLeft, 100, 100)
        };
        
        if (!File.Exists(ConfigPath))
        {
            AppLogger.Warn(nameof(ConfigService), $"Config file not found at '{ConfigPath}'. Using defaults.");
            return defaultConfig;
        }

        AppLogger.Info(nameof(ConfigService), $"Loading config from '{ConfigPath}'.");
        var json = File.ReadAllText(ConfigPath);
        // Use the JsonSerializer overload that accepts a JsonTypeInfo to ensure the source-generated metadata is used.
        var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? defaultConfig;
        AppLogger.Info(nameof(ConfigService), "Config loaded.");
        return config;
    }
}