using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SimpleBrowserPicker.Models;

namespace SimpleBrowserPicker.Services;

/// <summary>
/// Discovers installed browsers and their profiles from the Windows registry
/// and browser-specific profile directories.
/// </summary>
public class BrowserDetector
{
    // Registry hives/paths to scan for installed browsers
    private static readonly (RegistryHive Hive, string Path)[] BrowserRegistryPaths =
    [
        (RegistryHive.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet"),
        (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet"),
        (RegistryHive.CurrentUser,  @"SOFTWARE\Clients\StartMenuInternet"),
    ];

    // Known Chromium-based browser data directories relative to %LOCALAPPDATA%
    private static readonly Dictionary<string, string> ChromiumDataDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome.exe"]  = @"Google\Chrome\User Data",
        ["msedge.exe"]  = @"Microsoft\Edge\User Data",
        ["brave.exe"]   = @"BraveSoftware\Brave-Browser\User Data",
        ["vivaldi.exe"] = @"Vivaldi\User Data",
        ["opera.exe"]   = @"Opera Software\Opera Stable",
        ["arc.exe"]     = @"Arc\User Data",
    };

    // Known Firefox-based browser profile directories relative to %APPDATA%
    // Each exe maps to one or more possible profile roots (checked in order)
    private static readonly Dictionary<string, string[]> FirefoxDataDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firefox.exe"]         = [@"Mozilla\Firefox"],
        ["floorp.exe"]          = ["Floorp"],
        ["zen.exe"]             = ["zen", "Zen Browser"],
        ["librewolf.exe"]       = ["librewolf"],
        ["waterfox.exe"]        = ["Waterfox"],
        ["mullvad-browser.exe"] = [@"Mullvad Browser"],
    };

    private readonly string _localAppData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _appData =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    /// <summary>
    /// Scans the registry and profile directories for all browsers/profiles.
    /// Returns a deduplicated, sorted list.
    /// </summary>
    public List<Browser> Detect()
    {
        var browsers = new List<Browser>();

        // First pass: collect base browser entries from registry
        var baseBrowsers = ScanRegistry();

        foreach (var (name, exePath) in baseBrowsers)
        {
            if (!File.Exists(exePath))
                continue;

            string exe = Path.GetFileName(exePath).ToLowerInvariant();
            bool addedProfiles = false;

            // Try Chromium profile expansion
            if (ChromiumDataDirs.TryGetValue(exe, out string? dataSubDir))
            {
                var chromiumProfiles = ReadChromiumProfiles(name, exePath, dataSubDir);
                if (chromiumProfiles.Count > 0)
                {
                    browsers.AddRange(chromiumProfiles);
                    addedProfiles = true;
                }
            }

            // Try Firefox profile expansion
            if (!addedProfiles && FirefoxDataDirs.TryGetValue(exe, out string[]? profileRoots))
            {
                var ffProfiles = ReadFirefoxProfiles(name, exePath, profileRoots);
                if (ffProfiles.Count > 0)
                {
                    browsers.AddRange(ffProfiles);
                    addedProfiles = true;
                }
            }

            // Fall back to plain entry (no profile support or single default)
            if (!addedProfiles)
            {
                browsers.Add(new Browser
                {
                    Name    = name,
                    ExePath = exePath,
                    Icon    = ExtractIcon(exePath),
                });
            }
        }

        return browsers;
    }

    // -----------------------------------------------------------------------
    // Registry scanning
    // -----------------------------------------------------------------------

