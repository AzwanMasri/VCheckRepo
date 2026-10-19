using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Local_API.Views
{
    /// <summary>
    /// Interaction logic for TrayIcon.xaml
    /// </summary>
    public partial class TrayIcon : IDisposable
    {
        private bool disposed = false;

        public TrayIcon()
        {
            InitializeComponent();

            this.ShowBalloonTip("VCheck API Status", "VCheck API is running", BalloonIcon.Info);
        }

        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            // 'this' refers to the TaskbarIcon defined in XAML
            this.ShowBalloonTip(title, message, icon);
        }

        public void UpdateTooltip(string text)
        {
            Dispatcher.Invoke(() =>
            {
                this.ToolTipText = text;

                // 🔹 Force refresh by briefly hiding and showing the icon
                this.Visibility = System.Windows.Visibility.Collapsed;
                this.Visibility = System.Windows.Visibility.Visible;
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                // Dispose of the underlying WPF TaskbarIcon
                if (this is TaskbarIcon icon)
                    icon.Dispose();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
