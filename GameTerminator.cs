using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Threading;

namespace KioskApp
{
    public static class GameTerminator
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Attempts to terminate a game process gracefully, then forcefully if necessary
        /// </summary>
        /// <param name="processId">The process ID of the game</param>
        /// <param name="gameName">The name of the game (for logging)</param>
        /// <returns>True if termination was successful, false otherwise</returns>
        public static bool TerminateGame(int processId, string gameName)
        {
            if (processId <= 0)
            {
                logger.Error($"Invalid process ID ({processId}) for game {gameName}. Cannot terminate.");
                return false;
            }

            try
            {
                Process process = Process.GetProcessById(processId);

                if (process == null || process.HasExited)
                {
                    logger.Info($"Process {processId} for game {gameName} has already exited.");
                    return true;
                }

                logger.Info($"Attempting to close game {gameName} (PID: {processId})...");

                // Try graceful shutdown first
                bool gracefulClose = TryGracefulClose(process, gameName);

                if (gracefulClose)
                {
                    logger.Info($"Successfully closed game {gameName} gracefully.");
                    return true;
                }

                // If graceful close failed, force kill
                logger.Warn($"Graceful close failed for {gameName}. Attempting force kill...");
                bool forceKill = TryForceKill(process, gameName);

                if (forceKill)
                {
                    logger.Info($"Successfully force-killed game {gameName}.");
                    return true;
                }

                logger.Error($"Failed to terminate game {gameName} (PID: {processId}).");
                return false;
            }
            catch (ArgumentException)
            {
                logger.Info($"Process {processId} for game {gameName} does not exist or has already exited.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error terminating game {gameName} (PID: {processId})");
                return false;
            }
        }

        private static bool TryGracefulClose(Process process, string gameName)
        {
            try
            {
                if (process.HasExited)
                {
                    return true;
                }

                logger.Debug($"Attempting graceful close of {gameName} using CloseMainWindow()");

                // Try to close the main window gracefully
                bool closeResult = process.CloseMainWindow();

                if (!closeResult)
                {
                    logger.Debug($"CloseMainWindow() returned false for {gameName}. Process may not have a UI window.");
                    return false;
                }

                // Wait up to 10 seconds for the process to exit
                logger.Debug($"Waiting for {gameName} to exit gracefully (up to 10 seconds)...");
                bool exited = process.WaitForExit(10000);

                if (exited)
                {
                    logger.Debug($"{gameName} exited gracefully.");
                    return true;
                }

                logger.Debug($"{gameName} did not exit within 10 seconds after CloseMainWindow().");
                return false;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, $"Exception during graceful close attempt for {gameName}");
                return false;
            }
        }

        private static bool TryForceKill(Process process, string gameName)
        {
            try
            {
                if (process.HasExited)
                {
                    return true;
                }

                logger.Debug($"Force killing {gameName} using Kill()");
                process.Kill();

                // Wait a moment to confirm termination
                Thread.Sleep(1000);

                if (process.HasExited)
                {
                    logger.Debug($"{gameName} was force-killed successfully.");
                    return true;
                }

                logger.Debug($"{gameName} did not exit after Kill() command.");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Exception during force kill attempt for {gameName}");
                return false;
            }
        }
    }
}
