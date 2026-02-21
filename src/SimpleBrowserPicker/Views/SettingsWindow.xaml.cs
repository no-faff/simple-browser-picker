using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using SimpleBrowserPicker.ViewModels;

using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SimpleBrowserPicker.Views;

public partial class SettingsWindow : Window
{
    private const int WM_NCHITTEST = 0x0084;
    private const int BORDER_WIDTH = 6; // px resize-grab zone

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        KeyDown += SettingsWindow_KeyDown;
        SourceInitialized += (_, _) =>
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            var point = PointFromScreen(new System.Windows.Point(
                (short)(lParam.ToInt64() & 0xFFFF),
                (short)((lParam.ToInt64() >> 16) & 0xFFFF)));
            int b = BORDER_WIDTH;
            bool left   = point.X < b;
            bool right  = point.X > ActualWidth - b;
            bool top    = point.Y < b;
            bool bottom = point.Y > ActualHeight - b;

            if (top && left)         { handled = true; return 13; } // HTTOPLEFT
            if (top && right)        { handled = true; return 14; } // HTTOPRIGHT
            if (bottom && left)      { handled = true; return 16; } // HTBOTTOMLEFT
            if (bottom && right)     { handled = true; return 17; } // HTBOTTOMRIGHT
            if (left)                { handled = true; return 10; } // HTLEFT
            if (right)               { handled = true; return 11; } // HTRIGHT
            if (top)                 { handled = true; return 12; } // HTTOP
            if (bottom)              { handled = true; return 15; } // HTBOTTOM
        }
        return IntPtr.Zero;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click toggles maximise
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PillTab_Checked(object sender, RoutedEventArgs e)
    {
        // Guard: MainTabs may not be initialised yet during XAML loading
        if (MainTabs == null) return;

        if (sender == TabBrowsers)      MainTabs.SelectedIndex = 0;
        else if (sender == TabRules)    MainTabs.SelectedIndex = 1;
        else if (sender == TabAbout)    MainTabs.SelectedIndex = 2;
    }

    private void SettingsWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
