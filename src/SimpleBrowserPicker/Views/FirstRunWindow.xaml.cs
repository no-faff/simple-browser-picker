using System.Windows;
using System.Windows.Input;
using SimpleBrowserPicker.ViewModels;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SimpleBrowserPicker.Views;

public partial class FirstRunWindow : Window
{
    public FirstRunWindow(FirstRunViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
