# WPF vs alternative UI frameworks

Saved from Claude conversation, March 2026.
This folder is gitignored — Claude will not read these files unless explicitly asked.

## WPF (what we use)

Windows Presentation Foundation. Released 2006. Uses XAML + C#. All the .xaml files
in InstallerClean and Simple Browser Picker are WPF.

Good at: Rich styling (entire custom design system — pill buttons, dark theme, custom
scrollbars), data binding (MVVM with CommunityToolkit.Mvvm), mature/stable, excellent
VS tooling.

Less good at: Windows-only. Custom window chrome is awkward. From 2006 so some
patterns feel dated. No native hot-reload.

## WinUI 3

Microsoft's "modern successor" to WPF. Same language (C#/XAML).
Custom chrome: built-in, easy.
Dark theme: built-in.
Trade-off: Requires Windows App SDK, packaging is messier, still less mature than WPF.
Would give easy custom title bars but introduces a different set of headaches.
Verdict: Not worth switching for IC or SBP. Worth watching for future apps.

## WPF + WPF-UI (Lepo.co)

What InstallerClean already uses. Adds modern Fluent-style controls on top of WPF.
ui:ThemesDictionary and ui:ControlsDictionary in App.xaml.
Verdict: Good choice. Consider for SBP too.

## Avalonia UI

"Cross-platform WPF." Very similar XAML syntax. Works on Win/Mac/Linux.
Custom chrome: easy.
Dark theme: easy.
If cross-platform is ever genuinely needed, Avalonia is the port path for existing
WPF apps. Smaller ecosystem than WPF but growing.
Verdict: For future apps IF cross-platform matters. Don't port existing apps.

## MAUI

Microsoft's cross-platform framework. Desktop support feels like an afterthought.
Not recommended for Windows-only desktop apps.

## Electron / Tauri

Web tech (JS or Rust) for desktop. Custom chrome is trivial (just HTML).
Electron is bloated (Chromium bundled). Tauri is lightweight but Rust.
Completely different stack — not a migration path from WPF.

## WinForms

Older .NET UI framework. Much simpler but much uglier. Cannot produce IC's design
system without immense pain. Not recommended for anything new.

## Verdict for No Faff WPF apps

WPF with WPF-UI is the right call for Windows-only utilities with custom dark themes.
It gives full styling control, solid MVVM, .NET 8, and single-file publishing.
Don't change IC or SBP. For new Windows-only apps, keep using WPF + WPF-UI.
If cross-platform is ever genuinely required, evaluate Avalonia then.
