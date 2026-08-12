using Microsoft.Win32;
using Newtonsoft.Json;
using System.CodeDom;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VCheck.Interface.API;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;
using VCheckViewer_Others.Views.Pages.Login;

namespace VCheckViewer_Others.Views.Pages.Schedule
{
    /// <summary>
    /// Interaction logic for SchedulePage.xaml
    /// </summary>
    public partial class SchedulePage : Page
    {
        public static event EventHandler? ReloadSchedule;

        private DispatcherTimer _timer;

        ConfigurationDBContext ConfigurationContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());
        string themeColor = App.isLightTheme ? "black" : "white";

        public SchedulePage()
        {
            InitializeComponent();

            this.SizeChanged += MainWindow_SizeChanged;

            LoadScheduledTestList();
            LoadTestResultList();
            GetSummaryStats();

            ReloadSchedule = null;
            ReloadSchedule += ReloadTestSchedule;

            // Initialize the timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(20) // Set interval to 20 seconds
            };
            _timer.Tick += ReloadTestSchedule; // Attach the Tick event
            _timer.Start(); // Start the timer

        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            middleScrollViewer.Height = e.NewSize.Height * 0.83;
            rightScrollViewer.Height = e.NewSize.Height * 0.83;
        }

        public async Task LoadScheduledTestList()
        {
            var sConfigObj = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
            var listString = "";

            if (sConfigObj != null)
            {
                VCheckAPI vcheckAPI = new VCheckAPI();
                listString = await vcheckAPI.GetScheduleList(sConfigObj.ConfigurationValue, false, false);
            }

            List<ScheduledTestModel> sScheduledList = string.IsNullOrEmpty(listString) ? new List<ScheduledTestModel>() : JsonConvert.DeserializeObject<List<ScheduledTestModel>>(listString);

            if (sScheduledList != null && sScheduledList.Count > 0)
            {
                List<ScheduledTestModelExtended> sUpdateScheduledList = new List<ScheduledTestModelExtended>();
                foreach(var t in sScheduledList)
                {
                    var AnalyzerStatus = "";
                    var SentFunction = "";
                    var CancelFunction = "";

                    if (!string.IsNullOrEmpty(t.SentToAnalyzer))
                    {
                        AnalyzerStatus = t.SentToAnalyzer != "N/A" ? "Sent to " + t.SentToAnalyzer + "" : "No analyzer can receive this schedule.";
                    }
                    else
                    {
                        AnalyzerStatus = "Not yet sent to analyzer";
                    }

                    SentFunction = t.ScheduleTestStatus > 0 ? "Collapsed" : "Visible";
                    CancelFunction = t.ScheduleTestStatus < 2 ? "Visible" : "Collapsed";
                    var testArray = t.ScheduledTestType.Split(",");
                    var TestList = " ";

                    var status = "";

                    if (t.ScheduleTestStatus == 1 && t.SentToAnalyzer != "N/A" && !string.IsNullOrEmpty(t.SentToAnalyzer))
                    {
                        status = Properties.Resources.Schedule_Label_SentToAnalyzer;
                    }
                    else if (t.ScheduleTestStatus == 2)
                    {
                        status = Properties.Resources.Schedule_Label_Cancelled;
                    }
                    else if (t.ScheduleTestStatus == 3)
                    {
                        status = Properties.Resources.Schedule_Label_ResultSentToVV;
                    }
                    else if (t.ScheduleTestStatus == 4)
                    {
                        status = Properties.Resources.Schedule_Label_SentToPIMS;
                    }
                    else
                    {
                        status = Properties.Resources.Schedule_Label_Pending;
                    }


                    for (int i = 0; i < testArray.Length; i++)
                    {
                        TestList = TestList + testArray[i];

                        if (i != testArray.Length - 1) { TestList = TestList + "\n"; }
                    }

                    if(themeColor == "#FF006FC4") { }

                    sUpdateScheduledList.Add(new ScheduledTestModelExtended
                    {
                        ID = t.ID,
                        ScheduledTestType = t.ScheduledTestType,
                        ScheduledDateTime = t.ScheduledDateTime.Value.ToLocalTime(),
                        ScheduledBy = t.ScheduledBy,
                        ScheduleUniqueID = string.IsNullOrEmpty(t.ScheduleUniqueID) ? "" : string.Concat(t.ScheduleUniqueID.TakeLast(8)),
                        PatientName = t.PatientName,
                        PatientNameString = Properties.Resources.Results_Label_PatientName + ": ",
                        PatientID = t.PatientID,
                        PatientIDString = Properties.Resources.Schedule_Label_PatientID,
                        UniqueIDString = Properties.Resources.Schedule_Label_UniqueID,
                        InchargePerson = t.InchargePerson,
                        ScheduleTestStatus = t.ScheduleTestStatus,
                        AnalyzerName = AnalyzerStatus,
                        CreatedDate = t.CreatedDate.Value.ToLocalTime(),
                        CreatedBy = t.CreatedBy,
                        UpdatedDate = t.UpdatedDate != null ? t.UpdatedDate.Value.ToLocalTime() : null,
                        UpdatedBy = t.UpdatedBy, 
                        SentFunction = SentFunction, 
                        CancelFunction = CancelFunction,
                        TestListStringFirst = Properties.Resources.Schedule_Label_Tests + " : ", 
                        TestListStringSecond = "Hover to view",
                        TestList = TestList,
                        ThemeColor = themeColor,
                        Status = t.ScheduleTestStatus.ToString(),
                        StatusTranslate = status
                    });
                }

                icScheduledTest.ItemsSource = sUpdateScheduledList.ToList();

                borderScheduledTest.Visibility = Visibility.Visible;
                borderNoScheduledTest.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderScheduledTest.Visibility = Visibility.Collapsed;
                borderNoScheduledTest.Visibility = Visibility.Visible;
            }
        }

        public async void LoadTestResultList()
        {
            LoginPage loginPage = new LoginPage();
            await loginPage.CheckPMSUser();
            List<TestResultModel> sTestResultList = TestResultsRepository.GetLatestTestResultList(ConfigSettings.GetConfigurationSettings());
            if (sTestResultList != null && sTestResultList.Count > 0)
            {
                List<TestResultModelExtended> sUpdateTestResultList = new List<TestResultModelExtended>();
                foreach (var t in sTestResultList)
                {
                    var status = "";
                    var foreground = "";
                    if (t.OverallStatus == null || t.OverallStatus.ToLower() == "abnormal")
                    {
                        status = Properties.Resources.Schedule_Label_Abnormal;
                        foreground = "#ff2c29";
                    }
                    else if (t.OverallStatus.ToLower() == "normal")
                    {
                        status = Properties.Resources.Schedule_Label_Normal;
                        foreground = "#57baa5";
                    }
                    else if (t.OverallStatus.ToLower() == "positive")
                    {
                        status = Properties.Resources.Dashboard_Label_Positive;
                        foreground = "#57baa5";
                    }
                    else if (t.OverallStatus.ToLower() == "negative")
                    {
                        status = Properties.Resources.Dashboard_Label_Negative;
                        foreground = "#ff2c29";
                    }

                    sUpdateTestResultList.Add(new TestResultModelExtended
                    {
                        ID = t.ID,
                        TestResultDateTime = t.TestResultDateTime,
                        TestResultType = string.IsNullOrEmpty(t.TestResultType) ? Properties.Resources.Schedule_Label_General : t.TestResultType,
                        OperatorID = t.OperatorID,
                        PatientID = t.PatientID,
                        PatientIDString = Properties.Resources.Schedule_Label_PatientID,
                        InchargePerson = string.IsNullOrEmpty(t.InchargePerson) ? "N/A" : t.InchargePerson,
                        TestResultStatus = status,
                        StatusForeground = foreground,
                        CreatedDate = t.CreatedDate,
                        CreatedBy = t.CreatedBy,
                        UpdatedDate = t.UpdatedDate,
                        UpdatedBy = t.UpdatedBy,
                        ThemeColor = themeColor,
                        PMSFunction = App.PMSFunction
                    });
                }
                icTestResult.ItemsSource = sUpdateTestResultList.ToList();

                

                borderTestsCompleted.Visibility = Visibility.Visible;
                borderNoTestsCompleted.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderTestsCompleted.Visibility = Visibility.Collapsed;
                borderNoTestsCompleted.Visibility = Visibility.Visible;
            }
        }

        public void GetSummaryStats()
        {
            string totalPatient = "0";
            var sTotalTestCompleted = TestResultsRepository.GetTotalRecentTestDone(ConfigSettings.GetConfigurationSettings(), out totalPatient);

            lbTotalTest.Text = sTotalTestCompleted;
            lbTotalPatients.Text = totalPatient;
        }

        //private async void SendToAnalyzer_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    TextBlock schedule = sender as TextBlock;
        //    App.ScheduleTestInfo = ScheduledTestRepository.GetScheduledTestByID(ConfigSettings.GetConfigurationSettings(), long.Parse(schedule.Tag.ToString()));

        //    App.MainViewModel.Origin = "SendToAnalyzer";
        //    App.PopupHandler(null, null);      
        //}

        private async void CancelSchedule_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock schedule = sender as TextBlock;
            App.ScheduleTestInfo = ScheduledTestRepository.GetScheduledTestByID(ConfigSettings.GetConfigurationSettings(), long.Parse(schedule.Tag.ToString()));

            App.ScheduleTestInfo.ScheduleTestStatus = 2;
            App.ScheduleTestInfo.UpdatedBy = App.MainViewModel.CurrentUsers.FullName;
            App.ScheduleTestInfo.UpdatedDate = DateTime.Now;

            App.MainViewModel.Origin = "CancelSchedule";
            App.PopupHandler(e, sender);

        }

        public void ReloadTestSchedule(object sender, EventArgs e)
        {
            LoadScheduledTestList();
            LoadTestResultList();
        }

        private async void ViewReport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock report = sender as TextBlock;
            App.TestResultID = long.Parse(report.Tag.ToString());

            App.GoToViewResultPageHandler(e, sender);
        }

        private async void menuSendAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            MenuItem schedule = sender as MenuItem;
            var ClinicIDObject = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
            var ClinicID = ClinicIDObject != null ? ClinicIDObject.ConfigurationValue : "";
            VCheckAPI vCheckAPI = new VCheckAPI();
            //var scheduleString = await vCheckAPI.GetSchedule(ClinicID, null, null, schedule.Tag.ToString());
            //App.ScheduleTestInfo = JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);
            var scheduleString = await vCheckAPI.GetScheduleListNotSent(ClinicID, schedule.Tag.ToString());
            List<VCheck.Lib.Data.Models.ScheduledTestModelExtended> scheduleList = JsonConvert.DeserializeObject<List<VCheck.Lib.Data.Models.ScheduledTestModelExtended>>(scheduleString);
            App.ScheduleTestInfoExtended = scheduleList.FirstOrDefault();

            App.MainViewModel.Origin = "SendToAnalyzer";
            App.PopupHandler(null, null);
        }

        private async void menuCancelSchedule_Click(object sender, RoutedEventArgs e)
        {
            MenuItem schedule = sender as MenuItem;
            var ClinicIDObject = ConfigurationContext.GetConfigurationData("ClinicID").FirstOrDefault();
            var ClinicID = ClinicIDObject != null ? ClinicIDObject.ConfigurationValue : "";
            VCheckAPI vCheckAPI = new VCheckAPI();
            var scheduleString = await vCheckAPI.GetSchedule(ClinicID, null, null, schedule.Tag.ToString());
            App.ScheduleTestInfo = JsonConvert.DeserializeObject<ScheduledTestModel>(scheduleString);

            App.MainViewModel.Origin = "CancelSchedule";
            App.PopupHandler(e, sender);
        }

        private void menuViewReport_Click(object sender, RoutedEventArgs e)
        {
            MenuItem report = sender as MenuItem;
            App.TestResultID = long.Parse(report.Tag.ToString());

            App.GoToViewResultPageHandler(e, sender);
        }

        private void menuSendReport_Click(object sender, RoutedEventArgs e)
        {
            var sMenu = sender as MenuItem;

            App.isSchedulePage = true;
            App.MainViewModel.Origin = "GreywindSendUniqueID";
            App.MainViewModel.TestResultID = sMenu.Tag.ToString();
            App.PopupHandler(e, sender);
        }

        public static void ReloadScheduleHandler(EventArgs e, object sender)
        {
            if (ReloadSchedule != null)
            {
                ReloadSchedule(sender, e);
            }
        }

        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as Grid;
            var border = grid.Children.OfType<Border>().ToList().FirstOrDefault();
            var status = int.Parse(((TextBlock)((Border)((Grid)((Border)border.Child).Child).Children.OfType<Border>().ToList()[6].Child).Child).Tag.ToString());

            if (grid.ContextMenu != null)
            {
                grid.ContextMenu.PlacementTarget = grid;
                grid.ContextMenu.IsOpen = status < 2;
            }
        }
    }

    public class ScheduledTestModelExtended : VCheck.Lib.Data.Models.ScheduledTestModel
    {
        public String? PatientNameString { get; set; }
        public String? PatientIDString { get; set; }
        public String? UniqueIDString { get; set; }
        public String? TestListStringFirst { get; set; }
        public String? TestListStringSecond { get; set; }
        public String? TestList { get; set; }
        public String? SentFunction { get; set; }
        public String? CancelFunction { get; set; }
        public String? AnalyzerName { get; set; }
        public String? ThemeColor { get; set; }
        public String? Status { get; set; }
        public String? StatusTranslate { get; set; }
    }

    public class TestResultModelExtended : VCheck.Lib.Data.Models.TestResultExtendedModel
    {
        public String? PatientNameString { get; set; }
        public String? PatientIDString { get; set; }
        public String? StatusForeground { get; set; }
        public String? ThemeColor { get; set; }
        public String? PMSFunction { get; set; }
    }
}
