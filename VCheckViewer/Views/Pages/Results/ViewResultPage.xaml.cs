using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Converter;
using VCheckViewer.Lib.Function;
using VCheckViewer.Services;

namespace VCheckViewer.Views.Pages.Results
{

    /// <summary>
    /// Interaction logic for ViewResultPage.xaml
    /// </summary>
    public partial class ViewResultPage : Page, INotifyPropertyChanged
    {
        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());
        private List<TestResultDetailsModel> sTestResultDetails = new List<TestResultDetailsModel>();
        private TestResultModel sTestResult = new TestResultModel();
        private List<string> sParameterOrder = new List<string>();
        public DownloadPrintResultModel downloadPrintResultModel = new DownloadPrintResultModel();
        private TestResultSpecimenContainer sTestResultSpecimentContainer = new TestResultSpecimenContainer();
        public string PreviousDatetime { get; set; } = "-";

        public ViewResultPage()
        {
            InitializeComponent();
            DataContext = this;

            LoadReportInfo();
            LoadResultData();
            LoadResultGraph();

            this.SizeChanged += MainWindow_SizeChanged;

            ViewResultBackButton.DataContext = App.MainViewModel;
            App.MainViewModel.BackButtonText = Properties.Resources.Setting_Label_BackButton;

            PatientIDLabel.Text = Properties.Resources.Results_Label_PatientID + ":";
            PatientNameLabel.Text = Properties.Resources.Results_Label_PatientName + ":";
            DateTimeLabel.Text = Properties.Resources.Report_Label_DateTime + ":";
            DoctorNameLabel.Text = Properties.Resources.Results_Label_Doctor + ":";
            SpeciesLabel.Text = "Species:";
            AgeLabel.Text = "Age:";
            WeightLabel.Text = "Weight:";

            //PreviousDatetime = "(" + DateTime.Now.ToString("dd/MM/yyyy hh:mm") + ")";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadPage()
        {
            LoadReportInfo();
            LoadResultData();
            LoadResultGraph();
        }

        private void LoadReportInfo()
        {
            sTestResult = TestResultsRepository.GetTestResultByID(ConfigSettings.GetConfigurationSettings(), App.TestResultID);
            var sReportImagePath = configDBContext.GetConfigurationData("ReportImagePath").FirstOrDefault();
            var sClinicName = configDBContext.GetConfigurationData("ClinicName").FirstOrDefault();
            var sReportTitle = configDBContext.GetConfigurationData("ReportTitle").FirstOrDefault();
            var sClinicAddress = configDBContext.GetConfigurationData("ClinicAddress").FirstOrDefault();
            var sClinicPhoneNum = configDBContext.GetConfigurationData("ClinicPhoneNum").FirstOrDefault();

            if(sReportImagePath != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    Uri uri = new Uri(sReportImagePath != null ? sReportImagePath.ConfigurationValue : "pack://application:,,,/Content/Images/bio_logo_report.png");
                    bitmap = new BitmapImage(uri);

                    ViewReportLogo.Source = bitmap;
                }
                catch (Exception ex)
                {

                }
            }

            ClinicName.Text = sClinicName != null ? sClinicName.ConfigurationValue : "";
            ReportTitle.Text = sReportTitle != null ? sReportTitle.ConfigurationValue : "";
            ClinicAddress.Text = sClinicAddress != null ? sClinicAddress.ConfigurationValue : "";
            ClinicPhoneNum.Text = sClinicPhoneNum != null ? "+" + sClinicPhoneNum.ConfigurationValue : "";

            PatientID.Text = sTestResult.PatientID;
            PatientName.Text = sTestResult.PatientName;
            Species.Text = sTestResult.Species;
            Age.Text = sTestResult.Age;
            Weight.Text = sTestResult.Weight;
            DoctorName.Text = sTestResult.InchargePerson;

            /*// Species
            if (!string.IsNullOrWhiteSpace(sTestResult.Species))
            {
                SpeciesRow.Visibility = Visibility.Visible;

                SpeciesLabel.Text = "Species:";
                Species.Text = sTestResult.Species;
            }
            else
            {
                SpeciesRow.Visibility = Visibility.Collapsed;
            }

            // Age
            if (!string.IsNullOrWhiteSpace(sTestResult.Age))
            {
                AgeRow.Visibility = Visibility.Visible;

                AgeLabel.Text = "Age:";
                Age.Text = sTestResult.Age;
            }
            else
            {
                AgeRow.Visibility = Visibility.Collapsed;
            }

            // Weight
            if (!string.IsNullOrWhiteSpace(sTestResult.Weight))
            {
                WeightRow.Visibility = Visibility.Visible;

                WeightLabel.Text = "Weight:";
                Weight.Text = sTestResult.Weight;
            }
            else
            {
                WeightRow.Visibility = Visibility.Collapsed;
            }
            */

            var sConfigObj = configDBContext.GetConfigurationData("System_DateFormat").FirstOrDefault();

            if (sConfigObj != null)
            {
                TestDate.Text = sTestResult.TestResultDateTime.Value.ToString(sConfigObj.ConfigurationValue + " hh:mm");
            }
            else
            {
                TestDate.Text = sTestResult.TestResultDateTime.Value.ToString("dd/MM/yyyy hh:mm");
            }
        }

