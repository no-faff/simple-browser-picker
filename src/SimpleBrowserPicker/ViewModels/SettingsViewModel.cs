using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SimpleBrowserPicker.Models;
using SimpleBrowserPicker.Services;

namespace SimpleBrowserPicker.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly BrowserDetector _detector;
    private AppConfig _appConfig;

    // -----------------------------------------------------------------------
    // Open tab
    // -----------------------------------------------------------------------

    private string _openUrl = string.Empty;
    public string OpenUrl
    {
        get => _openUrl;
        set
        {
            if (SetField(ref _openUrl, value))
            {
                string unwrapped = Services.UrlParser.Unwrap(value?.Trim() ?? "");
                _unwrappedUrl = unwrapped;
                OnPropertyChanged(nameof(UnwrappedUrl));
                OnPropertyChanged(nameof(ShowRedirectInfo));
            }
        }
    }

    private string _unwrappedUrl = string.Empty;
    public string UnwrappedUrl => _unwrappedUrl;

    public bool ShowRedirectInfo =>
        !string.IsNullOrWhiteSpace(_openUrl) &&
        !string.IsNullOrWhiteSpace(_unwrappedUrl) &&
        !string.Equals(_openUrl.Trim(), _unwrappedUrl, StringComparison.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Browsers tab
    // -----------------------------------------------------------------------

    public ObservableCollection<Browser> Browsers { get; } = new();

    private string _browserFilter = string.Empty;
    public string BrowserFilter
    {
        get => _browserFilter;
        set
        {
            if (SetField(ref _browserFilter, value))
                CollectionViewSource.GetDefaultView(Browsers).Refresh();
        }
    }

    private Browser? _selectedBrowser;
    public Browser? SelectedBrowser
    {
        get => _selectedBrowser;
        set
        {
            if (SetField(ref _selectedBrowser, value))
            {
                OnPropertyChanged(nameof(HasSelectedBrowser));
                if (value is not null)
                {
                    EditBrowserName = value.Name;
                    EditBrowserPath = value.ExePath;
                    EditBrowserArgs = value.AdditionalArgs;
                }
            }
        }
    }

    public bool HasSelectedBrowser => SelectedBrowser is not null;

    private string _editBrowserName = string.Empty;
    public string EditBrowserName
    {
        get => _editBrowserName;
        set => SetField(ref _editBrowserName, value);
    }

    private string _editBrowserPath = string.Empty;
    public string EditBrowserPath
    {
        get => _editBrowserPath;
        set => SetField(ref _editBrowserPath, value);
    }

    private string _editBrowserArgs = string.Empty;
    public string EditBrowserArgs
    {
        get => _editBrowserArgs;
        set => SetField(ref _editBrowserArgs, value);
    }

    // Custom browser fields
    private string _customBrowserName = string.Empty;
    public string CustomBrowserName
    {
        get => _customBrowserName;
        set => SetField(ref _customBrowserName, value);
    }

    private string _customBrowserExe = string.Empty;
    public string CustomBrowserExe
    {
        get => _customBrowserExe;
        set => SetField(ref _customBrowserExe, value);
    }

    private string _customBrowserArgs = string.Empty;
    public string CustomBrowserArgs
    {
        get => _customBrowserArgs;
        set => SetField(ref _customBrowserArgs, value);
    }

    // -----------------------------------------------------------------------
    // Rules tab
    // -----------------------------------------------------------------------

    public ObservableCollection<BrowserRule> Rules { get; } = new();

    public bool HasNoRules => Rules.Count == 0;

    private string _ruleFilter = string.Empty;
    public string RuleFilter
    {
        get => _ruleFilter;
        set
        {
            if (SetField(ref _ruleFilter, value))
                CollectionViewSource.GetDefaultView(Rules).Refresh();
        }
    }

    private BrowserRule? _selectedRule;
    public BrowserRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetField(ref _selectedRule, value) && value is not null)
            {
                // Pre-populate the add form so editing is select → tweak → add
                NewRuleDomain  = value.Domain;
                NewRuleBrowser = Browsers.FirstOrDefault(b =>
                    string.Equals(b.ExePath, value.BrowserExePath, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(b.AdditionalArgs ?? "", value.ProfileArgs ?? "", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    // New rule fields
    private string _newRuleDomain = string.Empty;
    public string NewRuleDomain
    {
        get => _newRuleDomain;
        set => SetField(ref _newRuleDomain, value);
    }

    private Browser? _newRuleBrowser;
    public Browser? NewRuleBrowser
    {
        get => _newRuleBrowser;
        set => SetField(ref _newRuleBrowser, value);
    }

    // -----------------------------------------------------------------------
    // Behaviour — always ask vs fallback
    // -----------------------------------------------------------------------

    private bool _alwaysAsk;
    public bool AlwaysAsk
    {
        get => _alwaysAsk;
        set
        {
            if (SetField(ref _alwaysAsk, value))
            {
                _appConfig.AlwaysAsk = value;
                _configService.Save(_appConfig);
                OnPropertyChanged(nameof(ShowFallbackSection));
            }
        }
    }

    public bool ShowFallbackSection => !AlwaysAsk;

    private bool _suspendRules;
    public bool SuspendRules
    {
        get => _suspendRules;
        set
        {
            if (SetField(ref _suspendRules, value))
            {
                _appConfig.SuspendRules = value;
                _configService.Save(_appConfig);
            }
        }
    }

    private Browser? _fallbackBrowser;
    public Browser? FallbackBrowser
    {
        get => _fallbackBrowser;
        set
        {
            if (SetField(ref _fallbackBrowser, value))
                SaveFallbackBrowser(value);
        }
    }

    // -----------------------------------------------------------------------
    // About tab
    // -----------------------------------------------------------------------

    public string AppVersion { get; } =
        System.Reflection.Assembly.GetExecutingAssembly()
              .GetName().Version?.ToString() ?? "0.1";

    private string _registrationStatus = string.Empty;
    public string RegistrationStatus
    {
        get => _registrationStatus;
        set => SetField(ref _registrationStatus, value);
    }

    // -----------------------------------------------------------------------
    // Commands
    // -----------------------------------------------------------------------

    public ICommand RefreshBrowsersCommand  { get; }
    public ICommand AddCustomBrowserCommand  { get; }
    public ICommand RemoveBrowserCommand     { get; }
    public ICommand AddRuleCommand           { get; }
    public ICommand DeleteRuleCommand        { get; }
    public ICommand RegisterCommand          { get; }
    public ICommand UnregisterCommand        { get; }
    public ICommand BrowseExeCommand         { get; }
    public ICommand SaveBrowserEditCommand   { get; }
    public ICommand MoveBrowserUpCommand     { get; }
    public ICommand MoveBrowserDownCommand   { get; }
    public ICommand MoveRuleUpCommand        { get; }
    public ICommand MoveRuleDownCommand      { get; }
    public ICommand ExportConfigCommand      { get; }
    public ICommand ImportConfigCommand      { get; }
    public ICommand OpenInBrowserCommand     { get; }
    public ICommand SecurityCheckCommand     { get; }

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    public SettingsViewModel(
        ConfigService configService,
        BrowserDetector detector,
        AppConfig appConfig,
        List<Browser> detectedBrowsers)
    {
        _configService = configService;
        _detector      = detector;
        _appConfig     = appConfig;

        RefreshBrowsersCommand  = new RelayCommand(RefreshBrowsers);
        AddCustomBrowserCommand  = new RelayCommand(AddCustomBrowser,
            () => !string.IsNullOrWhiteSpace(CustomBrowserName) &&
                  !string.IsNullOrWhiteSpace(CustomBrowserExe));
        RemoveBrowserCommand     = new RelayCommand(RemoveBrowser,
            () => SelectedBrowser?.IsCustom == true);
        AddRuleCommand           = new RelayCommand(AddRule,
            () => !string.IsNullOrWhiteSpace(NewRuleDomain));
        DeleteRuleCommand        = new RelayCommand(DeleteRule,
            () => SelectedRule is not null);
        RegisterCommand          = new RelayCommand(Register);
        UnregisterCommand        = new RelayCommand(Unregister);
        BrowseExeCommand         = new RelayCommand(BrowseExe);
        SaveBrowserEditCommand   = new RelayCommand(SaveBrowserEdit,
            () => SelectedBrowser is not null);
        MoveBrowserUpCommand     = new RelayCommand(() => MoveItem(Browsers, SelectedBrowser, -1),
            () => SelectedBrowser is not null && Browsers.IndexOf(SelectedBrowser) > 0);
        MoveBrowserDownCommand   = new RelayCommand(() => MoveItem(Browsers, SelectedBrowser, +1),
            () => SelectedBrowser is not null && Browsers.IndexOf(SelectedBrowser) < Browsers.Count - 1);
        MoveRuleUpCommand        = new RelayCommand(() => MoveRule(SelectedRule, -1),
            () => SelectedRule is not null && Rules.IndexOf(SelectedRule) > 0);
        MoveRuleDownCommand      = new RelayCommand(() => MoveRule(SelectedRule, +1),
            () => SelectedRule is not null && Rules.IndexOf(SelectedRule) < Rules.Count - 1);
        ExportConfigCommand      = new RelayCommand(ExportConfig);
        ImportConfigCommand      = new RelayCommand(ImportConfig);
        OpenInBrowserCommand     = new RelayCommand<Browser?>(OpenInBrowser);
        SecurityCheckCommand     = new RelayCommand(SecurityCheck,
            () => !string.IsNullOrWhiteSpace(OpenUrl));

        Rules.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoRules));

        _alwaysAsk    = appConfig.AlwaysAsk;
        _suspendRules = appConfig.SuspendRules;

        LoadBrowsers(detectedBrowsers);
        LoadRules();
        LoadFallbackBrowser();
        RefreshRegistrationStatus();

        // Set up collection view filters
        var browserView = CollectionViewSource.GetDefaultView(Browsers);
        browserView.Filter = obj =>
        {
            if (string.IsNullOrWhiteSpace(_browserFilter)) return true;
            return obj is Browser b &&
                   b.Name.Contains(_browserFilter, StringComparison.OrdinalIgnoreCase);
        };

        var ruleView = CollectionViewSource.GetDefaultView(Rules);
        ruleView.Filter = obj =>
        {
            if (string.IsNullOrWhiteSpace(_ruleFilter)) return true;
            return obj is BrowserRule r &&
                   (r.Domain.Contains(_ruleFilter, StringComparison.OrdinalIgnoreCase) ||
                    r.BrowserName.Contains(_ruleFilter, StringComparison.OrdinalIgnoreCase));
        };
    }

    // -----------------------------------------------------------------------
    // Browsers tab
    // -----------------------------------------------------------------------

    private void LoadBrowsers(IEnumerable<Browser> detected)
    {
        Browsers.Clear();
        foreach (var b in detected)
        {
            ApplyOverride(b);
            Browsers.Add(b);
        }

        foreach (var custom in _appConfig.CustomBrowsers)
        {
            Browsers.Add(new Browser
            {
                Name           = custom.Name,
                ExePath        = custom.ExePath,
                AdditionalArgs = custom.AdditionalArgs,
                IsCustom       = true,
                Icon           = BrowserDetector.ExtractIcon(custom.ExePath),
            });
        }
    }

    private void ApplyOverride(Browser browser)
    {
        var ov = _appConfig.BrowserOverrides.FirstOrDefault(o =>
            string.Equals(o.ExePath, browser.ExePath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.OriginalArgs ?? "", browser.AdditionalArgs ?? "", StringComparison.OrdinalIgnoreCase));
        if (ov is null) return;
        if (ov.NameOverride is not null) browser.Name = ov.NameOverride;
        if (ov.ArgsOverride is not null) browser.AdditionalArgs = ov.ArgsOverride;
    }

    private void RefreshBrowsers()
    {
        var detected = _detector.Detect();
        LoadBrowsers(detected);
    }

    private void AddCustomBrowser()
    {
        var entry = new CustomBrowserEntry
        {
            Name           = CustomBrowserName.Trim(),
            ExePath        = CustomBrowserExe.Trim(),
            AdditionalArgs = CustomBrowserArgs.Trim(),
        };
        _appConfig.CustomBrowsers.Add(entry);
        _configService.Save(_appConfig);

        Browsers.Add(new Browser
        {
            Name           = entry.Name,
            ExePath        = entry.ExePath,
            AdditionalArgs = entry.AdditionalArgs,
            IsCustom       = true,
            Icon           = BrowserDetector.ExtractIcon(entry.ExePath),
        });

        CustomBrowserName = string.Empty;
        CustomBrowserExe  = string.Empty;
        CustomBrowserArgs = string.Empty;
    }

    private void RemoveBrowser()
    {
        if (SelectedBrowser is null || !SelectedBrowser.IsCustom) return;

        _appConfig.CustomBrowsers.RemoveAll(c =>
            c.ExePath == SelectedBrowser.ExePath &&
            c.AdditionalArgs == SelectedBrowser.AdditionalArgs);
        _configService.Save(_appConfig);
        Browsers.Remove(SelectedBrowser);
        SelectedBrowser = null;
    }

    private static void MoveItem<T>(ObservableCollection<T> collection, T? item, int direction) where T : class
    {
        if (item is null) return;
        int idx = collection.IndexOf(item);
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= collection.Count) return;
        collection.Move(idx, newIdx);
    }

    private void MoveRule(BrowserRule? rule, int direction)
    {
        if (rule is null) return;
        int idx = Rules.IndexOf(rule);
        int newIdx = idx + direction;
        if (newIdx < 0 || newIdx >= Rules.Count) return;

        Rules.Move(idx, newIdx);

        // Sync the config list order
        _appConfig.Rules.Clear();
        foreach (var r in Rules)
            _appConfig.Rules.Add(r);
        _configService.Save(_appConfig);
    }

    /// <summary>Called by the view after a drag-drop reorder.</summary>
    public void MoveRuleTo(BrowserRule dragged, BrowserRule target)
    {
        int from = Rules.IndexOf(dragged);
        int to   = Rules.IndexOf(target);
        if (from < 0 || to < 0 || from == to) return;

        Rules.Move(from, to);

        _appConfig.Rules.Clear();
        foreach (var r in Rules) _appConfig.Rules.Add(r);
        _configService.Save(_appConfig);
    }

    /// <summary>Called by the view after a drag-drop reorder.</summary>
    public void MoveBrowserTo(Browser dragged, Browser target)
    {
        int from = Browsers.IndexOf(dragged);
        int to   = Browsers.IndexOf(target);
        if (from < 0 || to < 0 || from == to) return;

        Browsers.Move(from, to);
        // Browser order is display-only — not persisted to config (resets on next browser refresh).
    }

    private void SaveBrowserEdit()
    {
        if (SelectedBrowser is null) return;

        string newName = EditBrowserName.Trim();
        string newArgs = EditBrowserArgs.Trim();

        // Capture the original args BEFORE modifying the browser object,
        // otherwise the override lookup below compares against the new value
        // and never finds the existing override (creating duplicates).
        string originalArgs = SelectedBrowser.AdditionalArgs;

        // Update the in-memory browser object
        SelectedBrowser.Name           = newName;
        SelectedBrowser.AdditionalArgs = newArgs;

        // Save an override in config so changes persist across restarts
        var existing = _appConfig.BrowserOverrides.FirstOrDefault(o =>
            string.Equals(o.ExePath, SelectedBrowser.ExePath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.OriginalArgs, originalArgs, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.NameOverride = newName;
            existing.ArgsOverride = newArgs;
        }
        else
        {
            _appConfig.BrowserOverrides.Add(new BrowserOverride
            {
                ExePath       = SelectedBrowser.ExePath,
                OriginalArgs  = originalArgs,
                NameOverride  = newName,
                ArgsOverride  = newArgs,
            });
        }
        _configService.Save(_appConfig);

        // Refresh the list to show the updated name
        int idx = Browsers.IndexOf(SelectedBrowser);
        if (idx >= 0)
        {
            Browsers.RemoveAt(idx);
            Browsers.Insert(idx, SelectedBrowser);
            SelectedBrowser = Browsers[idx];
        }
    }

    private void BrowseExe()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select browser executable",
            Filter = "Executables (*.exe)|*.exe",
        };
        if (dlg.ShowDialog() == true)
            CustomBrowserExe = dlg.FileName;
    }

    // -----------------------------------------------------------------------
    // Rules tab
    // -----------------------------------------------------------------------

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var r in _appConfig.Rules)
            Rules.Add(r);
    }

    private void AddRule()
    {
        if (string.IsNullOrWhiteSpace(NewRuleDomain)) return;

        string domain = NewRuleDomain.Trim().ToLowerInvariant();

        // Remove any existing rule for this domain
        var existing = _appConfig.Rules.FirstOrDefault(r =>
            string.Equals(r.Domain, domain, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _appConfig.Rules.Remove(existing);
            Rules.Remove(existing);
        }

        // No browser selected = exception rule (always show picker for this domain)
        var rule = new BrowserRule
        {
            Domain         = domain,
            BrowserExePath = NewRuleBrowser?.ExePath ?? string.Empty,
            BrowserName    = NewRuleBrowser?.Name ?? "(always ask)",
            ProfileArgs    = NewRuleBrowser?.AdditionalArgs ?? string.Empty,
        };
        _appConfig.Rules.Add(rule);
        _configService.Save(_appConfig);
        Rules.Add(rule);

        NewRuleDomain  = string.Empty;
        NewRuleBrowser = null;

        // Select the new rule so the user sees confirmation.
        // Done after clearing the form so the setter doesn't re-populate.
        _selectedRule = rule;
        OnPropertyChanged(nameof(SelectedRule));
    }

    private void DeleteRule()
    {
        if (SelectedRule is null) return;

        _appConfig.Rules.Remove(SelectedRule);
        _configService.Save(_appConfig);
        Rules.Remove(SelectedRule);
        SelectedRule = null;
    }

    // -----------------------------------------------------------------------
    // Fallback browser
    // -----------------------------------------------------------------------

    private void LoadFallbackBrowser()
    {
        if (string.IsNullOrEmpty(_appConfig.FallbackBrowserExePath)) return;

        // Find matching browser in the list
        _fallbackBrowser = Browsers.FirstOrDefault(b =>
            string.Equals(b.ExePath, _appConfig.FallbackBrowserExePath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(b.AdditionalArgs, _appConfig.FallbackProfileArgs ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(FallbackBrowser));
    }

    private void SaveFallbackBrowser(Browser? browser)
    {
        if (browser is null)
        {
            _appConfig.FallbackBrowserExePath = null;
            _appConfig.FallbackBrowserName    = null;
            _appConfig.FallbackProfileArgs    = null;
        }
        else
        {
            _appConfig.FallbackBrowserExePath = browser.ExePath;
            _appConfig.FallbackBrowserName    = browser.Name;
            _appConfig.FallbackProfileArgs    = browser.AdditionalArgs;
        }
        _configService.Save(_appConfig);
    }

    // -----------------------------------------------------------------------
    // About tab
    // -----------------------------------------------------------------------

    private void Register()
    {
        var registrar = new ProtocolRegistrar();
        registrar.Register(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "ms-settings:defaultapps",
            UseShellExecute = true,
        });
        RefreshRegistrationStatus();
    }

    private void Unregister()
    {
        var registrar = new ProtocolRegistrar();
        registrar.Unregister();
        RefreshRegistrationStatus();
    }

    // -----------------------------------------------------------------------
    // Open tab — launch URL in a browser
    // -----------------------------------------------------------------------

    private void OpenInBrowser(Browser? browser)
    {
        if (browser is null) return;
        string url = Services.UrlParser.Unwrap(OpenUrl?.Trim() ?? "");
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = browser.ExePath,
                Arguments       = string.IsNullOrWhiteSpace(browser.AdditionalArgs)
                                    ? $"\"{url}\""
                                    : $"{browser.AdditionalArgs} \"{url}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not launch {browser.Name}:\n{ex.Message}",
                "Simple Browser Picker", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void SecurityCheck()
    {
        string url = Services.UrlParser.Unwrap(OpenUrl?.Trim() ?? "");
        if (string.IsNullOrWhiteSpace(url)) return;

        string encoded = Uri.EscapeDataString(url);
        string checkUrl = $"https://transparencyreport.google.com/safe-browsing/search?url={encoded}";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = checkUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) { App.LogException(ex); }
    }

    // -----------------------------------------------------------------------
    // Config export / import
    // -----------------------------------------------------------------------

    private void ExportConfig()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Export configuration",
            Filter     = "JSON files (*.json)|*.json",
            FileName   = "simple-browser-picker-config.json",
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                System.IO.File.Copy(_configService.ConfigPath, dlg.FileName, overwrite: true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not export config:\n{ex.Message}",
                    "Simple Browser Picker", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
    }

    private void ImportConfig()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Import configuration",
            Filter = "JSON files (*.json)|*.json",
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                System.IO.File.Copy(dlg.FileName, _configService.ConfigPath, overwrite: true);
                _appConfig = _configService.Load();
                _alwaysAsk = _appConfig.AlwaysAsk;
                OnPropertyChanged(nameof(AlwaysAsk));
                OnPropertyChanged(nameof(ShowFallbackSection));
                LoadBrowsers(_detector.Detect());
                LoadRules();
                LoadFallbackBrowser();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not import config:\n{ex.Message}",
                    "Simple Browser Picker", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
    }

    private void RefreshRegistrationStatus()
    {
        var registrar = new ProtocolRegistrar();
        RegistrationStatus = registrar.IsRegistered()
            ? "Registered as default browser handler"
            : "Not registered";
    }
}
