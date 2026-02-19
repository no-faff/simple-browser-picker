using System.IO;
using System.Text.Json;
using SimpleBrowserPicker.Models;

namespace SimpleBrowserPicker.Services;

/// <summary>
/// Loads and saves the application configuration from/to
/// <c>%LOCALAPPDATA%\SimpleBrowserPicker\config.json</c>.
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "SimpleBrowserPicker");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Loads config from disk. Returns a fresh default config if the file
    /// does not exist or cannot be parsed.
    /// </summary>
    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    /// <summary>
    /// Saves config to disk, creating the directory if needed.
    /// </summary>
    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    /// <summary>Whether a config file already exists on disk.</summary>
    public bool ConfigExists() => File.Exists(ConfigPath);
}
