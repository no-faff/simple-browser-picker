using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SimpleBrowserPicker.ViewModels;
using KeyEventArgs   = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Button         = System.Windows.Controls.Button;
using MessageBox     = System.Windows.MessageBox;
using WinScreen      = System.Windows.Forms.Screen;

namespace SimpleBrowserPicker.Views;

public partial class PickerWindow : Window
{
    private readonly PickerViewModel _vm;
    private bool _suppressDeactivateClose;

    public PickerWindow(PickerViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;

        _vm.CloseRequested += (_, _) => Close();
        _vm.ErrorRaised += OnErrorRaised;

        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        Deactivated += OnDeactivated;
    }

    private void OnErrorRaised(object? sender, string message)
    {
        _suppressDeactivateClose = true;
        MessageBox.Show(message, "Simple Browser Picker",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        _suppressDeactivateClose = false;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_suppressDeactivateClose)
            Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Centre on the monitor that contains the mouse cursor.
        // Screen.WorkingArea returns physical pixels; WPF Left/Top use DIPs.
        // Convert using the DPI transform to avoid misplacement at >100% scaling.
        var screen = WinScreen.FromPoint(
            System.Windows.Forms.Cursor.Position);
        var wa = screen.WorkingArea;

        var source = System.Windows.PresentationSource.FromVisual(this);
        double scaleX = source!.CompositionTarget.TransformFromDevice.M11;
        double scaleY = source!.CompositionTarget.TransformFromDevice.M22;

        double waLeft   = wa.Left   * scaleX;
        double waTop    = wa.Top    * scaleY;
        double waWidth  = wa.Width  * scaleX;
        double waHeight = wa.Height * scaleY;

        // Cap the browser list so the window fits on screen. Subtract fixed
        // overhead for URL strip (~40), checkbox (~35), padding/margin (~30)
        // and drop-shadow bleed (~25) so the whole window stays within bounds.
        BrowserScroller.MaxHeight = Math.Max(200, waHeight - 130);
        MaxHeight = waHeight;

        // Defer positioning until SizeToContent has recalculated ActualHeight
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            Left = waLeft + (waWidth  - ActualWidth)  / 2;
            Top  = waTop  + (waHeight - ActualHeight) / 2;
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        // Arrow key navigation
        if (e.Key == Key.Down || e.Key == Key.Up)
        {
            MoveFocus(e.Key == Key.Down ? FocusNavigationDirection.Next : FocusNavigationDirection.Previous);
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

    private void MoveFocus(FocusNavigationDirection direction)
    {
        if (Keyboard.FocusedElement is Button focused)
        {
            focused.MoveFocus(new TraversalRequest(direction));
        }
        else
        {
            // Nothing focused yet — focus the first browser button
            FindFirstBrowserButton()?.Focus();
        }
    }

    private Button? FindFirstBrowserButton()
    {
        return FindBrowserButton(RootBorder);
    }

    private static Button? FindBrowserButton(DependencyObject parent)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is Button btn && btn.Tag is SimpleBrowserPicker.Models.Browser)
                return btn;
            var result = FindBrowserButton(child);
            if (result is not null) return result;
        }
        return null;
    }

    private void BrowserButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SimpleBrowserPicker.Models.Browser browser)
            _vm.SelectedBrowser = browser;
    }

    private void BrowserButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
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
