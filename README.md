# KioskApp - Playnite Time Tracking Extension

A Playnite plugin that tracks game playtime and monitors user activity to enforce healthy gaming habits.

## Features

### Time Limit Tracking
- Tracks time spent in games during each session
- Displays a blocking popup window when time limit is reached
- Configurable time limits per game or a global default
- Custom images can be displayed in the popup for each game
- Popup appears on top of game windows (including fullscreen games)

### Idle Detection & Auto-Close
- Monitors keyboard and mouse activity to detect idle users
- Automatically closes games when user has been inactive for a configured period
- Configurable idle timeouts per game or a global default
- Shows notification when a game is closed due to inactivity
- Helps prevent games from running indefinitely when users forget to close them

## Installation

1. Build the project in Visual Studio (Debug or Release configuration)
2. Copy the contents of `bin\Debug\` (or `bin\Release\`) to Playnite's extensions folder
3. **OR** for development: Add the build output folder to Playnite's "External extensions" in Settings → For developers
4. Restart Playnite

## Configuration

### Step 1: Locate the Configuration File

After the plugin runs for the first time, it will create a configuration file. To find the location:

1. Start Playnite with the plugin installed
2. Open Playnite's log file (Menu → About Playnite → Open Log Directory)
3. Look for the log entry: `Time tracking config file location: [path]`
4. The path will be something like:
   ```
   C:\Users\[YourUsername]\AppData\Local\Playnite\ExtensionsData\f6833c50-87d1-4359-a183-49580f6152b3\TimeTrackingConfig.json
   ```

### Step 2: Find Game IDs

To configure time limits for specific games, you need their Game IDs:

1. Start any game in Playnite
2. Check Playnite's log file
3. Look for the log entry: `Game started: [GameName] (ID: [guid])`
4. Copy the GUID for the game you want to configure

**Example log entry:**
```
Game started: Pentiment (ID: a4bca966-17e7-4b2f-845a-e0e5fac079fd)
```

### Step 3: Edit the Configuration File

Open `TimeTrackingConfig.json` in a text editor and configure your time limits.

**Configuration Structure:**

```json
{
  "DefaultTimeLimitMinutes": 60,
  "GameConfigurations": [
    {
      "GameId": "a4bca966-17e7-4b2f-845a-e0e5fac079fd",
      "GameName": "Pentiment",
      "TimeLimitMinutes": 90,
      "PopupImagePath": "C:\\Images\\pentiment_warning.png"
    },
    {
      "GameId": "b5cd0a77-28f8-5c3g-956b-f1f6gbd180ge",
      "GameName": "Another Game",
      "TimeLimitMinutes": 120,
      "PopupImagePath": "C:\\Images\\another_game.png"
    }
  ]
}
```

**Configuration Options:**

- **`DefaultTimeLimitMinutes`**: Global time limit applied to all games without specific configuration (in minutes)
- **`GameConfigurations`**: Array of game-specific configurations
  - **`GameId`**: The game's unique identifier (GUID) from the logs
  - **`GameName`**: Friendly name for reference (not used by the plugin, just for your convenience)
  - **`TimeLimitMinutes`**: Time limit for this specific game (overrides default)
  - **`PopupImagePath`**: Path to a PNG image to display in the popup window

**Important Notes:**

- **File Paths**: Use double backslashes (`\\`) in Windows paths or forward slashes (`/`)
  - ✅ Correct: `"C:\\Images\\warning.png"` or `"C:/Images/warning.png"`
  - ❌ Wrong: `"C:\Images\warning.png"` (will cause JSON parsing error)
- **Game Name**: Optional field for your reference only
- **Time Limit**: Set to `null` or omit `TimeLimitMinutes` to use the default time limit
- **Image Path**: Can be omitted if you don't want to display an image (popup will still show message)

### Step 4: Test Your Configuration

1. Set a short time limit (e.g., 1-2 minutes) for testing
2. Save the configuration file
3. Restart Playnite
4. Start the configured game
5. Wait for the time limit to be reached
6. The popup should appear on top of the game window

### Step 5: Adjust for Production Use

Once you've verified it works:

1. Set realistic time limits (e.g., 60-120 minutes)
2. Add configurations for all games you want to track
3. Prepare custom warning images for each game (optional)
4. Save and restart Playnite

## Idle Timer Configuration

The idle timer monitors user activity and automatically closes games when no input is detected for a configured period.

### Locate the Idle Timer Configuration

The idle timer uses a separate configuration file located in the same directory as the time tracking config:

```
C:\Users\[YourUsername]\AppData\Local\Playnite\ExtensionsData\f6833c50-87d1-4359-a183-49580f6152b3\IdleTimerConfig.json
```

Check Playnite logs for the exact path: `Idle timer config file location: [path]`

### Idle Timer Configuration Structure

```json
{
  "Enabled": true,
  "DefaultIdleTimeoutMinutes": 15,
  "CheckIntervalSeconds": 10,
  "GameIdleTimeouts": [
    {
      "GameId": "a4bca966-17e7-4b2f-845a-e0e5fac079fd",
      "GameName": "Pentiment",
      "IdleTimeoutMinutes": 20
    },
    {
      "GameId": "c7ef1d99-39g9-6d4h-067c-h2h7hce291hf",
      "GameName": "Strategy Game",
      "IdleTimeoutMinutes": 30
    }
  ]
}
```

### Configuration Options

- **`Enabled`**: Set to `true` to enable idle detection, `false` to disable it entirely
- **`DefaultIdleTimeoutMinutes`**: Global idle timeout for all games (in minutes)
- **`CheckIntervalSeconds`**: How often to check for idle activity (default: 10 seconds)
- **`GameIdleTimeouts`**: Array of game-specific idle timeout configurations
  - **`GameId`**: The game's unique identifier (GUID) from the logs
  - **`GameName`**: Friendly name for reference only
  - **`IdleTimeoutMinutes`**: Idle timeout for this specific game (overrides default)

### How Idle Detection Works

1. **Activity Monitoring**: The plugin uses Windows API to detect keyboard and mouse input
2. **Idle Check**: Every `CheckIntervalSeconds`, the plugin checks how long the user has been idle
3. **Timeout Action**: When idle time exceeds the configured timeout:
   - The game process is terminated (graceful close attempted first, then force kill if needed)
   - A notification appears informing the user the game was closed due to inactivity
   - Idle tracking stops

**Important Notes:**
- Idle detection tracks **system-wide** keyboard/mouse activity (not just game-specific)
- If you use your computer for anything (even outside the game), the idle timer resets
- Gamepad/controller input is currently **not detected** (keyboard/mouse only)
- Some games launched through launchers (Steam, Epic) may have invalid process IDs and won't be auto-closed

### Testing Idle Timer

1. Set a very short idle timeout (e.g., 1-2 minutes) in the config
2. Save the config and restart Playnite
3. Start a game
4. Don't touch keyboard or mouse for the configured duration
5. The game should automatically close and show a notification

### Disabling Idle Timer

To disable idle detection entirely, set `"Enabled": false` in `IdleTimerConfig.json`.

## Example Configuration

```json
{
  "DefaultTimeLimitMinutes": 90,
  "GameConfigurations": [
    {
      "GameId": "a4bca966-17e7-4b2f-845a-e0e5fac079fd",
      "GameName": "Pentiment",
      "TimeLimitMinutes": 60,
      "PopupImagePath": "C:\\Images\\pentiment_break.png"
    },
    {
      "GameId": "c7ef1d99-39g9-6d4h-067c-h2h7hce291hf",
      "GameName": "Strategy Game",
      "TimeLimitMinutes": 120,
      "PopupImagePath": "C:/Images/strategy_warning.png"
    }
  ]
}
```

In this example:
- Most games will use the 90-minute default limit
- Pentiment has a shorter 60-minute limit
- Strategy Game has a longer 120-minute limit
- Both games have custom images that will be displayed when the limit is reached

## Troubleshooting

### Configuration Not Loading

**Problem**: Changes to the config file are not being applied.

**Solution**:
1. Check the Playnite log for errors
2. Verify the JSON syntax is correct (use a JSON validator)
3. Ensure file paths use double backslashes (`\\`) or forward slashes (`/`)
4. Restart Playnite after making changes

### Popup Not Appearing

**Problem**: Time limit reached but no popup shows.

**Solution**:
1. Check Playnite logs for errors
2. Verify the game ID matches exactly (GUIDs are case-sensitive)
3. Ensure the time limit is set correctly in the config
4. Try setting a very short time limit (1 minute) to test

### Image Not Displaying

**Problem**: Popup shows but image is missing.

**Solution**:
1. Verify the image file exists at the specified path
2. Ensure the image is a valid PNG file
3. Check file path uses proper escaping (`\\` or `/`)
4. Check Playnite logs for image loading errors

### JSON Parsing Error

**Problem**: Config file fails to load with JSON error.

**Solution**:
1. Validate your JSON using an online JSON validator
2. Check for common issues:
   - Missing commas between array elements
   - Unescaped backslashes in file paths
   - Missing quotes around strings
   - Trailing commas (not allowed in JSON)

### Idle Timer Not Working

**Problem**: Game doesn't close after being idle.

**Solution**:
1. Check that `"Enabled": true` in IdleTimerConfig.json
2. Verify the game process ID is valid (check logs for "Invalid process ID" warnings)
3. Ensure you're not moving the mouse or pressing keys during the idle period
4. Remember: ANY system activity resets the idle timer, not just game-specific activity
5. Check Playnite logs for idle detection messages

### Game Closes Unexpectedly

**Problem**: Game closes even when actively playing.

**Solution**:
1. Check if idle timeout is too short
2. If using gamepad/controller, note that only keyboard/mouse input is detected
3. Increase the idle timeout or disable idle detection for that game
4. Check logs to confirm it's the idle timer causing the closure

## How It Works

### Time Limit Tracking

1. When a game starts, the plugin begins tracking the session time
2. A timer checks every 30 seconds if the configured time limit has been reached
3. When the limit is reached:
   - The timer stops
   - A modal popup window appears on top of all other windows (including the game)
   - The popup displays the game name, a custom image (if configured), and a message
   - The user must click "OK" to dismiss the popup
4. When the game stops, tracking ends and the session data is cleared

### Idle Detection

1. When a game starts, the plugin also begins monitoring user activity (if enabled)
2. Uses Windows API (`GetLastInputInfo`) to track time since last keyboard/mouse input
3. A timer checks idle time at the configured interval (default: every 10 seconds)
4. When idle time exceeds the configured timeout:
   - Idle monitoring stops
   - The game process is terminated:
     - First attempts graceful shutdown (`CloseMainWindow`)
     - Waits up to 10 seconds
     - If still running, forces termination (`Kill`)
   - A notification is displayed to inform the user
5. When the game stops (normally or via idle timeout), idle monitoring ends

## Development

- **Language**: C# (.NET Framework 4.6.2)
- **SDK**: Playnite SDK 6.2.0
- **Build Tool**: MSBuild

See `CLAUDE.md` for detailed development information.

## License

This is an extension for Playnite. Refer to your organization's licensing terms.
