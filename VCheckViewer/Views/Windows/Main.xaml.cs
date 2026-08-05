using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NHapi.Model.V23.Segment;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VCheck.Helper;
using VCheck.Interface.API;
using VCheck.Interface.API.Lib.General;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Converter;
using VCheckViewer.Lib.Culture;
using VCheckViewer.Lib.Function;
using VCheckViewer.Views.Pages;
using VCheckViewer.Views.Pages.Dashboard;
using VCheckViewer.Views.Pages.Login;
using VCheckViewer.Views.Pages.Maintenance;
using VCheckViewer.Views.Pages.Notification;
using VCheckViewer.Views.Pages.Results;
using VCheckViewer.Views.Pages.Schedule;
using VCheckViewer.Views.Pages.Setting.Clinic;
using VCheckViewer.Views.Pages.Setting.Device;
using VCheckViewer.Views.Pages.Setting.Interface;
using VCheckViewer.Views.Pages.Setting.LanguageCountry;
using VCheckViewer.Views.Pages.Setting.Report;
using VCheckViewer.Views.Pages.Setting.User;
using Brushes = System.Windows.Media.Brushes;
using CheckBox = System.Windows.Controls.CheckBox;

namespace VCheckViewer.Views.Windows
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        static MasterCodeDataDBContext sContext = App.GetService<MasterCodeDataDBContext>();
        static RolesDBContext rolesContext = App.GetService<RolesDBContext>();
        static UserDBContext usersContext = App.GetService<UserDBContext>();
        static CountryDBContext countryContext = App.GetService<CountryDBContext>();

        ConfigurationDBContext ConfigurationContext = App.GetService<ConfigurationDBContext>();
        TemplateDBContext TemplateContext = App.GetService<TemplateDBContext>();
        NotificationDBContext NotificationContext = App.GetService<NotificationDBContext>();

        public static event EventHandler InitializedUserPage;
        public static event EventHandler ResetCurrentPassword;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ObservableCollection<ComboBoxItem> deviceComboList = new ObservableCollection<ComboBoxItem>();
        public ObservableCollection<CheckedListItem> ParameterList = new ObservableCollection<CheckedListItem>();

        public string PMS;
        public string url;
        public string port;
        public string username;
        public string password;
        public bool isIP;
        public System.Net.IPAddress iIP;
        private DispatcherTimer _timer;
        private TestResultModel sTestResultObj;
        public string CurrentPage;

        private static readonly string subscriptDigits = "₀₁₂₃₄₅₆₇₈₉₋₊";
        private static readonly string superscriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹⁻⁺";
        private static readonly string normalDigits = "0123456789-+";
        private static readonly string[] calItems = { "A/G", "B/C", "GLOB", "Na/K" };

        public class CheckedListItem
        {
            public string Name { get; set; }
            public bool isChecked { get; set; }
        }

        public Main()
        {
            InitializeComponent();
            DataContext = this;

            //WindowStyle = WindowStyle.None;
            //WindowState = WindowState.Maximized;
            //ResizeMode = ResizeMode.NoResize;
            //Topmost = true;

            //page
            UserPage.GoToAddUserPage += new EventHandler(GoToAddUserPage);
            UserPage.GoToUpdateUserPage += new EventHandler(GoToUpdateUserPage);
            UserPage.GoToViewUserPage += new EventHandler(GoToViewUserPage);
            ViewUserPage.GoToUpdateCurrentUserPage += new EventHandler(GoToUpdateUserPage);
            UserPage.GoToLanguageCountryPage += new EventHandler(GoToLanguageCountryPage);
            App.GoToViewResultPage += new EventHandler(GoToViewResultPage);
            App.GoToResultPage += new EventHandler(GoToResultPage);

            App.GoToSettingUserPage += new EventHandler(SettingUserPage);
            App.GoToSettingLanguageCountryPage += new EventHandler(GoToLanguageCountryPage);
            App.GoToSettingDevicePage += new EventHandler(GoToDevicePage);
            App.GoToSettingConfigurationPage += new EventHandler(GoToConfigurationPage);
            App.GoToReportPage += new EventHandler(GoToReportPage);
            App.GoToClinicInfoPage += new EventHandler(GoToClinicInfoPage);
            App.TempChangeLanguage += new EventHandler(TempChangeLanguage);
            App.GoToInformationPage += new EventHandler(GoToInformationPage);

            //popup
            App.Popup += new EventHandler(Popup);
            App.GoPreviousPage += new EventHandler(PreviousPage);
            App.RefreshMaintenance += new EventHandler(Timer_Tick);
            App.RefreshUnreadNotification += new EventHandler(CheckUnReadNotification);

            this.SizeChanged += MainWindow_SizeChanged;

            initializedDropdownSelectionList();

            Username.Header = App.MainViewModel.CurrentUsers.StaffName;

            //System.Windows.Data.Binding b = new System.Windows.Data.Binding("Dashboard_Title_PageTitle");
            //b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            //PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);
            PageTitle.Text = "Device";

            CheckThemesSettings();

            refreshMenuItemStyle(mnDashboard);

            var sBuilder = new ConfigurationBuilder();
            //sBuilder.SetBasePath(Directory.GetCurrentDirectory())
            //        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            sBuilder.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            App.iConfig = sBuilder.Build();

            CheckStatus();
            CheckUnReadNotificationAsync(true);
            CheckExpiredSchedule();

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
            CheckStatus();
            CheckUnReadNotificationAsync(true);
            CheckExpiredSchedule();
        }

        private async Task CheckStatus()
        {
            try
            {
                VCheckAPI sAPI = new VCheckAPI();
                var apiStatus = await sAPI.TestConnection();
                var listenerStatus = Process.GetProcessesByName("VCheckListenerWorker").Any();

                ListenerStatus.Fill = listenerStatus ? Brushes.LightGreen : Brushes.Red;
                APIStatus.Fill = apiStatus ? Brushes.LightGreen : Brushes.Red;

                //ListenerStatusText.Foreground = listenerStatus ? Brushes.LightGreen : Brushes.Red;
                //APIStatusText.Foreground = apiStatus ? Brushes.Green : Brushes.Red;
            }
            catch (Exception ex)
            {

            }
        }

        private void CheckUnReadNotification(object sender, EventArgs e)
        {
            CheckUnReadNotificationAsync(false);
        }

        private async Task CheckUnReadNotificationAsync(bool Sync)
        {
            NotificationTranslate.Text = Properties.Resources.Main_Label_SideMenuNotification;

            if (Sync)
            {
                var clinicInfo = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();

                if (clinicInfo != null)
                {
                    VCheckAPI sAPI = new VCheckAPI();
                    string errorListString = await sAPI.GetAllErrorNotSync(clinicInfo.ConfigurationValue);

                    if (errorListString != null)
                    {
                        var errorList = JsonConvert.DeserializeObject<List<APIErrorModel>>(errorListString);
                        List<NotificationModel> errorNotification = new List<NotificationModel>();

                        ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                        var notificationTemplate = TemplateContext.GetTemplateByCodeLang("SE01", (sLangCode != null ? sLangCode.ConfigurationValue : ""));
                        var temp = notificationTemplate;

                        foreach (var error in errorList)
                        {
                            VCheck.Lib.Data.Models.ResponseModel responseModel = JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.ResponseModel>(error.ErrorMessage);
                            CreateScheduledDataRequest scheduledModel = JsonConvert.DeserializeObject<CreateScheduledDataRequest>(error.ScheduleData);
                            //var content = "Order test [" + scheduledModel.body.ScheduledTestName + "] for " + scheduledModel.body.PatientName + "(" + scheduledModel.body.PatientID + ") failed to be created. Error ''" + responseModel.Body.ResponseMessage + "''. \nPlease create the schedule again with correct information.";
                            var content = temp.TemplateContent.Replace("###<testname>###", scheduledModel.body.ScheduledTestName).Replace("###<name>###", scheduledModel.body.PatientName).Replace("###<id>###", scheduledModel.body.PatientID).Replace("###<message>###", responseModel.Body.ResponseMessage);

                            errorNotification.Add(new NotificationModel()
                            {
                                //NotificationID = error.ID,
                                NotificationType = "Schedule Error",
                                NotificationTitle = temp.TemplateTitle,
                                NotificationContent = content,
                                CreatedDateDatetime = error.CreatedDate.Value
                            });
                        }

                        if (errorNotification.Count() > 0) { NotificationContext.InsertNotificationRange(errorNotification); }
                    }
                }
            }

            var totalNotification = NotificationContext.GetTotalUnreadNotification();

            UnreadCount.Text = totalNotification == 0 ? "" : " (" + totalNotification + ")";
            UnreadCountMinimize.Text = totalNotification == 0 ? "" : " (" + totalNotification + ")";

            NotificationCountBorder.Visibility = totalNotification == 0 ? Visibility.Collapsed : Visibility.Visible;
            NotificationCount.Text = totalNotification < 100 ? totalNotification.ToString() : "99+";
        }

        private async Task CheckExpiredSchedule()
        {
            var sConfigObj = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
            List<NotificationModel> expiredNotification = new List<NotificationModel>();
            var listString = "";

            if (sConfigObj != null)
            {
                VCheckAPI vcheckAPI = new VCheckAPI();
                listString = await vcheckAPI.GetScheduleList(sConfigObj.ConfigurationValue, false, true);
            }

            List<ScheduledTestModel> sScheduledList = string.IsNullOrEmpty(listString) ? new List<ScheduledTestModel>() : JsonConvert.DeserializeObject<List<ScheduledTestModel>>(listString);

            ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
            var notificationTemplate = TemplateContext.GetTemplateByCodeLang("SE02", (sLangCode != null ? sLangCode.ConfigurationValue : ""));
            var temp = notificationTemplate;


            foreach (var schedule in sScheduledList)
            {
                //var content = "Order test [" + schedule.ScheduledTestType + "] for " + schedule.PatientName + "(" + schedule.PatientID + ") have expired. \nPlease create the schedule again if the test need to be done.";
                var content = temp.TemplateContent.Replace("###<testtype>###", schedule.ScheduledTestType).Replace("###<name>###", schedule.PatientName).Replace("###<id>###", schedule.PatientID);

                expiredNotification.Add(new NotificationModel()
                {
                    //NotificationID = schedule.ID,
                    NotificationType = "Schedule Error",
                    NotificationTitle = temp.TemplateTitle,
                    NotificationContent = content,
                    CreatedDateDatetime = DateTime.Now
                });
            }

            if (expiredNotification.Count() > 0) { NotificationContext.InsertNotificationRange(expiredNotification); }

            var totalNotification = NotificationContext.GetTotalUnreadNotification();

            UnreadCount.Text = totalNotification == 0 ? "" : " (" + totalNotification + ")";
            NotificationTranslate.Text = Properties.Resources.Main_Label_SideMenuNotification;
        }

        #region INavigationWindow methods

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        private void ViewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Users = App.MainViewModel.CurrentUsers;
            App.HideTopTabBar = true;

            if (frameContent.CanGoBack) { frameContent.GoBack(); }

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Setting_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            GoToViewUserPage(sender, e);
        }

        private void ResetPassword(object sender, RoutedEventArgs e)
        {
            checklanguage();

            frameContent.Content = new ResetCurrentUserPasswordPage();

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("ResetPassword_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);
        }

        private void MaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Maintenance_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            GoToMaintenancePage(sender, e);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Origin = "Logout";
            Popup(sender, e);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        void GoToAddUserPage(object sender, EventArgs e)
        {
            frameContent.Content = new AddUserPage();
        }

        void GoToUpdateUserPage(object sender, EventArgs e)
        {
            frameContent.Content = new UpdateUserPage();
        }

        void GoToViewUserPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new ViewUserPage();
        }

        void GoToMaintenancePage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new MaintenancePage();
        }

        void GoToLanguageCountryPage(object sender, EventArgs e)
        {
            frameContent.Content = new LanguageCountryPage();
        }
        void GoToDevicePage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new DevicePage();
        }

        void GoToConfigurationPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new ConfigurationPage();
        }

        void GoToReportPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new ReportPage();
        }
        void GoToClinicInfoPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new ClinicInfoPage();
        }

        void GoToViewResultPage(object sender, EventArgs e)
        {
            frameContent.Content = new ViewResultPage();

            refreshMenuItemStyle(mnResults);

            CurrentPage = "";
        }

        void GoToResultPage(object sender, EventArgs e)
        {
            frameContent.Content = new ResultPage();

            refreshMenuItemStyle(mnResults);

            CurrentPage = "";
        }

        void SettingUserPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new UserPage();
        }

        void GoToInformationPage(object sender, EventArgs e)
        {
            checklanguage();

            frameContent.Content = new ConnectionPage();
        }

        public void UpdatePatientName(object sender, EventArgs e)
        {
            ResultPage resultPage = frameContent.Content as ResultPage;

            resultPage.LoadResultDataGrid();
        }

        public void UpdateDoctorName(object sender, EventArgs e)
        {
            ResultPage resultPage = frameContent.Content as ResultPage;

            resultPage.LoadResultDataGrid();
        }

        public void TempChangeLanguage(object sender, EventArgs e)
        {
            NotificationTranslate.Text = Properties.Resources.Main_Label_SideMenuNotification;
        }

        async void Popup(object sender, EventArgs e)
        {
            var ShowPopup = true;

            if (App.MainViewModel.Origin == "UserDeleteRow") { PopupContent.Text = Properties.Resources.Popup_Message_DeleteUser; }
            if (App.MainViewModel.Origin == "UserAddRow") { PopupContent.Text = Properties.Resources.Popup_Message_CreateUser; }
            if (App.MainViewModel.Origin == "UserUpdateRow") { PopupContent.Text = Properties.Resources.Popup_Message_UpdateUser; }
            if (App.MainViewModel.Origin == "ChangeLanguageCountry") { PopupContent.Text = Properties.Resources.Popup_Message_LanguageCountryChange; }
            if (App.MainViewModel.Origin == "Logout") { PopupContent.Text = Properties.Resources.Popup_Message_Logout; }
            if (App.MainViewModel.Origin == "ResetPassword") { PopupContent.Text = Properties.Resources.Popup_Message_ResetPassword; }
            if (App.MainViewModel.Origin == "DeviceDelete") { ShowPopup = false; DeleteDeviceHandler(e, sender); }
            if (App.MainViewModel.Origin == "DeviceUpdate") { ShowPopup = false; UpdateDeviceHandler(e, sender); }
            if (App.MainViewModel.Origin == "SettingsUpdate" || App.MainViewModel.Origin == "ReportSettingsUpdate" || App.MainViewModel.Origin == "LISSettingsUpdate") { ShowPopup = false; UpdateSettingsHandlerAsync(e, sender); }
            if (App.MainViewModel.Origin == "CancelSchedule") { PopupContent.Text = Properties.Resources.Popup_Message_CancelSchedule; }
            if (App.MainViewModel.Origin == "ClinicInfoUpdate") { ShowPopup = false; UpdateClinicInfoHandlerAsync(e, sender); }
            if (App.MainViewModel.Origin == "CannotSendToAnalyzer") { PopupSetup(false, false, true, false, false, false, false, false); PopupContent.Text = Properties.Resources.Popup_Message_AnalyzerCannotReceive; }
            if (App.MainViewModel.Origin == "NoClinicFound") { PopupSetup(false, false, true, false, false, false, false, false); PopupContent.Text = Properties.Resources.Popup_Message_NoClinicFound; }
            if (App.MainViewModel.Origin == "DeviceAdd")
            {
                //PopupSetup(true, true, false, false, false, false, false);

                //PopupContent.Text = Properties.Resources.Popup_Message_AddAnalyzer;
                ShowPopup = false;
                AddDeviceHandler(e, sender);
            }
            if (App.MainViewModel.Origin == "ScheduleCancelled")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_ScheduleCancel;
            }
            if (App.MainViewModel.Origin == "FailedToCancelSchedule")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedToCancelSchedule;
            }
            if (App.MainViewModel.Origin == "ClinicInfoUpdateCompleted")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_ClinicInfoUpdateCompleted;
            }
            if (App.MainViewModel.Origin == "SettingsUpdateCompleted")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_SavePMSLISHIS; 
            }
            if (App.MainViewModel.Origin == "ListingDownloadCompleted")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Results_Message_DownloadComplete; 
            }
            if (App.MainViewModel.Origin == "TestResultDownloadCompleted")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                var sBuilder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

                PopupContent.Text = Properties.Resources.Results_Message_TestResultDownloadCompleted + "\r\n "+ Properties.Resources.Popup_Message_TheFileIsAt + " \r\n" + sBuilder.Configuration.GetSection("Configuration:DownloadFolderPath").Value;
            }
            if (App.MainViewModel.Origin == "FailedDownloadListing")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedDownloadListing;
            }
            if (App.MainViewModel.Origin == "FailedToShowPrint")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedOpenPrint;
            }
            if (App.MainViewModel.Origin == "FailedAddDevice")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedAddDevice;
            }
            if (App.MainViewModel.Origin == "FailedDeleteDevice")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedDeleteDevice;
            }
            if (App.MainViewModel.Origin == "FailedUpdateDevice")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedUpdateDevice;
            }
            if (App.MainViewModel.Origin == "GreywindSendUniqueID")
            {
                var sConfigObj = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
                sTestResultObj = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), Convert.ToInt64(App.MainViewModel.TestResultID));
                var listString = "";

                if (sConfigObj != null)
                {
                    VCheckAPI vcheckAPI = new VCheckAPI();
                    listString = await vcheckAPI.GetScheduleList(sConfigObj.ConfigurationValue, true, false, sTestResultObj.TestResultType);
                    App.ClinicID = sConfigObj.ConfigurationValue;
                }

                List<ScheduledTestModel> sScheduledList = string.IsNullOrEmpty(listString) ? new List<ScheduledTestModel>() : JsonConvert.DeserializeObject<List<ScheduledTestModel>>(listString);

                if (sScheduledList != null && sScheduledList.Count > 0)
                {
                    PopupSetup(false, false, true, false, true, false, false, false, false, true);

                    deviceComboList.Clear();
                    deviceComboList.Add(new ComboBoxItem
                    {
                        Content = "--- Please select a schedule ---",
                        Tag = ""
                    });

                    foreach (var s in sScheduledList)
                    {
                        var uniqueID = s.ScheduleUniqueID.Split("-")[3];

                        DateFormatConverter dateFormatConverter = new DateFormatConverter();
                        var scheduleDatetime = dateFormatConverter.ConvertSimpleDate(s.ScheduledDateTime.Value.ToLocalTime()).ToString();

                        deviceComboList.Add(new ComboBoxItem
                        {
                            Content = Properties.Resources.Schedule_Label_PatientID + " " + s.PatientID + ", " + "Name: " + s.PatientName + " (" + scheduleDatetime+")",
                            Tag = uniqueID
                        });
                    }
                    

                    deviceList.ItemsSource = deviceComboList;
                    deviceList.SelectedIndex = 0;

                    PopupContent.Text = Properties.Resources.Popup_Message_SelectSchedule;
                    deviceList.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                }
                else
                {
                    App.MainViewModel.Origin = "NoScheduleFound";
                    PopupContent.Text = Properties.Resources.Popup_Message_NoScheduleFound;
                    PopupSetup(false, false, true, false, true, false, false, false);
                }                

                //ShowPopup = false;
                //SendToPMSHandler(e, sender);
            }
            if (App.MainViewModel.Origin == "SendToAnalyzer")
            {
                var ignoreDeviceType = new string[]{ "H6", "V200", "V2400", "C1" };
                var analyzerList = new List<string>();

                if (App.ScheduleTestInfoExtended != null)
                {
                    var testAnalyzers = App.ScheduleTestInfoExtended.IDAnalyzers.Select(x => x.Analyzers);

                    foreach (var analyzer in testAnalyzers)
                    {
                        analyzerList.AddRange(analyzer.Split(", "));
                    }
                }

                List<DeviceModel> devices = DeviceRepository.GetTwoWayCommDevice(ConfigSettings.GetConfigurationSettings());
                var deviceTypes = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings()).Where(x => x.TwoWayCommunication == 1 && !ignoreDeviceType.Contains(x.TypeName)).ToList();
                //string[] deviceListExtended = App.ScheduleTestInfoExtended.IDAnalyzers.Count() > 0 ? App.ScheduleTestInfoExtended.IDAnalyzers.FirstOrDefault().Analyzers.Split(", ") : new string[0];
                //var deviceTypeIDList = deviceTypes.Where(x => Array.Exists(deviceListExtended, element => element == x.TypeName)).Select(y => y.id).ToArray();
                //devices = devices.Where(x => Array.Exists(deviceTypeIDList, element => element == x.DeviceTypeID)).ToList();
                var deviceTypeIDList = deviceTypes.Where(x => analyzerList.Contains(x.TypeName)).Select(y => y.id).ToArray();
                devices = devices.Where(x => Array.Exists(deviceTypeIDList, element => element == x.DeviceTypeID)).ToList();

                deviceComboList.Clear();

                if (devices.Count() > 0)
                {
                    PopupSetup(false, false, true, false, false, false, false, false, false, true);

                    var selected = new ComboBoxItem
                    {
                        Content = "--- Please select analyzer ---",
                        Tag = ""
                    };
                    deviceComboList.Add(selected);

                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            deviceComboList.Add(new ComboBoxItem
                            {
                                Content = d.DeviceName,
                                Tag = d.id
                            });
                        }
                    }

                    deviceList.ItemsSource = deviceComboList;
                    deviceList.SelectedItem = selected;

                    PopupContent.Text = Properties.Resources.Popup_Message_SelectDevice;
                    deviceList.Visibility = Visibility.Visible;
                    btnCancel.Visibility = Visibility.Visible;
                }
                else
                {
                    App.MainViewModel.Origin = "NoDeviceFound";
                    PopupContent.Text = Properties.Resources.Popup_Message_NoDeviceFound;
                    PopupSetup(false, false, true, false, false, false, false, false);
                }

            }
            if (App.MainViewModel.Origin == "SentToAnalyzer")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                SchedulePage.ReloadScheduleHandler(e, sender);

                PopupContent.Text = Properties.Resources.Popup_Message_SentToAnalyzer;
            }
            if (App.MainViewModel.Origin == "FailedToSendToAnalyzer")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedToSendToAnalyzer;
            }
            if (App.MainViewModel.Origin == "CanceledSendToAnalyzer")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_CanceledSendToAnalyzer;
            }
            if (App.MainViewModel.Origin == "UpdatePatientName")
            {
                PopupSetup(false, false, true, true, false, true, false, false);
                txtInput.Text = App.TestResultInfo.PatientName;

                PopupContent.Text = Properties.Resources.Popup_Message_UpdatePatientName;
            }
            if (App.MainViewModel.Origin == "UpdateDoctorName")
            {
                PopupSetup(false, false, true, true, false, true, false, false);
                txtInput.Text = App.TestResultInfo.InchargePerson;

                PopupContent.Text = "Input doctor name.";
            }
            if (App.MainViewModel.Origin == "PatientNameUpdated")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_PatientNameUpdated;
            }
            if (App.MainViewModel.Origin == "DoctorNameUpdated")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = "Doctor name have been updated.";
            }
            if (App.MainViewModel.Origin == "ReportSettingsUpdateCompleted")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_ReportSettingUpdated;
            }
            if (App.MainViewModel.Origin == "FailedToDownload")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedToDownload;
            }
            if (App.MainViewModel.Origin == "FailedToPrint")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedToPrint;
            }
            if (App.MainViewModel.Origin == "GeneralErrorOccur")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.General_Message_ErrorOccured;
            }

            if (App.MainViewModel.Origin == "SelectTestID")
            {
                PopupSetup(false, false, true, true, false, false, false, true);

                parameterList.Items.Clear();

                int currentTest = 1;
                int remainder = 0;
                bool excess = false;

                const int columnCount = 2;

                int totalRow = Math.DivRem(App.Device.Count - 1, columnCount, out remainder);

                if (remainder > 0)
                {
                    totalRow += 1;
                    excess = true;
                }

                for (int i = 0; i < totalRow; i++)
                {
                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                    int elementPerRow = (i == totalRow - 1 && excess)
                        ? remainder
                        : columnCount;

                    for (int j = 0; j < elementPerRow; j++)
                    {
                        CheckBox checkBox = new CheckBox();
                        checkBox.Foreground = Brushes.White;
                        checkBox.BorderBrush = Brushes.White;

                        checkBox.Width = 420;
                        checkBox.Margin = new Thickness(10);

                        checkBox.Content =
                            $"{App.Device[currentTest].DeviceName} | " +
                            $"{App.Device[currentTest].TestResultType} | " +
                            $"{App.Device[currentTest].CreatedDate:yyyy-MM-dd HH:mm}";

                        checkBox.Tag = App.Device[currentTest].TestID;

                        stackPanel.Children.Add(checkBox);

                        currentTest++;
                    }

                    parameterList.Items.Add(stackPanel);
                }

                PopupContent.Text = "Select Test Result";
            }
            if (App.MainViewModel.Origin == "SelectParameters")
            {
                PopupSetup(false, false, true, true, false, false, false, true);

                parameterList.Items.Clear();

                int currentParameter = 0;
                int remainder = 0;
                bool excess = false;
                int totalRow = Math.DivRem(App.Parameters.Count, 5, out remainder);
                if (remainder > 0)
                {
                    totalRow += 1;
                    excess = true;
                }

                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                CheckBox checkBox = new CheckBox();
                checkBox.Name = "SelectAll";
                checkBox.Foreground = Brushes.White;
                checkBox.BorderBrush = Brushes.White;
                checkBox.Content = Properties.Resources.Popup_Message_SelectAll; ;
                stackPanel.Children.Add(checkBox);
                parameterList.Items.Add(stackPanel);
                checkBox.Click += SelectAll_Click;

                for (int i = 0; i < totalRow; i++)
                {
                    stackPanel = new StackPanel();
                    stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                    stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    int elementPerRow = (i == (totalRow - 1) && excess) ? remainder : 5;

                    for (int j = 0; j < elementPerRow; j++)
                    {
                        System.Windows.Controls.Border border = new System.Windows.Controls.Border();
                        border.MinWidth = 150;
                        checkBox = new CheckBox();
                        checkBox.Foreground = Brushes.White;
                        checkBox.BorderBrush = Brushes.White;
                        checkBox.Content = App.Parameters[currentParameter];
                        border.Child = checkBox;
                        stackPanel.Children.Add(border);
                        currentParameter++;
                    }

                    parameterList.Items.Add(stackPanel);
                }

                if (App.IsPrint)
                {
                    PopupContent.Text = Properties.Resources.Popup_Message_SelectParameterPrint;
                }
                else
                {
                    PopupContent.Text = Properties.Resources.Popup_Message_SelectParameterDownload;
                }
            }
            if (App.MainViewModel.Origin == "AskAdditionalTestResult")
            {
                PopupSetup(true, true, false, false, false, false, false, false);

                PopupContent.Text = "Do you want to include another analyzer's test result?";
            }
            if (App.MainViewModel.Origin == "SelectAdditionalTestResult")
            {
                PopupSetup(false, false, true, true, false, false, false, true);

                //App.Device.AddRange(TestResultsRepository.GetRelatedTestResultsByPatientIDAndAnalyzer(ConfigSettings.GetConfigurationSettings(),App.DowloadPrintObject[0].TestResult.PatientID,App.Device[0].DeviceName));
                App.Device.AddRange(TestResultsRepository.GetRelatedTestResultByPatientID(ConfigSettings.GetConfigurationSettings(),App.DowloadPrintObject[0].TestResult.PatientID,App.Device[0].DeviceName));

                if(App.Device.Count > 1)
                {
                    parameterList.Items.Clear();

                    int currentParameter = 1;
                    int remainder = 0;
                    bool excess = false;

                    //int totalRow = Math.DivRem(App.Device.Count - 1, 5, out remainder);
                    const int columnCount = 2;
                    int totalRow = Math.DivRem(App.Device.Count - 1, columnCount, out remainder);

                    if (remainder > 0)
                    {
                        totalRow += 1;
                        excess = true;
                    }

                    for (int i = 0; i < totalRow; i++)
                    {
                        StackPanel stackPanel = new StackPanel();
                        stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
                        stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        int elementPerRow = (i == (totalRow - 1) && excess)
                            ? remainder
                            : columnCount;

                        for (int j = 0; j < elementPerRow; j++)
                        {
                            CheckBox checkBox = new CheckBox();
                            checkBox.Foreground = Brushes.White;
                            checkBox.BorderBrush = Brushes.White;
                            checkBox.Content = App.Device[currentParameter].DeviceName;
                            stackPanel.Children.Add(checkBox);
                            currentParameter++;
                        }

                        parameterList.Items.Add(stackPanel);
                    }

                    PopupContent.Text = Properties.Resources.Popup_Message_SelectAdditionalTestResult;
                }
                else
                {
                    App.MainViewModel.Origin = "SelectParameters";
                    App.PopupHandler(e, sender);
                }
            }
            if (App.MainViewModel.Origin == "FailedUpdateLIS")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_FailedUpdateLISInfo;
            }
            if (App.MainViewModel.Origin == "IpPortUnavailable")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = Properties.Resources.Popup_Message_IpPortUnavailable;
            }
            if (App.MainViewModel.Origin == "GreywindConnected")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                //PopupContent.Text = Properties.Resources.Popup_Message_GreywindConnected;
                PopupContent.Text = Properties.Resources.Popup_Message_ConnectPIMSSuccess;
            }
            if (App.MainViewModel.Origin == "FailedGreywindConnect")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                //PopupContent.Text = Properties.Resources.Popup_Message_FailedGreywindConnect;
                PopupContent.Text = "Failed to connect with PIMS.";
            }
            if (App.MainViewModel.Origin == "GeneralSettingsUpdated")
            {
                PopupSetup(false, false, true, false, false, false, false, false);

                PopupContent.Text = "Settings updated.";
            }

            if (ShowPopup)
            {
                PopupBackground.Background = Brushes.DimGray;
                PopupBackground.Opacity = 0.5;
                popup.IsOpen = true;
            }
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            popup.IsOpen = false;

            PopupBackground.Background = null;

            if (App.MainViewModel.Origin == "UserDeleteRow") { DeleteUserRowHandler(e, sender); }
            if (App.MainViewModel.Origin == "UserAddRow") { AddUserRowHandler(e, sender); }
            if (App.MainViewModel.Origin == "UserUpdateRow") { UpdateUserRowHandler(e, sender); }
            //if (App.MainViewModel.Origin == "DeviceAdd") { AddDeviceHandler(e, sender); }
            if (App.MainViewModel.Origin == "DeviceDelete") { DeleteDeviceHandler(e, sender); }
            //if (App.MainViewModel.Origin == "DeviceUpdate") { UpdateDeviceHandler(e, sender); }
            if (App.MainViewModel.Origin == "ChangeLanguageCountry") { ChangeLanguageCountryHandler(e, sender); }
            //if (App.MainViewModel.Origin == "SettingsUpdate" || App.MainViewModel.Origin == "ReportSettingsUpdate" || App.MainViewModel.Origin == "LISSettingsUpdate") { UpdateSettingsHandlerAsync(e, sender); }
            //if (App.MainViewModel.Origin == "ClinicInfoUpdate") { UpdateClinicInfoHandlerAsync(e, sender); }
            if (App.MainViewModel.Origin == "CancelSchedule") {

                var uniqueIDPart = App.ScheduleTestInfo.ScheduleUniqueID.Split("-");
                var orderID = uniqueIDPart[1];
                var accessionNum = uniqueIDPart[2];
                bool success = true;
                string location = App.ScheduleTestInfo.LocationID;

                if (App.ScheduleTestInfo.CreatedBy == "Greywind")
                {
                    GetInterfaceInfo();
                    GreywindAPI sAPI = new GreywindAPI();
                    VCheckAPI VcheckAPI = new VCheckAPI();
                    url = await VcheckAPI.GetPMSUrl(2);
                    success = sAPI.UpdateScheduleStatus(orderID, accessionNum, "C", url);
                }

                if (success)
                {
                    VCheckAPI vCheckAPI = new VCheckAPI();
                    var updateStatusSuccess = await vCheckAPI.UpdateScheduleStatus(App.ScheduleTestInfo.LocationID, App.ScheduleTestInfo.PatientID, orderID, App.ScheduleTestInfo.CreatedBy, 2);
                    SchedulePage.ReloadScheduleHandler(e, sender);

                    App.MainViewModel.Origin = "ScheduleCancelled";
                    App.PopupHandler(e, sender);
                }
                else
                {
                    App.MainViewModel.Origin = "FailedToCancelSchedule";
                    App.PopupHandler(e, sender);
                }

            }
            if (App.MainViewModel.Origin == "Logout")
            {
                checklanguage();

                foreach (var window in System.Windows.Application.Current.Windows.OfType<LoginWindow>())
                {
                    window.Close();
                }

                LoginWindow login = new LoginWindow();
                login.LoginFrame.Content = new LoginPage();
                login.Show();

                foreach (var window in System.Windows.Application.Current.Windows.OfType<Main>())
                {
                    window.Close();
                }
            }
            if (App.MainViewModel.Origin == "ResetPassword") { ResetPasswordHandler(e, sender); }

            if (App.MainViewModel.Origin == "AskAdditionalTestResult")
            {
                App.MainViewModel.Origin = "SelectAdditionalTestResult";
                App.PopupHandler(e, sender);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainViewModel.Origin == "AskAdditionalTestResult")
            {
                popup.IsOpen = false;
                PopupBackground.Background = null;

                PopupSetup(true, true, false, false, false, false, false, false);

                App.MainViewModel.Origin = "SelectParameters";
                App.PopupHandler(e, sender);

                return;
            }
            // Enable main window
            this.IsEnabled = true;
            // Close the popup
            popup.IsOpen = false;

            PopupBackground.Background = null;

            PopupSetup(true, true, false, false, false, false, false, false);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainViewModel.Origin == "SettingsUpdateCompleted" || 
                App.MainViewModel.Origin == "ListingDownloadCompleted" ||
                App.MainViewModel.Origin == "TestResultDownloadCompleted" ||
                App.MainViewModel.Origin == "FailedDownloadListing" ||
                App.MainViewModel.Origin == "FailedToShowPrint" ||
                App.MainViewModel.Origin == "FailedAddDevice" ||
                App.MainViewModel.Origin == "FailedDeleteDevice" ||
                App.MainViewModel.Origin == "FailedUpdateDevice" ||
                App.MainViewModel.Origin == "SentToAnalyzer" ||
                App.MainViewModel.Origin == "FailedToSendToAnalyzer" ||
                App.MainViewModel.Origin == "CanceledSendToAnalyzer" ||
                App.MainViewModel.Origin == "NoDeviceFound" ||
                App.MainViewModel.Origin == "NoScheduleFound" ||
                App.MainViewModel.Origin == "PatientNameUpdated" ||
                App.MainViewModel.Origin == "DoctorNameUpdated" ||
                App.MainViewModel.Origin == "ReportSettingsUpdateCompleted" ||
                App.MainViewModel.Origin == "FailedToDownload" ||
                App.MainViewModel.Origin == "FailedToPrint" ||
                App.MainViewModel.Origin == "GeneralErrorOccur" ||
                App.MainViewModel.Origin == "FailedUpdateLIS" ||
                App.MainViewModel.Origin == "IpPortUnavailable" ||
                App.MainViewModel.Origin == "ClinicInfoUpdateCompleted" ||
                App.MainViewModel.Origin == "GreywindConnected" ||
                App.MainViewModel.Origin == "FailedGreywindConnect" ||
                App.MainViewModel.Origin == "ScheduleCancelled" ||
                App.MainViewModel.Origin == "CannotSendToAnalyzer" ||
                App.MainViewModel.Origin == "NoClinicFound" ||
                App.MainViewModel.Origin == "GeneralSettingsUpdated")
            {
                CancelButton_Click(null, null);
            }

            else if (App.MainViewModel.Origin == "GreywindSendUniqueID")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                var sSelectedItem = ((ComboBoxItem)deviceList.SelectedItem);
                if (!string.IsNullOrEmpty(sSelectedItem.Tag.ToString()))
                {
                    App.MainViewModel.ScheduleUniqueID = sSelectedItem.Tag.ToString();

                    SendToPMSHandler(e, sender);
                } 
            }

            else if (App.MainViewModel.Origin == "SendToAnalyzer")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                var sSelectedItem = ((ComboBoxItem)deviceList.SelectedItem);
                App.AnalyzerID = Convert.ToInt32(sSelectedItem.Tag);

                SendRequestToAnalyzer(e, sender);
            }

            else if (App.MainViewModel.Origin == "UpdatePatientName")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                App.TestResultInfo.PatientName = txtInput.Text;

                PopupSetup(true, true, false, false, false, false, false, false);

                UpdatePatientName(e, sender);
            }

            else if (App.MainViewModel.Origin == "UpdateDoctorName")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                App.TestResultInfo.InchargePerson = txtInput.Text;

                PopupSetup(true, true, false, false, false, false, false, false);

                UpdateDoctorName(e, sender);
            }

            else if (App.MainViewModel.Origin == "SelectParameters")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                PopupSetup(true, true, false, false, false, false, false, false);

                GetSelectedParameters(e, sender);
            }

            //edited by aqil
            else if (App.MainViewModel.Origin == "SelectAdditionalTestResult")
            {
                var stackPanels = parameterList.Items.OfType<StackPanel>();

                List<string> selectedAnalyzers = new();

                foreach (var panel in stackPanels)
                {
                    selectedAnalyzers.AddRange(
                        panel.Children
                             .OfType<CheckBox>()
                             .Where(x => x.IsChecked == true)
                             .Select(x => x.Content.ToString())
                    );
                }

                if (selectedAnalyzers.Count == 0)
                    return;

                var currentDevice = App.Device[0];

                App.Device.Clear();
                App.Device.Add(currentDevice);

                foreach (var analyzerName in selectedAnalyzers)
                {
                    var selectedTests =
                        TestResultsRepository.GetRelatedTestResultsByPatientIDAndAnalyzer(
                            ConfigSettings.GetConfigurationSettings(),
                            App.DowloadPrintObject[0].TestResult.PatientID,
                            analyzerName,
                            App.DowloadPrintObject[0].TestResult.ID
                        );

                    App.Device.AddRange(selectedTests);
                }

                App.MainViewModel.Origin = "SelectTestID";
                App.PopupHandler(e, sender);
            }

            //edited by aqil
            else if (App.MainViewModel.Origin == "SelectTestID")
            {
                this.IsEnabled = true;
                popup.IsOpen = false;

                PopupBackground.Background = null;

                PopupSetup(true, true, false, false, false, false, false, false);

                GetTestResult(e, sender);
            }
        }

        private void PreviousPage(object sender, EventArgs e)
        {
            var originButton = (System.Windows.Controls.Button)sender;

            if (originButton.Name.ToString() == "UserPage" || App.MainViewModel.Origin == "UserUpdateRow")
            {
                MainUserPage();
            }
            else if (originButton.Name.ToString() == "ViewResultBackButton")
            {
                MainResultPage();
            }
            else
            {
                frameContent.GoBack();
            }
        }

        private void MainUserPage()
        {
            frameContent.Content = new UserPage();
        }

        private void MainResultPage()
        {
            frameContent.Content = new ResultPage();
        }

        public void initializedDropdownSelectionList()
        {
            var titleList = sContext.GetMasterCodeData("Title");
            var genderList = sContext.GetMasterCodeData("Gender");
            var rolesList = rolesContext.GetRoles();
            var statusList = sContext.GetMasterCodeData("UserStatus");
            var sortDirectionList = sContext.GetMasterCodeData("SortDirection");
            var countryPhoneMumList = countryContext.GetCountryPhoneNumData();

            App.MainViewModel.cbTitle = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbGender = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbRoles = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbStatus = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbSort = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbCountryPhoneNum = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbCountryPhoneNumSearch = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbDateFormat = new ObservableCollection<ComboBoxItem>();
            App.MainViewModel.cbConnectionType = new ObservableCollection<ComboBoxItem>();


            var cbDefaultItem = new ComboBoxItem { Content = "" };
            App.MainViewModel.SelectedcbTitle = cbDefaultItem;
            App.MainViewModel.cbTitle.Add(cbDefaultItem);
            foreach (var item in titleList) { App.MainViewModel.cbTitle.Add(new ComboBoxItem { Content = item.CodeID }); }

            cbDefaultItem = new ComboBoxItem { Content = "" };
            App.MainViewModel.SelectedcbGender = cbDefaultItem;
            App.MainViewModel.cbGender.Add(cbDefaultItem);
            foreach (var item in genderList) { App.MainViewModel.cbGender.Add(new ComboBoxItem { Content = item.CodeName, Tag = item.CodeID }); }

            cbDefaultItem = new ComboBoxItem { Content = "" };
            App.MainViewModel.SelectedcbRoles = cbDefaultItem;
            App.MainViewModel.cbRoles.Add(cbDefaultItem);
            foreach (var item in rolesList) { App.MainViewModel.cbRoles.Add(new ComboBoxItem { Content = item.RoleName, Tag = item.RoleID }); }

            cbDefaultItem = new ComboBoxItem { Content = "" };
            App.MainViewModel.SelectedcbStatus = cbDefaultItem;
            App.MainViewModel.cbStatus.Add(cbDefaultItem);
            foreach (var item in statusList) { App.MainViewModel.cbStatus.Add(new ComboBoxItem { Content = item.CodeName, Tag = item.CodeID }); }

            cbDefaultItem = new ComboBoxItem { Content = Properties.Resources.ClinicInfo_Label_CountryCode, Tag = "" };
            App.MainViewModel.SelectedcbCountryPhoneNum = cbDefaultItem;
            App.MainViewModel.cbCountryPhoneNum.Add(cbDefaultItem);
            foreach (var item in countryPhoneMumList) 
            { 
                foreach(var childItem in item.PhoneNum.Split(", "))
                {
                    App.MainViewModel.cbCountryPhoneNum.Add(new ComboBoxItem { Content = item.CountryCode + " +" + childItem, Tag = childItem });
                }
            }

            cbDefaultItem = new ComboBoxItem { Content = Properties.Resources.ClinicInfo_Label_CountryCode, Tag = "" };
            App.MainViewModel.cbCountryPhoneNumSearch.Add(cbDefaultItem);
            foreach (var item in countryPhoneMumList)
            {
                foreach (var childItem in item.PhoneNum.Split(", "))
                {
                    App.MainViewModel.cbCountryPhoneNumSearch.Add(new ComboBoxItem { Content = item.CountryCode + " +" + childItem, Tag = childItem });
                }
            }

            foreach (var item in sortDirectionList)
            {
                App.MainViewModel.cbSort.Add(new ComboBoxItem
                { 
                    Content = item.CodeName,
                    Tag = item.CodeID
                });
            }

            App.MainViewModel.cbDateFormat.Add(new ComboBoxItem { Tag = "dd/MM/yyyy", Content = "DD/MM/YYYY" });
            App.MainViewModel.cbDateFormat.Add(new ComboBoxItem { Tag = "MM/dd/yyyy", Content = "MM/DD/YYYY" });
            App.MainViewModel.cbDateFormat.Add(new ComboBoxItem { Tag = "yyyy/dd/MM", Content = "YYYY/DD/MM" });
            App.MainViewModel.cbDateFormat.Add(new ComboBoxItem { Tag = "yyyy/MM/dd", Content = "YYYY/MM/DD" });

            App.MainViewModel.cbConnectionType.Add(new ComboBoxItem { Tag = "wifi", Content = "Wireless" });
            App.MainViewModel.cbConnectionType.Add(new ComboBoxItem { Tag = "ethernet", Content = "Wired" });
        }

        private async static void DeleteUserRowHandler(EventArgs e, object sender)
        {
            try
            {
                if (usersContext.DeleteUser(App.MainViewModel.Users.UserId))
                {
                    var user = await App.UserManager.FindByIdAsync(App.MainViewModel.Users.UserId);

                    await App.UserManager.DeleteAsync(user);

                    if (InitializedUserPage != null)
                    {
                        InitializedUserPage(sender, e);
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                App.log.Error("User Deletion Error >>> ", ex);
            }
        }


        private async void AddUserRowHandler(EventArgs e, object sender)
        {

            try
            {
                var user = Activator.CreateInstance<IdentityUser>();

                var emailStore = (IUserEmailStore<IdentityUser>)App.UserStore;

                await App.UserStore.SetUserNameAsync(user, App.MainViewModel.Users.LoginID, CancellationToken.None);
                await emailStore.SetEmailAsync(user, App.MainViewModel.Users.EmailAddress, CancellationToken.None);
                var result = await App.UserManager.CreateAsync(user, App.newPassword);

                ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();

                if (result.Succeeded)
                {
                    App.MainViewModel.Users.UserId = user.Id;
                    App.MainViewModel.Users.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    App.MainViewModel.Users.CreatedBy = App.MainViewModel.CurrentUsers.FullName;

                    if (usersContext.InsertUser(App.MainViewModel.Users))
                    {
                        var roleResult = await App.UserManager.AddToRoleAsync(user, App.MainViewModel.Users.Role);

                        if (roleResult.Succeeded)
                        {
                            var notificationTemplate = TemplateContext.GetTemplateByCodeLang("US05", (sLangCode != null ? sLangCode.ConfigurationValue : ""));
                            notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("###<staff_id>###", App.MainViewModel.Users.EmployeeID).Replace("###<staff_fullname>###", App.MainViewModel.Users.FullName).Replace("'", "''");

                            NotificationModel notification = new NotificationModel()
                            {
                                NotificationType = "Updates",
                                NotificationTitle = notificationTemplate.TemplateTitle,
                                NotificationContent = notificationTemplate.TemplateContent,
                                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                CreatedBy = App.MainViewModel.CurrentUsers.FullName
                            };

                            if (NotificationContext.InsertNotification(notification))
                            {

                            }
                            else
                            {

                            }

                            notificationTemplate = TemplateContext.GetTemplateByCodeLang("EN01", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                            notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''").Replace("###<staff_fullname>###", App.MainViewModel.Users.FullName).Replace("###<password>###", App.newPassword).Replace("###<login_id>###", user.UserName);

                            string sErrorMessage = "";

                            try
                            {
                                EmailObject sEmail = new EmailObject();

                                sEmail.SenderEmail = App.SMTP.Sender;

                                List<string> sRecipientList = [App.MainViewModel.Users.EmailAddress];


                                sEmail.RecipientEmail = sRecipientList;
                                sEmail.IsHtml = true;
                                sEmail.Subject = "[VCheck Viewer] " + notificationTemplate.TemplateTitle;
                                sEmail.Body = notificationTemplate.TemplateContent;
                                sEmail.SMTPHost = App.SMTP.Host;
                                sEmail.PortNo = App.SMTP.Port;
                                sEmail.HostUsername = App.SMTP.Username;
                                sEmail.HostPassword = App.SMTP.Password;
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
                                    CreatedBy = App.MainViewModel.CurrentUsers.FullName
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
                                App.log.Error("Email Error >>> ", ex);
                            }

                            if (InitializedUserPage != null)
                            {
                                InitializedUserPage(sender, e);
                            }
                            PreviousPage(sender, e);
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

        private async void UpdateUserRowHandler(EventArgs e, object sender)
        {
            TemplateModel notificationTemplate;
            App.MainViewModel.Users.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            App.MainViewModel.Users.UpdatedBy = App.MainViewModel.CurrentUsers.FullName;

            ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();

            try
            {
                if (usersContext.UpdateUser(App.MainViewModel.Users))
                {
                    if (App.MainViewModel.CurrentUsers.UserId == App.MainViewModel.Users.UserId)
                    {
                        App.MainViewModel.CurrentUsers.EmployeeID = App.MainViewModel.Users.EmployeeID;
                        App.MainViewModel.CurrentUsers.Title = App.MainViewModel.Users.Title;
                        App.MainViewModel.CurrentUsers.StaffName = App.MainViewModel.Users.Title + " " + App.MainViewModel.Users.FullName;
                        App.MainViewModel.CurrentUsers.FullName = App.MainViewModel.Users.FullName;
                        App.MainViewModel.CurrentUsers.RegistrationNo = App.MainViewModel.Users.RegistrationNo;
                        App.MainViewModel.CurrentUsers.Gender = App.MainViewModel.Users.Gender == "M" ? "Male" : "Female";
                        App.MainViewModel.CurrentUsers.DateOfBirth = App.MainViewModel.Users.DateOfBirth;
                        App.MainViewModel.CurrentUsers.EmailAddress = App.MainViewModel.Users.EmailAddress;
                        App.MainViewModel.CurrentUsers.Status = App.MainViewModel.Users.Status;
                        App.MainViewModel.CurrentUsers.Role = App.MainViewModel.Users.Role;
                        App.MainViewModel.CurrentUsers.EmailAddressChanged = App.MainViewModel.Users.EmailAddressChanged;
                        App.MainViewModel.CurrentUsers.RoleChanged = App.MainViewModel.Users.RoleChanged;
                    }

                    if (App.MainViewModel.Users.StatusChanged)
                    {
                        if (App.MainViewModel.Users.Status == "Active")
                        {
                            notificationTemplate = TemplateContext.GetTemplateByCodeLang("US03", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                        }
                        else
                        {
                            notificationTemplate = TemplateContext.GetTemplateByCodeLang("US04", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                        }

                        notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''").Replace("###<staff_id>###", App.MainViewModel.Users.EmployeeID).Replace("###<staff_fullname>###", App.MainViewModel.Users.FullName).Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID).Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName);
                    }
                    else if (App.MainViewModel.CurrentUsers.UserId == App.MainViewModel.Users.UserId)
                    {
                        App.MainViewModel.Users = App.MainViewModel.CurrentUsers;
                        Username.Header = App.MainViewModel.CurrentUsers.StaffName;

                        notificationTemplate = TemplateContext.GetTemplateByCodeLang("US02", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                        notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''");
                    }
                    else
                    {
                        notificationTemplate = TemplateContext.GetTemplateByCodeLang("US01", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                        notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''").Replace("###<staff_id>###", App.MainViewModel.Users.EmployeeID).Replace("###<staff_fullname>###", App.MainViewModel.Users.FullName).Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID).Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName);
                    }

                    if (App.MainViewModel.Users.EmailAddressChanged)
                    {
                        var user = await App.UserManager.FindByIdAsync(App.MainViewModel.Users.UserId);
                        await App.UserManager.SetEmailAsync(user, App.MainViewModel.Users.EmailAddress);
                    }

                    if (App.MainViewModel.Users.RoleChanged)
                    {
                        var user = await App.UserManager.FindByIdAsync(App.MainViewModel.Users.UserId);
                        var roleList = await App.UserManager.GetRolesAsync(user);
                        await App.UserManager.RemoveFromRolesAsync(user, roleList);
                        await App.UserManager.AddToRoleAsync(user, App.MainViewModel.Users.Role);
                    }

                    NotificationModel notification = new NotificationModel()
                    {
                        NotificationType = "Updates",
                        NotificationTitle = notificationTemplate.TemplateTitle,
                        NotificationContent = notificationTemplate.TemplateContent,
                        CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedBy = App.MainViewModel.CurrentUsers.FullName
                    };

                    if (NotificationContext.InsertNotification(notification))
                    {

                    }
                    else
                    {

                    }


                    PreviousPage(sender, e);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                App.log.Error("Update User Error >>> ", ex);
            }
            
        }

        private void AddDeviceHandler(EventArgs e, object sender)
        {
            if (DeviceRepository.InsertDevice(App.MainViewModel.DeviceModel, ConfigSettings.GetConfigurationSettings()))
            {
                //ConfigurationModel? sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                //String sNotificationContent = "";

                //var sTemplateObj = TemplateContext.GetTemplateByCodeLang("DS01", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                //if (sTemplateObj != null)
                //{
                //    sNotificationContent = sTemplateObj.TemplateContent;
                //}
                //sNotificationContent = sNotificationContent.Replace("###<analyzer_name>###", App.MainViewModel.DeviceModel.DeviceName)
                //                                           .Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName)
                //                                           .Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID);

                //NotificationModel sNotificationSend = new NotificationModel()
                //{
                //    NotificationType = "Updates",
                //    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                //    NotificationContent = sNotificationContent,
                //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    CreatedBy = App.MainViewModel.CurrentUsers.FullName
                //};
                //NotificationContext.InsertNotification(sNotificationSend);

                frameContent.Content = new DevicePage();
            }
            else
            {
                System.Windows.Controls.Primitives.Popup sErrorPopup = new System.Windows.Controls.Primitives.Popup();
                sErrorPopup.IsOpen = true;

                App.MainViewModel.Origin = "FailedAddDevice";
                App.PopupHandler(e, sender);
            }
        }

        private void DeleteDeviceHandler(EventArgs e, object sender)
        {
            if (DeviceRepository.UpdateDevice(App.MainViewModel.DeviceModel, ConfigSettings.GetConfigurationSettings()))
            {
                //ConfigurationModel? sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                //String sNotificationContent = "";

                //var sTemplateObj = TemplateContext.GetTemplateByCodeLang("DS03", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                //if (sTemplateObj != null)
                //{
                //    sNotificationContent = sTemplateObj.TemplateContent;
                //}
                //sNotificationContent = sNotificationContent.Replace("###<analyzer_name>###", App.MainViewModel.DeviceModel.DeviceName)
                //                                           .Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName)
                //                                           .Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID);

                //NotificationModel sNotificationSend = new NotificationModel()
                //{
                //    NotificationType = "Updates",
                //    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                //    NotificationContent = sNotificationContent,
                //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    CreatedBy = App.MainViewModel.CurrentUsers.FullName
                //};
                //NotificationContext.InsertNotification(sNotificationSend);

                frameContent.Content = new DevicePage();
            }
            else
            {
                System.Windows.Controls.Primitives.Popup sErrorPopup = new System.Windows.Controls.Primitives.Popup();
                sErrorPopup.IsOpen = true;

                App.MainViewModel.Origin = "FailedDeleteDevice";
                App.PopupHandler(e, sender);
            }
        }

        private void UpdateDeviceHandler(EventArgs e, object sender)
        {
            if (DeviceRepository.UpdateDevice(App.MainViewModel.DeviceModel, ConfigSettings.GetConfigurationSettings()))
            {
                //ConfigurationModel? sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                //String sNotificationContent = "";

                //var sTemplateObj = TemplateContext.GetTemplateByCodeLang("DS02", (sLangCode != null) ? sLangCode.ConfigurationValue : "");
                //if (sTemplateObj != null)
                //{
                //    sNotificationContent = sTemplateObj.TemplateContent;
                //}
                //sNotificationContent = sNotificationContent.Replace("###<analyzer_name>###", App.MainViewModel.OldDeviceName)
                //                                           .Replace("###<new_analyzer_name>###", App.MainViewModel.DeviceModel.DeviceName)
                //                                           .Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName)
                //                                           .Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID);

                //NotificationModel sNotificationSend = new NotificationModel()
                //{
                //    NotificationType = "Updates",
                //    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                //    NotificationContent = sNotificationContent,
                //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    CreatedBy = App.MainViewModel.CurrentUsers.FullName
                //};
                //NotificationContext.InsertNotification(sNotificationSend);

                frameContent.Content = new DevicePage();
            }
            else
            {
                System.Windows.Controls.Primitives.Popup sErrorPopup = new System.Windows.Controls.Primitives.Popup();
                sErrorPopup.IsOpen = true;

                App.MainViewModel.Origin = "FailedUpdateDevice";
                App.PopupHandler(e, sender);
            }
        }

        private async Task UpdateSettingsHandlerAsync(EventArgs e, object sender)
        {
            try
            {
                var sSettingsObj = App.MainViewModel.ConfigurationModel;
                foreach (var s in sSettingsObj)
                {
                    var sConfigObj = ConfigurationContext.GetConfigurationData(s.ConfigurationKey).FirstOrDefault();
                    if (sConfigObj != null)
                    {
                        ConfigurationContext.UpdateConfiguration(s.ConfigurationKey, s.ConfigurationValue);
                    }
                    else
                    {
                        ConfigurationContext.AddConfiguration(s.ConfigurationKey, s.ConfigurationValue);
                    }
                }

                // --- Add Notification --//
                //ConfigurationModel? sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();
                //String sNotificationContent = "";

                //var sTemplateObj = TemplateContext.GetTemplateByCodeLang("CS01", sLangCode.ConfigurationValue);
                //if (sTemplateObj != null)
                //{
                //    sNotificationContent = sTemplateObj.TemplateContent;
                //}
                //sNotificationContent = sNotificationContent.Replace("###<admin_fullname>###", App.MainViewModel.CurrentUsers.FullName)
                //                                           .Replace("###<admin_id>###", App.MainViewModel.CurrentUsers.EmployeeID);

                //NotificationModel sNotificationSend = new NotificationModel()
                //{
                //    NotificationType = "Updates",
                //    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                //    NotificationContent = sNotificationContent,
                //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                //    CreatedBy = App.MainViewModel.CurrentUsers.FullName
                //};
                //NotificationContext.InsertNotification(sNotificationSend);

                System.Windows.Controls.Primitives.Popup sMessagePopup = new System.Windows.Controls.Primitives.Popup();
                sMessagePopup.IsOpen = true;

                if(App.MainViewModel.Origin == "ReportSettingsUpdate")
                {
                    //App.MainViewModel.Origin = "ReportSettingsUpdateCompleted";
                }
                else if (App.MainViewModel.Origin == "LISSettingsUpdate")
                {
                    //App.MainViewModel.Origin = "SettingsUpdateCompleted";

                    ConfigurationPage.ConnectionStatusHandler(e, sender);
                }
                else
                {
                    //App.MainViewModel.Origin = "SettingsUpdateCompleted";
                }

                App.MainViewModel.ConfigurationModel = ConfigurationContext.GetConfigurationData("");

                //App.PopupHandler(e, sender);
            }
            catch (Exception ex)
            {
                App.log.Error("Update Setting Error >>> ", ex);
                return;
            }
        }

        private async Task UpdateClinicInfoHandlerAsync(EventArgs e, object sender)
        {
            try
            {
                var sSettingsObj = App.MainViewModel.ConfigurationModel;
                foreach (var s in sSettingsObj)
                {
                    var sConfigObj = ConfigurationContext.GetConfigurationData(s.ConfigurationKey).FirstOrDefault();
                    if (sConfigObj != null)
                    {
                        ConfigurationContext.UpdateConfiguration(s.ConfigurationKey, s.ConfigurationValue);
                    }
                    else
                    {
                        ConfigurationContext.AddConfiguration(s.ConfigurationKey, s.ConfigurationValue);
                    }
                }


                ConfigurationModel? ClinicID = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
                ConfigurationModel? PMS = ConfigurationContext.GetConfigurationData("InterfaceSettingsPMS").FirstOrDefault();

                if (ClinicID != null && !string.IsNullOrEmpty(ClinicID.ConfigurationValue))
                {
                    VCheckAPI vcheckAPI = new VCheckAPI();
                    GreywindAPI greywindAPI = new GreywindAPI();

                    var name = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicName").ConfigurationValue;
                    var address = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicAddress").ConfigurationValue;
                    var city = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicCity").ConfigurationValue;
                    var state = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicState").ConfigurationValue;
                    var phoneNum = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicPhoneNum").ConfigurationValue;
                    var clinicID = ClinicID.ConfigurationValue;

                    VCheck.Interface.API.Lib.General.LocationModel location = new VCheck.Interface.API.Lib.General.LocationModel()
                    {
                        ID = ClinicID.ConfigurationValue,
                        Name = name,
                        Address = address,
                        ContactName = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicContactName").ConfigurationValue,
                        PhoneNum = phoneNum,
                        Email = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicEmail").ConfigurationValue,
                        CreatedBy = "VCheck Viewer"
                    };

                    var locationID = await vcheckAPI.UpdateLocation(location);

                    if(PMS != null && PMS.ConfigurationValue == "Greywind")
                    {
                        var PMSURL = await vcheckAPI.GetPMSUrl(2);

                        VCheck.Interface.API.Greywind.RequestMessage.InsertLocationRequest insertLocation = new VCheck.Interface.API.Greywind.RequestMessage.InsertLocationRequest()
                        {
                            name = name,
                            address_1 = address,
                            city = city,
                            state = state,
                            phone = phoneNum,
                            api_access = "1"
                        };

                        await greywindAPI.InsertClinicInfo(insertLocation, PMSURL, clinicID);
                    }
                    
                }

                App.MainViewModel.Origin = "ClinicInfoUpdateCompleted";
                App.MainViewModel.ConfigurationModel = ConfigurationContext.GetConfigurationData("");

                App.PopupHandler(e, sender);
            }
            catch (Exception ex)
            {
                App.log.Error("Update Setting Error >>> ", ex);
                return;
            }
        }

        private async void SendRequestToAnalyzer(EventArgs e, object sender)
        {
            Services.HL7MessageSender.Main sendMessage = new Services.HL7MessageSender.Main();
            await sendMessage.SendMessage(App.ScheduleTestInfoExtended);

            App.PopupHandler(e, sender);
        }

        private void UpdatePatientName(EventArgs e, object sender)
        {
            //TestResultsRepository.UpdateTestResult(ConfigSettings.GetConfigurationSettings(), App.TestResultInfo);
            ResultPage.UpdatePatientNameHandler(e, sender);

            //edited by aqil
            if (App.isEmptyName)
            {
                App.DowloadPrintObject[0].TestResult.PatientName = App.TestResultInfo.PatientName;

                //App.MainViewModel.Origin = "SelectAdditionalTestResult";
                App.MainViewModel.Origin = "AskAdditionalTestResult";
                App.PopupHandler(e, sender);
            }
            else
            {
                App.MainViewModel.Origin = "PatientNameUpdated";
                App.PopupHandler(e, sender);
            }

        }

        private void UpdateDoctorName(EventArgs e, object sender)
        {
            //TestResultsRepository.UpdateTestResult(ConfigSettings.GetConfigurationSettings(), App.TestResultInfo);
            ResultPage.UpdateDoctorNameHandler(e, sender);

            App.MainViewModel.Origin = "DoctorNameUpdated";
            App.PopupHandler(e, sender);

        }

        private void GetTestResult(EventArgs e, object sender)
        {
            var stackPanels = parameterList.Items.OfType<StackPanel>();
            List<CheckBox> selectedTest = new List<CheckBox>();

            foreach (var stackpanel in stackPanels)
            {
                selectedTest.AddRange(stackpanel.Children.OfType<CheckBox>().Where(x => x.IsChecked == true));
            }

            foreach(var test in selectedTest)
            {
                //edited by aqil
                //var TestID = App.Device.FirstOrDefault(x => x.DeviceName == test.Content).TestID;
                long TestID = Convert.ToInt64(test.Tag);
                var TestResult = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), TestID);

                DownloadPrintResultModel downloadPrintModelTemp = new DownloadPrintResultModel();
                TestResultModel previousTest = new TestResultModel();
                downloadPrintModelTemp.TestResult = TestResult;
                downloadPrintModelTemp.TestResultDetails = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), TestID);
                downloadPrintModelTemp.PreviousTestResultDetails = TestResultsRepository.GetPreviousTestRecord(ConfigSettings.GetConfigurationSettings(), TestResult, out previousTest);
                downloadPrintModelTemp.PreviousTestResult = previousTest;
                downloadPrintModelTemp.TestResultsGraph = TestResultsRepository.GetResultGraphsByTestResultID(ConfigSettings.GetConfigurationSettings(), TestID);

                App.DowloadPrintObject.Add(downloadPrintModelTemp);
                App.Parameters.AddRange(downloadPrintModelTemp.TestResultDetails.Select(x => x.TestParameter).Where(y => !App.Parameters.Contains(y)));
            }

            var parameterOrder = TestResultsRepository.GetAllParameters(ConfigSettings.GetConfigurationSettings()).Select(x => x.Parameter).ToList();
            App.Parameters = App.Parameters.OrderBy(d => parameterOrder.IndexOf(d)).ToList();

            App.MainViewModel.Origin = "SelectParameters";
            App.PopupHandler(e, sender);
        }

        private void GetSelectedParameters(EventArgs e, object sender)
        {
            var stackPanels = parameterList.Items.OfType<StackPanel>();
            List<CheckBox> selectedParameters = new List<CheckBox>();

            foreach (var stackpanel in stackPanels)
            {
                var borders = stackpanel.Children.OfType<System.Windows.Controls.Border>().ToList();

                foreach (var border in borders)
                {
                    CheckBox checkBox = (border.Child as CheckBox);
                    if(checkBox.IsChecked == true && checkBox.Name.ToString() != "SelectAll")
                    {
                        selectedParameters.Add(checkBox);
                    }
                }
            }

            App.Parameters = selectedParameters.Select(x => x.Content.ToString()).ToList();

            ResultPage.DownloadPrintReportHandler(e, sender);
        }

        private static void ResetPasswordHandler(EventArgs e, object sender)
        {
            if (ResetCurrentPassword != null)
            {
                ResetCurrentPassword(sender, e);
            }
        }

        private void ChangeLanguageCountryHandler(EventArgs e, object sender)
        {
            try
            {
                System.Globalization.CultureInfo sZHCulture = new(App.MainViewModel.ConfigurationModel.FirstOrDefault(x => x.ConfigurationKey == "SystemSettings_Language").ConfigurationValueTemp);

                CultureResources.ChangeCulture(sZHCulture);

                bool updateCountrySuccess = ConfigurationContext.UpdateConfiguration("SystemSettings_Country", App.MainViewModel.ConfigurationModel.FirstOrDefault(x => x.ConfigurationKey == "SystemSettings_Country").ConfigurationValueTemp);
                bool updateLanguageSuccess = ConfigurationContext.UpdateConfiguration("SystemSettings_Language", App.MainViewModel.ConfigurationModel.FirstOrDefault(x => x.ConfigurationKey == "SystemSettings_Language").ConfigurationValueTemp);

                if (updateCountrySuccess && updateLanguageSuccess)
                {
                    var currentCountry = App.MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "SystemSettings_Country").FirstOrDefault();
                    var currentLanguage = App.MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "SystemSettings_Language").FirstOrDefault();

                    currentCountry.ConfigurationValue = currentCountry.ConfigurationValueTemp;
                    currentLanguage.ConfigurationValue = currentLanguage.ConfigurationValueTemp;

                    //var notificationTemplate = TemplateContext.GetTemplateByCodeLang("LC01", sZHCulture.Name);
                    //notificationTemplate.TemplateContent = notificationTemplate.TemplateContent.Replace("'", "''");

                    //NotificationModel notification = new NotificationModel()
                    //{
                    //    NotificationType = "Updates",
                    //    NotificationTitle = notificationTemplate.TemplateTitle,
                    //    NotificationContent = notificationTemplate.TemplateContent,
                    //    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    //    CreatedBy = App.MainViewModel.CurrentUsers.FullName
                    //};

                    //if (NotificationContext.InsertNotification(notification))
                    //{

                    //}
                    //else
                    //{

                    //}
                }
            }
            catch(Exception ex)
            {
                App.log.Error("Change language Error >>> ", ex);
            }
        }

        // ------------- Temporary ------------------- //
        private async void SendToPMSHandler(EventArgs e, object sender)
        {
            VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest sRequestAPI = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest();
            long iTestResultID = 0;
            String sUniqueID = App.MainViewModel.ScheduleUniqueID;
            var test = App.ClinicID;

            if (!String.IsNullOrEmpty(App.MainViewModel.TestResultID))
            {
                iTestResultID = Convert.ToInt64(App.MainViewModel.TestResultID);

                //var sTestResultObj = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), iTestResultID);
                var sTestResultNotes = TestResultsRepository.GetTestResultNotesByID(ConfigSettings.GetConfigurationSettings(), iTestResultID);
                if (sTestResultObj != null)
                {
                    string notes = "";

                    if(sTestResultNotes != null && sTestResultNotes.Count() > 0)
                    {
                        foreach (var note in sTestResultNotes)
                        {
                            notes = string.IsNullOrEmpty(notes) ? notes + "-" + note.Comment : notes + "\n-" + note.Comment;
                        }
                    }

                    List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject> sResultListing = new List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject>();
                    List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject> sPanelListing = new List<VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject>();

                    String sOrderID = "";
                    var sDetailsObj = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), iTestResultID);
                    //var parameters = sDetailsObj.Select(x => x.TestParameter).ToList();
                    VCheckAPI VcheckAPI = new VCheckAPI();
                    //var scheduleString = await VcheckAPI.GetSchedule(App.ClinicID, sTestResultObj.PatientID, parameters);
                    var scheduleString = await VcheckAPI.GetSchedule(App.ClinicID, null, null, sUniqueID);
                    var sScheduledTestObj = JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);

                    if (sScheduledTestObj != null && sScheduledTestObj.ID != 0)
                    {
                        string[] ownerName = sScheduledTestObj.OwnerName.Split(" ");
                        var lastName = "";
                        int start = 0;

                        foreach (var name in ownerName)
                        {
                            if(ownerName.Length == 1)
                            {
                                lastName = name;
                            }
                            else if(start != 0)
                            {
                                lastName = string.IsNullOrEmpty(lastName) ? lastName + ownerName[start] : lastName + " " + ownerName[start];
                            }

                            start++;
                        }

                        if (sScheduledTestObj.ScheduleUniqueID.Contains("-"))
                        {
                            var UniqueIDSplit = sScheduledTestObj.ScheduleUniqueID.Split("-");
                            if (UniqueIDSplit.Length > 0)
                            {
                                sOrderID = UniqueIDSplit[1];
                                sRequestAPI.accessionnumber = UniqueIDSplit[2];
                            }
                        }

                        sRequestAPI.clinic_id = sScheduledTestObj.LocationID.ToString();
                        sRequestAPI.reportdate = sTestResultObj.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        sRequestAPI.providerid = "";

                        VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPatientObject sPatientObj = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPatientObject();
                        sPatientObj.patientid = sScheduledTestObj.PatientID;
                        sPatientObj.firstname = (sScheduledTestObj != null) ? sScheduledTestObj.PatientName : "";
                        sPatientObj.lastname = lastName;
                        sPatientObj.gender = (sScheduledTestObj != null) ? sScheduledTestObj.Gender : "";
                        sPatientObj.birthday = sScheduledTestObj.PatientDOB != null ? sScheduledTestObj.PatientDOB.Value.ToString("yyyy-MM-dd") : "";
                        sPatientObj.species = (sScheduledTestObj != null) ? sScheduledTestObj.Species : "";
                        sPatientObj.breed = "";

                        sRequestAPI.patient = sPatientObj;

                        VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject sPanelObj = new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelsObject();

                        var panel = string.IsNullOrEmpty(sTestResultObj.TestResultType) ? "VCheck" : sTestResultObj.TestResultType;
                        var testResponseString = await VcheckAPI.GetTestByNameOrCode(panel, null);
                        var sResultTestCode = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testid;

                        sPanelObj.code = sResultTestCode;
                        sPanelObj.name = panel;
                        sPanelObj.status = "F";
                        sPanelObj.source = "";
                        sPanelObj.resultdate = sTestResultObj.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");


                        if (sDetailsObj != null && sDetailsObj.Count > 0)
                        {
                            int count = 0;

                            foreach (var d in sDetailsObj)
                            {
                                String[] sRange = Array.Empty<string>();
                                string referenceLow = "";
                                string referenceHigh = "";

                                if (!string.IsNullOrEmpty(d.ReferenceRange))
                                {
                                    String sReferenceRange = d.ReferenceRange.Replace("[", "").Replace("]", "");
                                    if (sReferenceRange != "")
                                    {
                                        if (sReferenceRange.Contains(","))
                                        {
                                            var temp = sReferenceRange.Split(",");
                                            string[] result = new string[2];
                                            Array.Copy(temp, 0, result, 0, 1);
                                            Array.Copy(temp, 2, result, 1, 1);
                                            sRange = result;
                                        }
                                        else if (sReferenceRange.Contains(";"))
                                        {
                                            sRange = sReferenceRange.Split(";");
                                        }
                                        else if (sReferenceRange.Contains("-"))
                                        {
                                            sRange = sReferenceRange.Split("-");
                                        }
                                        else if (sReferenceRange.Contains("<"))
                                        {
                                            sRange = new string[] { sReferenceRange, "" };
                                        }
                                        else if (sReferenceRange.Contains(">"))
                                        {
                                            sRange = new string[] { "", sReferenceRange };
                                        }

                                        if (sRange.Length > 1)
                                        {
                                            referenceLow = (sRange.Length > 0) ? sRange[0] : "";
                                            referenceHigh = (sRange.Length > 0) ? sRange[1] : "";
                                        }
                                    }
                                }
                                else if (!string.IsNullOrEmpty(d.MeasuringRange))
                                {
                                    String sReferenceRange = d.MeasuringRange.Replace("[", "").Replace("]", "");
                                    if (sReferenceRange != "")
                                    {
                                        if (sReferenceRange.Contains(";"))
                                        {
                                            sRange = sReferenceRange.Split(";");
                                        }
                                        else if (sReferenceRange.Contains("-"))
                                        {
                                            sRange = sReferenceRange.Split("-");
                                        }

                                        referenceLow = (sRange.Length > 0) ? sRange[0] : "";
                                        referenceHigh = (sRange.Length > 0) ? sRange[1] : "";
                                    }
                                }

                                string flags = null;

                                if (d.TestResultStatus == "Low") { flags = "L"; }
                                else if (d.TestResultStatus == "High") { flags = "H"; }

                                var Parameter = CheckParameter(d.TestParameter);

                                sResultListing.Add(new VCheck.Interface.API.Greywind.RequestMessage.UpdateResultPanelTestObject
                                {
                                    name = Parameter,
                                    code = sResultTestCode + "-" + count++,
                                    result = d.TestResultValue,
                                    referencelow = referenceLow,
                                    referencehigh = referenceHigh,
                                    unitofmeasure = d.TestResultUnit,
                                    nature = d.TestResultStatus == "Normal" | d.TestResultStatus == "Positive" ? "N" : "A",
                                    flags = flags,
                                    status = "F",
                                    notes = d.Interpretation
                                });

                            }

                            sPanelObj.tests = sResultListing;
                            sPanelObj.notes = notes;
                        }
                        sPanelListing.Add(sPanelObj);

                        sRequestAPI.panels = sPanelListing;

                        //var temp = JsonConvert.SerializeObject(sRequestAPI);

                        GetInterfaceInfo();

                        var sRespAPI = false;

                        if (PMS == "Greywind")
                        {
                            GreywindAPI sAPI = new GreywindAPI();
                            url = await VcheckAPI.GetPMSUrl(2);
                            sRespAPI = sAPI.UpdateResult(sRequestAPI, sOrderID, url);
                        }
                        else
                        {
                            if (isIP)
                            {
                                Services.HL7MessageSender.Main senddata = new Services.HL7MessageSender.Main();
                                sRespAPI = await senddata.SendData(sRequestAPI, iIP, int.Parse(port));
                            }
                            else
                            {
                                VCheck.Interface.API.GeneralAPI sAPI = new VCheck.Interface.API.GeneralAPI();
                                sRespAPI = await sAPI.SendData(url + ":" + port, sRequestAPI, username, password);
                            }
                        }

                        var orderID = sScheduledTestObj.ScheduleUniqueID.Split("-")[1];
                        await VcheckAPI.UpdateScheduleStatus(sScheduledTestObj.LocationID, sScheduledTestObj.PatientID, orderID, sScheduledTestObj.CreatedBy, sRespAPI ? 4 : 3, sTestResultObj.TestResultType);

                        if (sRespAPI)
                        {
                            System.Windows.Forms.MessageBox.Show(Properties.Resources.Schedule_Label_SentToPIMS);

                            sTestResultObj.PMSFunction = "Collapsed";

                            if(sScheduledTestObj.PatientID != sTestResultObj.PatientID)
                            {
                                sTestResultObj.PatientID = sScheduledTestObj.PatientID;
                            }

                            if(sScheduledTestObj.PatientName != sTestResultObj.PatientName)
                            {
                                sTestResultObj.PatientName = sScheduledTestObj.PatientName;
                            }

                            TestResultsRepository.UpdateTestResult(ConfigSettings.GetConfigurationSettings(), sTestResultObj);

                            if (App.isSchedulePage)
                            {
                                SchedulePage.ReloadScheduleHandler(e, sender);
                            }
                            else
                            {
                                ResultPage.UpdatePatientNameHandler(e, sender);
                            }

                            App.isSchedulePage = false;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Failed to send result to PIMS.");
                        }
                    }
                    else
                    {
                        //System.Windows.Forms.MessageBox.Show(Properties.Resources.Popup_Message_APINoLinkFound);
                        System.Windows.Forms.MessageBox.Show("Failed to link the result with this schedule.");
                    }

                    
                }
            }
        }

        private void mnDashboard_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();

            frameContent.Content = new DashboardPage();

            //System.Windows.Data.Binding b = new System.Windows.Data.Binding("Dashboard_Title_PageTitle");
            //b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            //PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            PageTitle.Text = "Device";

            refreshMenuItemStyle(mnDashboard);

            CurrentPage = "";
        }

        private void mnConnection_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();

            frameContent.Content = new ConnectionPage();

            PageTitle.Text = "Connection";

            refreshMenuItemStyle(mnConnection);

            CurrentPage = "";
        }

        private void mnSchedule_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();

            frameContent.Content = new SchedulePage();

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Schedule_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            refreshMenuItemStyle(mnSchedule);

            CurrentPage = "";
        }

        private void mnResults_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();
            App.AnalyzerID = 0;

            frameContent.Content = new ResultPage();

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Results_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            refreshMenuItemStyle(mnResults);

            CurrentPage = "";
        }

        private void mnNotifications_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();

            frameContent.Content = new NotificationPage();

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Notification_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            PageTitle.Text = Properties.Resources.Main_Label_SideMenuNotification;

            refreshMenuItemStyle(mnNotifications);

            String? sColor = System.Windows.Application.Current.Resources["Themes_MenuHighligted"].ToString();

            thumbNotification.Background = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

            CurrentPage = "Notification";
        }

        private void thumbNotification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            checklanguage();

            frameContent.Content = new NotificationPage();

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Notification_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            PageTitle.Text = Properties.Resources.Main_Label_SideMenuNotification;

            refreshMenuItemStyle(mnNotifications);

            String? sColor = System.Windows.Application.Current.Resources["Themes_MenuHighligted"].ToString();

            thumbNotification.Background = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

            CurrentPage = "Notification";
        }

        private void thumbNotification_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentPage))
            {
                if(thumbNotification.Background == Brushes.Transparent)
                {
                    String? sColor = System.Windows.Application.Current.Resources["Themes_MenuHighligted"].ToString();

                    thumbNotification.Background = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                }
                else
                {
                    thumbNotification.Background = Brushes.Transparent;
                }
            }
        }


        private void mnSettings_Click(object sender, RoutedEventArgs e)
        {
            checklanguage();

            if (App.MainViewModel.CurrentUsers.Role == "User") { frameContent.Content = new LanguageCountryPage(); }
            else { frameContent.Content = new UserPage();}
            

            System.Windows.Data.Binding b = new System.Windows.Data.Binding("Setting_Title_PageTitle");
            b.Source = System.Windows.Application.Current.TryFindResource("Resources");
            PageTitle.SetBinding(System.Windows.Controls.TextBlock.TextProperty, b);

            refreshMenuItemStyle(mnSettings);

            CurrentPage = "";
        }

        private void btnCollapse_Click(object sender, RoutedEventArgs e)
        {
            menuslide("hidemenu", panel1);
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            menuslide("showmenu", panel1);
        }

        private void menuslide(String p, StackPanel leftMenu)
        {
            Storyboard? sb = Resources[p] as Storyboard;
            sb.Begin(leftMenu);

            if (p.Contains("show"))
            {
                btnCollapse.Visibility = Visibility.Visible;
                btnOpen.Visibility = Visibility.Hidden;

                thumbDashboard.Visibility = Visibility.Hidden;
                thumbConnection.Visibility = Visibility.Hidden;
                thumbSchedule.Visibility = Visibility.Hidden;
                thumbResult.Visibility = Visibility.Hidden;
                thumbNotification.Visibility = Visibility.Hidden;
                thumbSetting.Visibility = Visibility.Hidden;

                mnNotifications.Visibility = Visibility.Visible;
            }
            else
            {
                if (p.Contains("hide"))
                {
                    btnCollapse.Visibility = Visibility.Hidden;
                    btnOpen.Visibility = Visibility.Visible;

                    thumbDashboard.Visibility = Visibility.Visible;
                    thumbConnection.Visibility = Visibility.Visible;
                    thumbSchedule.Visibility = Visibility.Visible;
                    thumbResult.Visibility = Visibility.Visible;
                    thumbNotification.Visibility = Visibility.Visible;
                    thumbSetting.Visibility = Visibility.Visible;

                    mnNotifications.Visibility = Visibility.Hidden;
                }
            }
        }

        private void ClearMenuItemStyle()
        {
            mnSettings.Background = Brushes.Transparent;
            mnDashboard.Background = Brushes.Transparent;
            mnSchedule.Background = Brushes.Transparent;
            mnResults.Background = Brushes.Transparent;
            mnNotifications.Background = Brushes.Transparent;
            thumbNotification.Background = Brushes.Transparent;
            mnConnection.Background = Brushes.Transparent;
        }

        private void btnDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            var sButton = (System.Windows.Controls.Button)sender;
            ConfigurationContext.UpdateConfiguration("SystemSettings_Themes", (sButton != null) ? sButton.Tag.ToString() : "");

            CheckThemesSettings();
            refreshContent();
        }

        private void btnLightTheme_Click(object sender, RoutedEventArgs e)
        {
            var sButton = (System.Windows.Controls.Button)sender;
            ConfigurationContext.UpdateConfiguration("SystemSettings_Themes", (sButton != null) ? sButton.Tag.ToString() : "");
            
            CheckThemesSettings();
            refreshContent();
        }

        private void CheckThemesSettings()
        {
            String? sTheme = "Light";

            var sConfigData = ConfigurationContext.GetConfigurationData("SystemSettings_Themes");
            if (sConfigData != null && sConfigData.Count > 0)
            {
                sTheme = sConfigData[0].ConfigurationValue;
            }

            String sThemeName = sTheme + ".xaml";
            AppTheme.ChangeTheme(new Uri("Themes/" + sThemeName, UriKind.Relative));
            if (sTheme.ToLower() == "light")
            {
                btnDarkTheme.Background = Brushes.Transparent;

                System.Windows.Media.Color sBlue = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e76fb");
                btnLightTheme.Background = new SolidColorBrush(sBlue);
            }
            else if (sTheme.ToLower() == "dark")
            {
                btnLightTheme.Background = Brushes.Transparent;

                System.Windows.Media.Color sBlue = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e76fb");
                btnDarkTheme.Background = new SolidColorBrush(sBlue);
            }
        }

        private void refreshContent()
        {
            System.Windows.Controls.MenuItem sMenu = new System.Windows.Controls.MenuItem();

            var sCurrentContent = frameContent.Content;
            if (sCurrentContent.ToString().Contains("Dashboard")){ 
                frameContent.Content = new DashboardPage();
                sMenu = mnDashboard;
            }
            if (sCurrentContent.ToString().Contains("LanguageCountryPage")){ 
                frameContent.Content = new LanguageCountryPage();
                sMenu = mnSettings;
            }
            if (sCurrentContent.ToString().Contains("DevicePage")){ 
                frameContent.Content = new DevicePage();
                sMenu = mnSettings;
            }
            if (sCurrentContent.ToString().Contains("SchedulePage"))
            {
                sMenu = mnSchedule;
            }
            if (sCurrentContent.ToString().Contains("ResultPage"))
            {
                sMenu = mnResults;
            }
            if (sCurrentContent.ToString().Contains("User"))
            {
                sMenu = mnSettings;
            }
            if (sCurrentContent.ToString().Contains("Configuration"))
            {
                sMenu = mnSettings;
            }
            if (sCurrentContent.ToString().Contains("ViewResultPage"))
            {
                ViewResultPage viewResultPage = new ViewResultPage();
                viewResultPage.LoadPage();
                frameContent.Content = viewResultPage;
            }

            if (sCurrentContent.ToString().Contains("NotificationPage"))
            {
                sMenu = mnNotifications;

                var sSearchModel = App.MainViewModel.SearchModel;

                if (sSearchModel != null)
                {
                    var Notification = new NotificationPage();
                    Notification.reloadData(sSearchModel.SearchStart, sSearchModel.SearchEnd, 
                                            (sSearchModel.SearchType == "All" ? null : sSearchModel.SearchType), 
                                            sSearchModel.SearchStartDate, sSearchModel.SearchEndDate, sSearchModel.SearchKeyword, 
                                            sSearchModel.SearchReset);

                    // Pre-selected search criteria //
                    Notification.KeywordSearchBar.Text = sSearchModel.SearchKeyword;

                    foreach(var t in Notification.NotificationType.Items)
                    {
                        var comboItem = t as ComboBoxItem;

                        if (comboItem.Content.ToString() == sSearchModel.SearchType)
                        {
                            comboItem.IsSelected = true;
                        }
                        else
                        {
                            comboItem.IsSelected = false;
                        }
                    }

                    if (sSearchModel.SearchStartDate != null && sSearchModel.SearchEndDate != null)
                    {
                        //Notification.RangeDate.SelectedDates = GenerateSelectedDateRange(sSearchModel.SearchStartDate, sSearchModel.SearchEndDate);
                        Notification.RangeDateStart.SelectedDates = GenerateSelectedDateRange(sSearchModel.SearchStartDate, sSearchModel.SearchStartDate);
                        Notification.RangeDateEnd.SelectedDates = GenerateSelectedDateRange(sSearchModel.SearchEndDate, sSearchModel.SearchEndDate);

                        String sStart = Notification.RangeDateStart.SelectedDates.FirstOrDefault().ToString();
                        String sEnd = Notification.RangeDateEnd.SelectedDates.FirstOrDefault().ToString();
                        DateTime dtStart = DateTime.ParseExact(sStart, "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                        DateTime dtEnd = DateTime.ParseExact(sEnd, "M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);
                        Notification.RangeDateStart.DateRangePicker_TextBox.Text = dtStart.ToString("dd/MM/yyyy");
                        Notification.RangeDateEnd.DateRangePicker_TextBox.Text = dtEnd.ToString("dd/MM/yyyy");
                    }


                    frameContent.Content = Notification;
                }
                else
                {
                    frameContent.Content = new NotificationPage();
                }

            }

            refreshMenuItemStyle(sMenu);
        }

        private void refreshMenuItemStyle(System.Windows.Controls.MenuItem sItem)
        {
            ClearMenuItemStyle();

            String? sColor = System.Windows.Application.Current.Resources["Themes_MenuHighligted"].ToString();

            sItem.Background = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
            sItem.BorderBrush = new BrushConverter().ConvertFrom("#404D5B") as SolidColorBrush;
        }

        private ObservableCollection<DateTime> GenerateSelectedDateRange(String sStart, String sEnd)
        {
            ObservableCollection<DateTime> sDateList = new ObservableCollection<DateTime>();

            if (sStart == null && sEnd == null)
            {
                return null;
            }
            else if (sStart == sEnd)
            {
                DateTime sDate = DateTime.ParseExact(sStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                sDateList.Add(sDate);
            }
            else
            {
                DateTime sSDate = DateTime.ParseExact(sStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime sEDate = DateTime.ParseExact(sEnd, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                
                sDateList.Add(sSDate);

                while (sSDate < sEDate.AddDays(-1))
                {
                    sSDate = sSDate.AddDays(1);

                    sDateList.Add(sSDate);
                }
            }

            return sDateList;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            App.WindowHeight = e.NewSize.Height;
            var windowHeight = e.NewSize.Height;

            GridElement.Height = windowHeight * 0.04;
            BorderElement.Height = (windowHeight * 0.04) + 5;
            CollapseGrid.Height = windowHeight * 0.04;
            DashboardGrid.Height = windowHeight * 0.08;
            ScheduleGrid.Height = windowHeight * 0.08;
            ResultGrid.Height = windowHeight * 0.08;
            NotificationGrid.Height = windowHeight * 0.08;
            SettingsGrid.Height = windowHeight * 0.08;
            BackgroundElement.Height = windowHeight;
        }

        private void checklanguage()
        {
            if (App.isLanguagePage)
            {
                System.Globalization.CultureInfo sZHCulture = new (App.MainViewModel.ConfigurationModel.FirstOrDefault(x => x.ConfigurationKey == "SystemSettings_Language").ConfigurationValue);

                CultureResources.ChangeCulture(sZHCulture);

                App.isLanguagePage = false;

                NotificationTranslate.Text = Properties.Resources.Main_Label_SideMenuNotification;
            }

        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;

            // Your logic here
            if (checkbox.IsChecked == true)
            {
                var stackPanels = parameterList.Items.OfType<StackPanel>();

                foreach (var stackpanel in stackPanels)
                {
                    var borders = stackpanel.Children.OfType<System.Windows.Controls.Border>();

                    foreach (var border in borders)
                    {
                        var parameter = (border.Child as CheckBox);

                        parameter.IsChecked = true;
                    }
                }
            }
        }

        private void GetInterfaceInfo()
        {
            var configuration = ConfigurationContext.GetConfigurationData("InterfaceSettingsPMS").FirstOrDefault();

            PMS = configuration != null ? configuration.ConfigurationValue : "";

            configuration = ConfigurationContext.GetConfigurationData("InterfaceSettingsIP").FirstOrDefault();
            url = configuration != null ? configuration.ConfigurationValue : "";
            isIP = System.Net.IPAddress.TryParse(url, out iIP);
            configuration = ConfigurationContext.GetConfigurationData("InterfaceSettingsPortNo").FirstOrDefault();
            port = configuration != null ? configuration.ConfigurationValue : "80";

            configuration = ConfigurationContext.GetConfigurationData("InterfaceSettingsUsername").FirstOrDefault();
            username = configuration != null ? configuration.ConfigurationValue : "";
            configuration = ConfigurationContext.GetConfigurationData("InterfaceSettingsPassword").FirstOrDefault();
            password = configuration != null ? configuration.ConfigurationValue : "";
        }

        private void PopupSetup(bool btnYesShow, bool btnNoShow, bool btnOkShow, bool btnCancelShow, bool btnLookOlderOrder, bool txtInputShow, bool deviceListShow, bool ParameterViewShow, bool CautionTextShow = false, bool moveButton = false)
        {
            btnYes.Visibility = btnYesShow ? Visibility.Visible : Visibility.Collapsed;
            btnNo.Visibility = btnNoShow ? Visibility.Visible : Visibility.Collapsed;
            btnOk.Visibility = btnOkShow ? Visibility.Visible : Visibility.Collapsed;
            btnCancel.Visibility = btnCancelShow ? Visibility.Visible : Visibility.Collapsed;
            btnOlderSchedule.Visibility = btnLookOlderOrder ? Visibility.Visible : Visibility.Collapsed;
            txtInput.Visibility = txtInputShow ? Visibility.Visible : Visibility.Collapsed;
            deviceList.Visibility = deviceListShow ? Visibility.Visible : Visibility.Collapsed;
            ParameterView.Visibility = ParameterViewShow ? Visibility.Visible : Visibility.Collapsed;
            CautionText.Visibility = CautionTextShow ? Visibility.Visible : Visibility.Collapsed;

            if (moveButton)
            {
                PopupButtonBorder1.Visibility = Visibility.Collapsed;
                PopupButtonBorder2.Visibility = Visibility.Visible;
            }
            else
            {
                PopupButtonBorder1.Visibility = Visibility.Visible;
                PopupButtonBorder2.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private async void btnOlderSchedule_Click(object sender, RoutedEventArgs e)
        {
            var sConfigObj = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
            sTestResultObj = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), Convert.ToInt64(App.MainViewModel.TestResultID));
            var listString = "";

            if (sConfigObj != null)
            {
                VCheckAPI vcheckAPI = new VCheckAPI();
                listString = await vcheckAPI.GetScheduleList(sConfigObj.ConfigurationValue, true, false, sTestResultObj.TestResultType, true);
                App.ClinicID = sConfigObj.ConfigurationValue;
            }

            List<ScheduledTestModel> sScheduledList = string.IsNullOrEmpty(listString) ? new List<ScheduledTestModel>() : JsonConvert.DeserializeObject<List<ScheduledTestModel>>(listString);

            if (sScheduledList != null && sScheduledList.Count > 0)
            {
                PopupSetup(false, false, true, false, false, false, false, false, true, true);

                deviceComboList.Clear();
                deviceComboList.Add(new ComboBoxItem
                {
                    Content = "--- Please select a schedule ---",
                    Tag = ""
                });
                foreach (var s in sScheduledList)
                {
                    var uniqueID = s.ScheduleUniqueID.Split("-")[3];

                    DateFormatConverter dateFormatConverter = new DateFormatConverter();
                    var scheduleDatetime = dateFormatConverter.ConvertSimpleDate(s.ScheduledDateTime.Value.ToLocalTime()).ToString();

                    deviceComboList.Add(new ComboBoxItem
                    {
                        Content = Properties.Resources.Schedule_Label_PatientID + " " + s.PatientID + ", " + "Name: " + s.PatientName + " (" + scheduleDatetime + ")",
                        Tag = uniqueID
                    });
                }


                deviceList.ItemsSource = deviceComboList;
                deviceList.SelectedIndex = 0;

                PopupContent.Text = Properties.Resources.Popup_Message_SelectSchedule;
                deviceList.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Visible;
                App.MainViewModel.Origin = "GreywindSendUniqueID";
            }
            else
            {
                App.MainViewModel.Origin = "NoScheduleFound";
                PopupContent.Text = Properties.Resources.Popup_Message_NoScheduleFound;
                PopupSetup(false, false, true, false, false, false, false, false);
            }
        }



        public static string CheckParameter(string input)
        {
            foreach (var item in calItems)
            {
                if (input.ToLower().Contains(item.ToLower())) { return item + "*"; }
            }

            return ConvertSubscriptsToNormal(input);
        }

        public static string ConvertSubscriptsToNormal(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                int subindex = subscriptDigits.IndexOf(c);
                int superindex = superscriptDigits.IndexOf(c);
                if (subindex >= 0 || superindex >= 0)
                {
                    result.Append(subindex >= 0 ? normalDigits[subindex] : normalDigits[superindex]); // Replace with normal digit
                    break;
                }
                else
                    result.Append(c); // Keep unchanged
            }

            return result.ToString();
        }

        private void mnDashboard_Click(object sender, MouseButtonEventArgs e)
        {

        }
    }

}
