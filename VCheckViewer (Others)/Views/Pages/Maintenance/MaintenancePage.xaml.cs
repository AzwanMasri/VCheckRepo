using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;
using VCheckViewer_Others.Services.MessageBox;
using VCheckViewer_Others.ViewModels.Windows;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace VCheckViewer_Others.Views.Pages.Maintenance
{
    /// <summary>
    /// Interaction logic for MaintenancePage.xaml
    /// </summary>
    public partial class MaintenancePage : Page
    {
        public ObservableCollection<ComboBoxItem> cbDateFormat { get; set; }
        public ComboBoxItem SelectedcbDateFormat { get; set; }

        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public MaintenancePage()
        {
            InitializeComponent();
            DataContext = this;

            BackButton.DataContext = App.MainViewModel;
            App.MainViewModel.BackButtonText = Properties.Resources.Setting_Label_BackButton;
            cbDateFormat = App.MainViewModel.cbDateFormat;

            var sConfigObj = configDBContext.GetConfigurationData("System_DateFormat").FirstOrDefault();
            if(sConfigObj != null)
            {
                SelectedcbDateFormat = cbDateFormat.Where(a => (string)a.Tag == sConfigObj.ConfigurationValue).FirstOrDefault();
            }
            else
            {
                SelectedcbDateFormat = cbDateFormat.FirstOrDefault();
            }

            var SystemVersion = configDBContext.GetConfigurationData("System_Version").FirstOrDefault();
            AppVersion.Text = SystemVersion.ConfigurationValue;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoPreviousPageHandler(e, sender);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)DateFormat.SelectedItem;

            var sConfigObj = configDBContext.GetConfigurationData("System_DateFormat").FirstOrDefault();
            if (sConfigObj != null)
            {
                configDBContext.UpdateConfiguration("System_DateFormat", selectedItem.Tag.ToString());
            }
            else
            {
                configDBContext.AddConfiguration("System_DateFormat", selectedItem.Tag.ToString());
            }

            App.MainViewModel.Origin = "GeneralSettingsUpdated";

            App.PopupHandler(e, sender);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            (string SelectedButton, bool IsCheckBoxChecked) result = ("", false);

            VCheckAPI vcheckAPI = new VCheckAPI();
            var SystemVersion = configDBContext.GetConfigurationData("System_Version").FirstOrDefault();

            var IsLatestVersion = false;
            IsLatestVersion = await vcheckAPI.IsLatestVersion(SystemVersion.ConfigurationValue);

            if (!IsLatestVersion)
            {
                result = CustomMessageBox.Show(1, false);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Current VCheck already the latest version.");
            }


            if (result.SelectedButton == "Yes")
            {
                LoadingMessageForm msg = new LoadingMessageForm("Downloading patch...");

                msg.Show();

                var downloadSuccess = await vcheckAPI.DownloadLatestPatch();

                msg.Close();

                if (downloadSuccess)
                {
                    string downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
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
}
