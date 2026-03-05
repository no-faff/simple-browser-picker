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
            // Deep palette matching InstallerClean (Upscayl-inspired Tailwind slate)
            res["SurfaceBrush"]   = new SolidColorBrush(Color.FromRgb(0x02, 0x06, 0x17)); // #020617 slate-950+
            res["CardBrush"]      = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)); // #0F172A slate-950
            res["InputBrush"]     = new SolidColorBrush(Color.FromRgb(0x02, 0x06, 0x17)); // #020617 recessed below card
            res["HoverBrush"]     = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)); // #1E293B slate-800
            res["BorderBrush"]    = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)); // #334155 slate-700
            res["ForegroundBrush"]= new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)); // #E2E8F0 slate-200
            res["MutedBrush"]     = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // #94A3B8 slate-400
            res["ShortcutFgBrush"]= new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)); // #64748B slate-500
        }
        else
        {
            res["SurfaceBrush"]   = new SolidColorBrush(Colors.White);
            res["BorderBrush"]    = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)); // #E2E8F0 slate-200
            res["HoverBrush"]     = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)); // #F1F5F9 slate-100
            res["ForegroundBrush"]= new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)); // #0F172A slate-950
            res["MutedBrush"]     = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)); // #64748B slate-500
            res["ShortcutFgBrush"]= new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // #94A3B8 slate-400
            res["CardBrush"]      = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)); // #F1F5F9 slate-100
            res["InputBrush"]     = new SolidColorBrush(Colors.White);
        }

        res["AccentBrush"] = new SolidColorBrush(accent);
    }
}
