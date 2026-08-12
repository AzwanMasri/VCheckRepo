using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using VCheck.Lib.Data.DBContext;
using VCheckViewer_Others.Lib.Function;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace VCheckViewer_Others.Views.Pages.Dashboard
{
    /// <summary>
    /// Interaction logic for ConnectionPage.xaml
    /// </summary>
    public partial class ConnectionPage : Page
    {
        public ObservableCollection<ComboBoxItem> cbConnectionType { get; set; }
        public ComboBoxItem SelectedcbConnectionType { get; set; }

        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public ConnectionPage()
        {
            InitializeComponent();
            //DataContext = this;

            //cbConnectionType = App.MainViewModel.cbConnectionType;

            //var sConfigObj = configDBContext.GetConfigurationData("Connection_Type").FirstOrDefault();
            //if (sConfigObj != null)
            //{
            //    SelectedcbConnectionType = cbConnectionType.Where(a => (string)a.Tag == sConfigObj.ConfigurationValue).FirstOrDefault();
            //}
            //else
            //{
            //    SelectedcbConnectionType = cbConnectionType.FirstOrDefault();
            //}

            if (App.MainViewModel.CurrentUsers.Role == "Lab User")
            {
                btnSettings.IsEnabled = false;
                btnDevice.IsEnabled = false;
            }
            else
            {
                btnSettings.IsEnabled = true;
                btnDevice.IsEnabled = true;
            }

            RefreshAll();
        }

        private async Task RefreshAll()
        {
            RefreshButton.IsEnabled = false;

            var PMS = configDBContext.GetConfigurationData("InterfaceSettingsPMS").FirstOrDefault();
            var SystemVersion = configDBContext.GetConfigurationData("System_Version").FirstOrDefault();

            APIStatus.Text = Properties.Resources.Maintenance_Label_Checking;
            APIStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#fa8219");
            ListenerStatus.Text = Properties.Resources.Maintenance_Label_Checking;
            ListenerStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#fa8219");
            ListenerIP.Text = GetAssignedIPAddress();
            ListenerIP.Foreground = (Brush)new BrushConverter().ConvertFromString("#fa8219");
            ListenerPort.Text = "8585";
            ListenerPort.Foreground = (Brush)new BrushConverter().ConvertFromString("#fa8219");
            PMSConnected.Text = PMS != null ? PMS.ConfigurationValue : "None";
            PMSConnected.Foreground = (Brush)new BrushConverter().ConvertFromString("#fa8219");

            var APIRunning = await CheckAPI();
            APIStatus.Text = APIRunning ? Properties.Resources.Maintenance_Label_Connected : Properties.Resources.Maintenance_Label_NotConnected;
            APIStatus.Foreground = APIRunning ? (Brush)new BrushConverter().ConvertFromString("#16c933") : Brushes.Red;

            var ListenerRunning = CheckListener();
            ListenerStatus.Text = ListenerRunning ? Properties.Resources.Maintenance_Label_Running : Properties.Resources.Maintenance_Label_NotRunning;
            ListenerStatus.Foreground = ListenerRunning ? (Brush)new BrushConverter().ConvertFromString("#16c933") : Brushes.Red;
            ListenerIP.Foreground = ListenerRunning ? (Brush)new BrushConverter().ConvertFromString("#16c933") : Brushes.Red;
            ListenerPort.Foreground = ListenerRunning ? (Brush)new BrushConverter().ConvertFromString("#16c933") : Brushes.Red;

            var PMSConnect = PMS != null;
            PMSConnected.Text = PMSConnect ? PMS.ConfigurationValue : Properties.Resources.Maintenance_Label_None;
            PMSConnected.Foreground = PMSConnect ? (Brush)new BrushConverter().ConvertFromString("#16c933") : Brushes.Red;


            RefreshButton.IsEnabled = true;
        }

        public async Task<bool> CheckAPI()
        {
            try
            {
                VCheck.Interface.API.VCheckAPI sAPI = new VCheck.Interface.API.VCheckAPI();

                return await sAPI.TestConnection();
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        public bool CheckListener()
        {
            return Process.GetProcessesByName("VCheckListenerWorker").Any();
        }

        /// <summary>
        /// Get current IP Address
        /// </summary>
        public static string GetAssignedIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RestartListener();

            RefreshAll();

            App.RefreshMaintenanceHandler(e, sender);
        }

        private void RestartListener()
        {
            Process[] processes = Process.GetProcessesByName("VCheckListenerWorker");

            if (!processes.Any())
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "C:\\VCheck\\VCheckListenerWorker\\VCheckListenerWorker.exe", // Replace with your executable or script
                    UseShellExecute = true,   // Required for 'runas'
                    Verb = "runas",           // Triggers UAC prompt for admin rights
                    Arguments = ""            // Optional: pass arguments here
                };

                Process.Start(psi);
            }
            else
            {
                PingPortAsync(processes, GetAssignedIPAddress(), 8585);
            }
        }

        public static async Task PingPortAsync(Process[] processes, string host, int port, int timeoutMs = 3000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask || completedTask.Status == TaskStatus.Faulted)
                    {
                        foreach (Process proc in processes)
                        {
                            try
                            {
                                proc.Kill(); // Forcefully terminate
                                proc.WaitForExit(); // Wait until it exits
                            }
                            catch (Exception ex)
                            {

                            }
                        }

                        string exePath = @"C:\VCheck\VCheckListenerWorker\VCheckListenerWorker.exe";

                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true, // Required for 'runas'
                            Verb = "runas",         // Triggers elevation prompt
                            WorkingDirectory = System.IO.Path.GetDirectoryName(exePath)
                        };

                        Process.Start(psi);
                        //Process.Start(exePath);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        //private void SaveButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ComboBoxItem selectedItem = (ComboBoxItem)ConnectionType.SelectedItem;

        //    var sConfigObj = configDBContext.GetConfigurationData("Connection_Type").FirstOrDefault();
        //    if (sConfigObj != null)
        //    {
        //        configDBContext.UpdateConfiguration("Connection_Type", selectedItem.Tag.ToString());
        //    }
        //    else
        //    {
        //        configDBContext.AddConfiguration("Connection_Type", selectedItem.Tag.ToString());
        //    }

        //    App.MainViewModel.Origin = "GeneralSettingsUpdated";

        //    App.PopupHandler(e, sender);

        //    RestartListener();
        //    RefreshAll();
        //    App.RefreshMaintenanceHandler(e, sender);
        //}


        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingConfigurationPageHandler(e, sender);
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingDevicePageHandler(e, sender);
        }

        private void DownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.UpdateLink) { UseShellExecute = true });
        }
    }
}
