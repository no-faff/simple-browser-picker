using System.IO;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using SimpleBrowserPicker.Models;
using SimpleBrowserPicker.Services;
using SimpleBrowserPicker.ViewModels;
using SimpleBrowserPicker.Views;

namespace SimpleBrowserPicker;

public partial class App : Application
{
    private ConfigService _configService = new();
    private BrowserDetector _detector    = new();
    private AppConfig _config            = new();
    private List<Browser> _browsers      = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers — log and show a message instead of silently crashing
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Apply Windows theme (light/dark + accent colour)
        ThemeService.Apply(this);

        _config   = _configService.Load();
        _browsers = _detector.Detect();

        // Apply user overrides (renamed browsers, custom args)
        foreach (var b in _browsers)
        {
            var ov = _config.BrowserOverrides.FirstOrDefault(o =>
                string.Equals(o.ExePath, b.ExePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(o.OriginalArgs ?? "", b.AdditionalArgs ?? "", StringComparison.OrdinalIgnoreCase));
            if (ov is null) continue;
            if (ov.NameOverride is not null) b.Name = ov.NameOverride;
            if (ov.ArgsOverride is not null) b.AdditionalArgs = ov.ArgsOverride;
        }

        string[] args = e.Args;

        if (args.Length > 0)
        {
            // Launched with a URL
            string rawUrl = args[0];
            string url    = UrlParser.Unwrap(rawUrl);
            string domain = UrlParser.ExtractDomain(url);

            // Check for Office document URL — open in desktop app if applicable
            string? officeUri = UrlParser.GetOfficeProtocolUri(url);
            if (officeUri is not null)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = officeUri,
                        UseShellExecute = true,
                    });
                    Shutdown();
                    return;
                }
                catch (Exception ex) { LogException(ex); } // Fall through to normal routing
            }

            // Check for a matching rule — if found, launch immediately without showing the picker
            BrowserRule? rule = _config.SuspendRules ? null : FindMatchingRule(url);
            if (rule is not null)
            {
                // Exception rule (no browser) — skip to picker
                if (string.IsNullOrEmpty(rule.BrowserExePath))
                {
                    // Fall through to picker
                }
                else if (LaunchWithRule(rule, url))
                {
                    Shutdown();
                    return;
                }
            }

            // No rule matched — try the fallback browser (unless "always ask" is on)
            if (!_config.AlwaysAsk &&
                !string.IsNullOrEmpty(_config.FallbackBrowserExePath) &&
                LaunchFallback(url))
            {
                Shutdown();
                return;
            }

            // No fallback — show first-run wizard if needed, then the picker
            if (!_config.SetupComplete)
                RunFirstRunThenPicker(url);
            else
                ShowPicker(url);
        }
        else
        {
            // No URL argument — show settings (or first-run on very first launch)
            if (!_config.SetupComplete)
                ShowFirstRun();
            else
                ShowSettings();
        }
    }

    // -----------------------------------------------------------------------
    // Rule matching
    // -----------------------------------------------------------------------

    private BrowserRule? FindMatchingRule(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        foreach (var rule in _config.Rules)
        {
            if (UrlParser.UrlMatches(url, rule.Domain))
                return rule;
        }
        return null;
    }

    private bool LaunchFallback(string url)
    {
        try
        {
            string args = string.IsNullOrWhiteSpace(_config.FallbackProfileArgs)
                ? $"\"{url}\""
                : $"{_config.FallbackProfileArgs} \"{url}\"";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = _config.FallbackBrowserExePath!,
                Arguments       = args,
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// Attempts to launch the browser specified by a rule.
    /// Returns true on success, false on failure (caller should fall back to the picker).
    /// </summary>
    private static bool LaunchWithRule(BrowserRule rule, string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = rule.BrowserExePath,
                Arguments       = string.IsNullOrWhiteSpace(rule.ProfileArgs)
                                    ? $"\"{url}\""
                                    : $"{rule.ProfileArgs} \"{url}\"",
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception ex)
        {
            LogException(ex);
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // Window presentation
    // -----------------------------------------------------------------------

    private void ShowPicker(string url)
    {
        var vm     = new PickerViewModel(url, _browsers, _configService, _config);
        var window = new PickerWindow(vm);
        window.SettingsRequested += (_, _) => ShowSettings();
        window.Closed += (_, _) =>
        {
            // If no other windows are open, shut down
            if (Windows.Count == 0)
                Shutdown();
        };
        window.Show();
    }

    private void ShowSettings()
    {
        try
        {
            var vm     = new SettingsViewModel(_configService, _detector, _config, _browsers);
            var window = new SettingsWindow(vm);
            window.Closed += (_, _) => Shutdown();
            window.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Settings error");
            Shutdown();
        }
    }

    private void ShowFirstRun()
    {
        var vm     = new FirstRunViewModel(_configService, _config);
        var window = new FirstRunWindow(vm);
        window.Closed += (_, _) =>
        {
            // After first-run completes, show settings
            ShowSettings();
        };
        window.Show();
    }

    private void RunFirstRunThenPicker(string url)
    {
        var vm     = new FirstRunViewModel(_configService, _config);
        var window = new FirstRunWindow(vm);
        window.Closed += (_, _) =>
        {
            _config = _configService.Load();
            ShowPicker(url);
        };
        window.Show();
    }

    // -----------------------------------------------------------------------
    // Global error handling
    // -----------------------------------------------------------------------

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        ShowErrorMessage(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        LogException(ex);
        ShowErrorMessage(ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.SetObserved();
    }

    /// <summary>
    /// Logs an exception to %LOCALAPPDATA%\SimpleBrowserPicker\error.log.
    /// Safe to call from anywhere; silently does nothing if logging fails.
    /// </summary>
    internal static void LogException(Exception? ex)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimpleBrowserPicker");
            Directory.CreateDirectory(folder);

            var logPath = Path.Combine(folder, "error.log");
            var entry   = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n";
            File.AppendAllText(logPath, entry);
        }
        catch
        {
            // Nothing we can do if logging itself fails
        }
    }

    private static void ShowErrorMessage(Exception? ex)
    {
        try
        {
            var message = "Simple Browser Picker ran into an unexpected error and needs to close.\n\n"
                        + "Details have been saved to:\n"
                        + Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "SimpleBrowserPicker", "error.log")
                        + "\n\n"
                        + (ex?.Message ?? "Unknown error");

            System.Windows.MessageBox.Show(message, "Simple Browser Picker", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Can't show UI — nothing more to do
        }
    }
}
