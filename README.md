# Simple browser picker

Choose which browser opens each link. No faff.

Part of the [No faff](https://github.com/no-faff) suite of small Windows utilities.

## What it does

Registers itself as your default browser. When any app opens a link, it shows a
clean popup letting you pick which browser — or browser profile — to open it in.
You can also set rules so certain domains always open in a specific browser.

**Profiles are first-class citizens.** "Vivaldi – Work" and "Vivaldi – Personal"
appear as two separate entries, not buried in a sub-menu.

## Running

Just download and run the exe — no runtime or installer needed.

On first run it walks you through registering it as your default browser.

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

> **Tip:** if the SDK installer hangs, kill it via Task Manager, reboot, then
> retry. The reboot clears the Windows Installer lock.

```shell
git clone https://github.com/no-faff/simple-browser-picker
cd simple-browser-picker
dotnet build src/SimpleBrowserPicker/SimpleBrowserPicker.csproj
```

To publish a self-contained single exe:

```shell
dotnet publish src/SimpleBrowserPicker/SimpleBrowserPicker.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## Licence

MIT
