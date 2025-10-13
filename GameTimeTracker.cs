using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Windows.Threading;

namespace KioskApp
{
    public class GameTimeTracker
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private DispatcherTimer timer;
        private DateTime sessionStartTime;
        private int timeLimitMinutes;
        private Game currentGame;
        private bool isTracking = false;

        public event Action<Game, string> TimeLimitReached;

        public GameTimeTracker()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30); // Check every 30 seconds
            timer.Tick += Timer_Tick;
        }

        public void StartTracking(Game game, int timeLimitMinutes, string imagePath)
        {
            if (isTracking)
            {
                logger.Warn($"Already tracking a game. Stopping previous session.");
                StopTracking();
            }

            currentGame = game;
            this.timeLimitMinutes = timeLimitMinutes;
            sessionStartTime = DateTime.Now;
            isTracking = true;

            logger.Info($"Started tracking game: {game.Name} (ID: {game.Id}) with time limit of {timeLimitMinutes} minutes");

            timer.Start();
        }

        public void StopTracking()
        {
            if (!isTracking)
            {
                return;
            }

            timer.Stop();

            if (currentGame != null)
            {
                var elapsedTime = DateTime.Now - sessionStartTime;
                logger.Info($"Stopped tracking game: {currentGame.Name}. Session duration: {elapsedTime.TotalMinutes:F2} minutes");
            }

            currentGame = null;
            isTracking = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isTracking || currentGame == null)
            {
                return;
            }

            var elapsed = DateTime.Now - sessionStartTime;
            var elapsedMinutes = elapsed.TotalMinutes;

            logger.Debug($"Time check for {currentGame.Name}: {elapsedMinutes:F2} / {timeLimitMinutes} minutes");

            if (elapsedMinutes >= timeLimitMinutes)
            {
                logger.Info($"Time limit reached for {currentGame.Name}. Elapsed: {elapsedMinutes:F2} minutes");

                // Stop tracking before showing popup to prevent multiple triggers
                timer.Stop();
                isTracking = false;

                // Trigger the time limit reached event
                TimeLimitReached?.Invoke(currentGame, GetImagePathForCurrentGame());
            }
        }

        private string imagePathForCurrentGame;

        public void SetImagePath(string imagePath)
        {
            imagePathForCurrentGame = imagePath;
        }

        private string GetImagePathForCurrentGame()
        {
            return imagePathForCurrentGame;
        }

        public bool IsTracking => isTracking;

        public Game CurrentGame => currentGame;

        public double GetElapsedMinutes()
        {
            if (!isTracking)
            {
                return 0;
            }

            var elapsed = DateTime.Now - sessionStartTime;
            return elapsed.TotalMinutes;
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
