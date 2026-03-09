using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using SimpleBrowserPicker.Models;
using SimpleBrowserPicker.Services;

namespace SimpleBrowserPicker.ViewModels;

public class PickerViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly AppConfig _appConfig;
    private readonly string _url;
    private readonly string _domain;

    public ObservableCollection<Browser> Browsers { get; } = new();

    private Browser? _selectedBrowser;
    public Browser? SelectedBrowser
    {
        get => _selectedBrowser;
        set => SetField(ref _selectedBrowser, value);
    }

    private bool _alwaysUseChecked;
    public bool AlwaysUseChecked
    {
        get => _alwaysUseChecked;
        set => SetField(ref _alwaysUseChecked, value);
    }

    public string DisplayUrl  { get; }
    public string RawUrl      { get; }
    public string UrlDomain   { get; }

    /// <summary>True when the URL was unwrapped from a SafeLinks/redirect wrapper.</summary>
    public bool WasRedirected { get; }

    public string AlwaysUseLabel => $"_Remember my choice for {_domain}";

    public bool AlwaysUseLabelVisible => !string.IsNullOrEmpty(_domain);

    private bool _setAsFallbackChecked;
    public bool SetAsFallbackChecked
    {
        get => _setAsFallbackChecked;
        set => SetField(ref _setAsFallbackChecked, value);
    }

    public string SetAsFallbackLabel =>
        string.IsNullOrEmpty(_appConfig.FallbackBrowserName)
            ? "_Use as my default for all sites"
            : $"Change my defa_ult (currently {_appConfig.FallbackBrowserName})";

    public bool ShowSetAsFallback => true;

    /// <summary>Set to true when the picker should close.</summary>
    public bool ShouldClose { get; private set; }

    public ICommand OpenCommand { get; }
    public ICommand CancelCommand { get; }

    public PickerViewModel(string url, List<Browser> browsers, ConfigService config, AppConfig appConfig)
    {
        RawUrl = url;
        _url = UrlParser.Unwrap(url);
        _config = config;
        _appConfig = appConfig;
        _domain = UrlParser.ExtractDomain(_url);

        WasRedirected = !string.Equals(url, _url, StringComparison.OrdinalIgnoreCase);
        DisplayUrl = _url;
        UrlDomain  = _domain;

        // Filter out our own app and Internet Explorer
        string ownExe = Environment.ProcessPath ?? string.Empty;
        int shortcut = 1;
        foreach (var b in browsers)
        {
            if (b.ExePath.Equals(ownExe, StringComparison.OrdinalIgnoreCase))
                continue;
            if (Path.GetFileName(b.ExePath).Equals("iexplore.exe", StringComparison.OrdinalIgnoreCase))
                continue;

            if (shortcut <= 9)
                b.ShortcutLabel = shortcut.ToString();
            shortcut++;

            Browsers.Add(b);
        }

        OpenCommand   = new RelayCommand<Browser?>(b => Launch(b));
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>Keyboard shortcut: launch the Nth browser (1-indexed).</summary>
    public void LaunchByIndex(int oneBasedIndex)
    {
        if (oneBasedIndex >= 1 && oneBasedIndex <= Browsers.Count)
            Launch(Browsers[oneBasedIndex - 1]);
    }

    public void Launch(Browser? browser)
    {
        if (browser is null) return;
        SelectedBrowser = browser;

        if (AlwaysUseChecked && !string.IsNullOrEmpty(_domain))
            SaveRule(browser);

        if (SetAsFallbackChecked)
            SaveFallback(browser);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = browser.ExePath,
                Arguments       = string.IsNullOrWhiteSpace(browser.AdditionalArgs)
                                    ? $"\"{_url}\""
                                    : $"{browser.AdditionalArgs} \"{_url}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            ErrorRaised?.Invoke(this, $"Could not launch {browser.Name}:\n{ex.Message}");
            return;
        }

        ShouldClose = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        ShouldClose = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveRule(Browser browser)
    {
        // Remove any existing rule for this domain first
        _appConfig.Rules.RemoveAll(r =>
            string.Equals(r.Domain, _domain, StringComparison.OrdinalIgnoreCase));

        _appConfig.Rules.Add(new BrowserRule
        {
            Domain        = _domain,
            BrowserExePath = browser.ExePath,
            BrowserName   = browser.Name,
            ProfileArgs   = browser.AdditionalArgs,
        });

        _config.Save(_appConfig);
    }

    private void SaveFallback(Browser browser)
    {
        _appConfig.FallbackBrowserExePath = browser.ExePath;
        _appConfig.FallbackBrowserName    = browser.Name;
        _appConfig.FallbackProfileArgs    = browser.AdditionalArgs;
        _config.Save(_appConfig);
    }

    public event EventHandler? CloseRequested;
    public event EventHandler<string>? ErrorRaised;
}
