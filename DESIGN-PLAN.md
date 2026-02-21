# Settings window redesign — "Upscayl/Vibe" integrated dark design

## Context

The settings window currently looks like "Windows controls painted dark" — native
title bar, GroupBox border lines, cramped spacing, system-themed ComboBox dropdowns.
Vibe, Upscayl and Stacher all share: borderless custom chrome, card-based layouts
with background-shade separation (no drawn borders), generous spacing, and accent
colour used sparingly. This plan rebuilds the settings window to match that standard.

## New brushes

Add two brushes (dark / light values):

| Brush | Dark | Light | Purpose |
|-------|------|-------|---------|
| `CardBrush` | `#333333` | `#F7F7F7` | Card section backgrounds |
| `InputBrush` | `#2A2A2A` | `#FFFFFF` | Input fields inside cards (subtle inset) |

Added to both `Styles.xaml` (defaults) and `ThemeService.cs` (runtime overrides).

## Implementation steps

### 1. Borderless window + custom title bar

Replace native chrome with `WindowStyle="None"` + `AllowsTransparency="True"` +
outer Border with `CornerRadius="8"`, `DropShadowEffect`, and `Margin="10"` for
shadow space. `ResizeMode="CanResizeWithGrip"` keeps it resizable.

Custom title bar contains:
- 2px accent stripe at the very top (matches picker)
- **Pill-style tab buttons** centred (Browsers / Rules / About)
- Close (X) button right-aligned
- Title bar area wired to `DragMove()` for window dragging

### 2. Pill tabs replacing TabControl chrome

Three `RadioButton`s styled as pills in the title bar:
- Unselected: transparent bg, muted text
- Selected: `HoverBrush` bg, white text, `CornerRadius="4"`
- Hover: subtle `HoverBrush` bg

The `TabControl` stays for content switching but its native tab strip is hidden
(`TabItem` visibility collapsed). Pill clicks set `TabControl.SelectedIndex`.

### 3. Card sections replacing GroupBox/ListBorder

New `CardBorder` style: `CardBrush` background, `CornerRadius="6"`, `Padding="16"`,
**no border lines**. Background contrast alone separates sections.

Replace every `GroupBox` with a card + muted label above it. Replace `ListBorder`
with a card. Section headers become 12px SemiBold `MutedBrush` TextBlocks.

### 4. Full ComboBox dark template

Complete ControlTemplate override covering:
- Toggle button: `InputBrush` bg, chevron path, accent border on focus
- Dropdown popup: `CardBrush` bg, rounded corners, drop shadow
- ComboBoxItem: transparent bg, `HoverBrush` on highlight, rounded corners

### 5. Input field refinement

TextBox switches to `InputBrush` bg with transparent border (no outline). Focus
state shows accent border ring. This matches the "no visible borders" goal.

### 6. Spacing overhaul

- Window content padding: **20px** (up from 12px)
- Between cards: **12px** vertical gap
- Inside cards: **16px** padding
- Between form rows: **8px**

## Files to modify

| File | Changes |
|------|---------|
| `Resources/Styles.xaml` | New brushes, PillTab, CardBorder, full ComboBox template, TextBox border tweak |
| `Services/ThemeService.cs` | CardBrush + InputBrush in dark/light branches |
| `Views/SettingsWindow.xaml` | Borderless shell, custom title bar, pill tabs, card sections, spacing |
| `Views/SettingsWindow.xaml.cs` | DragMove, close handler, tab pill click, Esc key |

## Verification

1. Build: `dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj`
2. Launch with no args → settings opens
3. Check: borderless window with rounded corners and shadow
4. Check: pill tabs switch content, accent underline/highlight on selected
5. Check: all sections are cards (no border lines, subtle background separation)
6. Check: ComboBox dropdown matches dark theme
7. Check: TextBox inputs have no visible border, accent ring on focus
8. Check: generous spacing throughout
9. Launch with `"https://example.com"` → picker → gear icon → settings transition
