using System.Windows;
using SimpleBrowserPicker.ViewModels;

namespace SimpleBrowserPicker.Views;

public partial class FirstRunWindow : Window
{
    public FirstRunWindow(FirstRunViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
