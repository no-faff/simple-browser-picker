# Simple Browser Picker

Part of the **No Faff** suite of small Windows utilities (github.com/no-faff).

## What it does

A silent link router. Registers as your default browser, then transparently
routes links to the right browser/profile based on rules. Most links open
instantly in your default browser with zero interruption. Rules let you send
specific domains to specific browsers or browser profiles.

The picker popup is **not** the main interaction — it's an optional fallback
for when no rule matches and no default is set, or when the user explicitly
wants to choose.

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
4. **Silent by default** — not a "pick every time" popup machine.

## Core interaction model

1. **Default browser** — user sets one during first run. All links with no
   matching rule open here silently. Zero popups.
2. **Rules** — domain → browser/profile mappings. Also silent.
3. **Picker** — only appears when deliberately invoked or when no default is
   set. Optional "always pick" mode exists but is off by default.
4. **"Always use" checkbox** — in the picker, lets you create a rule on the
   spot so you never see the picker for that domain again.

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

The core rework is done: fallback browser, rule creation UI, silent routing.
Commit `6aab08e`. The app compiles clean but hasn't been end-to-end tested
yet (registration → link opens → rule fires → fallback fires). That's the
next session task — do it together with the user.

### UX polish pass in progress

After the rework, Opus self-assessed against BrowserPicker and found these
gaps. Items 2–7 are being fixed now. Item 1 is deferred (test together).

1. **End-to-end testing** — DEFERRED. Do with the user.
2. **Settings rules tab is dense** — fallback dropdown, rule list and add-rule
   form all in one tab. Not terrible, but could be cleaner. Fix: placeholder
   text on the domain field ("e.g. github.com or *.google.com"), tighter
   layout, visual breathing room.
3. **No edit on rules** — can add and delete but not edit. Typo = delete and
   re-add. BP has the same limitation. Fix: not full inline editing, but at
   least pre-populate the add form when a rule is selected so editing is
   select → tweak → add (replaces by domain).
4. **Fallback dropdown shows raw detected names** — profile names like
   "Profile 1" come from Chromium internals. Fix: this is actually already
   handled by BrowserDetector which reads `shortcut_name` / `name` from
   Local State. If those are still cryptic it's because the user hasn't
   renamed them in the browser. No code fix needed, but worth checking.
5. **No visual confirmation when a rule is added** — fix: scroll to and
   highlight the new rule, or at minimum select it in the list.
6. **No way to change fallback from picker** — the "use for all other links"
   checkbox disappears once a fallback is set. Fix: show it always, but
   pre-check it and show the current fallback name so the user can switch.
7. **No wildcard hint** — domain field accepts `*.google.com` but nothing
   tells the user. Fix: placeholder text.

### Design philosophy (for future Opus sessions)

This app should feel like BrowserPicker but better in every way a user would
notice. The bar is not "does the code work" but "would a BP user switch to
this and never look back". That means:
- Every BP feature must exist and work at least as well
- Profile detection (our killer feature) must be obviously better
- The settings UI must be at least as clear as BP's, ideally clearer
- Zero-config happy path: install → register → previous browser becomes
  fallback → done. Rules are gravy.

## Known WPF + WinForms issues

`UseWindowsForms=true` is set (for `Screen.FromPoint`). This causes namespace
clashes. Each affected file needs explicit using aliases — see MEMORY.md.
