using System.Windows.Media;

namespace SimpleBrowserPicker.Models;

/// <summary>
/// Represents a single browser or browser profile entry shown in the picker.
/// </summary>
public class Browser
{
    /// <summary>Display name, e.g. "Vivaldi - Work".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Full path to the browser executable.</summary>
    public string ExePath { get; set; } = string.Empty;

    /// <summary>
    /// Additional command-line arguments prepended before the URL, e.g.
    /// <c>--profile-directory="Profile 1"</c> for Chromium profiles or
    /// <c>-P "Work"</c> for Firefox profiles.
    /// </summary>
    public string AdditionalArgs { get; set; } = string.Empty;

    /// <summary>
    /// Icon image source extracted from the executable.
    /// Null if extraction failed.
    /// </summary>
    public ImageSource? Icon { get; set; }

    /// <summary>
    /// Whether this entry was added manually by the user (not auto-detected).
    /// </summary>
    public bool IsCustom { get; set; }

    /// <summary>
    /// Keyboard shortcut label ("1"–"9") or empty if none.
    /// Set by the picker view model, not by the detector.
    /// </summary>
    public string ShortcutLabel { get; set; } = string.Empty;

    public override string ToString() => Name;
}
