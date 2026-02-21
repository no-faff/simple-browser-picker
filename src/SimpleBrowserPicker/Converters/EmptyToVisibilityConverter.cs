using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleBrowserPicker.Converters;

/// <summary>
/// Returns Visible when the string is empty, Collapsed otherwise.
/// Used for placeholder text that should disappear when the user types.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class EmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
