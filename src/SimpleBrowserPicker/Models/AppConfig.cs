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
    /// When true, the picker appears for every link that doesn't match a
    /// rule — the fallback browser is ignored. When false, unmatched links
    /// open silently in the fallback browser (if set).
    /// </summary>
    public bool AlwaysAsk { get; set; }

    /// <summary>
    /// When true, all rules are temporarily ignored — every link shows
    /// the picker (or goes to fallback depending on AlwaysAsk). Useful
    /// for debugging or reviewing URLs.
    /// </summary>
    public bool SuspendRules { get; set; }

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
    /// User overrides for detected browser names, args or visibility.
    /// </summary>
    public List<BrowserOverride> BrowserOverrides { get; set; } = new();

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

/// <summary>
/// User override for a detected browser's display name or arguments.
/// Keyed by the original exe path + original args.
/// </summary>
public class BrowserOverride
{
    /// <summary>Original exe path from detection.</summary>
    public string ExePath { get; set; } = string.Empty;

    /// <summary>Original profile args from detection.</summary>
    public string OriginalArgs { get; set; } = string.Empty;

    /// <summary>User-chosen display name (null = keep detected name).</summary>
    public string? NameOverride { get; set; }

    /// <summary>User-chosen extra args (null = keep detected args).</summary>
    public string? ArgsOverride { get; set; }

    /// <summary>If true, this browser is hidden from the picker.</summary>
    public bool Hidden { get; set; }
}
