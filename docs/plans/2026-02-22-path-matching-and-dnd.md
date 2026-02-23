# Path Matching and Drag-and-Drop Implementation Plan

> **Status: COMPLETE** — all 8 tasks implemented and merged to `main` (commits `9a64384`–`5b0fbfa`, Feb 2026).

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extend rule matching to support URL path patterns (e.g. `github.com/gist/*`) and add drag-and-drop reordering to the Rules and Browsers lists.

**Architecture:** The `BrowserRule.Domain` field (name kept for JSON backward compat) is extended to optionally include a path segment. `UrlParser` gains a new `UrlMatches(url, pattern)` method that handles both domain-only and domain+path patterns. DnD is implemented in code-behind with a `MoveRuleTo`/`MoveBrowserTo` method on the ViewModel.

**Tech Stack:** C# / .NET 8 / WPF. xUnit for unit-testing UrlParser logic (no test project yet — one is created in Task 1).

---

## Task 1: Create a test project for UrlParser

**Files:**
- Create: `src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj`
- Create: `src/SimpleBrowserPicker.Tests/UrlParserTests.cs`

**Step 1: Scaffold the test project**

```bash
dotnet new xunit -n SimpleBrowserPicker.Tests -o src/SimpleBrowserPicker.Tests --framework net8.0
dotnet add src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj reference src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

> Note: the main project targets `net8.0-windows` but xUnit can reference it from `net8.0`. If the build fails with a TFM mismatch, change the test project's `<TargetFramework>` to `net8.0-windows` in its csproj.

**Step 2: Verify the test project builds**

```bash
dotnet build src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj
```

Expected: Build succeeded.

**Step 3: Write the first failing test (domain-only, confirming existing behaviour)**

Replace the default `UnitTest1.cs` content (or rename to `UrlParserTests.cs`) with:

```csharp
using SimpleBrowserPicker.Services;
using Xunit;

namespace SimpleBrowserPicker.Tests;

public class UrlParserTests
{
    // -----------------------------------------------------------------------
    // Existing domain-only behaviour — must not regress
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("https://github.com/foo",        "github.com",     true)]
    [InlineData("https://www.github.com/foo",    "github.com",     true)]
    [InlineData("https://gist.github.com/foo",   "github.com",     false)]
    [InlineData("https://gist.github.com/foo",   "*.github.com",   true)]
    [InlineData("https://github.com/foo",        "*.github.com",   false)]
    [InlineData("https://notgithub.com/foo",     "github.com",     false)]
    public void DomainOnly_MatchesSameAsBefore(string url, string pattern, bool expected)
    {
        Assert.Equal(expected, UrlParser.UrlMatches(url, pattern));
    }
}
```

**Step 4: Run — expect failure** (`UrlMatches` does not exist yet)

```bash
dotnet test src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj --no-build 2>&1 | head -20
```

Expected: compile error — `UrlParser` has no `UrlMatches` method.

**Step 5: Commit the test project skeleton**

```bash
git add src/SimpleBrowserPicker.Tests/
git commit -m "Add test project for UrlParser"
```

---

## Task 2: Add `UrlParser.UrlMatches` — domain-only cases

**Files:**
- Modify: `src/SimpleBrowserPicker/Services/UrlParser.cs`

**Step 1: Add the new method (domain-only, delegates to existing `DomainMatches`)**

Add this method to `UrlParser` right after `DomainMatches`:

```csharp
/// <summary>
/// Returns true if <paramref name="url"/> matches <paramref name="pattern"/>.
///
/// Pattern syntax:
///   "github.com"          — domain-only, matches github.com and www.github.com
///   "*.google.com"        — subdomain wildcard, matches any subdomain of google.com
///   "github.com/gist"     — domain + path prefix, matches any URL under /gist
///   "github.com/gist/*"   — domain + path wildcard, same but explicit
///   "*.corp.com/internal" — combined subdomain wildcard + path prefix
/// </summary>
public static bool UrlMatches(string url, string pattern)
{
    if (string.IsNullOrWhiteSpace(pattern))
        return false;

    if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        return false;

    pattern = pattern.ToLowerInvariant();

    int slashIdx = pattern.IndexOf('/');
    if (slashIdx < 0)
    {
        // Domain-only pattern — existing behaviour
        return DomainMatches(uri.Host.ToLowerInvariant(), pattern);
    }

    string domainPart = pattern[..slashIdx];
    string pathPart   = pattern[slashIdx..]; // includes the leading /

    if (!DomainMatches(uri.Host.ToLowerInvariant(), domainPart))
        return false;

    return PathMatches(uri.AbsolutePath.ToLowerInvariant(), pathPart);
}

