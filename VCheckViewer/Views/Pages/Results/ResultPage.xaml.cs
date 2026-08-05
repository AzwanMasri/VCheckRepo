using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using log4net;
using QuestPDF.Fluent;
using Spire.Xls;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VCheck.Lib.Data;
using VCheck.Lib.Data.Models;
using VCheckViewer.Converter;
using VCheckViewer.Lib.DocumentTemplate;
using VCheckViewer.Lib.Function;
using VCheckViewer.UserControls;
using VCheckViewer.Views.Pages.Login;
using Wpf.Ui.Controls;

namespace VCheckViewer.Views.Pages.Results
{
    /// <summary>
    /// Interaction logic for ResultPage.xaml
    /// </summary>
    public partial class ResultPage : Page
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public ObservableCollection<ComboBoxItem> cbSort { get; set; }
        public ComboBoxItem SelectedcbSort { get; set;  }

        public int currentPage = 1;
        public int paginationSize = 20;
        public int startPagination = 1;
        public int endPagination = 0;
        public int iTotalCount = 0;
        public string oldName;
        public TestResultListingExtendedObj currentRow;

        public static event EventHandler? DownloadPrintReport;
        public static event EventHandler? UpdatePatientNameEvent;
        public static event EventHandler? UpdateDoctorNameEvent;

        public ResultPage()
        {
            InitializeComponent();
            DataContext = this;

            cbSort = translateSort(App.MainViewModel.cbSort.ToList());
            SelectedcbSort = cbSort.FirstOrDefault();

            pagination.ButtonNextControlClick += new EventHandler(PaginationNextButton_Click);
            pagination.ButtonPrevControlClick += new EventHandler(PaginationPrevButton_Click);
            pagination.ButtonPageControlClick += new EventHandler(PaginationNumButton_Click);

            LoadResultDataGrid();

            UpdatePatientNameEvent = null;
            DownloadPrintReport = null;

            UpdatePatientNameEvent += new EventHandler(UpdatePatientName);
            UpdateDoctorNameEvent += new EventHandler(UpdatePatientName);
            DownloadPrintReport += new EventHandler(LoadTestResultData);

            RangeDateStart.DateRangePicker_Calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            RangeDateEnd.DateRangePicker_Calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            RangeDateStart.DateRangePicker_TextBlock.Text = "Start Date";
            RangeDateEnd.DateRangePicker_TextBlock.Text = "End Date";

            RangeDateStart.DateRangePicker_Calendar.SelectedDatesChanged += RangeDate_SelectedDateChanged;
            RangeDateEnd.DateRangePicker_Calendar.SelectedDatesChanged += RangeDate_SelectedDateChanged;
            RangeDateStart.DateRangePicker_Calendar.LostFocus += CloseCalender;
            RangeDateEnd.DateRangePicker_Calendar.LostFocus += CloseCalender;
        }


        private ObservableCollection<ComboBoxItem> translateSort(List<ComboBoxItem> cbSort)
        {
            ObservableCollection<ComboBoxItem> translatedSort = new ObservableCollection<ComboBoxItem>();

            foreach (var sort in cbSort)
            {
                if(sort.Content.ToString() == "Latest") { translatedSort.Add(new ComboBoxItem { Content = Properties.Resources.Results_Label_Latest, Tag = sort.Tag }); }
                else if (sort.Content.ToString() == "Earliest") { translatedSort.Add(new ComboBoxItem { Content = Properties.Resources.Results_Label_Earliest, Tag = sort.Tag }); }
                //else if (sort.Content.ToString() == "Result (Ascending)") { translatedSort.Add(new ComboBoxItem { Content = Properties.Resources.Results_Label_Ascending, Tag = sort.Tag }); }
                //else if (sort.Content.ToString() == "Result (Descending)") { translatedSort.Add(new ComboBoxItem { Content = Properties.Resources.Results_Label_Descending, Tag = sort.Tag }); }
            }

            return translatedSort;
        }

        private void menuView_Click(object sender, RoutedEventArgs e)
        {
            //TestResultListingExtendedObj sTestResultObj = dgResult.SelectedItem as TestResultListingExtendedObj;
            App.TestResultID = currentRow.ID;

            App.GoToViewResultPageHandler(e, sender);
        }

