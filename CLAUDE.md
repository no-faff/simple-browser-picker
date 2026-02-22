# Simple Browser Picker

Part of the **No Faff** suite of small Windows utilities (github.com/no-faff).

## What it does

A browser picker and silent link router. Registers as your default browser,
then either shows a picker popup or silently routes links to the right
browser/profile based on rules — user's choice.

## Why it exists — competitive landscape

The user was already using **BrowserPicker** (Andrew Longmore) and liked it.
Sonnet and Opus researched alternatives and found gaps worth filling:

| App | Profiles | Rules | Default fallback | SafeLinks | Maintained | Free | Notes |
|-----|----------|-------|-------------------|-----------|------------|------|-------|
| BrowserPicker (Longmore) | Chrome/Edge only | Text match | Yes (auto-pick) | Yes | Yes | Yes | The one the user was using |
| BrowserPicker (mortenn) | Chrome/Firefox | Regex | Yes | No (open issue) | Yes (.NET 9) | Yes | 408 stars, needs runtime |
| BrowserSelect (zumoshi) | Chrome only | Yes | No | No | Dead (2019) | Yes | .NET Framework 4 |
| Switchbar | Most browsers | Yes | Yes | Yes | Yes | Freemium ($5) | Cross-platform, most polished |

**Gaps our app fills:**
1. **Broad profile support** — all Chromium browsers + all Firefox-family
   (Zen, Floorp, LibreWolf, Waterfox, Mullvad). No existing tool does this.
2. **Single exe, no runtime, no installer** — self-contained .NET 8. Others
   need .NET 9 runtime, .NET Framework 4, or are Electron/paid.
3. **SafeLinks/redirect unwrapping** — only Longmore's and Switchbar do this.
4. **Better rule matching** — domain-based with wildcards and optional
   path/regex support. BP uses substring matching which is error-prone.

## Core interaction model

Two modes, user-configurable:

1. **"Always ask" mode** (like BrowserPicker) — picker appears for every
   link that doesn't match a rule. No fallback browser.
2. **"Use fallback" mode** — a default browser catches everything that
   doesn't match a rule. Silent. The picker only appears if no fallback
   is set or if the user explicitly wants to choose.

Either way:
- **Rules** route specific domains/patterns to specific browsers silently.
- **"Remember my choice" checkbox** in the picker creates a rule on the
  spot so you never see the picker for that domain again.
- **Fallback checkbox** in the picker lets you set/change the fallback.

## Tech stack

- C# / .NET 8 / WPF
- Single-exe distribution (self-contained, trimmed)
- Config: JSON in %LOCALAPPDATA%\SimpleBrowserPicker\config.json

## Brand / conventions

- Title case for the app name: "Simple Browser Picker"
- Sentence case for UI labels and body text
- British English — colour, organise, etc.
- No Oxford comma

## Project structure

- `src/SimpleBrowserPicker/` — main application
- Models, Services, ViewModels, Views pattern (MVVM)

## Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

> **Installation note:** the .NET SDK installer can hang on Windows if another
> MSI install is in progress. If it stalls, kill it via Task Manager and reboot
> before retrying — the reboot clears the Windows Installer lock.

```shell
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

## Publishing (single exe)

```shell
dotnet publish src/SimpleBrowserPicker/SimpleBrowserPicker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## Testing

```shell
dotnet run --project src/SimpleBrowserPicker -- "https://example.com"
```

## Key decisions

- HKCU registration (no admin required)
- JSON config (not registry) — portable, human-readable
- No installer — single exe, first-run wizard handles setup
- Move-don't-delete philosophy: no destructive defaults
- End users need no runtime installed (self-contained exe)
- Developers building from source need the .NET 8 SDK

## Current state (Feb 2026)

BP feature parity complete. Commit `edb20ed`.

All three windows borderless with consistent dark/light theme. Light mode
follows the Windows theme setting automatically.

### BrowserPicker feature parity — all done

1. ✅ **Open tab** — address bar, clipboard paste, browser list for manual URL opening
2. ✅ **Redirect check** — picker shows "unwrapped from redirect" for SafeLinks/Google
3. ✅ **Security check** — Google Transparency Report button on Open tab
4. ✅ **Config export/import** — buttons in About tab
5. ✅ **Browser editing** — select a browser to edit name, path, args; saved as overrides
6. ✅ **Reordering** — up/down buttons and drag-and-drop for browsers and rules
7. ✅ **Filter/search** — filter boxes on Browsers and Rules tabs
8. ✅ **Rule exceptions** — add rule with no browser = always show picker
9. ✅ **SharePoint/Office** — .xlsx/.docx/.pptx URLs open in desktop Office apps
10. ✅ **"Always ask" mode** — toggle in Rules tab
12. ✅ **Suspend rules** — temporarily ignore all rules

Skipped: 11 (custom icon paths), 13 (collapse/expand) — not needed.

Beyond BP parity: path pattern matching in rules (`github.com/gist/*`, `*.corp.com/path`) and xUnit test coverage for UrlParser.

### Still to do
- End-to-end testing with user
- Translations (deferred)
- Drag-and-drop reordering (up/down buttons work; full DnD is v2)
- Path-based rule matching (github.com/gist/*) and regex — v2

### Design philosophy

This app should feel like BrowserPicker but better in every way a user would
notice. The bar is not "does the code work" but "would a BP user switch to
this and never look back". That means:
- Every BP feature must exist and work at least as well
- Profile detection (our killer feature) must be obviously better
- The settings UI must be at least as clear as BP's, ideally clearer
- Both "always ask" and "silent fallback" modes work well
- UI feels like Upscayl/Stacher/Vibe — integrated dark design, not
  "Windows with a dark coat of paint"

## Known WPF + WinForms issues

`UseWindowsForms=true` is set (for `Screen.FromPoint`). This causes namespace
clashes. Each affected file needs explicit using aliases — see MEMORY.md.
