using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace SimpleBrowserPicker.Services;

/// <summary>
/// Reads the Windows light/dark mode setting and the system accent colour,
/// then applies them to the application's resource dictionary.
/// </summary>
public static class ThemeService
{
    private const string PersonalizeKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static void Apply(Application app)
    {
        bool isDark = IsDarkMode();
        Color accent = GetAccentColour();
        UpdateResources(app.Resources, isDark, accent);
    }

    private static bool IsDarkMode()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            // AppsUseLightTheme = 0 means dark
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is int i)
                return i == 0;
        }
        catch { }
        return false;
    }

    private static Color GetAccentColour()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\DWM");
            object? value = key?.GetValue("AccentColor");
            if (value is int argb)
            {
                // Registry stores AABBGGRR (little-endian RGBA)
                byte r = (byte)( argb        & 0xFF);
                byte g = (byte)((argb >>  8) & 0xFF);
                byte b = (byte)((argb >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch { }
        return Color.FromRgb(0x00, 0x78, 0xD4); // Windows blue fallback
    }

    private static void UpdateResources(ResourceDictionary res, bool isDark, Color accent)
    {
        if (isDark)
        {
            res["SurfaceBrush"]   = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            res["BorderBrush"]    = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));
            res["HoverBrush"]     = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
            res["ForegroundBrush"]= new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
            res["MutedBrush"]     = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            res["ShortcutFgBrush"]= new SolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70));
            res["CardBrush"]      = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            res["InputBrush"]     = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
        }
        else
        {
            res["SurfaceBrush"]   = new SolidColorBrush(Colors.White);
            res["BorderBrush"]    = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            res["HoverBrush"]     = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
            res["ForegroundBrush"]= new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            res["MutedBrush"]     = new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x6E));
            res["ShortcutFgBrush"]= new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            res["CardBrush"]      = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7));
            res["InputBrush"]     = new SolidColorBrush(Colors.White);
        }

        res["AccentBrush"] = new SolidColorBrush(accent);
    }
}