        private void menuDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadPrint(false);
        }

        private void menuPrint_Click(object sender, RoutedEventArgs e)
        {
            DownloadPrint(true);
        }

        private void DownloadPrint(bool IsPrint)
        {
            var parameterOrder = TestResultsRepository.GetAllParameters(ConfigSettings.GetConfigurationSettings()).Select(x => x.Parameter).ToList();
            App.IsPrint = IsPrint;
            //var selectedResult = dgResult.SelectedItem as TestResultListingExtendedObj;
            //selectedResult = selectedResult != null ? selectedResult : currentRow;
            App.TestResultID = currentRow.ID;
            var TestResult = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), currentRow.ID);
            App.Parameters = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), currentRow.ID).Select(x => x.TestParameter).ToList();

            if (!App.Parameters.Contains("IC"))
            {
                App.Parameters = App.Parameters.OrderBy(d => parameterOrder.IndexOf(d)).ToList();
            }

            App.isEmptyName = false;

            if (currentRow.PatientName == "-")
            {
                App.isEmptyName = true;
            }

            DownloadPrintResultModel downloadPrintResultModel = new DownloadPrintResultModel();
            App.DowloadPrintObject = new List<DownloadPrintResultModel>();

            TestResultModel previousTest = new TestResultModel();
            downloadPrintResultModel.TestResult = TestResult;
            downloadPrintResultModel.TestResultDetails = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), TestResult.ID);
            downloadPrintResultModel.PreviousTestResultDetails = TestResultsRepository.GetPreviousTestRecord(ConfigSettings.GetConfigurationSettings(), TestResult, out previousTest);
            downloadPrintResultModel.PreviousTestResult = previousTest;
            downloadPrintResultModel.TestResultsGraph = TestResultsRepository.GetResultGraphsByTestResultID(ConfigSettings.GetConfigurationSettings(), TestResult.ID);

            App.DowloadPrintObject.Add(downloadPrintResultModel);
            var DeviceName = DeviceRepository.GetDeviceNameBySerialNo(ConfigSettings.GetConfigurationSettings(), TestResult.DeviceSerialNo);
            DeviceName = DeviceName == "General" ? Properties.Resources.Schedule_Label_General : DeviceName;

            App.Device = new List<TestDeviceName>();
            App.Device.Add(new TestDeviceName() { DeviceName = DeviceRepository.GetDeviceNameBySerialNo(ConfigSettings.GetConfigurationSettings(), TestResult.DeviceSerialNo), TestID = TestResult.ID });

            if (App.isEmptyName)
            {
                updateName(TestResult.ID.ToString(), true, null, null);
            }
            else
            {
                //App.MainViewModel.Origin = "SelectAdditionalTestResult";
                App.MainViewModel.Origin = "AskAdditionalTestResult";
                App.PopupHandler(null, null);
            }

        }

        public void LoadTestResultData(object sender, EventArgs e)
        {
            try
            {
                var parameterOrder = TestResultsRepository.GetAllParameters(ConfigSettings.GetConfigurationSettings()).Select(x => x.Parameter).ToList();

                foreach(var test in App.DowloadPrintObject)
                {
                    test.TestResultDetails = test.TestResultDetails.Where(x => App.Parameters.Contains(x.TestParameter)).ToList();


                    if (!App.Parameters.Contains("IC"))
                    {
                        test.TestResultDetails = test.TestResultDetails.OrderBy(d => parameterOrder.IndexOf(d.TestParameter)).ToList();
                    }

                    test.PreviousTestResultDetails = test.PreviousTestResultDetails != null ? test.PreviousTestResultDetails.Where(x => App.Parameters.Contains(x.TestParameter)).OrderBy(d => App.Parameters.IndexOf(d.TestParameter)).ToList() : null;
                }

                TestResultTemplate sTestResultTemplate = new TestResultTemplate(App.DowloadPrintObject, App.IsPrint);

                if (App.IsPrint)
                {
                    sTestResultTemplate.GeneratePdfAndShow();
                }
                else
                {
                    sTestResultTemplate.GeneratePdf();
                }
            }
            catch (QuestPDF.Drawing.Exceptions.DocumentDrawingException drawEx)
            {
                Debug.WriteLine(drawEx.ToString());
            }
            catch (Exception ex)
            {
                log.Error("Error >>> " + ex.ToString());
                App.ErrorOccur = true;
            }

            if (!App.IsPrint)
            {
                Popup sConfirmPopup = new Popup();
                sConfirmPopup.IsOpen = true;

                if (App.ErrorOccur)
                {
                    App.MainViewModel.Origin = "FailedToDownload";
                }
                else
                {
                    App.MainViewModel.Origin = "TestResultDownloadCompleted";
                }

                App.PopupHandler(null, null);
            }
            else
            {
                if (App.ErrorOccur)
                {
                    App.MainViewModel.Origin = "FailedToPrint";
                    App.PopupHandler(null, null);
                }
            }
        }

        public void UpdatePatientName(object sender, EventArgs e)
        {
            if(App.TestResultInfo != null) { TestResultsRepository.UpdateTestResult(ConfigSettings.GetConfigurationSettings(), App.TestResultInfo); }

            LoadResultDataGrid();
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            startPagination = 1;

            LoadResultDataGrid();
        }

        public async Task LoadResultDataGrid()
        {
            dgResult.ItemsSource = await GetTestResultBySearch(paginationSize);

            if(App.PMSFunction == "Collapsed") { menuTransfer.Visibility = Visibility.Collapsed; }
            else { menuTransfer.Visibility = Visibility.Visible; }

            App.AnalyzerID = 0;

            if (iTotalCount <= 0)
            {
                btnPrint.Visibility = Visibility.Collapsed;
                btnDownload.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnPrint.Visibility = Visibility.Visible;
                btnDownload.Visibility = Visibility.Visible;
            }

            txtTotalCount.Text = iTotalCount.ToString();

            pagination.iTotalRecords = iTotalCount;
            pagination.iPageIndex = startPagination;
            pagination.iPageSize = paginationSize;
            pagination.LoadPagingNumber();
        }

        public async Task<List<TestResultListingExtendedObj>> GetTestResultBySearch(int pageSize)
        {
            String sSortDirection = "DESC";
            if (cboSort.SelectedItem != null)
            {
                var sSortObjItem = (ComboBoxItem)cboSort.SelectedItem;
                sSortDirection = sSortObjItem.Tag.ToString();
            }

            String sStart = "";
            String sEnd = "";

            if (RangeDateStart.SelectedDates.Count() == 1)
            {
                sStart = RangeDateStart.SelectedDates.FirstOrDefault().ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (RangeDateEnd.SelectedDates.Count() == 1)
            {
                sEnd = RangeDateEnd.SelectedDates.FirstOrDefault().AddDays(1).AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (!string.IsNullOrEmpty(sStart))
            {
                sEnd = RangeDateStart.SelectedDates.FirstOrDefault().AddDays(1).AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }

            String sKeyword = KeywordSearchBar.Text;

            LoginPage loginPage = new LoginPage();
            await loginPage.CheckPMSUser();

            return VCheck.Lib.Data.TestResultsRepository.GetTestResultListBySearch(
                                            ConfigSettings.GetConfigurationSettings(),
                                            sStart, sEnd, sKeyword, sSortDirection,
                                            startPagination, pageSize, App.PMSFunction, out iTotalCount, App.AnalyzerID);
        }

        protected void PaginationNextButton_Click(object sender, EventArgs e)
        {
            startPagination++;

            LoadResultDataGrid();
        }

        protected void PaginationPrevButton_Click(object sender, EventArgs e)
        {
            startPagination--;

            LoadResultDataGrid();
        }

        protected void PaginationNumButton_Click(object sender, EventArgs e)
        {
            System.Windows.Controls.Button btnNum = sender as System.Windows.Controls.Button;
            int iNumberSelected = Convert.ToInt32(btnNum.Tag);

            startPagination = iNumberSelected;

            LoadResultDataGrid();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            //CloseCalenderPopup(null, null);
            DownloadSearchResult();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            //CloseCalenderPopup(null, null);
            DownloadSearchResult(true);
        }

        private async void DownloadSearchResult(Boolean isPrint = false)
        {
            var sBuilder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            String sDownloadPath = sBuilder.Configuration.GetSection("Configuration:DownloadFolderPath").Value;
            String sProcessingPath = System.IO.Path.Combine(sDownloadPath, "Processing");

            if (!System.IO.Path.Exists(sProcessingPath))
            {
                System.IO.Directory.CreateDirectory(sProcessingPath);
            }

            int iPageSize = 0;

            try
            {
                String sFileName = "TestResultsListing_Download_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                String sExcelOutputPath = System.IO.Path.Combine(sProcessingPath, sFileName + ".xlsx"); 
                String sPDFOutputPath = System.IO.Path.Combine(sDownloadPath, sFileName + ".pdf");

                int.TryParse(txtTotalCount.Text, out iPageSize);

                List<TestResultListingExtendedObj> sResult = new List<TestResultListingExtendedObj>();
                sResult = await GetTestResultBySearch(iPageSize);

                DataTable sDatatable = Lib.General.Utils.ToDataTable(sResult);
                sDatatable.Columns.Remove("TestResultValue");
                sDatatable.Columns.Remove("TestResultDateTime");
                sDatatable.Columns.Remove("statusBackground");
                sDatatable.Columns.Remove("statusFontColor");
                sDatatable.Columns.Remove("printedBy");
                sDatatable.Columns.Remove("printedOn");
                sDatatable.Columns.Remove("isPrint");
                sDatatable.Columns.Remove("ID");
                sDatatable.Columns.Remove("ObservationStatus");
                sDatatable.Columns.Remove("TestResultValueString");
                sDatatable.Columns.Remove("TestResultRules");
                sDatatable.Columns.Remove("CreatedDate");
                sDatatable.Columns.Remove("CreatedBy");
                sDatatable.Columns.Remove("UpdatedDate");
                sDatatable.Columns.Remove("UpdatedBy");


                sDatatable.Columns["RowNo"].SetOrdinal(0);
                sDatatable.Columns["RowNo"].ColumnName = "No.";
                sDatatable.Columns["TestResultDateTimeString"].SetOrdinal(1);
                sDatatable.Columns["TestResultDateTimeString"].ColumnName = "Observation DateTime";
                sDatatable.Columns["OperatorID"].SetOrdinal(2);
                sDatatable.Columns["OperatorID"].ColumnName = "Operator ID";
                sDatatable.Columns["PatientID"].SetOrdinal(3);
                sDatatable.Columns["PatientID"].ColumnName = "Patient ID";
                sDatatable.Columns["InchargePerson"].SetOrdinal(4);
                sDatatable.Columns["InchargePerson"].ColumnName = "Doctor";
                sDatatable.Columns["TestResultType"].SetOrdinal(5);
                sDatatable.Columns["TestResultType"].ColumnName = "Observation Type";
                sDatatable.Columns["TestResultStatus"].SetOrdinal(6);
                sDatatable.Columns["TestResultStatus"].ColumnName = "Overall Status";
                sDatatable.Columns["DeviceSerialNo"].SetOrdinal(7);
                sDatatable.Columns["DeviceSerialNo"].ColumnName = "Device Serial No";
                
                               
                // ----- Generate Excel file ------- //
                XLWorkbook workbook = new XLWorkbook();
                workbook.Worksheets.Add(sDatatable, "Test Results")
                                   .Columns().AdjustToContents();
                workbook.SaveAs(sExcelOutputPath);

                // --- Output to PDF ----- //
                Spire.Xls.Workbook spireWorkbook = new Spire.Xls.Workbook();
                spireWorkbook.LoadFromFile(sExcelOutputPath);
                spireWorkbook.Worksheets[0].PageSetup.Orientation = PageOrientationType.Landscape;
                spireWorkbook.Worksheets[0].PageSetup.IsFitToPage = true;

                //spireWorkbook.SaveToFile(sPDFOutputPath, Spire.Xls.FileFormat.PDF);
                //if (System.IO.File.Exists(sPDFOutputPath))
                //{
                //    System.IO.File.Delete(sExcelOutputPath);
                //}

                if (isPrint)
                {
                    // --- Prompt Print Dialog --- //
                    //ProcessStartInfo infoPrint = new ProcessStartInfo();
                    //infoPrint.FileName = sPDFOutputPath;
                    //infoPrint.Verb = "PrintTo";
                    //infoPrint.CreateNoWindow = false;
                    //infoPrint.WindowStyle = ProcessWindowStyle.Normal;
                    //infoPrint.UseShellExecute = true;

                    //Process printProcess = new Process();
                    //printProcess = Process.Start(infoPrint);

                    spireWorkbook.SaveToFile("Report-temp.pdf", Spire.Xls.FileFormat.PDF);
                    System.IO.File.Delete(sExcelOutputPath);

                    Process process = new Process();
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = "Report-temp.pdf",
                        UseShellExecute = true
                    };
                    process.Start();
                }
                else
                {
                    spireWorkbook.SaveToFile(sPDFOutputPath, Spire.Xls.FileFormat.PDF);
                    if (System.IO.File.Exists(sPDFOutputPath))
                    {
                        System.IO.File.Delete(sExcelOutputPath);
                    }

                    Popup sConfirmPopup = new Popup();
                    sConfirmPopup.IsOpen = true;

                    App.MainViewModel.Origin = "ListingDownloadCompleted";
                    App.PopupHandler(null, null);
                }
            }
            catch (Exception ex) { 
                Popup sNotificationPopup = new Popup();
                sNotificationPopup.IsOpen = true;

                App.MainViewModel.Origin = (!isPrint) ? "FailedDownloadListing" : "FailedToShowPrint";
                App.PopupHandler(null, null);
            }
        }

        private void CloseCalender(object sender, RoutedEventArgs e)
        {
            var focusedControl = Keyboard.FocusedElement as FrameworkElement;

            if (focusedControl == null || focusedControl.GetType() == typeof(CalendarDayButton) || focusedControl.GetType() == typeof(CalendarButton) ||
                focusedControl.GetType() == typeof(Calendar))
            {
                return;
            }

            if(focusedControl.GetType() == typeof(ScrollViewer))
            {
                focusedControl.LostFocus += CloseCalender;

                return;
            }


            DateRangePicker calender = sender as DateRangePicker;

            if (calender == RangeDateStart || RangeDateStart.DateRangePicker_Popup.IsOpen)
            {
                RangeDateStart.DateRangePicker_Popup.IsOpen = false;
            }
            else if (calender == RangeDateEnd || RangeDateEnd.DateRangePicker_Popup.IsOpen)
            {
                RangeDateEnd.DateRangePicker_Popup.IsOpen = false;
            }
        }

        private void CloseCalenderPopup(object? sender, MouseButtonEventArgs? e)
        {
            RangeDateStart.DateRangePicker_Popup.IsOpen = false;
            RangeDateEnd.DateRangePicker_Popup.IsOpen = false;
        }

        //---------- POC Testing -----------------//
        private void menuTransfer_Click(object sender, RoutedEventArgs e)
        {
            var sMenu = sender as System.Windows.Controls.MenuItem;

            Popup sInputID = new Popup();
            sInputID.IsOpen = true;

            App.MainViewModel.Origin = "GreywindSendUniqueID";
            App.MainViewModel.TestResultID = sMenu.Tag == null ? currentRow.ID.ToString() : sMenu.Tag.ToString();
            App.PopupHandler(e, sender);

        }

        private void updateNameButton_Click(object sender, RoutedEventArgs e)
        {
            App.isEmptyName = false;
            var sMenu = sender as System.Windows.Controls.MenuItem;
            string testID = sMenu.Tag == null ? currentRow.ID.ToString() : sMenu.Tag.ToString();

            if(sMenu.Name.ToString() == "menuEditPatientName")
            {
                updateName(testID, true, sender, e);
            }
            else
            {
                updateName(testID, false, sender, e);
            }
        }

        private void updateName(string TestResultID, bool isNameChange, object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TestResultID))
            {
                Popup sInputID = new Popup();
                sInputID.IsOpen = true;

                if (isNameChange)
                {
                    App.MainViewModel.Origin = "UpdatePatientName";
                }
                else
                {
                    App.MainViewModel.Origin = "UpdateDoctorName";
                }
                App.MainViewModel.TestResultID = TestResultID;

                App.TestResultInfo = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), Convert.ToInt64(App.MainViewModel.TestResultID));

                App.PopupHandler(e, sender);
            }
        }

        public static void DownloadPrintReportHandler(EventArgs e, object sender)
        {
            if (DownloadPrintReport != null)
            {
                DownloadPrintReport(sender, e);
            }
        }

        public static void UpdatePatientNameHandler(EventArgs e, object sender)
        {
            if (UpdatePatientNameEvent != null)
            {
                UpdatePatientNameEvent(sender, e);
            }
        }

        public static void UpdateDoctorNameHandler(EventArgs e, object sender)
        {
            if (UpdateDoctorNameEvent != null)
            {
                UpdateDoctorNameEvent(sender, e);
            }
        }

        private void dgResult_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            var editedTextbox = e.EditingElement as System.Windows.Controls.TextBox;
            oldName = editedTextbox.Text;
        }

        public void dgResult_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var editedTextbox = e.EditingElement as System.Windows.Controls.TextBox;
                if (editedTextbox != null)
                {
                    if(editedTextbox.Text != oldName)
                    {
                        int rowIndex = e.Row.GetIndex();
                        var testID = ((sender as System.Windows.Controls.DataGrid).Items[rowIndex] as TestResultListingExtendedObj).ID;
                        string newValue = editedTextbox.Text;

                        App.TestResultInfo = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), Convert.ToInt64(testID));

                        if(e.Column.Header.ToString() == Properties.Resources.Results_Label_PatientName.ToString())
                        {
                            App.TestResultInfo.PatientName = newValue;
                        }
                        else if (e.Column.Header.ToString() == Properties.Resources.Results_Label_PatientID.ToString())
                        {
                            App.TestResultInfo.PatientID = newValue;
                        }
                        else
                        {
                            App.TestResultInfo.InchargePerson = newValue;
                        }

                        UpdatePatientName(null, null);
                    }
                }
            }
        }

        private void dgResult_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentRow = null;
            if (e.OriginalSource.GetType() == typeof(Border)) { currentRow = ((e.OriginalSource as Border).DataContext as TestResultListingExtendedObj); }
            else if (e.OriginalSource.GetType() == typeof(System.Windows.Controls.TextBlock)) { currentRow = ((e.OriginalSource as System.Windows.Controls.TextBlock).DataContext as TestResultListingExtendedObj); }
            
            if (dgResult.ContextMenu != null && currentRow != null)
            {
                dgResult.ContextMenu.IsOpen = true;
            }
        }

        private void dgResult_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentRow = null;
            if (e.OriginalSource.GetType() == typeof(Border)) { currentRow = ((e.OriginalSource as Border).DataContext as TestResultListingExtendedObj); }
            else if (e.OriginalSource.GetType() == typeof(System.Windows.Controls.TextBlock)) { currentRow = ((e.OriginalSource as System.Windows.Controls.TextBlock).DataContext as TestResultListingExtendedObj); }

            if (currentRow != null)
            {
                TestResultListingExtendedObj sTestResultObj = currentRow;
                App.TestResultID = sTestResultObj != null ? sTestResultObj.ID : currentRow.ID;

                App.GoToViewResultPageHandler(e, sender);
            }
        }

        private void menuDelete_Click(object sender, RoutedEventArgs e)
        {
            //var selectedResult = dgResult.SelectedItem as TestResultListingExtendedObj;
            //selectedResult = selectedResult != null ? selectedResult : currentRow;
            TestResultsRepository.DeleteTestResultByID(ConfigSettings.GetConfigurationSettings(), currentRow.ID.ToString());
            LoadResultDataGrid();
        }

        private void RangeDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            RangeDateStart.SelectedDates = RangeDateStart.DateRangePicker_Calendar.SelectedDates;
            RangeDateEnd.SelectedDates = RangeDateEnd.DateRangePicker_Calendar.SelectedDates;

            if (RangeDateStart.SelectedDates.Count() == 1 && RangeDateEnd.SelectedDates.Count() == 0)
            {
                RangeDateEnd.SelectedDates = RangeDateStart.SelectedDates;
            }
            else if (RangeDateEnd.SelectedDates.Count() == 1 && RangeDateStart.SelectedDates.Count() == 0)
            {
                RangeDateStart.SelectedDates = RangeDateEnd.SelectedDates;
            }
            else if (RangeDateEnd.SelectedDates.FirstOrDefault() < RangeDateStart.SelectedDates.FirstOrDefault())
            {
                RangeDateEnd.SelectedDates = RangeDateStart.SelectedDates;
                RangeDateEnd.DateRangePicker_Calendar.SelectedDate = RangeDateStart.DateRangePicker_Calendar.SelectedDate;
            }

            RangeDateEnd.DateRangePicker_Calendar.DisplayDateStart = RangeDateStart.DateRangePicker_Calendar.SelectedDate;

            DateFormatConverter dateFormatConverter = new DateFormatConverter();

            RangeDateStart.DateRangePicker_TextBox.Text = dateFormatConverter.ConvertSimpleDate(RangeDateStart.SelectedDates.FirstOrDefault()).ToString();
            RangeDateEnd.DateRangePicker_TextBox.Text = dateFormatConverter.ConvertSimpleDate(RangeDateEnd.SelectedDates.FirstOrDefault()).ToString();

            CloseCalenderPopup(null, null);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            KeywordSearchBar.Text = string.Empty;
            RangeDateStart.SelectedDates = new ObservableCollection<DateTime>();
            RangeDateStart.DateRangePicker_TextBox.Text = string.Empty;
            RangeDateEnd.SelectedDates = new ObservableCollection<DateTime>();
            RangeDateEnd.DateRangePicker_TextBox.Text = string.Empty;
            cboSort.SelectedItem = cbSort.Where(x => x.Content.ToString() == "Latest").FirstOrDefault();

            LoadResultDataGrid();
        }

        private void KeywordSearchBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Check if Enter key is pressed
            {
                LoadResultDataGrid();
            }
        }
    }
}
