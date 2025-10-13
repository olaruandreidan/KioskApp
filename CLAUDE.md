# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Playnite Generic Plugin** called KioskApp. Playnite is a game library manager for Windows, and this extension adds kiosk mode functionality to the application.

- **Plugin ID**: `f6833c50-87d1-4359-a183-49580f6152b3`
- **Plugin Type**: GenericPlugin
- **Framework**: .NET Framework 4.6.2
- **Playnite SDK**: Version 6.2.0
- **Output**: `KioskApp.dll` (loaded by Playnite at runtime)

## Build Commands

### Build the project
```bash
msbuild KioskApp.sln /p:Configuration=Debug
```

### Build for release
```bash
msbuild KioskApp.sln /p:Configuration=Release
```

### Clean build artifacts
```bash
msbuild KioskApp.sln /t:Clean
```

## Architecture

### Core Plugin Structure

The plugin follows Playnite's standard plugin architecture with three main components:

1. **KioskApp.cs** - Main plugin class
   - Inherits from `GenericPlugin` (Playnite SDK)
   - Implements game lifecycle event handlers (OnGameStarted, OnGameStopped, etc.)
   - Implements application lifecycle event handlers (OnApplicationStarted, OnApplicationStopped)
   - Provides settings integration via `GetSettings()` and `GetSettingsView()`
   - Uses Playnite's logger: `LogManager.GetLogger()`

2. **KioskAppSettings.cs** - Settings persistence layer
   - `KioskAppSettings`: Data model inheriting from `ObservableObject` for property change notifications
   - `KioskAppSettingsViewModel`: View model implementing `ISettings` interface
   - Settings are JSON-serialized by Playnite via `plugin.SavePluginSettings()` / `plugin.LoadPluginSettings()`
   - Use `[DontSerialize]` attribute to exclude properties from persistence
   - Implements edit lifecycle: `BeginEdit()`, `CancelEdit()`, `EndEdit()`, `VerifySettings()`

3. **KioskAppSettingsView.xaml** - Settings UI
   - WPF UserControl with data binding to `KioskAppSettingsViewModel`
   - Uses standard WPF controls (TextBox, CheckBox, etc.)
   - Binding path format: `{Binding Settings.PropertyName}`

### Extension Metadata

The `extension.yaml` file defines the plugin manifest that Playnite reads:
- Must be copied to output directory (set in .csproj)
- Contains plugin ID, name, author, version, module path, type, and icon
- This file is required for Playnite to recognize the plugin

### Localization

Localization files are stored in the `Localization/` directory as XAML ResourceDictionaries:
- Follow the pattern: `{locale}.xaml` (e.g., `en_US.xaml`)
- Define string resources accessible throughout the plugin
- Automatically copied to output directory during build

## Key Integration Points

### Playnite Event Hooks

The plugin can respond to these lifecycle events (all defined in KioskApp.cs):
- `OnGameInstalled` - Game finished installing
- `OnGameStarting` - Game preparing to start
- `OnGameStarted` - Game is running
- `OnGameStopped` - Game has stopped
- `OnGameUninstalled` - Game was uninstalled
- `OnApplicationStarted` - Playnite initialized
- `OnApplicationStopped` - Playnite shutting down
- `OnLibraryUpdated` - Game library was updated

### Settings Validation

The `VerifySettings()` method in `KioskAppSettingsViewModel` validates settings before save:
- Return `true` if settings are valid
- Return `false` and populate the `errors` list to show validation messages to the user
- `EndEdit()` is only called if validation passes

## Development Workflow

### Testing the Plugin

1. Make changes to C# code files
2. Build with `msbuild KioskApp.sln /p:Configuration=Debug`
3. Output DLL and dependencies will be in `bin\Debug\`
4. Load plugin in Playnite:
   - Open Playnite settings
   - Go to "For developers" section
   - Add this plugin's build output folder to "External extensions" (e.g., `C:\extension\KioskApp\bin\Debug\`)
5. Restart Playnite to load the updated plugin

Note: Extension installation always replaces the entire extension directory.

### Debugging with Visual Studio

**Method 1: Attach to Process**
1. Start Playnite with the plugin loaded
2. In Visual Studio: Debug → Attach to Process
3. Select the Playnite.DesktopApp.exe or Playnite.FullscreenApp.exe process
4. Set breakpoints and debug

**Method 2: Start External Program (Recommended)**
1. Right-click the plugin project in Visual Studio
2. Open project properties
3. Go to the "Debug" section
4. Set "Start action" to "Start external program"
5. Set the path to the Playnite executable
6. Press F5 to start debugging - Playnite will launch automatically

### Threading Considerations

The Playnite SDK is **not fully thread-safe**. When accessing UI objects or making UI changes from background threads:
- Use `PlayniteAPI.MainView.UIDispatcher` to run code on the UI thread
- Example: `PlayniteAPI.MainView.UIDispatcher.Invoke(() => { /* UI code here */ });`

## Accessing the Playnite API

Two ways to access the Playnite API:

1. **Instance property** (recommended in plugin classes):
   ```csharp
   PlayniteAPI.Database.Games  // Access game database
   PlayniteAPI.Dialogs.ShowMessage("Hello")  // Show dialog
   ```

2. **Static singleton** (for use outside plugin class):
   ```csharp
   using Playnite.SDK;
   API.Instance.Database.Games
   ```

The API provides access to:
- Game database (add, remove, query games)
- Dialogs and notifications
- Paths and file locations
- Main window and UI dispatcher
- Application info and paths

## Important Notes

- The plugin GUID in `KioskApp.cs` must match the ID in `extension.yaml`
- **DO NOT reference non-SDK Playnite assemblies** - only reference `Playnite.SDK.dll`
  - If you need functionality not in the SDK, open a GitHub issue or link source code to your project
- SDK versions are backwards compatible within the same major version (e.g., plugin for SDK 6.0 works with all 6.x versions)
- Playnite checks SDK version references before loading plugins
- Settings changes must go through the `BeginEdit`/`EndEdit` lifecycle to persist
- Always use `SetValue()` in settings properties to trigger property change notifications
- The `IPlayniteAPI` instance (accessed via base class) provides access to Playnite's database, dialogs, paths, and other services
- Keep dependency versions compatible with what Playnite uses to avoid conflicts
