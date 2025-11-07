using Playnite.SDK;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace KioskApp
{
    public partial class TimeLimitPopup : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private Window parentWindow;
        private DispatcherTimer countdownTimer;
        private int remainingSeconds = 20;

        public TimeLimitPopup(string gameName, string imagePath, Window parent)
        {
            InitializeComponent();
            parentWindow = parent;

            // Set message text
            MessageText.Text = $"We hope you enjoy playing {gameName}";
            SubMessageText.Text = "Now it's your chance to scan the QR code to continue your Gamify TAG journey";

            // Load image if path is provided
            LoadImage(imagePath);

            // Start countdown timer
            StartCountdownTimer();
        }

        private void LoadImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    logger.Debug("No image path provided for popup.");
                    GameImage.Visibility = Visibility.Collapsed;
                    return;
                }

                if (!File.Exists(imagePath))
                {
                    logger.Warn($"Image file not found at path: {imagePath}");
                    GameImage.Visibility = Visibility.Collapsed;
                    return;
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                GameImage.Source = bitmap;
                GameImage.Visibility = Visibility.Visible;

                logger.Info($"Successfully loaded popup image from: {imagePath}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to load image from path: {imagePath}");
                GameImage.Visibility = Visibility.Collapsed;
            }
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Time limit popup dismissed by user.");
            StopCountdownTimer();
            parentWindow?.Close();
        }

        private void StartCountdownTimer()
        {
            try
            {
                countdownTimer = new DispatcherTimer();
                countdownTimer.Interval = TimeSpan.FromSeconds(1);
                countdownTimer.Tick += CountdownTimer_Tick;

                // Update initial countdown text
                UpdateCountdownText();

                countdownTimer.Start();
                logger.Info("Started 20-second auto-close countdown for popup");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to start countdown timer");
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;

            if (remainingSeconds <= 0)
            {
                logger.Info("Auto-closing popup after 20-second countdown");
                StopCountdownTimer();
                parentWindow?.Close();
            }
            else
            {
                UpdateCountdownText();
            }
        }

        private void UpdateCountdownText()
        {
            CountdownText.Text = $"This window will close automatically in {remainingSeconds} seconds";
        }

        private void StopCountdownTimer()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Tick -= CountdownTimer_Tick;
                countdownTimer = null;
            }
        }
    }
}
