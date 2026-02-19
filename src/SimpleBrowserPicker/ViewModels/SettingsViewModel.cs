using System.Collections.ObjectModel;
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
    // Browsers tab
    // -----------------------------------------------------------------------

    public ObservableCollection<Browser> Browsers { get; } = new();

    private Browser? _selectedBrowser;
    public Browser? SelectedBrowser
    {
        get => _selectedBrowser;
        set => SetField(ref _selectedBrowser, value);
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

    private BrowserRule? _selectedRule;
    public BrowserRule? SelectedRule
    {
        get => _selectedRule;
        set => SetField(ref _selectedRule, value);
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
    public ICommand DeleteRuleCommand        { get; }
    public ICommand RegisterCommand          { get; }
    public ICommand UnregisterCommand        { get; }
    public ICommand BrowseExeCommand         { get; }

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
        DeleteRuleCommand        = new RelayCommand(DeleteRule,
            () => SelectedRule is not null);
        RegisterCommand          = new RelayCommand(Register);
        UnregisterCommand        = new RelayCommand(Unregister);
        BrowseExeCommand         = new RelayCommand(BrowseExe);

        LoadBrowsers(detectedBrowsers);
        LoadRules();
        RefreshRegistrationStatus();
    }

    // -----------------------------------------------------------------------
    // Browsers tab
    // -----------------------------------------------------------------------

    private void LoadBrowsers(IEnumerable<Browser> detected)
    {
        Browsers.Clear();
        foreach (var b in detected)
            Browsers.Add(b);

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

    private void DeleteRule()
    {
        if (SelectedRule is null) return;

        _appConfig.Rules.Remove(SelectedRule);
        _configService.Save(_appConfig);
        Rules.Remove(SelectedRule);
        SelectedRule = null;
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

    private void RefreshRegistrationStatus()
    {
        var registrar = new ProtocolRegistrar();
        RegistrationStatus = registrar.IsRegistered()
            ? "Registered as default browser handler"
            : "Not registered";
    }
}
