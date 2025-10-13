using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Windows.Threading;

namespace KioskApp
{
    public class IdleTracker
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private DispatcherTimer timer;
        private int idleTimeoutMinutes;
        private int checkIntervalSeconds;
        private Game currentGame;
        private int currentGameProcessId;
        private bool isMonitoring = false;

        public event Action<Game, int> IdleTimeoutReached;

        public IdleTracker()
        {
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
        }

        public void StartMonitoring(Game game, int processId, int timeoutMinutes, int checkInterval = 10)
        {
            if (isMonitoring)
            {
                logger.Warn($"Already monitoring idle time for a game. Stopping previous session.");
                StopMonitoring();
            }

            if (processId <= 0)
            {
                logger.Warn($"Invalid process ID ({processId}) for game {game.Name}. Cannot monitor idle time.");
                return;
            }

            currentGame = game;
            currentGameProcessId = processId;
            idleTimeoutMinutes = timeoutMinutes;
            checkIntervalSeconds = checkInterval;
            isMonitoring = true;

            timer.Interval = TimeSpan.FromSeconds(checkIntervalSeconds);

            logger.Info($"Started idle monitoring for game: {game.Name} (PID: {processId}) with timeout of {timeoutMinutes} minutes");
            logger.Info($"Checking idle time every {checkIntervalSeconds} seconds");

            timer.Start();
        }

        public void StopMonitoring()
        {
            if (!isMonitoring)
            {
                return;
            }

            timer.Stop();

            if (currentGame != null)
            {
                logger.Info($"Stopped idle monitoring for game: {currentGame.Name}");
            }

            currentGame = null;
            currentGameProcessId = 0;
            isMonitoring = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isMonitoring || currentGame == null)
            {
                return;
            }

            double idleMinutes = WindowsInputMonitor.GetIdleTimeMinutes();

            if (idleMinutes < 0)
            {
                logger.Debug($"Unable to determine idle time for {currentGame.Name}");
                return;
            }

            logger.Info($"Idle time check for {currentGame.Name}: {idleMinutes:F2} / {idleTimeoutMinutes} minutes");

            if (idleMinutes >= idleTimeoutMinutes)
            {
                logger.Warn($"Idle timeout reached for {currentGame.Name}. Idle time: {idleMinutes:F2} minutes");

                // Stop monitoring before triggering event to prevent multiple triggers
                timer.Stop();
                isMonitoring = false;

                // Trigger the idle timeout event
                IdleTimeoutReached?.Invoke(currentGame, currentGameProcessId);
            }
        }

        public bool IsMonitoring => isMonitoring;

        public Game CurrentGame => currentGame;

        public double GetCurrentIdleTime()
        {
            return WindowsInputMonitor.GetIdleTimeMinutes();
        }

        public void Dispose()
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }
    }
}
