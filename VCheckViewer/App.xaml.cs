using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Microsoft.Windows.Themes;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using VCheck.Helper;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Lib.Culture;
using VCheckViewer.Services;
using VCheckViewer.Services.MessageBox;
using VCheckViewer.ViewModels.Windows;
using VCheckViewer.Views.Pages.Login;
using VCheckViewer.Views.Windows;
using Wpf.Ui;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VCheckViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string currentVersion = "1.6";
        public static MainViewModel MainViewModel { get; } = new MainViewModel();

        public static event EventHandler GoPreviousPage;
        public static event EventHandler Popup;
        public static event EventHandler RefreshMaintenance;
        public static event EventHandler RefreshUnreadNotification;

        public static event EventHandler GoToSettingUserPage;
        public static event EventHandler GoToSettingLanguageCountryPage;
        public static event EventHandler GoToSettingDevicePage;
        public static event EventHandler GoToSettingConfigurationPage;
        public static event EventHandler GoToReportPage;
        public static event EventHandler GoToClinicInfoPage;
        public static event EventHandler TempChangeLanguage;
        public static event EventHandler GoToInformationPage;

        public static event EventHandler? GoToViewResultPage;
        public static event EventHandler? GoToResultPage;

        public static SignInManager<IdentityUser> SignInManager { get; set; }
        public static UserManager<IdentityUser> UserManager { get; set; }
        public static RoleManager<IdentityRole> RoleManager { get; set; }
        public static IUserStore<IdentityUser> UserStore { get; set; }

        public static string newPassword { get; set; }
        public static SMTPModel SMTP { get; set; }
        public static string UpdateLink {  get; set; }
        public static string LatestAppVersionFolder { get; set; }
        public static long TestResultID { get; set; }
        public static ScheduledTestModel ScheduleTestInfo { set; get; }
        public static ScheduledTestModelExtended ScheduleTestInfoExtended { set; get; }
        public static int AnalyzerID { set; get; }
        public static List<string> Parameters { get; set; }
        public static List<TestDeviceName> Device { get; set; }
        public static TestResultModel TestResultInfo { get; set; }
        public static string FilePath { get; set; }
        public static bool ErrorOccur { get; set; }
        public static bool IsPrint { get; set; }
        public static string PMSFunction { get; set; }
        public static bool isLanguagePage { get; set; }
        public static bool isSchedulePage { get; set; }
        public static bool isEmptyName { get; set; }
        public static List<DownloadPrintResultModel> DowloadPrintObject { get; set; }
        public static string ClinicID { get; set; }
        public static bool ShowUpdateNotification { get; set; } = false;
        public static bool ConnectionStatus { get; set; } = false;
        public static double WindowHeight { get; set; }
        public static bool isLightTheme { get; set; }
        public static bool HideTopTabBar { get; set; }

        public static IConfiguration iConfig { get; set; }



        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging

        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => 
            {
                try
                {
                    //File.WriteAllText(
                    //System.IO.Path.Combine(AppContext.BaseDirectory, "ConfigureAppConfiguration.txt"),
                    //"Can configure app configuration");
                    c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location));
                }
                catch (Exception ex)
                {
                    //File.WriteAllText(
                    //System.IO.Path.Combine(AppContext.BaseDirectory, "App_Configuration_Error.txt"),
                    //ex.ToString());
                    //Environment.Exit(1);
                    log.Error("App Configuration Error >>> ", ex);
                }
            })
            .ConfigureServices((context, services) =>
            {
                try
                {
                    services.AddHostedService<ApplicationHostService>();

                    services.AddSingleton<IPageService, PageServices>();
                    services.AddSingleton<IThemeService, ThemeService>();
                    services.AddSingleton<ITaskBarService, TaskBarService>();
                    services.AddSingleton<INavigationService, NavigationService>();


                    services.Add(new ServiceDescriptor(typeof(UserDBContext), new UserDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(MasterCodeDataDBContext), new MasterCodeDataDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(RolesDBContext), new RolesDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(CountryDBContext), new CountryDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(ConfigurationDBContext), new ConfigurationDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(TemplateDBContext), new TemplateDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(NotificationDBContext), new NotificationDBContext(context.Configuration)));
                    services.Add(new ServiceDescriptor(typeof(DeviceDBContext), new DeviceDBContext(context.Configuration)));

                    services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(context.Configuration.GetConnectionString("DefaultConnection"), new MySqlServerVersion(new Version(8, 0, 21))));

                    var smtpSetting = context.Configuration.GetSection("SMTP");

                    SMTP = new SMTPModel()
                    {
                        Host = smtpSetting.GetValue<string>("Host"),
                        Port = smtpSetting.GetValue<int>("Port"),
                        Username = smtpSetting.GetValue<string>("Username"),
                        Password = smtpSetting.GetValue<string>("Password"),
                        Sender = smtpSetting.GetValue<string>("Sender")
                    };

                    var identitySettings = context.Configuration.GetSection("IdentitySettings");

                    services.AddIdentityCore<IdentityUser>(options =>
                    {
                        // Password settings.
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = 8;

                        //Lockedout setting
                        options.SignIn.RequireConfirmedAccount = identitySettings.GetValue<bool>("RequireConfirmedAccount");
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identitySettings.GetValue<int>("DefaultLockedOutTimeSpan"));
                        options.Lockout.MaxFailedAccessAttempts = identitySettings.GetValue<int>("MaxFailedAccessAttempts");
                    })
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();

                    services.AddScoped<SignInManager<IdentityUser>>();
                    services.AddHttpContextAccessor();
                    services.AddAuthentication();

                    UpdateLink = context.Configuration.GetValue<string>("UpdateLink");
                    LatestAppVersionFolder = context.Configuration.GetValue<string>("LatestAppVersionFolder");
                }
                catch(Exception ex)
                {
                    //File.WriteAllText(
                    //System.IO.Path.Combine(AppContext.BaseDirectory, "Service_Configuration_Error.txt"),
                    //ex.ToString());
                    //Environment.Exit(1);
                    log.Error("Service Configuration Error >>> ", ex);
                }
                
            })
            .Build();


        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                //File.WriteAllText(
                //System.IO.Path.Combine(AppContext.BaseDirectory, "OnStartup.txt"),
                //"Can run on start up");

                ConfigurationDBContext ConfigurationContext = GetService<ConfigurationDBContext>();
                MainViewModel.ConfigurationModel = ConfigurationContext.GetConfigurationData("");

                if (MainViewModel.ConfigurationModel.Count() == 0)
                {
                    System.Windows.Forms.MessageBox.Show("The VCheck Viewer is unable to connect with its database – please contact support.");
                    Environment.Exit(0);
                }

                var Language = MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "SystemSettings_Language").FirstOrDefault()?.ConfigurationValue;

                CultureInfo sZHCulture = new CultureInfo(Language != null ? Language : "en");
                CultureResources.ChangeCulture(sZHCulture);

                ResourceManager rm = new ResourceManager("VCheckViewer.Properties.Resources", typeof(App).Assembly);

                var processes = Process.GetProcessesByName("VcheckViewer");
                if (processes.Length > 1)
                {
                    System.Windows.Forms.MessageBox.Show(rm.GetString("General_Message_VCheckAlreadyOpen", sZHCulture));
                    processes[1].Kill(); // Terminate the process
                    processes[1].WaitForExit(); // Wait for the process to exit
                }
                else
                {

                    const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                    const string registryValueName = "AppsUseLightTheme";

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
                    {
                        if (key != null)
                        {
                            object registryValue = key.GetValue(registryValueName);
                            if (registryValue != null && registryValue is int value)
                            {
                                isLightTheme = value == 1;
                                // 0 means Dark Theme, 1 means Light Theme
                            }
                        }
                    }

                    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                    UserDBContext usersContext = GetService<UserDBContext>();
                    RolesDBContext roleContext = GetService<RolesDBContext>();

                    SignInManager = GetService<SignInManager<IdentityUser>>();
                    UserManager = GetService<UserManager<IdentityUser>>();
                    RoleManager = GetService<RoleManager<IdentityRole>>();
                    UserStore = GetService<IUserStore<IdentityUser>>();

                    string downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

                    if (File.Exists(downloadFolderPath + @"\Vcheck Patch.exe"))
                    {
                        File.Delete(downloadFolderPath + @"\Vcheck Patch.exe");

                        if (File.Exists(downloadFolderPath + @"\Vcheck Patch.exe"))
                        {
                            System.Windows.Forms.MessageBox.Show("Failed to delete patch installer. Can delete manually at below path:\n\n" + downloadFolderPath + @"\Vcheck Patch.exe");
                        }
                    }

                    (string SelectedButton, bool IsCheckBoxChecked) result = ("", false);

                    var SystemVersionReminder = MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "System_VersionReminder").FirstOrDefault()?.ConfigurationValue;
                    VCheckAPI vcheckAPI = new VCheckAPI();
                    var SystemVersion = MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "System_Version").FirstOrDefault();

                    if (SystemVersionReminder != null && !DateTime.TryParseExact(SystemVersionReminder, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        SystemVersionReminder = DateOnly.FromDateTime(DateTime.Now.Date).AddDays(-1).ToString("dd-MM-yyyy");
                    }

                    if (SystemVersionReminder == null || DateTime.ParseExact(SystemVersionReminder, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None) < DateTime.Now || (SystemVersion != null && currentVersion != SystemVersion.ConfigurationValue))
                    {
                        var IsLatestVersion = false;
                        if (SystemVersion == null) { ConfigurationContext.AddConfiguration("System_Version", currentVersion); }
                        else if (currentVersion != SystemVersion.ConfigurationValue)
                        {
                            ConfigurationContext.UpdateConfiguration("System_Version", currentVersion);
                        }

                        IsLatestVersion = await vcheckAPI.IsLatestVersion(currentVersion);

                        if (!IsLatestVersion)
                        {
                            //ShowUpdateNotification = true;
                            result = CustomMessageBox.Show(1, false);
                        }
                        else
                        {
                            var oneDaysDate = DateOnly.FromDateTime(DateTime.Now.Date).AddDays(1).ToString("dd-MM-yyyy");
                            if (SystemVersionReminder != null)
                            {
                                ConfigurationContext.UpdateConfiguration("System_VersionReminder", oneDaysDate);
                            }
                            else
                            {
                                ConfigurationContext.AddConfiguration("System_VersionReminder", oneDaysDate);
                            }
                        }
                    }


                    if (result.SelectedButton == "No")
                    {
                        System.Windows.Forms.MessageBox.Show(rm.GetString("General_Message_RemindThreeDays", sZHCulture));

                        var threeDaysDate = DateOnly.FromDateTime(DateTime.Now).AddDays(3).ToString("dd-MM-yyyy");
                        if (SystemVersionReminder != null)
                        {
                            ConfigurationContext.UpdateConfiguration("System_VersionReminder", threeDaysDate);
                        }
                        else
                        {
                            ConfigurationContext.AddConfiguration("System_VersionReminder", threeDaysDate);
                        }
                    }
                    else if (result.SelectedButton == "Yes")
                    {
                        //Process.Start(new ProcessStartInfo(LatestAppVersionFolder) { UseShellExecute = true });

                        //result = CustomMessageBox.Show(0, false);
                        //if (result.SelectedButton == "Yes") { Process.Start("C:\\VCheck\\VCheck Viewer Installer (Staging).exe"); }

                        //var downloadSuccess = await vcheckAPI.DownloadLatestInstaller();
                        //if (downloadSuccess)
                        //{
                        //    System.Windows.Forms.MessageBox.Show("Download successful.");

                        //    string downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
                        //    string zipPath = downloadFolderPath + @"\Vcheck Patch.zip";
                        //    string extractPath = downloadFolderPath;

                        //    ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

                        //    ProcessStartInfo startInfo = new ProcessStartInfo
                        //    {
                        //        FileName = downloadFolderPath + @"\Deploy Patch.bat", // Replace with your batch file path
                        //        //CreateNoWindow = true,           // Prevents the CMD window from showing
                        //        UseShellExecute = true, // Required for 'runas'
                        //        Verb = "runas",         // Triggers UAC for admin rights
                        //        //WindowStyle = ProcessWindowStyle.Hidden, // Hide window
                        //    };

                        //    using (Process process = new Process { StartInfo = startInfo })
                        //    {
                        //        process.Start();
                        //    }

                        //    // Start the batch file in a new process
                        //    //Process.Start(new ProcessStartInfo
                        //    //{
                        //    //    FileName = "cmd.exe",
                        //    //    Arguments = $"/c start \"\" \"{downloadFolderPath + @"\Deploy Patch.bat"}\"",
                        //    //    Verb = "runas",
                        //    //    UseShellExecute = true,
                        //    //    WorkingDirectory = @"C:\Windows\System32"
                        //    //});

                        //    //Current.Shutdown();
                        //    Environment.Exit(0);
                        //}
                        //else { System.Windows.Forms.MessageBox.Show("Download unsuccessful. Please try again later."); }

                        LoadingMessageForm msg = new LoadingMessageForm("Downloading patch...");

                        msg.Show();

                        var downloadSuccess = await vcheckAPI.DownloadLatestPatch();

                        msg.Close();

                        if (downloadSuccess)
                        {
                            string patchPath = downloadFolderPath + @"\Vcheck Patch.exe";

                            Process.Start(patchPath);
                            Environment.Exit(0);
                        }
                        else { System.Windows.Forms.MessageBox.Show("Download unsuccessful. Please try again later."); }

                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                //File.WriteAllText(
                //    System.IO.Path.Combine(AppContext.BaseDirectory, "startup_error.txt"),
                //    ex.ToString());
                //Environment.Exit(1);

                log.Error("Startup Error >>> ", ex);
            }

            //File.WriteAllText(
            //System.IO.Path.Combine(AppContext.BaseDirectory, "OnStartup.txt"),
            //"before host start");

            _host.Start();

            //LoginWindow LoginPage = new LoginWindow();
            //LoginPage.Navigate(new RegisterPage());
            //Current.MainWindow = LoginPage;
            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            //LoginPage.Show();
        }

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);
        //    var w = new System.Windows.Window { Width = 800, Height = 600, Title = "Test Window" };
        //    w.Content = new TextBlock { Text = "Hello Kiosk!", FontSize = 32, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
        //    Current.MainWindow = w;
        //    w.Show();
        //}

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            try
            {
                await _host.StopAsync();

                _host.Dispose();
            }
            catch (Exception ex)
            {
                log.Error("Exit Error >>> ", ex);
            }
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var ex = e.Exception;
            log.Error("General Error >>> ", ex);
            e.Handled = true;
        }

        public static void GoPreviousPageHandler(EventArgs e, object sender)
        {
            if (GoPreviousPage != null)
            {
                GoPreviousPage(sender, e);
            }
        }

        public static void PopupHandler(EventArgs e, object sender)
        {
            if (Popup != null)
            {
                Popup(sender, e);
            }
        }

        public static void RefreshMaintenanceHandler(EventArgs e, object sender)
        {
            if (RefreshMaintenance != null)
            {
                RefreshMaintenance(sender, e);
            }
        }

        public static void RefreshUnreadNotificationHandler(EventArgs e, object sender)
        {
            if (RefreshUnreadNotification != null)
            {
                RefreshUnreadNotification(sender, e);
            }
        }

        public static void GoToSettingUserPageHandler(EventArgs e, object sender)
        {
            if (GoToSettingUserPage != null)
            {
                GoToSettingUserPage(sender, e);
            }
        }

        public static void GoToSettingLanguageCountryPageHandler(EventArgs e, object sender)
        {
            if (GoToSettingLanguageCountryPage != null)
            {
                GoToSettingLanguageCountryPage(sender, e);
            }
        }

        public static void GoToSettingDevicePageHandler(EventArgs e, object sender)
        {
            if (GoToSettingDevicePage != null)
            {
                GoToSettingDevicePage(sender, e);
            }
        }

        public static void GoToSettingConfigurationPageHandler(EventArgs e, object sender)
        {
            if (GoToSettingConfigurationPage != null)
            {
                GoToSettingConfigurationPage(sender, e);
            }
        }

        public static void GoToSettingReportPageHandler(EventArgs e, object sender)
        {
            if (GoToReportPage != null)
            {
                GoToReportPage(sender, e);
            }
        }

        public static void GoToClinicInfoPageHandler(EventArgs e, object sender)
        {
            if (GoToClinicInfoPage != null)
            {
                GoToClinicInfoPage(sender, e);
            }
        }

        public static void GoToViewResultPageHandler(EventArgs e, object sender)
        {
            if (GoToViewResultPage != null)
            {
                GoToViewResultPage(sender, e);
            }
        }

        public static void GoToResultPageHandler(EventArgs e, object sender)
        {
            if (GoToResultPage != null)
            {
                GoToResultPage(sender, e);
            }
        }

        public static void TempChangeLanguageHandler(EventArgs e, object sender)
        {
            if (TempChangeLanguage != null)
            {
                TempChangeLanguage(sender, e);
            }
        }

        public static void GoToInformationPageHandler(EventArgs e, object sender)
        {
            if (GoToInformationPage != null)
            {
                GoToInformationPage(sender, e);
            }
        }

        public async static void CreateUser()
        {
            ConfigurationDBContext ConfigurationContext = GetService<ConfigurationDBContext>();
            MasterCodeDataDBContext sContext = GetService<MasterCodeDataDBContext>();
            RolesDBContext rolesContext = GetService<RolesDBContext>();
            UserDBContext usersContext = GetService<UserDBContext>();
            TemplateDBContext TemplateContext = GetService<TemplateDBContext>();
            NotificationDBContext NotificationContext = GetService<NotificationDBContext>();

            try
            {
                var user = Activator.CreateInstance<IdentityUser>();

                var emailStore = (IUserEmailStore<IdentityUser>)UserStore;

                await UserStore.SetUserNameAsync(user, MainViewModel.Users.LoginID, CancellationToken.None);
                if(MainViewModel.Users.EmailAddress != "")
                {
                    await emailStore.SetEmailAsync(user, MainViewModel.Users.EmailAddress, CancellationToken.None);
                }
                var result = await UserManager.CreateAsync(user, newPassword);

                ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();

                if (result.Succeeded)
                {
                    MainViewModel.Users.UserId = user.Id;
                    MainViewModel.Users.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    MainViewModel.Users.CreatedBy = (MainViewModel.CurrentUsers != null) ? MainViewModel.CurrentUsers.FullName : MainViewModel.Users.LoginID;

                    if (usersContext.InsertUser(MainViewModel.Users))
                    {
                        var roleResult = await UserManager.AddToRoleAsync(user, MainViewModel.Users.Role);

                        if (roleResult.Succeeded)
                        {
                            var notificationTemplate = TemplateContext.GetTemplateByCodeLang("US05", (sLangCode != null ? sLangCode.ConfigurationValue : ""));
                            notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("###<staff_id>###", MainViewModel.Users.EmployeeID).Replace("###<staff_fullname>###", MainViewModel.Users.FullName).Replace("'", "''");

                            NotificationModel notification = new NotificationModel()
                            {
                                NotificationType = "Updates",
                                NotificationTitle = notificationTemplate.TemplateTitle,
                                NotificationContent = notificationTemplate.TemplateContent,
                                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                CreatedBy = (MainViewModel.CurrentUsers != null) ? MainViewModel.CurrentUsers.FullName : MainViewModel.Users.LoginID
                            };

                            if (NotificationContext.InsertNotification(notification))
                            {

                            }
                            else
                            {

                            }

                            notificationTemplate = TemplateContext.GetTemplateByCodeLang("EN01", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                            notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''").Replace("###<staff_fullname>###", MainViewModel.Users.FullName).Replace("###<password>###", newPassword).Replace("###<login_id>###", user.UserName);

                            string sErrorMessage = "";

                            try
                            {
                                EmailObject sEmail = new EmailObject();

                                sEmail.SenderEmail = SMTP.Sender;

                                List<string> sRecipientList = [MainViewModel.Users.EmailAddress];


                                sEmail.RecipientEmail = sRecipientList;
                                sEmail.IsHtml = true;
                                sEmail.Subject = "[VCheck Viewer] " + notificationTemplate.TemplateTitle;
                                sEmail.Body = notificationTemplate.TemplateContent;
                                sEmail.SMTPHost = SMTP.Host;
                                sEmail.PortNo = SMTP.Port;
                                sEmail.HostUsername = SMTP.Username;
                                sEmail.HostPassword = SMTP.Password;
                                sEmail.EnableSsl = true;
                                sEmail.UseDefaultCredentials = false;

                                EmailHelper.SendEmail(sEmail, out sErrorMessage);

                                if (!String.IsNullOrEmpty(sErrorMessage)) { throw new Exception(sErrorMessage); }

                                notification = new NotificationModel()
                                {
                                    NotificationType = "Email",
                                    NotificationTitle = notificationTemplate.TemplateTitle,
                                    NotificationContent = notificationTemplate.TemplateContent,
                                    Receiver = string.Join(", ", sRecipientList),
                                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    CreatedBy = (MainViewModel.CurrentUsers != null) ? MainViewModel.CurrentUsers.FullName : MainViewModel.Users.LoginID
                                };

                                if (NotificationContext.InsertNotification(notification))
                                {

                                }
                                else
                                {

                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error("Email Error >>> ", ex);
                            }
                        }
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                App.log.Error("Add User Error >>> ", ex);
            }
        }
    }

}
