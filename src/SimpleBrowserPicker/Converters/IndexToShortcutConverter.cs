using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleBrowserPicker.Converters;

/// <summary>
/// Converts a 0-based alternation index to a 1-based keyboard shortcut label.
/// Returns the digit "1"–"9" for indices 0–8, empty string for anything else.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
public class IndexToShortcutConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0 && index < 9)
            return (index + 1).ToString();
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
