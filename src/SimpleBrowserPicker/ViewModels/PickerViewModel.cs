using System.Collections.ObjectModel;
using System.Diagnostics;
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
        set
        {
            SetField(ref _selectedBrowser, value);
            OnPropertyChanged(nameof(AlwaysUseLabel));
            OnPropertyChanged(nameof(AlwaysUseLabelVisible));
        }
    }

    private bool _alwaysUseChecked;
    public bool AlwaysUseChecked
    {
        get => _alwaysUseChecked;
        set => SetField(ref _alwaysUseChecked, value);
    }

    public string DisplayUrl { get; }
    public string UrlDomain  { get; }

    public string AlwaysUseLabel =>
        SelectedBrowser is null
            ? string.Empty
            : $"Always use {SelectedBrowser.Name} for {_domain}";

    public bool AlwaysUseLabelVisible => SelectedBrowser is not null && !string.IsNullOrEmpty(_domain);

    /// <summary>Set to true when the picker should close.</summary>
    public bool ShouldClose { get; private set; }

    public ICommand OpenCommand { get; }
    public ICommand CancelCommand { get; }

    public PickerViewModel(string url, List<Browser> browsers, ConfigService config, AppConfig appConfig)
    {
        _url = UrlParser.Unwrap(url);
        _config = config;
        _appConfig = appConfig;
        _domain = UrlParser.ExtractDomain(_url);

        DisplayUrl = _url;
        UrlDomain  = _domain;

        foreach (var b in browsers)
            Browsers.Add(b);

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
            System.Windows.MessageBox.Show(
                $"Could not launch {browser.Name}:\n{ex.Message}",
                "Simple browser picker",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
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

    public event EventHandler? CloseRequested;
}
