# The `microsoft-edge:` protocol and why some Windows links bypass your default browser

## Background

Some links in Windows don't use standard `https://` URLs. Instead, Microsoft
wraps them in a proprietary `microsoft-edge:https://example.com/` protocol.
This forces Edge to open regardless of your default browser setting.

You'll hit this with:

- **Windows Spotlight** captions on the lock screen and desktop ("Learn more")
- **Widgets panel** / "News and Interests" links
- **Cortana** results (on older builds)
- **Start menu** web search results
- Various built-in Windows help and "what's new" links

Because these aren't standard `https://` links, Simple Browser Picker (and
every other default browser / browser picker) never sees them. They go straight
to Edge at the OS level.

## EdgeDeflector (2017–2022, now dead)

[EdgeDeflector](https://github.com/da2x/EdgeDeflector) by Daniel Aleksandersen
was a small utility that registered itself as a handler for the
`microsoft-edge:` protocol. It intercepted forced-Edge links, stripped the
protocol wrapper and passed the plain `https://` URL to your actual default
browser.

It worked well on Windows 10 and early Windows 11 builds.

Microsoft killed it in **Windows 11 build 22494** (late 2021) by hardcoding the
OS to route `microsoft-edge:` directly to Edge, bypassing the normal protocol
handler registry entirely. No third-party app can register for that protocol
any more. The developer archived the project, calling it "sabotaged."

Blog post with full details:
https://www.ctrl.blog/entry/edgedeflector-default-browser.html

## What this means for Simple Browser Picker

Simple Browser Picker registers as a standard `http:`/`https:` protocol
handler — the same mechanism every third-party browser uses. This is a
published, supported API that Microsoft has to honour (and is under EU scrutiny
for).

However, any link that uses the `microsoft-edge:` protocol will bypass us
completely. There is no technical workaround on modern Windows 11. This is a
**Windows limitation, not a bug** in Simple Browser Picker or any other browser
picker.

## Why this isn't a risk to our app

EdgeDeflector was intercepting a proprietary Microsoft protocol, which gave
Microsoft a defensible reason to lock it down. Simple Browser Picker uses the
legitimate default browser API — the same path Chrome, Firefox, Brave and every
other browser relies on. Microsoft can't block this without breaking every
third-party browser on Windows, which would be an antitrust problem.

Different mechanism, different risk profile.
