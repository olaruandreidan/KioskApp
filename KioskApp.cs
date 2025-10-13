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
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Log game information for config setup
            var game = args.Game;
            logger.Info($"Game started: {game.Name} (ID: {game.Id})");

            // Get time limit and image path for this game
            int timeLimit = timeTrackingConfig.GetTimeLimitForGame(game.Id);
            string imagePath = timeTrackingConfig.GetImagePathForGame(game.Id);

            logger.Info($"Starting time tracking for {game.Name} with {timeLimit} minute limit");

            // Start tracking
            gameTimeTracker.SetImagePath(imagePath);
            gameTimeTracker.StartTracking(game, timeLimit, imagePath);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            logger.Info($"Game stopped: {args.Game.Name}");
            gameTimeTracker.StopTracking();
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            logger.Info("KioskApp plugin initialized successfully");
            logger.Info($"Time tracking config file location: {configFilePath}");
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Clean up time tracker
            if (gameTimeTracker != null)
            {
                gameTimeTracker.StopTracking();
                gameTimeTracker.Dispose();
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
    }
}