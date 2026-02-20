using System.Diagnostics;
using System.Windows.Input;
using SimpleBrowserPicker.Models;
using SimpleBrowserPicker.Services;

namespace SimpleBrowserPicker.ViewModels;

public class FirstRunViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private AppConfig _appConfig;

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand SetAsDefaultCommand { get; }
    public ICommand SkipCommand         { get; }

    /// <summary>Raised when the window should close (either path).</summary>
    public event EventHandler? CloseRequested;

    public FirstRunViewModel(ConfigService configService, AppConfig appConfig)
    {
        _configService = configService;
        _appConfig     = appConfig;

        SetAsDefaultCommand = new RelayCommand(SetAsDefault);
        SkipCommand         = new RelayCommand(Skip);
    }

    private void SetAsDefault()
    {
        // Detect the current default browser before we replace it
        if (string.IsNullOrEmpty(_appConfig.FallbackBrowserExePath))
        {
            string? previousDefault = ProtocolRegistrar.DetectCurrentDefaultBrowser();
            if (!string.IsNullOrEmpty(previousDefault) && System.IO.File.Exists(previousDefault))
            {
                _appConfig.FallbackBrowserExePath = previousDefault;
                _appConfig.FallbackBrowserName    = System.IO.Path.GetFileNameWithoutExtension(previousDefault);
                _configService.Save(_appConfig);
            }
        }

        var registrar = new ProtocolRegistrar();
        string exePath = Process.GetCurrentProcess().MainModule!.FileName!;
        registrar.Register(exePath);

        StatusMessage = "Select 'Simple Browser Picker' in the settings window that just opened.";

        Process.Start(new ProcessStartInfo
        {
            FileName        = "ms-settings:defaultapps",
            UseShellExecute = true,
        });

        CompleteSetup();
    }

    private void Skip()
    {
        CompleteSetup();
    }

    private void CompleteSetup()
    {
        _appConfig.SetupComplete = true;
        _configService.Save(_appConfig);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
