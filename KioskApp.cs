using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KioskApp
{
    public class KioskApp : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private KioskAppSettingsViewModel settings { get; set; }
        private TimeTrackingConfig timeTrackingConfig;
        private GameTimeTracker gameTimeTracker;
        private string configFilePath;
        private IdleTimerConfig idleTimerConfig;
        private IdleTracker idleTracker;
        private string idleConfigFilePath;
        private int currentGameProcessId;

        public override Guid Id { get; } = Guid.Parse("f6833c50-87d1-4359-a183-49580f6152b3");

        public KioskApp(IPlayniteAPI api) : base(api)
        {
            settings = new KioskAppSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            // Initialize time tracking components
            InitializeTimeTracking();

            // Initialize idle tracking components
            InitializeIdleTracking();
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Log game information for config setup
            var game = args.Game;

            // Try to get process ID - this property might not exist in older SDK versions
            currentGameProcessId = TryGetGameProcessId(args, game);
            logger.Info($"Game started: {game.Name} (ID: {game.Id}, PID: {currentGameProcessId})");

            // Get time limit and image path for this game
            int timeLimit = timeTrackingConfig.GetTimeLimitForGame(game.Id);
            string imagePath = timeTrackingConfig.GetImagePathForGame(game.Id);

            logger.Info($"Starting time tracking for {game.Name} with {timeLimit} minute limit");

            // Start time tracking
            gameTimeTracker.SetImagePath(imagePath);
            gameTimeTracker.StartTracking(game, timeLimit, imagePath);

            // Start idle tracking if enabled
            if (idleTimerConfig.Enabled)
            {
                int idleTimeout = idleTimerConfig.GetIdleTimeoutForGame(game.Id);
                int checkInterval = idleTimerConfig.CheckIntervalSeconds;

                if (currentGameProcessId > 0)
                {
                    logger.Info($"Starting idle monitoring for {game.Name} with {idleTimeout} minute timeout");
                    idleTracker.StartMonitoring(game, currentGameProcessId, idleTimeout, checkInterval);
                }
                else
                {
                    logger.Warn($"Cannot start idle monitoring for {game.Name}: Process ID not available in SDK version 6.2.0. Idle detection requires SDK 6.3.0 or newer.");
                }
            }
            else
            {
                logger.Debug("Idle tracking is disabled in configuration");
            }
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            logger.Info($"Game stopped: {args.Game.Name}");
            gameTimeTracker.StopTracking();
            idleTracker.StopMonitoring();
            currentGameProcessId = 0;
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            logger.Info("KioskApp plugin initialized successfully");
            logger.Info($"Time tracking config file location: {configFilePath}");
            logger.Info($"Idle timer config file location: {idleConfigFilePath}");
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Clean up time tracker
            if (gameTimeTracker != null)
            {
                gameTimeTracker.StopTracking();
                gameTimeTracker.Dispose();
            }

            // Clean up idle tracker
            if (idleTracker != null)
            {
                idleTracker.StopMonitoring();
                idleTracker.Dispose();
            }
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new KioskAppSettingsView();
        }

        private void InitializeTimeTracking()
        {
            try
            {
                // Get config file path in plugin data directory
                var pluginDataPath = GetPluginUserDataPath();
                configFilePath = Path.Combine(pluginDataPath, "TimeTrackingConfig.json");

                logger.Info($"Loading time tracking configuration from: {configFilePath}");

                // Load configuration
                timeTrackingConfig = TimeTrackingConfig.LoadConfig(configFilePath);

                // Initialize game time tracker
                gameTimeTracker = new GameTimeTracker();
                gameTimeTracker.TimeLimitReached += OnTimeLimitReached;

                logger.Info("Time tracking initialized successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize time tracking");
                // Create default config as fallback
                timeTrackingConfig = TimeTrackingConfig.CreateDefaultConfig();
                gameTimeTracker = new GameTimeTracker();
                gameTimeTracker.TimeLimitReached += OnTimeLimitReached;
            }
        }

        private void OnTimeLimitReached(Game game, string imagePath)
        {
            logger.Info($"Time limit reached for game: {game.Name}. Displaying popup.");

            // DispatcherTimer already runs on UI thread, so we can call ShowTimeLimitPopup directly
            ShowTimeLimitPopup(game, imagePath);
        }

        private void ShowTimeLimitPopup(Game game, string imagePath)
        {
            try
            {
                // Create a styled window using Playnite's dialog factory
                var windowOptions = new WindowCreationOptions
                {
                    ShowCloseButton = false,
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                };

                var window = PlayniteApi.Dialogs.CreateWindow(windowOptions);
                window.Title = "Time Limit Reached";
                window.Width = 600;
                window.Height = 500;
                window.ResizeMode = ResizeMode.NoResize;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Make window always on top and ensure it gets focus
                window.Topmost = true;
                window.WindowState = WindowState.Normal;

                // Set the content to our custom popup control
                var popupContent = new TimeLimitPopup(game.Name, imagePath, window);
                window.Content = popupContent;

                // Set owner to current app window if available
                try
                {
                    window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                }
                catch
                {
                    // If getting owner fails, continue without it
                }

                // Show as modal dialog (blocks until closed)
                logger.Info("Displaying time limit popup window");
                window.ShowDialog();

                // After showing, activate and focus the window
                window.Activate();
                window.Focus();

                logger.Info("Time limit popup dismissed");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to show time limit popup");
                // Fallback to simple message dialog
                PlayniteApi.Dialogs.ShowMessage($"Time limit reached for {game.Name}. Please take a break.");
            }
        }

        private void InitializeIdleTracking()
        {
            try
            {
                // Get config file path in plugin data directory
                var pluginDataPath = GetPluginUserDataPath();
                idleConfigFilePath = Path.Combine(pluginDataPath, "IdleTimerConfig.json");

                logger.Info($"Loading idle timer configuration from: {idleConfigFilePath}");

                // Load configuration
                idleTimerConfig = IdleTimerConfig.LoadConfig(idleConfigFilePath);

                // Initialize idle tracker
                idleTracker = new IdleTracker();
                idleTracker.IdleTimeoutReached += OnIdleTimeoutReached;

                logger.Info("Idle tracking initialized successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize idle tracking");
                // Create default config as fallback
                idleTimerConfig = IdleTimerConfig.CreateDefaultConfig();
                idleTracker = new IdleTracker();
                idleTracker.IdleTimeoutReached += OnIdleTimeoutReached;
            }
        }

        private void OnIdleTimeoutReached(Game game, int processId)
        {
            logger.Warn($"Idle timeout reached for game: {game.Name} (PID: {processId}). Terminating game.");

            // Stop idle monitoring
            idleTracker.StopMonitoring();

            // Terminate the game process
            bool success = GameTerminator.TerminateGame(processId, game.Name);

            if (success)
            {
                // Show notification to user
                PlayniteApi.Notifications.Add(
                    new NotificationMessage(
                        $"idle-timeout-{game.Id}",
                        $"Game closed due to inactivity: {game.Name}",
                        NotificationType.Info
                    )
                );

                logger.Info($"Game {game.Name} terminated successfully due to idle timeout");
            }
            else
            {
                logger.Error($"Failed to terminate game {game.Name} after idle timeout");
            }
        }

        private int TryGetGameProcessId(OnGameStartedEventArgs args, Game game)
        {
            try
            {
                // Try to get StartedProcessId using reflection for SDK compatibility
                var property = args.GetType().GetProperty("StartedProcessId");
                if (property != null)
                {
                    var value = property.GetValue(args);
                    if (value is int processId)
                    {
                        return processId;
                    }
                }

                logger.Debug($"StartedProcessId property not found in OnGameStartedEventArgs (SDK 6.2.0). Idle detection unavailable.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error retrieving game process ID");
                return 0;
            }
        }
    }
}