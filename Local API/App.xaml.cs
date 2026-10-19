using Hardcodet.Wpf.TaskbarNotification;
using Local_API.Services;
using Local_API.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Net;
using System.Text;
using System.Windows;

namespace Local_API
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static TrayIcon trayIcon = new TrayIcon();
        public static APIServer? _apiServer = new APIServer();

        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            await _apiServer.StartAsync();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_apiServer != null)
            {
                await _apiServer.StopAsync();
            }

            trayIcon?.Dispose();

            base.OnExit(e);
        }
    }

}
