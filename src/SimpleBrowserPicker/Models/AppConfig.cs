namespace SimpleBrowserPicker.Models;

/// <summary>
/// Root configuration model serialised to
/// <c>%LOCALAPPDATA%\SimpleBrowserPicker\config.json</c>.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Whether first-run setup has been completed.
    /// </summary>
    public bool SetupComplete { get; set; }

    /// <summary>
    /// Manually added custom browser entries. Auto-detected browsers are not
    /// persisted here — they are re-scanned at startup and cached in memory.
    /// </summary>
    public List<CustomBrowserEntry> CustomBrowsers { get; set; } = new();

    /// <summary>
    /// Domain → browser routing rules.
    /// </summary>
    public List<BrowserRule> Rules { get; set; } = new();

    /// <summary>
    /// Exe path of the fallback browser used when no rule matches.
    /// </summary>
    public string? FallbackBrowserExePath { get; set; }

    /// <summary>
    /// Display name of the fallback browser (e.g. "Vivaldi – Personal").
    /// </summary>
    public string? FallbackBrowserName { get; set; }

    /// <summary>
    /// Profile arguments for the fallback browser, if any.
    /// </summary>
    public string? FallbackProfileArgs { get; set; }
}

/// <summary>
/// A custom browser entry added by the user (name + exe + optional args).
/// </summary>
public class CustomBrowserEntry
{
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string AdditionalArgs { get; set; } = string.Empty;
}
