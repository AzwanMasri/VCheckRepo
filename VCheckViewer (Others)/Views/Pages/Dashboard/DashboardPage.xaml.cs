//using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VCheck.Lib.Data;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;
using VCheckViewer_Others.Services;
using VCheckViewer_Others.Services.MessageBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace VCheckViewer_Others.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        List<DeviceModel> deviceList = DeviceRepository.GetDeviceList(ConfigSettings.GetConfigurationSettings());
        List<DeviceTypeModel> deviceTypeList = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings());

        List<TestResultExtendedModel> resultList = TestResultsRepository.GetTestResultByDates(ConfigSettings.GetConfigurationSettings(), DateTime.Now);

        private DispatcherTimer _timer;

        public DashboardPage()
        {
            InitializeComponent();
            generateView();

            var message = Properties.Resources.Dashboard_Message_DownloadLatest.Split("<nextline>");

            //updateMessage.Text = message[0] + "\r\n" + message[1];

            //this.SizeChanged += MainWindow_SizeChanged;

            //if (App.ShowUpdateNotification)
            //{
            //    VersionUpdate.Visibility = Visibility.Visible;
            //}

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
            deviceList = DeviceRepository.GetDeviceList(ConfigSettings.GetConfigurationSettings());
            deviceTypeList = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings());
            generateView();
        }

        public void generateView()
        {
            //int totalElement = deviceList.Count;
            int totalElement = deviceTypeList.Count;
            int imageHeight = 250;
            int borderHeight = 400;
            int borderWidth = 420;
            int margin = 15;
            int totalRow = 1;
            int totalElementPerRow = 3;
            bool excess = false;
            int remainder = 0;

            if(totalElement == 0)
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

            //createElementUsingGridByDevice(totalElementPerRow, imageHeight, borderHeight, borderWidth, margin, totalRow, excess, remainder);
            createElementUsingGridByDeviceType(totalElementPerRow, imageHeight, borderHeight, borderWidth, margin, totalRow, excess, remainder);
        }
        
        public async Task createElementUsingGridByDevice(int totalElementPerRow, int imageHeight, int borderHeight, int borderWidth, int margin, int totalRow, bool excess, int remainder)
        {            
            String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();
            String? sFrameColor = System.Windows.Application.Current.Resources["Themes_DashboardAnalyzerFrameBackground"].ToString();
            SolidColorBrush sBrushFontColor = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
            SolidColorBrush sBrushFrameColor = new BrushConverter().ConvertFrom(sFrameColor) as SolidColorBrush;
            int currentDevice = 0;

            List<int> twoWayDevices = DeviceRepository.GetTwoWayCommDevice(ConfigSettings.GetConfigurationSettings()).Select(x => x.id).ToList();
            //List<int> posNegRequired = DeviceRepository.GetPosNegRequiredDevice(ConfigSettings.GetConfigurationSettings()).Select(x => x.id).ToList();
            List<int> posNegRequired = new List<int>();


            for (int i = 0; i < totalRow; i++)
            {
                Grid testGrid = new Grid() { Margin = new Thickness(0, 10, 0, 10) };

                if (totalElementPerRow == 0) { TextBlock textBlock = new TextBlock() { Text = Properties.Resources.General_Message_NoDevice, FontSize = 50, TextAlignment = TextAlignment.Center, Foreground = sBrushFontColor }; testGrid.Children.Add(textBlock); }

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
                    DeviceModel device = deviceList[currentDevice++];
                    int totalPositive = resultList.Where(x => x.DeviceSerialNo == device.DeviceSerialNo && (x.TestResultStatus == "Positive" || x.TestResultStatus == "Abnormal")).Count();
                    int totalNegative = resultList.Where(x => x.DeviceSerialNo == device.DeviceSerialNo && (x.TestResultStatus == "Negative" || x.TestResultStatus == "Normal")).Count();

                    Border parentBorder = new Border() { Height = borderHeight, Width = borderWidth, CornerRadius = new CornerRadius(7), Margin = new Thickness(10, 0, 10, 0), Padding = new Thickness(5) };

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

                    StackPanel secondStackPanel = new StackPanel() { Orientation = Orientation.Vertical };

                    bool isReady = false;
                    bool resultRequired = false;

                    //if (twoWayDevices.Contains(device.id))
                    //{
                    //    DeviceChecker deviceChecker = new DeviceChecker();
                    //    isReady = await deviceChecker.IsOnline(device.DeviceIPAddress, 5067);
                    //}
                    //else
                    //{
                    //    isReady = device.status != 2 ? true : false;
                    //}

                    if (posNegRequired.Contains(device.id))
                    {
                        resultRequired = true;
                    }


                    TextBlock statusTextBlock = new TextBlock() { FontSize = 14, FontWeight = FontWeights.DemiBold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                    statusTextBlock.Foreground = isReady ? (Brush)new BrushConverter().ConvertFromString("#16c933") : (Brush)new BrushConverter().ConvertFromString("#fa8219");
                    statusTextBlock.Text = isReady ? Properties.Resources.General_Label_Ready : Properties.Resources.General_Label_Busy;
                    Border childBorder = new Border() { Height = 30, Width = 70, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, CornerRadius = new CornerRadius(5), Child = statusTextBlock };
                    childBorder.Background = isReady ? (Brush)new BrushConverter().ConvertFromString("#e0ffe5") : (Brush)new BrushConverter().ConvertFromString("#ffeed1");
                    childBorder.Visibility = Visibility.Hidden;

                    Image image = new Image();
                    var uri = new Uri(@"pack://application:,,,/VCheckViewer_Others;component/" + device.DeviceImagePath);
                    var bitmap = new BitmapImage(uri);
                    image.Source = bitmap;
                    image.Height = imageHeight;
                    image.Margin = new Thickness(margin);

                    TextBlock nameTextBlock = new TextBlock() { Text = device.DeviceName, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(margin), Foreground = sBrushFontColor };
                    StackPanel thirdStackPanel = new StackPanel();

                    if (resultRequired)
                    {
                        TextBlock resultTextBlock1 = new TextBlock() { Text = Properties.Resources.Dashboard_Label_Positive + "  ", Foreground = sBrushFontColor };
                        TextBlock resultTextBlock2 = new TextBlock() { Text = totalPositive.ToString(), FontWeight = FontWeights.Bold, Foreground = Brushes.Green, TextAlignment = TextAlignment.Center };
                        Border positiveBorder = new Border() { Width = 20, Child = resultTextBlock2, Margin = new Thickness(0, 0, 20, 0) };
                        Rectangle resultSeperator = new Rectangle() { VerticalAlignment = VerticalAlignment.Stretch, Width = 1, Height = 20, Margin = new Thickness(2), Stroke = Brushes.Black };
                        TextBlock resultTextBlock3 = new TextBlock() { Text = Properties.Resources.Dashboard_Label_Negative + "  ", Margin = new Thickness(20, 0, 0, 0), Foreground = sBrushFontColor };
                        TextBlock resultTextBlock4 = new TextBlock() { Text = totalNegative.ToString(), FontWeight = FontWeights.Bold, Foreground = Brushes.Red, TextAlignment = TextAlignment.Center };
                        Border negativeBorder = new Border() { Width = 20, Child = resultTextBlock4 };
                        thirdStackPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
                        thirdStackPanel.Children.Add(resultTextBlock1);
                        thirdStackPanel.Children.Add(positiveBorder);
                        thirdStackPanel.Children.Add(resultSeperator);
                        thirdStackPanel.Children.Add(resultTextBlock3);
                        thirdStackPanel.Children.Add(negativeBorder);
                    }


                    secondStackPanel.Children.Add(childBorder);
                    secondStackPanel.Children.Add(image);
                    secondStackPanel.Children.Add(nameTextBlock);
                    secondStackPanel.Children.Add(thirdStackPanel);

                    parentBorder.Child = secondStackPanel;
                    parentBorder.Background = sBrushFrameColor;
                    DropShadowEffect effect = new DropShadowEffect() { ShadowDepth = 0, Opacity = 0.3, BlurRadius = 15 };
                    parentBorder.Effect = effect;
                    parentBorder.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                    parentBorder.Tag = device.id;

                    Grid.SetColumn(parentBorder, j);

                    testGrid.Children.Add(parentBorder);
                }

                if(i == 0) { responsiveView.Children.Clear(); }
                
                responsiveView.Children.Add(testGrid);
            }
        }

        public async Task createElementUsingGridByDeviceType(int totalElementPerRow, int imageHeight, int borderHeight, int borderWidth, int margin, int totalRow, bool excess, int remainder)
        {
            List<DeviceTypeModel> ExistedDevice = new List<DeviceTypeModel>();
            List<DeviceTypeModel> NotExistedDevice = new List<DeviceTypeModel>();

            foreach (var device in deviceTypeList)
            {
                if (deviceList.Any(x => x.DeviceTypeID == device.id)) { ExistedDevice.Add(device); }
                else { NotExistedDevice.Add(device); }
            }

            deviceTypeList.Clear();
            deviceTypeList.AddRange(ExistedDevice);
            deviceTypeList.AddRange(NotExistedDevice);

            String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();
            String? sFrameColor = System.Windows.Application.Current.Resources["Themes_DashboardAnalyzerFrameBackground"].ToString();
            SolidColorBrush sBrushFontColor = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
            SolidColorBrush sBrushFrameColor = new BrushConverter().ConvertFrom(sFrameColor) as SolidColorBrush;
            int currentDevice = 0;


            for (int i = 0; i < totalRow; i++)
            {
                Grid testGrid = new Grid() { Margin = new Thickness(0, 10, 0, 10) };

                if (totalElementPerRow == 0) { TextBlock textBlock = new TextBlock() { Text = Properties.Resources.General_Message_NoDevice, FontSize = 50, TextAlignment = TextAlignment.Center, Foreground = sBrushFontColor }; testGrid.Children.Add(textBlock); }

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
                    DeviceTypeModel deviceType = deviceTypeList[currentDevice++];
                    bool NotExist = NotExistedDevice.Any(x => x.id == deviceType.id);

                    Border parentBorder = new Border() { Height = borderHeight, Width = borderWidth, CornerRadius = new CornerRadius(7), Margin = new Thickness(10, 0, 10, 0), Padding = new Thickness(5) };

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

                    StackPanel secondStackPanel = new StackPanel() { Orientation = Orientation.Vertical };

                    Border childBorder = new Border() { Height = 30, Width = 70, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Visibility = Visibility.Hidden };
                    

                    Image image = new Image();
                    var uri = new Uri(@"pack://application:,,,/VCheckViewer_Others;component/" + deviceType.ImageSource);
                    var bitmap = new BitmapImage(uri);
                    image.Source = bitmap;
                    image.Height = imageHeight;
                    image.Margin = new Thickness(margin);

                    TextBlock nameTextBlock = new TextBlock() { Text = deviceType.TypeName, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(margin), Foreground = sBrushFontColor };
                    StackPanel thirdStackPanel = new StackPanel();


                    secondStackPanel.Children.Add(childBorder);
                    secondStackPanel.Children.Add(image);
                    secondStackPanel.Children.Add(nameTextBlock);
                    secondStackPanel.Children.Add(thirdStackPanel);

                    parentBorder.Child = secondStackPanel;
                    parentBorder.Background = sBrushFrameColor;
                    DropShadowEffect effect = new DropShadowEffect() { ShadowDepth = 0, Opacity = 0.3, BlurRadius = 15 };
                    parentBorder.Effect = effect;
                    parentBorder.Tag = deviceType.id;
                    parentBorder.Opacity = NotExist ? 0.5 : 1;

                    if (!NotExist)
                    {
                        parentBorder.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                    }

                    Grid.SetColumn(parentBorder, j);

                    testGrid.Children.Add(parentBorder);
                }

                if (i == 0) { responsiveView.Children.Clear(); }

                responsiveView.Children.Add(testGrid);
            }
        }

        void DownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.UpdateLink) { UseShellExecute = true });
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //var windowHeight = App.WindowHeight;

            //if(windowHeight > 1016) { deviceListView.MaxHeight = 1000; }
            //else { deviceListView.MaxHeight = windowHeight * 0.39; }
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            dynamic originElemnt = e.OriginalSource;
            Border border = null;
            int max = 0;

            while (border == null)
            {
                border = originElemnt.Parent as Border;

                originElemnt = originElemnt.Parent;

                if (max++ == 5) { return; }
            }

            var sDeviceID = border.Tag.ToString();
            App.AnalyzerID = int.Parse(sDeviceID);

            App.GoToResultPageHandler(null, null);
        }
    }
}
