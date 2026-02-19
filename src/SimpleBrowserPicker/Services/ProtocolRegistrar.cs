using Microsoft.Win32;

namespace SimpleBrowserPicker.Services;

/// <summary>
/// Registers and unregisters Simple browser picker as a browser in HKCU,
/// so it appears in Windows Settings → Default apps.
/// No administrator privileges are required.
/// </summary>
public class ProtocolRegistrar
{
    private const string AppName       = "Simple browser picker";
    private const string AppDesc       = "Choose which browser opens each link";
    private const string RegAppName    = "SimpleBrowserPicker";
    private const string ProgId        = "SimpleBrowserPickerURL";

    private const string StartMenuPath = @"SOFTWARE\Clients\StartMenuInternet\" + RegAppName;
    private const string ClassesPath   = @"SOFTWARE\Classes\" + ProgId;
    private const string RegisteredAppsPath = @"SOFTWARE\RegisteredApplications";

    /// <summary>
    /// Registers the app as a browser. <paramref name="exePath"/> should be
    /// the full path to the running executable.
    /// </summary>
    public void Register(string exePath)
    {
        string command = $"\"{exePath}\" \"%1\"";

        // StartMenuInternet entry
        SetValue(StartMenuPath, null, AppName);
        SetValue($@"{StartMenuPath}\Capabilities", "ApplicationDescription", AppDesc);
        SetValue($@"{StartMenuPath}\Capabilities", "ApplicationName", AppName);
        SetValue($@"{StartMenuPath}\Capabilities\URLAssociations", "http",  ProgId);
        SetValue($@"{StartMenuPath}\Capabilities\URLAssociations", "https", ProgId);
        SetValue($@"{StartMenuPath}\shell\open\command", null, command);

        // ProgID / URL class
        SetValue(ClassesPath, null, $"{AppName} URL");
        SetValue(ClassesPath, "URL Protocol", string.Empty);
        SetValue($@"{ClassesPath}\shell\open\command", null, command);

        // RegisteredApplications
        SetValue(RegisteredAppsPath, RegAppName,
                 $@"SOFTWARE\Clients\StartMenuInternet\{RegAppName}\Capabilities");
    }

    /// <summary>
    /// Removes all registry keys created by <see cref="Register"/>.
    /// </summary>
    public void Unregister()
    {
        DeleteKey(StartMenuPath);
        DeleteKey(ClassesPath);

        using RegistryKey? regApps = Registry.CurrentUser.OpenSubKey(RegisteredAppsPath, writable: true);
        regApps?.DeleteValue(RegAppName, throwOnMissingValue: false);
    }

    /// <summary>Returns true if the app is currently registered.</summary>
    public bool IsRegistered()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartMenuPath);
        return key != null;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void SetValue(string keyPath, string? valueName, string data)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
        key.SetValue(valueName, data);
    }

    private static void DeleteKey(string keyPath)
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); }
        catch { /* best effort */ }
    }
}
