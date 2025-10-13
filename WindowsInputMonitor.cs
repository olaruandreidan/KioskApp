using Playnite.SDK;
using System;
using System.Runtime.InteropServices;

namespace KioskApp
{
    /// <summary>
    /// Monitors user input activity using Windows API
    /// </summary>
    public static class WindowsInputMonitor
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Gets the time in seconds since the last user input (keyboard or mouse)
        /// </summary>
        /// <returns>Idle time in seconds, or -1 if unable to determine</returns>
        public static double GetIdleTimeSeconds()
        {
            try
            {
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                if (GetLastInputInfo(ref lastInputInfo))
                {
                    uint currentTickCount = (uint)Environment.TickCount;
                    uint idleTickCount = currentTickCount - lastInputInfo.dwTime;

                    // Convert milliseconds to seconds
                    double idleSeconds = idleTickCount / 1000.0;

                    return idleSeconds;
                }
                else
                {
                    logger.Error("Failed to get last input info from Windows API");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting idle time from Windows API");
                return -1;
            }
        }

        /// <summary>
        /// Gets the time in minutes since the last user input (keyboard or mouse)
        /// </summary>
        /// <returns>Idle time in minutes, or -1 if unable to determine</returns>
        public static double GetIdleTimeMinutes()
        {
            double idleSeconds = GetIdleTimeSeconds();
            if (idleSeconds < 0)
            {
                return -1;
            }

            return idleSeconds / 60.0;
        }

        /// <summary>
        /// Checks if the system has been idle for longer than the specified duration
        /// </summary>
        /// <param name="thresholdMinutes">Idle threshold in minutes</param>
        /// <returns>True if idle time exceeds threshold, false otherwise</returns>
        public static bool IsIdleFor(double thresholdMinutes)
        {
            double idleMinutes = GetIdleTimeMinutes();
            if (idleMinutes < 0)
            {
                // Unable to determine idle time, assume not idle
                return false;
            }

            return idleMinutes >= thresholdMinutes;
        }
    }
}
