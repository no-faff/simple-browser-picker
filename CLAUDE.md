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

- Repo cloned from `no-faff/simple-browser-picker` ✓
- `LICENSE`, `CLAUDE.md`, `README.md` created ✓
- `.gitignore` — not yet (needs `dotnet new gitignore`)
- Solution and project — not yet created
- No code written yet

Next step: once .NET 8 SDK is installed, run `dotnet new gitignore` then follow
the build order in the spec (§ "What to build first").

## Key decisions

- HKCU registration (no admin required)
- JSON config (not registry) — portable, human-readable
- No installer — single exe, first-run wizard handles setup
- Move-don't-delete philosophy: no destructive defaults
- End users need no runtime installed (self-contained exe)
- Developers building from source need the .NET 8 SDK
