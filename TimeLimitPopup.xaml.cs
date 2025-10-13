using Playnite.SDK;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KioskApp
{
    public partial class TimeLimitPopup : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private Window parentWindow;

        public TimeLimitPopup(string gameName, string imagePath, Window parent)
        {
            InitializeComponent();
            parentWindow = parent;

            // Set message text
            MessageText.Text = $"Time limit reached for {gameName}";
            SubMessageText.Text = "Please take a break and return later.";

            // Load image if path is provided
            LoadImage(imagePath);
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
            parentWindow?.Close();
        }
    }
}
