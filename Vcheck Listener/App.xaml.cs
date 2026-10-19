using Hardcodet.Wpf.TaskbarNotification;
using LiteDB;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Vcheck_Listener.Lib.Models;
using Vcheck_Listener.Lib.Services;
using Vcheck_Listener.Lib.Themes;
using Vcheck_Listener.Views;

namespace Vcheck_Listener
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TrayIcon trayIcon;
        private SocketListener listener;
        private ScheduleAutomation scheduleAutomation;
        private LogWindow logWindow;
        private ConfigurationWindow configurationWindow;
        private int connectionCount = 0;
        private DispatcherTimer _timer;
        public static event Action<bool> OnThemeChanged;
        public static string DeviceType { get; set; }
        public static string IPAddress { get; set; }
        public static int Port { get; set; } = 8585;
        public static bool reRrunSocketListener { get; set; } = false;
        public static bool runScheduleAutomation { get; set; } = false;

        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static LocalizationService _localizationService = new LocalizationService();

        public static LiteDatabase db = new LiteDatabase("Filename=C:\\VCheck\\Listener.db;Password=Vch@ck123;");
        private CancellationTokenSource _cts;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Subscribe to theme change events
            OnThemeChanged += (dark) =>
            {
                ChangeTheme(dark);
            };
            StartThemeListener();
            ChangeTheme(IsWindowsInDarkMode());

            _localizationService.SetCulture("en");

            var listenerConfig = App.db.GetCollection<ListenerConfig>("ListenerConfig");
            var result = listenerConfig.FindOne(x => x.Id == 1);
            var connectionType = result.ConnectionType;
            IPAddress = GetIPAddressAccordingToType();

            //var host = Dns.GetHostEntry(Dns.GetHostName());
            //foreach (var ip in host.AddressList)
            //{
            //    if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
            //    {
            //        IPAddress = ip.ToString();
            //    }
            //}

            var deviceType = result != null && !string.IsNullOrEmpty(result.AnalyzerType) ? result.AnalyzerType : "";
            bool activateScheduleAutomation = result != null && result.IsScheduleAutomationActive == 1 && result.AnalyzerType == "C10" ? true : false;

            trayIcon = new TrayIcon();
            listener = new SocketListener();
            scheduleAutomation = new ScheduleAutomation();
            configurationWindow = new ConfigurationWindow();

            logWindow = new LogWindow() { Title = _localizationService.GetString("General_Label_Logs") + " (" + IPAddress + ":8585)" };

            listener.OnClientConnect = msg =>
            {
                var ipPort = msg.Split(":");
                connectionCount++;
                trayIcon.ShowBalloon(_localizationService.GetString("General_Message_ClientConnected"), $"IP Address: {ipPort[0]} \n Port : {ipPort[1]}", BalloonIcon.Info);
                logWindow.AddLog($"{_localizationService.GetString("General_Message_ClientConnected")} ({msg})");
                trayIcon.UpdateTooltip($"{_localizationService.GetString("General_Message_VCheckListener")} ({IPAddress}) — {connectionCount} connection(s)");
            };

            listener.OnDataReceived = msg =>
            {
                //logWindow.AddLog($"[Data] \n\n{msg}\n");
                logWindow.AddLog("Data received.");
            };

            listener.OnError = msg =>
            {
                logWindow.AddLog($"[Error - Listener] {msg}");
            };

            listener.OnClientDisconnect = msg =>
            {
                var ipPort = msg.Split(":");
                connectionCount--;
                trayIcon.ShowBalloon(_localizationService.GetString("General_Message_ClientDisconnected"), $"IP Address: {ipPort[0]} \n Port : {ipPort[1]}", BalloonIcon.Info);
                logWindow.AddLog($"{_localizationService.GetString("General_Message_ClientDisconnected")} ({msg})");
                trayIcon.UpdateTooltip($"{_localizationService.GetString("General_Message_VCheckListener")} ({IPAddress}) — {connectionCount} connection(s)");
            };

            scheduleAutomation.OnError = msg =>
            {
                logWindow.AddLog($"[Error - Schedule Automation] {msg}");
            };

            scheduleAutomation.OnScheduleAutomationProcessUpdate = msg =>
            {
                if (msg)
                {
                    logWindow.AddLog("Schedule Automation is running.");
                }
                else
                {
                    logWindow.AddLog("Schedule Automation is stopped.");
                }
            };

            configurationWindow.OnConfigurationUpdated = msg =>
            {
                trayIcon.ShowBalloon("Listener Configuration", "Configuration is updated.", BalloonIcon.Info);
            };

            configurationWindow.OnReRunSocketListenernProcess = msg =>
            {
                if (msg) { ReRunSocketListenerProcess(); }
                else
                {
                    if (string.IsNullOrEmpty(IPAddress))
                    {
                        trayIcon.UpdateTooltip($"{_localizationService.GetString("General_Message_VCheckListener")} (No Ip Address found) — 0 connection(s)");
                    }

                    PingPortAsync(IPAddress, 8585);
                }
            };

            configurationWindow.OnRunScheduleAutomationProcess = msg =>
            {
                if (msg) { RunScheduleAutomationProcess(); }
            };

            Task.Run(() => listener.Start(IPAddress, Port));
            if(activateScheduleAutomation) { RunScheduleAutomationProcess(); }
            trayIcon.ShowBalloon(_localizationService.GetString("General_Message_VCheckListener"), _localizationService.GetString("General_Message_ServerNowListen") + Port + ".", BalloonIcon.Info);
            trayIcon.UpdateTooltip($"{_localizationService.GetString("General_Message_VCheckListener")} ({IPAddress}) — 0 connection(s)");

            // Don’t show any window
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ShowConfigurationWindow();

            // Initialize the timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Set interval to 5 minutes
                //Interval = TimeSpan.FromSeconds(5) // Set interval to 5 seconds
            };
            _timer.Tick += Timer_Tick; // Attach the Tick event
            _timer.Start(); // Start the timer
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            PingPortAsync(IPAddress, 8585);
        }

        /// <summary>
        /// Starts listening for Windows theme changes.
        /// </summary>
        public static void StartThemeListener()
        {
            SystemEvents.UserPreferenceChanged += (sender, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    bool isDark = IsWindowsInDarkMode();
                    OnThemeChanged?.Invoke(isDark);
                }
            };
        }

        /// <summary>
        /// Checks if Windows is currently using Dark Mode for apps.
        /// </summary>
        /// <returns>True if Dark Mode is enabled, otherwise false.</returns>
        public static bool IsWindowsInDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object registryValue = key.GetValue("AppsUseLightTheme");
                        if (registryValue is int value)
                        {
                            return value == 0; // 0 = Dark Mode, 1 = Light Mode
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error when checking windows theme >>> " + ex.ToString());
            }

            return false;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            listener?.Stop();
            trayIcon?.Dispose();
        }

        public void ShowLogWindow()
        {
            if (logWindow.IsVisible)
                logWindow.Activate();
            else
                logWindow.Show();
        }

        public void ShowConfigurationWindow()
        {
            if (configurationWindow.IsVisible)
                configurationWindow.Activate();
            else
                configurationWindow.Show();
        }

        public async void ReRunSocketListenerProcess()
        {
            _timer.Stop();
            reRrunSocketListener = true;

            try
            {
                while (true)
                {
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(IPAddress, Port);
                        var timeoutTask = Task.Delay(3000);

                        var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                        if (completedTask == timeoutTask || completedTask.Status == TaskStatus.Faulted)
                        {
                            Task.Run(() => listener.Start(IPAddress, Port));
                            break;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error when rerun listener >>> " + ex.ToString());
            }

            _timer.Start();
        }

        public void RunScheduleAutomationProcess()
        {
            runScheduleAutomation = true;
            Task.Run(() => scheduleAutomation.Start());
        }

        public void ChangeTheme(bool isDark)
        {
            String sThemeName = isDark ? "Dark.xaml" : "Light.xaml";
            AppTheme.ChangeTheme(new Uri("Lib/Themes/" + sThemeName, UriKind.Relative));
        }

        public async Task PingPortAsync(string ipAddress, int port, int timeoutMs = 3000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask || completedTask.Status == TaskStatus.Faulted)
                    {
                        IPAddress = GetIPAddressAccordingToType();

                        Task.Run(() => listener.Start(IPAddress, Port));
                        configurationWindow.LoadListenerInfo(null, null);
                        trayIcon.ShowBalloon(_localizationService.GetString("General_Message_VCheckListener"), _localizationService.GetString("General_Message_ServerNowListen") + Port + ".", BalloonIcon.Info);
                        trayIcon.UpdateTooltip($"{_localizationService.GetString("General_Message_VCheckListener")} ({IPAddress}) — 0 connection(s)");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error when checking listener >>> " + ex.ToString());
            }
        }

        public string GetIPAddressAccordingToType()
        {
            try
            {
                var listenerConfig = App.db.GetCollection<ListenerConfig>("ListenerConfig");
                var result = listenerConfig.FindOne(x => x.Id == 1);
                var connectionType = result.ConnectionType;
                string preferredType = "ethernet";

                if (!string.IsNullOrEmpty(connectionType))
                {
                    var value = connectionType.Trim().ToLowerInvariant();
                    if (value == "wifi" || value == "ethernet")
                    {
                        preferredType = value;
                    }
                }

                string ip = FindIpByConnectionType(preferredType);
                if (!string.IsNullOrEmpty(ip))
                {
                    return ip;
                }

                preferredType = preferredType == "ethernet" ? "wifi" : "ethernet";
                ip = FindIpByConnectionType(preferredType);

                listenerConfig.Update(new ListenerConfig { Id = 1, IsScheduleAutomationActive = result.IsScheduleAutomationActive, AnalyzerIP = result.AnalyzerIP, AnalyzerPort = result.AnalyzerPort, AnalyzerType = result.AnalyzerType, PIMSIP = result.PIMSIP, PIMSPort = result.PIMSPort, PIMSUsername = result.PIMSUsername, PIMSPassword = result.PIMSPassword, ConversionChart = result.ConversionChart, ClinicID = result.ClinicID, ConnectionType = string.IsNullOrEmpty(ip) ? "" : preferredType });

                return ip;
            }
            catch
            {
                return "";
            }
        }

        private static string FindIpByConnectionType(string connectionType)
        {
            if (string.IsNullOrEmpty(connectionType)) { return ""; }

            NetworkInterfaceType networkType = connectionType == "ethernet"
                ? NetworkInterfaceType.Ethernet
                : NetworkInterfaceType.Wireless80211;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType != networkType)
                    continue;

                var ipv4 = ni.GetIPProperties().UnicastAddresses
                    .Select(x => x.Address)
                    .FirstOrDefault(a =>
                        a.AddressFamily == AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(a) &&
                        !IsApipaAddress(a));

                if (ipv4 != null)
                {
                    return ipv4.ToString();
                }
            }

            return "";
        }

        private static bool IsApipaAddress(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
        }
    }

}