private static bool PathMatches(string urlPath, string pathPattern)
{
    if (!pathPattern.Contains('*'))
        return urlPath.StartsWith(pathPattern, StringComparison.Ordinal);

    // Convert wildcard pattern to regex: escape, then replace \* with .*
    string regexPattern = "^" +
        System.Text.RegularExpressions.Regex.Escape(pathPattern)
            .Replace(@"\*", ".*") +
        "$";
    return System.Text.RegularExpressions.Regex.IsMatch(
        urlPath, regexPattern, System.Text.RegularExpressions.RegexOptions.None);
}
```

**Step 2: Run the domain-only tests — expect pass**

```bash
dotnet test src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj -v minimal
```

Expected: 6 passed.

**Step 3: Commit**

```bash
git add src/SimpleBrowserPicker/Services/UrlParser.cs
git commit -m "Add UrlParser.UrlMatches with domain-only support"
```

---

## Task 3: Add path-matching tests and make them pass

**Files:**
- Modify: `src/SimpleBrowserPicker.Tests/UrlParserTests.cs`

**Step 1: Add path-matching test cases to the existing theory, or add a new theory**

Add below the existing `DomainOnly_MatchesSameAsBefore` theory:

```csharp
[Theory]
// Path prefix (no wildcard)
[InlineData("https://github.com/gist/abc",      "github.com/gist",    true)]
[InlineData("https://github.com/gist/abc/def",  "github.com/gist",    true)]
[InlineData("https://github.com/orgs/foo",      "github.com/gist",    false)]
// Path wildcard
[InlineData("https://github.com/gist/abc",      "github.com/gist/*",  true)]
[InlineData("https://github.com/orgs/foo",      "github.com/gist/*",  false)]
// Trailing-slash patterns
[InlineData("https://github.com/gist/abc",      "github.com/gist/",   true)]  // prefix includes /
[InlineData("https://github.com/gist",          "github.com/gist",    true)]  // exact path, no trailing slash
// Subdomain wildcard + path
[InlineData("https://maps.google.com/maps/dir", "*.google.com/maps",  true)]
[InlineData("https://maps.google.com/search",   "*.google.com/maps",  false)]
// Domain mismatch, path would match
[InlineData("https://notgithub.com/gist/abc",   "github.com/gist",    false)]
public void PathPattern_MatchesCorrectly(string url, string pattern, bool expected)
{
    Assert.Equal(expected, UrlParser.UrlMatches(url, pattern));
}
```

**Step 2: Run — expect all pass**

```bash
dotnet test src/SimpleBrowserPicker.Tests/SimpleBrowserPicker.Tests.csproj -v minimal
```

Expected: all tests pass. If any fail, fix `PathMatches` in `UrlParser.cs` before committing.

**Step 3: Commit**

```bash
git add src/SimpleBrowserPicker.Tests/UrlParserTests.cs
git commit -m "Add path-matching tests for UrlParser.UrlMatches"
```

---

## Task 4: Wire `UrlMatches` into the routing engine

The main app currently calls `UrlParser.DomainMatches(domain, rule.Domain)` where `domain` is the extracted hostname. It needs to call `UrlParser.UrlMatches(url, rule.Domain)` with the full URL instead.

**Files:**
- Modify: `src/SimpleBrowserPicker/App.xaml.cs` (line ~109–118, `FindMatchingRule`)

**Step 1: Change `FindMatchingRule` signature and body**

Find:
```csharp
private BrowserRule? FindMatchingRule(string domain)
{
    if (string.IsNullOrEmpty(domain)) return null;

    foreach (var rule in _config.Rules)
    {
        if (UrlParser.DomainMatches(domain, rule.Domain))
            return rule;
    }
    return null;
}
```

Replace with:
```csharp
private BrowserRule? FindMatchingRule(string url)
{
    if (string.IsNullOrEmpty(url)) return null;

    foreach (var rule in _config.Rules)
    {
        if (UrlParser.UrlMatches(url, rule.Domain))
            return rule;
    }
    return null;
}
```

**Step 2: Fix the call site**

In `OnStartup`, find:
```csharp
string domain = UrlParser.ExtractDomain(url);
// ...
BrowserRule? rule = _config.SuspendRules ? null : FindMatchingRule(domain);
```

Change the `FindMatchingRule` call to pass `url` (not `domain`). The `domain` variable is still needed for the picker display — keep extracting it, just don't pass it to `FindMatchingRule`:
```csharp
BrowserRule? rule = _config.SuspendRules ? null : FindMatchingRule(url);
```

**Step 3: Build**

```bash
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