        private void LoadResultData()
        {
            try
            {
                var ThemeIsDark = configDBContext.GetConfigurationData("SystemSettings_Themes").FirstOrDefault().ConfigurationValue == "Dark";
                sParameterOrder = TestResultsRepository.GetAllParameters(ConfigSettings.GetConfigurationSettings()).Select(x => x.Parameter).ToList();
                sTestResultDetails = TestResultsRepository.GetResultDetailsByTestResultID(ConfigSettings.GetConfigurationSettings(), App.TestResultID);
                List<TestResultDetailsExtension> sTestResultDetailsExtension = new List<TestResultDetailsExtension>();
                int HemIndex = 0;
                int LipIndex = 0;
                int IctIndex = 0;

                TestResultModel previousTest = new TestResultModel();
                downloadPrintResultModel.TestResult = sTestResult;
                downloadPrintResultModel.TestResultDetails = sTestResultDetails;
                downloadPrintResultModel.PreviousTestResultDetails = TestResultsRepository.GetPreviousTestRecord(ConfigSettings.GetConfigurationSettings(), sTestResult, out previousTest);
                downloadPrintResultModel.PreviousTestResult = previousTest;
                downloadPrintResultModel.TestResultsGraph = TestResultsRepository.GetResultGraphsByTestResultID(ConfigSettings.GetConfigurationSettings(), sTestResult.ID);

                DateFormatConverter dateFormatConverter = new DateFormatConverter();

                PreviousDatetime = previousTest != null ? "(" + dateFormatConverter.Convert(previousTest.TestResultDateTime, typeof(DateTime), null, CultureInfo.CurrentCulture).ToString() + ")" : "(-)";

                if (sTestResultDetails.FirstOrDefault(x => x.TestParameter == "IC") == null)
                {
                    sTestResultDetails = sTestResultDetails.OrderBy(d => sParameterOrder.IndexOf(d.TestParameter)).ToList();
                }

                bool hasIC = sTestResultDetails.Any(x => x.TestParameter.Contains(" IC"));

                foreach (var testDetail in sTestResultDetails)
                {
                    float firstLayer = 0;
                    float secondLayer = 0;
                    var barVisibility = "Visible";
                    var lowNormalHigh = 1;
                    var previousValue = "-";
                    string[] range = new string[2];
                    bool isReferenceRange = false;
                    var resultValue = "";
                    bool isResultCorrectFormat = false;

                    if (!string.IsNullOrEmpty(testDetail.TestResultValue))
                    {
                        resultValue = testDetail.TestResultValue.Replace("<", "").Replace(">", "").Replace("=", "");

                        if (!(testDetail.TestResultValue.Contains("-") && !testDetail.TestResultValue.StartsWith("-")) && double.TryParse(resultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        {
                            isResultCorrectFormat = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(testDetail.ReferenceRange) && testDetail.ReferenceRange != "0" && !testDetail.ReferenceRange.Contains("<") && !Regex.IsMatch(testDetail.ReferenceRange, "[A-Za-z]"))
                    {
                        range = testDetail.ReferenceRange.Replace("[", "").Replace("]", "").Contains(";") ? testDetail.ReferenceRange.Replace(" ", "").Replace("[", "").Replace("]", "").Split(";") : testDetail.ReferenceRange.Replace(" ", "").Replace("[", "").Replace("]", "").Split("-");
                        isReferenceRange = true;
                    }

                    if (isReferenceRange && isResultCorrectFormat && !hasIC)
                    {
                        if (!string.IsNullOrEmpty(range[0]) && range.Count() == 2)
                        {
                            barVisibility = "Collapsed";
                            firstLayer = CalculateReference(float.Parse(range[0].Replace("[", ""), CultureInfo.InvariantCulture) - (float)0.001, float.Parse(range[1].Replace("]", ""), CultureInfo.InvariantCulture) + (float)0.001, float.Parse(resultValue, CultureInfo.InvariantCulture), out lowNormalHigh);
                            secondLayer = firstLayer - (float)1.5;
                        }
                    }

                    if (previousTest != null)
                    {
                        var previousValueDetails = downloadPrintResultModel.PreviousTestResultDetails.FirstOrDefault(x => x.TestParameter == testDetail.TestParameter);

                        previousValue = previousValueDetails != null ? previousValueDetails.TestResultValue + " " + previousValueDetails.TestResultUnit : "-";
                    }

                    var brush = (this.TryFindResource("Themes_GridUserFontColor") as SolidColorBrush);
                    System.Windows.Media.Color colorValue = brush.Color;

                    var testStatus = "";
                    var foreground = colorValue.ToString();
                    var arrowIcon = "/Content/Images/Down-Arrow.png";
                    var iconVisibility = "Collapsed";
                    var referenceRange = testDetail.ReferenceRange;

                    if (testDetail.TestResultValue == "-" || testDetail.TestResultValue == "--")
                    {
                        testStatus = "-";
                    }
                    else if (string.IsNullOrEmpty(testDetail.TestResultStatus))
                    {
                        testStatus = "Error";
                    }
                    else if (testDetail.TestResultStatus.ToLower() == "positive")
                    {
                        testStatus = Properties.Resources.Dashboard_Label_Positive;
                    }
                    else if (testDetail.TestResultStatus.ToLower() == "abnormal")
                    {
                        testStatus = Properties.Resources.Schedule_Label_Abnormal;
                        foreground = "#ff0000";
                    }
                    else if (testDetail.TestResultStatus.ToLower() == "normal")
                    {
                        testStatus = Properties.Resources.Schedule_Label_Normal;
                    }
                    else if (testDetail.TestResultStatus.ToLower() == "negative")
                    {
                        testStatus = Properties.Resources.Dashboard_Label_Negative;
                    }
                    else
                    {
                        testStatus = testDetail.TestResultStatus;
                    }

                    if (lowNormalHigh == 0)
                    {
                        arrowIcon = "/Content/Images/" + (ThemeIsDark ? "Down-Arrow-Dark.png" : "Down-Arrow-Light.png");
                        foreground = ThemeIsDark ? "#009dff" : "#0011ff";
                        iconVisibility = "Visible";
                    }
                    else if (lowNormalHigh == 2)
                    {
                        arrowIcon = "/Content/Images/Up-Arrow.png";
                        foreground = "#ff0000";
                        iconVisibility = "Visible";
                    }

                    var rangeString = string.IsNullOrEmpty(range[0]) ? "" : range[0] + " - " + range[1];
                    rangeString = string.IsNullOrEmpty(rangeString) ? referenceRange : rangeString;

                    TestResultDetailsExtension testDetailExtension = new TestResultDetailsExtension()
                    {
                        ID = testDetail.ID,
                        TestResultValue = testDetail.TestResultValue != null ? testDetail.TestResultValue + (testDetail.TestResultValue.ToLower() == "invalid" ? "" : " " + testDetail.TestResultUnit) : "",
                        TestParameter = testDetail.TestParameter.Contains(" IC") ? "IC" : testDetail.TestParameter,
                        TestResultStatus = testStatus,
                        ReferenceRange = string.IsNullOrEmpty(rangeString) || hasIC ? "-" : rangeString,
                        FirstLayerReference = firstLayer == 0 ? 1 : firstLayer,
                        SecondLayerReference = firstLayer == 0 ? 0 : secondLayer,
                        BarVisibility = barVisibility,
                        ArrowIcon = arrowIcon,
                        ForeGround = foreground,
                        IconVisibility = iconVisibility,
                        PreviousValue = previousValue
                    };

                    sTestResultDetailsExtension.Add(testDetailExtension);
                }

                dgResultDetails.ItemsSource = sTestResultDetailsExtension.OrderBy(d => d.ID).ToList();

                if(sTestResult.Analyze_TableRowID != null && sTestResult.Analyze_TableRowID != 0)
                {
                    sTestResultSpecimentContainer = TestResultsRepository.GetTestResultSpecimenContainerByID(ConfigSettings.GetConfigurationSettings(), sTestResult.Analyze_TableRowID);

                    if (sTestResultSpecimentContainer != null)
                    {
                        HemLipIctIndex.Text = "HEM: " + sTestResultSpecimentContainer.HemolysisIndex + " / LIP: " + sTestResultSpecimentContainer.LipemiaIndex + " / ICT: " + sTestResultSpecimentContainer.IcterusIndex;
                        serumIndexView.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                App.log.Error("View Result Error >>> ", ex);
                App.MainViewModel.Origin = "GeneralErrorOccur";
                App.PopupHandler(null, null);
            }
        }

        public void LoadResultGraph()
        {
            List<TestResultGraphModel> TestResultsGraph = downloadPrintResultModel.TestResultsGraph;

            int totalElement = TestResultsGraph.Count;
            int imageHeight = 250;
            int borderHeight = 400;
            int borderWidth = 420;
            int margin = 15;
            int totalRow = 1;
            int totalElementPerRow = 3;
            bool excess = false;
            int remainder = 0;


            string graphFolder = Host.CreateApplicationBuilder().Configuration.GetSection("Configuration:GraphFolder").Value;

            if (TestResultsGraph.Count() > 0)
            {
                if(!File.Exists(graphFolder + TestResultsGraph.FirstOrDefault().TestResultRowID + "\\" + TestResultsGraph.FirstOrDefault().FileName))
                {
                    try
                    {
                        string sourceFolder = @"C:\VCheck\VcheckViewer\" + TestResultsGraph.FirstOrDefault().TestResultRowID;
                        string destinationFolder = graphFolder + TestResultsGraph.FirstOrDefault().TestResultRowID;

                        string parentDir = Path.GetDirectoryName(destinationFolder);
                        if (!Directory.Exists(parentDir))
                        {
                            Directory.CreateDirectory(parentDir);
                        }

                        Directory.Move(sourceFolder, destinationFolder);
                    }
                    catch (Exception ex)
                    {
                        App.log.Error("Moving Folder Error >>> ", ex);
                    }
                }

                using (System.Drawing.Image img = System.Drawing.Image.FromFile(graphFolder + TestResultsGraph.FirstOrDefault().TestResultRowID + "\\" + TestResultsGraph.FirstOrDefault().FileName))
                {
                    totalElementPerRow = img.Width > 800 ? 1 : totalElementPerRow;
                }
            }

            if (totalElementPerRow != 3)
            {
                totalRow = totalElement;
            }
            else if (totalElement == 0)
            {
                totalElementPerRow = 0;
            }
            else if (totalElement > 0 && totalElement < 3)
            {
                totalElementPerRow = totalElement;
            }
            else if (totalElement == 3)
            {
                imageHeight = 170;
                borderHeight = 300;
                borderWidth = 290;
                margin = 10;
            }
            else if (totalElement == 4)
            {
                totalRow = 2;
                totalElementPerRow = 2;
                imageHeight = 100;
                borderHeight = 199;
                borderWidth = 290;
                margin = 3;
            }
            else
            {
                totalRow = Math.DivRem(totalElement, 3, out remainder);
                if (remainder > 0)
                {
                    totalRow += 1;
                    excess = true;
                }
                imageHeight = 100;
                borderHeight = 199;
                borderWidth = 290;
                margin = 3;
            }

            createElementUsingGridByGraph(TestResultsGraph, totalElementPerRow, imageHeight, borderHeight, borderWidth, margin, totalRow, excess, remainder);
        }

        public async Task createElementUsingGridByGraph(List<TestResultGraphModel> graphs, int totalElementPerRow, int imageHeight, int borderHeight, int borderWidth, int margin, int totalRow, bool excess, int remainder)
        {
            try
            {
                string graphFolder = Host.CreateApplicationBuilder().Configuration.GetSection("Configuration:GraphFolder").Value;
                String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();
                String? sFrameColor = System.Windows.Application.Current.Resources["Themes_DashboardAnalyzerFrameBackground"].ToString();
                SolidColorBrush sBrushFontColor = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                SolidColorBrush sBrushFrameColor = new BrushConverter().ConvertFrom(sFrameColor) as SolidColorBrush;
                int currentGraph = 0;

                for (int i = 0; i < totalRow; i++)
                {
                    Grid testGrid = new Grid() { Margin = new Thickness(0, 10, 0, 10) };

                    if (totalElementPerRow == 0) { TextBlock textBlock = new TextBlock() { Text = "No graph found.", FontSize = 50, TextAlignment = TextAlignment.Center, Foreground = sBrushFontColor }; testGrid.Children.Add(textBlock); graphView.Visibility = Visibility.Collapsed; }

                    for (int column = 0; column < totalElementPerRow; column++)
                    {
                        ColumnDefinition columnDefinition = new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("*") };
                        testGrid.ColumnDefinitions.Add(columnDefinition);
                    }

                    RowDefinition rowDefinition = new RowDefinition() { Height = (GridLength)new GridLengthConverter().ConvertFromString("*") };
                    testGrid.RowDefinitions.Add(rowDefinition);

                    if (i == (totalRow - 1) && excess) { totalElementPerRow = remainder; }

                    for (int j = 0; j < totalElementPerRow; j++)
                    {
                        TestResultGraphModel graph = graphs[currentGraph++];

                        Border parentBorder = new Border() { CornerRadius = new CornerRadius(7), Margin = new Thickness(10, 0, 10, 0), Padding = new Thickness(5) };

                        if (totalElementPerRow > 2)
                        {
                            if (j == 0) { parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; }
                            else if (j == totalElementPerRow - 1) { parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Right; }
                            else { parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Center; }
                        }
                        else
                        {
                            parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                            if (excess)
                            {
                                if (j == 0) { parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left; }
                                else { parentBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Center; }
                            }
                        }

                        StackPanel secondStackPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical };

                        bool isReady = false;
                        bool resultRequired = false;

                        System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                        //var bitmap = new BitmapImage(new Uri(graphFolder + graph.TestResultRowID + "\\" + graph.FileName, UriKind.Absolute));
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load into memory
                        bitmap.UriSource = new Uri(graphFolder + graph.TestResultRowID + "\\" + graph.FileName, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        image.Source = bitmap;
                        image.Height = borderHeight;
                        image.Margin = new Thickness(margin);


                        secondStackPanel.Children.Add(image);

                        parentBorder.Child = secondStackPanel;
                        parentBorder.Background = sBrushFrameColor;
                        DropShadowEffect effect = new DropShadowEffect() { ShadowDepth = 0, Opacity = 0.3, BlurRadius = 15 };
                        parentBorder.Effect = effect;

                        Grid.SetColumn(parentBorder, j);

                        testGrid.Children.Add(parentBorder);
                    }

                    if (i == 0) { responsiveView.Children.Clear(); }
                    responsiveView.Children.Add(testGrid);
                }
            }
            catch (Exception ex)
            {
                App.log.Error("View Result Error >>> ", ex);
                App.MainViewModel.Origin = "GeneralErrorOccur";
                App.PopupHandler(null, null);
            }
            
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var windowHeight = App.WindowHeight;

            //dgResultDetails.MaxHeight = windowHeight * 0.48;
            //graphView.MaxHeight = windowHeight * 0.2;
            ViewReportLogo.Height = windowHeight * 0.2;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoPreviousPageHandler(e, sender);
        }

        public float CalculateReference(float start, float end, float value, out int lowNormalHigh)
        {
            float range = end - start;
            float actualStart = start - range;
            float actualEnd = end + range;
            float percentage = 0;
            lowNormalHigh = 0;

            float onceWidth = (float)33.3333;
            float twiceWidth = (float)66.6666;
            float fullwidth = 100;

            if (value > start && end > value)
            {
                float balance = value - start;
                percentage = (balance / range) * onceWidth + onceWidth;
                lowNormalHigh = 1;
            }
            else if (value < start && actualStart < value)
            {
                float balance = value - actualStart;
                percentage = (balance / range) * onceWidth;
                lowNormalHigh = 0;
            }
            else if (value > end && actualEnd > value)
            {
                float balance = value - end;
                percentage = (balance / range) * onceWidth + twiceWidth;
                lowNormalHigh = 2;
            }
            else if (value > actualEnd)
            {
                percentage = fullwidth;
                lowNormalHigh = 2;
            }

            return percentage;
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            DownloadPrint(true);
        }

        private void DownloadPrint(bool IsPrint)
        {
            App.IsPrint = IsPrint;
            App.Parameters = sTestResultDetails.Select(x => x.TestParameter).ToList();

            if (!App.Parameters.Contains("IC"))
            {
                App.Parameters = App.Parameters.OrderBy(d => sParameterOrder.IndexOf(d)).ToList();
            }

            App.isEmptyName = false;

            App.DowloadPrintObject = new List<DownloadPrintResultModel>() { downloadPrintResultModel };
            var DeviceName = DeviceRepository.GetDeviceNameBySerialNo(ConfigSettings.GetConfigurationSettings(), sTestResult.DeviceSerialNo);
            DeviceName = DeviceName == "General" ? Properties.Resources.Schedule_Label_General : DeviceName;

            App.Device = new List<TestDeviceName>();
            App.Device.Add(new TestDeviceName() { DeviceName = DeviceRepository.GetDeviceNameBySerialNo(ConfigSettings.GetConfigurationSettings(), sTestResult.DeviceSerialNo), TestID = sTestResult.ID });

            App.MainViewModel.Origin = "SelectParameters";
            App.PopupHandler(null, null);

        }
    }

    public class TestResultDetailsExtension : TestResultDetailsModel
    {
        public float FirstLayerReference { get; set; }
        public float SecondLayerReference { get; set; }
        public string BarVisibility { get; set; }
        public string ReferenceValue { get; set; }
        public string ArrowIcon { get; set; }
        public string ForeGround { get; set; }
        public string IconVisibility { get; set; }
        public string PreviousValue { get; set; }
    }
}
