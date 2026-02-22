namespace SimpleBrowserPicker.Models;

/// <summary>
/// Maps a domain pattern to a specific browser/profile.
/// Stored in config and evaluated before showing the picker.
/// </summary>
public class BrowserRule
{
    /// <summary>
    /// URL pattern matched against the full URL (domain + optional path).
    /// Field is named Domain for JSON backward compatibility.
    /// Examples:
    ///   "github.com"          — matches github.com and www.github.com
    ///   "*.google.com"        — matches any subdomain of google.com
    ///   "github.com/gist"     — matches any URL under /gist (prefix)
    ///   "github.com/gist/*"   — matches any URL under /gist (wildcard)
    ///   "*.corp.com/internal" — subdomain wildcard + path prefix
    /// Evaluated by <see cref="Services.UrlParser.UrlMatches"/>.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Full path to the browser executable.</summary>
    public string BrowserExePath { get; set; } = string.Empty;

    /// <summary>Display name used in the rules list UI.</summary>
    public string BrowserName { get; set; } = string.Empty;

    /// <summary>
    /// Profile arguments, e.g. <c>--profile-directory="Profile 1"</c>.
    /// Empty string for browsers without profile support.
    /// </summary>
    public string ProfileArgs { get; set; } = string.Empty;
}