Expected: Build succeeded, 0 errors.

**Step 4: Manual smoke test**

```bash
dotnet run --project src/SimpleBrowserPicker -- "https://github.com/gist/abc123"
```

Add a rule `github.com/gist` pointing to a browser, then run the above. The browser should open directly without showing the picker.

**Step 5: Commit**

```bash
git add src/SimpleBrowserPicker/App.xaml.cs
git commit -m "Wire UrlMatches into rule routing — support path patterns"
```

---

## Task 5: Update the Settings UI for path patterns

The "Domain" label and placeholder text in the Rules tab need to reflect the new capability.

**Files:**
- Modify: `src/SimpleBrowserPicker/Views/SettingsWindow.xaml` (Rules tab, add-rule card)
- Modify: `src/SimpleBrowserPicker/ViewModels/SettingsViewModel.cs` (AddRule, filter)

**Step 1: Update XAML label and placeholder**

In `SettingsWindow.xaml`, in the add-rule card (around line 499):

Find:
```xml
<TextBlock Grid.Row="0" Grid.Column="0" Text="Domain"
           VerticalAlignment="Center" Margin="0,4,8,4"/>
```
Replace `Text="Domain"` with `Text="Pattern"`.

Find the placeholder TextBlock:
```xml
<TextBlock Text="e.g. github.com or *.google.com"
```
Replace with:
```xml
<TextBlock Text="e.g. github.com, *.corp.com, github.com/gist/*"
```

Also update the section description text (around line 483–484):

Find:
```xml
<TextBlock Style="{StaticResource SectionDescription}"
           Text="Type a domain and choose which browser it should open in. Use *.example.com to match all subdomains."/>
```
Replace with:
```xml
<TextBlock Style="{StaticResource SectionDescription}"
           Text="Type a domain or URL pattern and choose which browser to open it in. Use *.example.com for subdomains, or example.com/path/* for path matching."/>
```

In the rules ListBox `DataTemplate`, the pattern column binds to `{Binding Domain}` — this is fine to leave as-is since the property is still called `Domain`. No change needed there.

**Step 2: Update SettingsViewModel.AddRule — don't force domain-only normalisation**

In `SettingsViewModel.cs`, `AddRule()`, find:
```csharp
string domain = NewRuleDomain.Trim().ToLowerInvariant();
```
Change to:
```csharp
string domain = NewRuleDomain.Trim().ToLowerInvariant();
// Note: "domain" may include a path pattern (e.g. github.com/gist/*) — that is intentional.
// ToLowerInvariant is safe for URLs.
```

(No code change needed — just verify the existing `.ToLowerInvariant()` call is harmless for paths, which it is.)

Also update the duplicate-detection check in `AddRule`:

Find:
```csharp
var existing = _appConfig.Rules.FirstOrDefault(r =>
    string.Equals(r.Domain, domain, StringComparison.OrdinalIgnoreCase));
```
This is correct — it deduplicates by the full pattern. No change needed.

**Step 3: Update the rule filter in SettingsViewModel constructor**

The filter already searches `r.Domain` — since `Domain` now holds path patterns too, it will naturally search within them. No change needed.

**Step 4: Build and verify**

