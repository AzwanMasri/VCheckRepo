using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;

namespace VCheckViewer_Others.Views.Pages.Setting.Report
{
    /// <summary>
    /// Interaction logic for ReportPage.xaml
    /// </summary>
    public partial class ReportPage : Page
    {
        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public ReportPage()
        {
            InitializeComponent();
            LoadInformation();

            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void LoadInformation()
        {
            var sReportImagePath = configDBContext.GetConfigurationData("ReportImagePath").FirstOrDefault();
            var sReportTitle = configDBContext.GetConfigurationData("ReportTitle").FirstOrDefault();

            try
            {
                var bitmap = new BitmapImage();
                Uri uri = new Uri(sReportImagePath != null ? sReportImagePath.ConfigurationValue : "pack://application:,,,/Content/Images/Report Logo Default.png");
                bitmap = new BitmapImage(uri);

                Logo.Source = bitmap;
                LogoPath.Text = sReportImagePath != null ? sReportImagePath.ConfigurationValue : "";
            }
            catch (Exception ex)
            {

            }

            ReportTitle.Text = sReportTitle != null ? sReportTitle.ConfigurationValue : "";
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingDevicePageHandler(e, sender);
        }

        private void btnLanguage_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingLanguageCountryPageHandler(e, sender);
        }

        private void btnUser_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingUserPageHandler(e, sender);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingConfigurationPageHandler(e, sender);
        }

        private void ClinicInfoPage(object sender, RoutedEventArgs e)
        {
            App.GoToClinicInfoPageHandler(e, sender);
        }

        private void image_click(object sender, MouseButtonEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                FileInfo fileInfo = new FileInfo(dlg.FileName);
                long fileSizeInBytes = fileInfo.Length;

                if(fileSizeInBytes > 2000000)
                {
                    System.Windows.Forms.MessageBox.Show("File size too big. Please select file below 2mb.");
                }
                else
                {
                    LogoPath.Text = dlg.FileName;
                    var uri = new Uri(dlg.FileName);
                    var bitmap = new BitmapImage(uri);

                    Logo.Source = bitmap;
                }

            }

            UpdateButton.IsEnabled = isSufficient();
        }


        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!isSufficient())
            {
                UpdateButton.IsEnabled = false;
            }
            else
            {
                List<ConfigurationModel> sConfigList = new List<ConfigurationModel>();

                sConfigList.Add(new ConfigurationModel
                {
                    ConfigurationKey = "ReportImagePath",
                    ConfigurationValue = LogoPath.Text.Replace("\\", "\\\\")
                });

                sConfigList.Add(new ConfigurationModel
                {
                    ConfigurationKey = "ReportTitle",
                    ConfigurationValue = ReportTitle.Text
                });

                Popup sConfirmPopup = new Popup();
                sConfirmPopup.IsOpen = true;

                App.MainViewModel.Origin = "ReportSettingsUpdate";
                App.MainViewModel.ConfigurationModel = sConfigList;

                App.PopupHandler(e, sender);
            }
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateButton.IsEnabled = isSufficient();
        }

        private bool isSufficient()
        {
            return true;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Logo.Height = e.NewSize.Height * 0.109;
            LogoSample.Height = e.NewSize.Height * 0.078;
            ReportSampleElement.Height = e.NewSize.Height * 0.844;
            ReportElement.Height = e.NewSize.Height * 0.782;
        }
    }
}
