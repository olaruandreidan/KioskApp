using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KioskApp
{
    public class GameConfig
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public string PopupImagePath { get; set; }
    }

    public class TimeTrackingConfig
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public int DefaultTimeLimitMinutes { get; set; } = 60;
        public List<GameConfig> GameConfigurations { get; set; } = new List<GameConfig>();

        public static TimeTrackingConfig LoadConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    logger.Debug($"Raw JSON content: {jsonContent}");
                    var config = Serialization.FromJson<TimeTrackingConfig>(jsonContent);
                    logger.Info($"Loaded time tracking configuration from {configPath}");
                    logger.Info($"Default time limit: {config.DefaultTimeLimitMinutes} minutes");
                    logger.Info($"Loaded {config.GameConfigurations?.Count ?? 0} game-specific configurations");

                    if (config.GameConfigurations != null)
                    {
                        foreach (var gameConfig in config.GameConfigurations)
                        {
                            logger.Debug($"  - Game: {gameConfig.GameName} ({gameConfig.GameId}), Limit: {gameConfig.TimeLimitMinutes} min, Image: {gameConfig.PopupImagePath}");
                        }
                    }

                    return config;
                }
                else
                {
                    logger.Warn($"Configuration file not found at {configPath}. Creating default configuration.");
                    var defaultConfig = CreateDefaultConfig();
                    SaveConfig(defaultConfig, configPath);
                    return defaultConfig;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load configuration from {configPath}. Using default configuration.");
                return CreateDefaultConfig();
            }
        }

        public static void SaveConfig(TimeTrackingConfig config, string configPath)
        {
            try
            {
                var jsonContent = Serialization.ToJson(config, true);
                File.WriteAllText(configPath, jsonContent);
                logger.Info($"Saved time tracking configuration to {configPath}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to save configuration to {configPath}");
            }
        }

        public static TimeTrackingConfig CreateDefaultConfig()
        {
            return new TimeTrackingConfig
            {
                DefaultTimeLimitMinutes = 60,
                GameConfigurations = new List<GameConfig>
                {
                    new GameConfig
                    {
                        GameId = Guid.Empty,
                        GameName = "Example Game (replace with actual game GUID)",
                        TimeLimitMinutes = 90,
                        PopupImagePath = "C:\\path\\to\\image.png"
                    }
                }
            };
        }

        public GameConfig GetConfigForGame(Guid gameId)
        {
            return GameConfigurations?.FirstOrDefault(g => g.GameId == gameId);
        }

        public int GetTimeLimitForGame(Guid gameId)
        {
            var gameConfig = GetConfigForGame(gameId);
            if (gameConfig?.TimeLimitMinutes.HasValue == true)
            {
                logger.Debug($"Using game-specific time limit for {gameId}: {gameConfig.TimeLimitMinutes.Value} minutes");
                return gameConfig.TimeLimitMinutes.Value;
            }
            logger.Debug($"Using default time limit for {gameId}: {DefaultTimeLimitMinutes} minutes");
            return DefaultTimeLimitMinutes;
        }

        public string GetImagePathForGame(Guid gameId)
        {
            var gameConfig = GetConfigForGame(gameId);
            return gameConfig?.PopupImagePath;
        }
    }
}