```bash
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

**Step 5: Commit**

```bash
git add src/SimpleBrowserPicker/Views/SettingsWindow.xaml
git commit -m "Update rule UI labels for path pattern support"
```

---

## Task 6: Drag-and-drop reordering — Rules list

WPF DnD for the Rules `ListBox`. The existing up/down buttons are kept. DnD is additive.

**Files:**
- Modify: `src/SimpleBrowserPicker/Views/SettingsWindow.xaml` (Rules ListBox)
- Modify: `src/SimpleBrowserPicker/Views/SettingsWindow.xaml.cs`
- Modify: `src/SimpleBrowserPicker/ViewModels/SettingsViewModel.cs`

**Step 1: Add `MoveRuleTo` to SettingsViewModel**

In `SettingsViewModel.cs`, add this method near `MoveRule`:

```csharp
/// <summary>Called by the view after a drag-drop reorder.</summary>
public void MoveRuleTo(BrowserRule dragged, BrowserRule target)
{
    int from = Rules.IndexOf(dragged);
    int to   = Rules.IndexOf(target);
    if (from < 0 || to < 0 || from == to) return;

    Rules.Move(from, to);

    _appConfig.Rules.Clear();
    foreach (var r in Rules) _appConfig.Rules.Add(r);
    _configService.Save(_appConfig);
}
```

**Step 2: Enable DnD on the Rules ListBox in XAML**

Find the Rules `ListBox` element (around line 441):
```xml
<ListBox ItemsSource="{Binding Rules}"
         SelectedItem="{Binding SelectedRule}"
         BorderThickness="0"
         Background="Transparent"
         MaxHeight="180">
```
Add `AllowDrop="True"` and event handlers:
```xml
<ListBox x:Name="RulesListBox"
         ItemsSource="{Binding Rules}"
         SelectedItem="{Binding SelectedRule}"
         BorderThickness="0"
         Background="Transparent"
         MaxHeight="180"
         AllowDrop="True"
         PreviewMouseLeftButtonDown="RulesListBox_PreviewMouseLeftButtonDown"
         PreviewMouseMove="RulesListBox_PreviewMouseMove"
         Drop="RulesListBox_Drop">
```

**Step 3: Add DnD code-behind to SettingsWindow.xaml.cs**

Open `SettingsWindow.xaml.cs`. Add these fields and methods:

```csharp
// -----------------------------------------------------------------------
// Rules drag-and-drop
// -----------------------------------------------------------------------

private Point _ruleDragStart;
private BrowserRule? _draggedRule;

private void RulesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    _ruleDragStart = e.GetPosition(null);
}

private void RulesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
{
    if (e.LeftButton != MouseButtonState.Pressed || _draggedRule is not null) return;

    var pos  = e.GetPosition(null);
    var diff = pos - _ruleDragStart;
    if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
        Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

    if (sender is ListBox lb && lb.SelectedItem is BrowserRule rule)
    {
        _draggedRule = rule;
        DragDrop.DoDragDrop(lb, rule, DragDropEffects.Move);
        _draggedRule = null;
    }
}

private void RulesListBox_Drop(object sender, DragEventArgs e)
{
    if (_draggedRule is null) return;
    if (sender is not ListBox lb) return;

    var target = FindListBoxItem<BrowserRule>(lb, e.GetPosition(lb));
    if (target is null || target == _draggedRule) return;

    ((SettingsViewModel)DataContext).MoveRuleTo(_draggedRule, target);
}
```

Also add the shared hit-test helper (used by both Rules and Browsers DnD):

```csharp
private static T? FindListBoxItem<T>(ListBox listBox, Point point) where T : class
{
    var element = listBox.InputHitTest(point) as DependencyObject;
    while (element is not null)
    {
        if (listBox.ItemContainerGenerator.ItemFromContainer(element) is T item)
            return item;
        element = VisualTreeHelper.GetParent(element);
    }
    return null;
}
```

> **Namespace note:** `SettingsWindow.xaml.cs` may need `using System.Windows.Controls;` and `using System.Windows.Input;` — check the existing using block and add only what's missing. Also check for WinForms ambiguity aliases (see CLAUDE.md).

**Step 4: Build**

```bash
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

**Step 5: Manual test**

```bash
dotnet run --project src/SimpleBrowserPicker -- --settings
```

