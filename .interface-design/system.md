# Simple Browser Picker — Design System

## Direction

**Upscayl-inspired.** Clean, bold, simple, cartoon-like. The app should feel
like a well-made toy, not a control panel. "Unpretentious but sexy" — integrated
dark design that shouts "simple to use".

Reference apps: Upscayl (primary), LosslessCut, Stacher, Vibe.

## Who uses this

Someone clicking links all day — email, Slack, Teams. The picker appears
*between* tasks. They choose and move on in under a second. The settings user
is the same person in a rarer "let me sort this out" mode.

## Palette

### Dark mode (primary)

Deep navy, not neutral grey. Tailwind slate scale.

| Token          | Hex       | Role                        |
|----------------|-----------|-----------------------------|
| Surface        | `#111827` | Window background (slate-900) |
| Card           | `#1E293B` | Raised sections (slate-800)   |
| Input          | `#0F172A` | Text fields (slate-950)       |
| Border         | `#2D3A4F` | Subtle dividers               |
| Hover          | `#1E293B` | Interactive hover (slate-800) |
| Foreground     | `#E2E8F0` | Primary text (slate-200)      |
| Muted          | `#94A3B8` | Secondary text (slate-400)    |
| ShortcutFg     | `#64748B` | Keyboard hints (slate-500)    |
| Accent         | System    | Windows accent colour         |

### Light mode

| Token          | Hex       | Role                        |
|----------------|-----------|-----------------------------|
| Surface        | `#FFFFFF` | Window background             |
| Card           | `#F1F5F9` | Raised sections (slate-100)   |
| Input          | `#FFFFFF` | Text fields                   |
| Border         | `#E2E8F0` | Subtle dividers (slate-200)   |
| Hover          | `#F1F5F9` | Interactive hover (slate-100) |
| Foreground     | `#0F172A` | Primary text (slate-950)      |
| Muted          | `#64748B` | Secondary text (slate-500)    |
| ShortcutFg     | `#94A3B8` | Keyboard hints (slate-400)    |
| Accent         | System    | Windows accent colour         |

### Why navy, not grey

Navy has personality. Neutral grey is anonymous. The blue undertone gives warmth
without being warm — it's what makes Upscayl feel inviting rather than clinical.

## Shape

| Element         | Corner radius | Notes                              |
|-----------------|---------------|------------------------------------|
| Buttons         | 20px          | Pill-shaped — the signature shape  |
| Cards           | 14px          | Softer than buttons but not pill   |
| Inputs          | 8px           | Deliberate contrast with buttons   |
| Picker window   | 12px          | Outer border                       |
| ListBoxItem     | 6px           | Tight, functional                  |
| Tooltip         | 6px           | Small, accent-coloured             |

### Why pill buttons

The pill shape says "click me, I'm simple". It's the single biggest visual
trait borrowed from Upscayl. Every action button in Upscayl is a pill.

## Typography

- **Font:** Segoe UI Variable Text, fallback Segoe UI
- **Base size:** 13.5px
- **Section headers:** 16px, Bold, accent colour
- **Section descriptions:** 12.5px, muted colour, line-height 18px
- **Code/paths:** Consolas, 11.5-12.5px

### Why Segoe UI

Windows-native. The Variable version adds optical sizing. Using the OS font
makes the app feel like it belongs on the system.

## Spacing

- **Base unit:** 8px
- **Button padding:** 18px horizontal, 9px vertical
- **Card padding:** 20px
- **Between section groups:** 24-28px
- **Between cards within a group:** 10px
- **Section description bottom margin:** 20px

### Breathing room

Upscayl is generous with whitespace. Sections breathe. The current design
spaces section groups at 24-28px — noticeably more than the previous 16-20px.

## Depth

- **Picker window:** Drop shadow (blur 28, depth 6, opacity 0.35) — it's a
  transient overlay, needs presence
- **Settings window:** Drop shadow (blur 32, depth 8, opacity 0.4)
- **Cards:** No shadow, differentiated by background colour alone
- **Popups (ComboBox):** Drop shadow (blur 12, depth 4, opacity 0.25)
- **Tooltips:** Drop shadow (blur 12, depth 3, opacity 0.25)

## Components

### Buttons
- **Primary:** Accent background, white text, pill-shaped
- **Secondary:** Hover background, foreground text, 1px border, pill-shaped
- **Browser button (picker):** Transparent, 8px radius, full-width hover

### Toggle switch
- Track: 44x24px, 12px radius
- Thumb: 18x18px circle
- Off: border colour track, muted thumb
- On: accent track, white thumb

### Pill tabs (settings nav)
- RadioButtons styled as pills, 8px radius
- Active: hover background, semibold, foreground text
- Inactive: transparent, muted text

### Checkbox (picker)
- 14x14px box, 3px radius
- Tick: accent colour path
- Used only in the picker for "remember" and "set fallback"

### Section pattern
1. Section header (accent, bold)
2. Section description (muted, wrapping)
3. Card with content

## Accent stripe

Both picker and settings windows have a thin (3px) accent-coloured stripe at
the very top. In the picker it's at 50% opacity. This is a subtle brand mark.

## Tooltips

Accent-coloured background, white text, 6px radius. Most apps use grey
tooltips — the accent colour makes these feel considered. Borrowed from
Upscayl's purple tooltip style.

## Scrollbars

Thin (4px), rounded thumb, no track. Opacity-based visibility:
- Default: 35% opacity
- Hover: 60%
- Dragging: 80%

## Theme switching

Light defaults are set in Styles.xaml. ThemeService.Apply() reads the Windows
registry (AppsUseLightTheme) and overwrites all colour tokens at runtime.
The system accent colour is also read from the registry (DWM AccentColor).
