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

    private static readonly string _configPath = Path.Combine(ConfigDir, "config.json");

    /// <summary>Full path to the config file on disk.</summary>
    public string ConfigPath => _configPath;

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
        if (!File.Exists(_configPath))
            return new AppConfig();

        try
        {
            string json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            App.LogException(ex);
            return new AppConfig();
        }
    }

    /// <summary>
    /// Saves config to disk, creating the directory if needed.
    /// Writes to a temp file first, then renames, so a crash mid-write
    /// can't corrupt the config.
    /// </summary>
    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        string json = JsonSerializer.Serialize(config, JsonOptions);

        string tempPath = _configPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _configPath, overwrite: true);
    }

    /// <summary>Whether a config file already exists on disk.</summary>
    public bool ConfigExists() => File.Exists(_configPath);
}
