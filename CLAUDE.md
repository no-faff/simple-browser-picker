# Simple browser picker

Part of the **No faff** suite of small Windows utilities (github.com/no-faff).

## What it does

Registers as your default browser. When any app opens a link, it shows a
clean picker letting you choose which browser to use. Supports rules to
auto-route domains to specific browsers.

## Tech stack

- C# / .NET 8 / WPF
- Single-exe distribution (self-contained, trimmed)
- Config: JSON in %LOCALAPPDATA%\SimpleBrowserPicker\config.json

## Brand / conventions

- Sentence case throughout — no title case
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

## Build spec

Full spec written by Opus lives at:
`C:\windows-installer-cleaner\sonnet-prompt-browser-picker.md`

## Current state

- Repo, `.gitignore`, solution and project all created ✓
- Full application built and compiles clean ✓
- Smoke-tested with `dotnet run -- "https://example.com"` ✓
- Placeholder icon in Assets/icon.ico ✓

**What's implemented:**
- Browser detection (registry scan + Chromium/Firefox profiles)
- URL parsing + SafeLinks/Google redirect unwrapping
- Domain-based rules engine (auto-open without picker)
- Picker window — borderless, multi-monitor centred, keyboard shortcuts 1–9
- "Always use for [domain]" checkbox
- Settings window — browsers tab, rules tab, about tab
- First-run wizard
- HKCU protocol registration / unregistration
- Light/dark mode + Windows accent colour

**Still to do / for Opus review:**
- Real icon (placeholder blue square currently)
- Polish pass on UI (fonts, spacing, animations)
- Edge cases in profile detection (guest profiles, default profile only)
- Keyboard navigation within picker
- Start-a-new-Opus-chat for review

Next step: start a new Opus chat in this folder for review.

## Key decisions

- HKCU registration (no admin required)
- JSON config (not registry) — portable, human-readable
- No installer — single exe, first-run wizard handles setup
- Move-don't-delete philosophy: no destructive defaults
- End users need no runtime installed (self-contained exe)
- Developers building from source need the .NET 8 SDK
