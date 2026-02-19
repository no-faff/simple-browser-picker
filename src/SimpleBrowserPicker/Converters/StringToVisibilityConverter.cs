using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleBrowserPicker.Converters;

/// <summary>
/// Returns Visible when the string is non-empty, Collapsed otherwise.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