Open Rules tab. Add at least 2 rules. Drag one rule over another. Verify the order changes and persists after closing and reopening settings.

**Step 6: Commit**

```bash
git add src/SimpleBrowserPicker/Views/SettingsWindow.xaml \
        src/SimpleBrowserPicker/Views/SettingsWindow.xaml.cs \
        src/SimpleBrowserPicker/ViewModels/SettingsViewModel.cs
git commit -m "Add drag-and-drop reordering to Rules list"
```

---

## Task 7: Drag-and-drop reordering — Browsers list

Same pattern as Task 6, applied to the Browsers `ListBox`.

**Files:**
- Modify: `src/SimpleBrowserPicker/Views/SettingsWindow.xaml` (Browsers ListBox)
- Modify: `src/SimpleBrowserPicker/Views/SettingsWindow.xaml.cs`
- Modify: `src/SimpleBrowserPicker/ViewModels/SettingsViewModel.cs`

**Step 1: Add `MoveBrowserTo` to SettingsViewModel**

```csharp
/// <summary>Called by the view after a drag-drop reorder.</summary>
public void MoveBrowserTo(Browser dragged, Browser target)
{
    int from = Browsers.IndexOf(dragged);
    int to   = Browsers.IndexOf(target);
    if (from < 0 || to < 0 || from == to) return;

    Browsers.Move(from, to);
    // Note: browser order is display-only (not persisted to config).
    // If the user wants persistence, that's a separate task.
}
```

> Browser order is not currently saved to config (unlike rules). Check `AppConfig` — if there's no browser order list, display order resets on next browser refresh. That is acceptable for now; a separate enhancement would persist it.

**Step 2: Enable DnD on the Browsers ListBox**

Find the Browsers ListBox in the Browsers tab. Add:
```xml
x:Name="BrowsersListBox"
AllowDrop="True"
PreviewMouseLeftButtonDown="BrowsersListBox_PreviewMouseLeftButtonDown"
PreviewMouseMove="BrowsersListBox_PreviewMouseMove"
Drop="BrowsersListBox_Drop"
```

**Step 3: Add code-behind**

```csharp
private Point _browserDragStart;
private Browser? _draggedBrowser;

private void BrowsersListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    _browserDragStart = e.GetPosition(null);
}

private void BrowsersListBox_PreviewMouseMove(object sender, MouseEventArgs e)
{
    if (e.LeftButton != MouseButtonState.Pressed || _draggedBrowser is not null) return;

    var pos  = e.GetPosition(null);
    var diff = pos - _browserDragStart;
    if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
        Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

    if (sender is ListBox lb && lb.SelectedItem is Browser browser)
    {
        _draggedBrowser = browser;
        DragDrop.DoDragDrop(lb, browser, DragDropEffects.Move);
        _draggedBrowser = null;
    }
}

private void BrowsersListBox_Drop(object sender, DragEventArgs e)
{
    if (_draggedBrowser is null) return;
    if (sender is not ListBox lb) return;

    var target = FindListBoxItem<Browser>(lb, e.GetPosition(lb));
    if (target is null || target == _draggedBrowser) return;

    ((SettingsViewModel)DataContext).MoveBrowserTo(_draggedBrowser, target);
}
```

**Step 4: Build and test**

```bash
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
dotnet run --project src/SimpleBrowserPicker -- --settings
```

**Step 5: Commit**

```bash
git add src/SimpleBrowserPicker/Views/SettingsWindow.xaml \
        src/SimpleBrowserPicker/Views/SettingsWindow.xaml.cs \
        src/SimpleBrowserPicker/ViewModels/SettingsViewModel.cs
git commit -m "Add drag-and-drop reordering to Browsers list"
```

---

## Task 8: Update CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

**Step 1: Mark the two remaining items as done in the "Still to do" section**

Find:
```markdown
### Still to do
- End-to-end testing with user
- Translations (deferred)
- Drag-and-drop reordering (up/down buttons work; full DnD is v2)
- Path-based rule matching (github.com/gist/*) and regex — v2
```

Replace with:
```markdown
### Still to do
- End-to-end testing with user
- Translations (deferred)
```

Also update the feature parity table to add the two new items as done.

**Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "Mark path matching and DnD as complete in CLAUDE.md"
```
