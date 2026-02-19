using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SimpleBrowserPicker.ViewModels;
using KeyEventArgs   = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Button         = System.Windows.Controls.Button;
using WinScreen      = System.Windows.Forms.Screen;

namespace SimpleBrowserPicker.Views;

public partial class PickerWindow : Window
{
    private readonly PickerViewModel _vm;

    public PickerWindow(PickerViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;

        _vm.CloseRequested += (_, _) => Close();

        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        Deactivated += (_, _) => Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Centre on the monitor that contains the mouse cursor
        var screen = WinScreen.FromPoint(
            System.Windows.Forms.Cursor.Position);
        var wa = screen.WorkingArea;

        Left = wa.Left + (wa.Width  - ActualWidth)  / 2;
        Top  = wa.Top  + (wa.Height - ActualHeight) / 2;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        // 1–9 shortcut keys
        int index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            _ => 0,
        };

        if (index > 0)
        {
            _vm.LaunchByIndex(index);
            e.Handled = true;
        }
    }

    private void BrowserButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SimpleBrowserPicker.Models.Browser browser)
            _vm.SelectedBrowser = browser;
    }

    private void RootBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Allow dragging the window
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
