using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KioskApp
{
    public class GameIdleConfig
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public int? IdleTimeoutMinutes { get; set; }
    }

    public class IdleTimerConfig
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public bool Enabled { get; set; } = true;
        public int DefaultIdleTimeoutMinutes { get; set; } = 15;
        public int CheckIntervalSeconds { get; set; } = 10;
        public List<GameIdleConfig> GameIdleTimeouts { get; set; } = new List<GameIdleConfig>();

        public static IdleTimerConfig LoadConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var jsonContent = File.ReadAllText(configPath);
                    logger.Debug($"Raw idle config JSON: {jsonContent}");
                    var config = Serialization.FromJson<IdleTimerConfig>(jsonContent);
                    logger.Info($"Loaded idle timer configuration from {configPath}");
                    logger.Info($"Idle detection enabled: {config.Enabled}");
                    logger.Info($"Default idle timeout: {config.DefaultIdleTimeoutMinutes} minutes");
                    logger.Info($"Check interval: {config.CheckIntervalSeconds} seconds");
                    logger.Info($"Loaded {config.GameIdleTimeouts?.Count ?? 0} game-specific idle configurations");

                    if (config.GameIdleTimeouts != null)
                    {
                        foreach (var gameConfig in config.GameIdleTimeouts)
                        {
                            logger.Debug($"  - Game: {gameConfig.GameName} ({gameConfig.GameId}), Idle Timeout: {gameConfig.IdleTimeoutMinutes} min");
                        }
                    }

                    return config;
                }
                else
                {
                    logger.Warn($"Idle timer configuration file not found at {configPath}. Creating default configuration.");
                    var defaultConfig = CreateDefaultConfig();
                    SaveConfig(defaultConfig, configPath);
                    return defaultConfig;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load idle timer configuration from {configPath}. Using default configuration.");
                return CreateDefaultConfig();
            }
        }

        public static void SaveConfig(IdleTimerConfig config, string configPath)
        {
            try
            {
                var jsonContent = Serialization.ToJson(config, true);
                File.WriteAllText(configPath, jsonContent);
                logger.Info($"Saved idle timer configuration to {configPath}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to save idle timer configuration to {configPath}");
            }
        }

        public static IdleTimerConfig CreateDefaultConfig()
        {
            return new IdleTimerConfig
            {
                Enabled = true,
                DefaultIdleTimeoutMinutes = 15,
                CheckIntervalSeconds = 10,
                GameIdleTimeouts = new List<GameIdleConfig>
                {
                    new GameIdleConfig
                    {
                        GameId = Guid.Empty,
                        GameName = "Example Game (replace with actual game GUID)",
                        IdleTimeoutMinutes = 20
                    }
                }
            };
        }

        public GameIdleConfig GetConfigForGame(Guid gameId)
        {
            return GameIdleTimeouts?.FirstOrDefault(g => g.GameId == gameId);
        }

        public int GetIdleTimeoutForGame(Guid gameId)
        {
            var gameConfig = GetConfigForGame(gameId);
            if (gameConfig?.IdleTimeoutMinutes.HasValue == true)
            {
                logger.Debug($"Using game-specific idle timeout for {gameId}: {gameConfig.IdleTimeoutMinutes.Value} minutes");
                return gameConfig.IdleTimeoutMinutes.Value;
            }
            logger.Debug($"Using default idle timeout for {gameId}: {DefaultIdleTimeoutMinutes} minutes");
            return DefaultIdleTimeoutMinutes;
        }
    }
}