    private List<(string Name, string ExePath)> ScanRegistry()
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, path) in BrowserRegistryPaths)
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using RegistryKey? browsersKey = baseKey.OpenSubKey(path);
            if (browsersKey == null) continue;

            foreach (string subkeyName in browsersKey.GetSubKeyNames())
            {
                using RegistryKey? entry = browsersKey.OpenSubKey(subkeyName);
                if (entry == null) continue;

                string? displayName = entry.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = subkeyName;

                using RegistryKey? commandKey = entry.OpenSubKey(@"shell\open\command");
                string? commandValue = commandKey?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(commandValue)) continue;

                string exePath = ParseExePath(commandValue);
                if (string.IsNullOrWhiteSpace(exePath)) continue;

                // Deduplicate by exe path; keep the first found display name
                if (!results.ContainsKey(exePath))
                    results[exePath] = displayName;
            }
        }

        return results.Select(kv => (kv.Value, kv.Key)).ToList();
    }

    /// <summary>
    /// Extracts the exe path from a registry command string.
    /// Handles both quoted ("C:\foo\bar.exe" "%1") and unquoted paths
    /// with spaces (C:\Program Files\Google\Chrome\Application\chrome.exe "%1").
    /// </summary>
    private static string ParseExePath(string command)
    {
        command = command.Trim();
        if (command.StartsWith('"'))
        {
            int end = command.IndexOf('"', 1);
            if (end > 1)
                return command[1..end];
        }

        // No quotes — look for .exe and take everything up to and including it
        int exePos = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exePos > 0)
            return command[..(exePos + 4)];

        // Last resort — take everything up to the first space
        int space = command.IndexOf(' ');
        return space > 0 ? command[..space] : command;
    }

    // -----------------------------------------------------------------------
    // Chromium profiles
    // -----------------------------------------------------------------------

    private List<Browser> ReadChromiumProfiles(string browserName, string exePath, string dataSubDir)
    {
        string localStateFile = Path.Combine(_localAppData, dataSubDir, "Local State");
        if (!File.Exists(localStateFile))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(localStateFile));
            if (!doc.RootElement.TryGetProperty("profile", out var profileEl) ||
                !profileEl.TryGetProperty("info_cache", out var infoCache))
                return [];

            // First pass: collect raw profile data, skipping ephemeral/internal profiles
            var raw = new List<(string FolderName, string DisplayName)>();
            foreach (var profileEntry in infoCache.EnumerateObject())
            {
                string folderName = profileEntry.Name;

                // Skip guest and system profiles — they're ephemeral or internal
                if (folderName.StartsWith("Guest Profile", StringComparison.OrdinalIgnoreCase) ||
                    folderName.Equals("System Profile", StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = profileEntry.Value;

                string profileDisplayName = string.Empty;
                if (info.TryGetProperty("shortcut_name", out var shortcut) &&
                    shortcut.ValueKind == JsonValueKind.String)
                    profileDisplayName = shortcut.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(profileDisplayName) &&
                    info.TryGetProperty("name", out var nameProp) &&
                    nameProp.ValueKind == JsonValueKind.String)
                    profileDisplayName = nameProp.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(profileDisplayName))
                    profileDisplayName = folderName;

                raw.Add((folderName, profileDisplayName));
            }

            if (raw.Count == 0)
                return [];

            // Second pass: build Browser entries with correct naming
            var icon = ExtractIcon(exePath);
            bool singleProfile = raw.Count == 1;

            return raw.Select(p => new Browser
            {
                Name           = singleProfile ? browserName : $"{browserName} – {p.DisplayName}",
                ExePath        = exePath,
                AdditionalArgs = $"--profile-directory=\"{p.FolderName}\"",
                Icon           = icon,
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    // -----------------------------------------------------------------------
    // Firefox profiles
    // -----------------------------------------------------------------------

    private List<Browser> ReadFirefoxProfiles(string browserName, string exePath, string[] profileRoots)
    {
        // Try each profile root for this specific browser
        foreach (string root in profileRoots)
        {
            string iniPath = Path.Combine(_appData, root, "profiles.ini");
            if (!File.Exists(iniPath)) continue;

            var profiles = ParseFirefoxProfilesIni(iniPath);
            if (profiles.Count == 0) continue;

            var icon = ExtractIcon(exePath);
            var entries = profiles
                .Select(p => new Browser
                {
                    Name           = profiles.Count == 1 ? browserName : $"{browserName} – {p.Name}",
                    ExePath        = exePath,
                    AdditionalArgs = $"-P \"{p.Name}\"",
                    Icon           = icon,
                })
                .ToList();

            return entries;
        }

        return [];
    }

    private static List<(string Name, string Path)> ParseFirefoxProfilesIni(string iniPath)
    {
        var profiles = new List<(string, string)>();
        string? currentName = null;
        string? currentPath = null;
        bool inProfile = false;

        foreach (string line in File.ReadLines(iniPath))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('['))
            {
                // Save previous profile
                if (inProfile && currentName != null && currentPath != null)
                    profiles.Add((currentName, currentPath));

                inProfile = trimmed.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase);
                currentName = null;
                currentPath = null;
            }
            else if (inProfile)
            {
                if (trimmed.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                    currentName = trimmed[5..];
                else if (trimmed.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                    currentPath = trimmed[5..];
            }
        }

        if (inProfile && currentName != null && currentPath != null)
            profiles.Add((currentName, currentPath));

        return profiles;
    }

    // -----------------------------------------------------------------------
    // Icon extraction
    // -----------------------------------------------------------------------

    public static ImageSource? ExtractIcon(string exePath)
    {
        try
        {
            using Icon? icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon == null) return null;

            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        catch
        {
            return null;
        }
    }
}
