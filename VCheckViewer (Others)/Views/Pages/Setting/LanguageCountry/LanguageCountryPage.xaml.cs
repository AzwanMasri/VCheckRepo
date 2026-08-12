using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Culture;
using VCheckViewer_Others.Lib.Util;
using Brushes = System.Windows.Media.Brushes;

namespace VCheckViewer_Others.Views.Pages.Setting.LanguageCountry
{
    /// <summary>
    /// Interaction logic for LanguageCountryPage.xaml
    /// </summary>
    public partial class LanguageCountryPage : Page
    {
        CountryDBContext sContext = App.GetService<CountryDBContext>();
        MasterCodeDataDBContext MasterCodeDataContext = App.GetService<MasterCodeDataDBContext>();

        ConfigurationModel currentCountry;
        ConfigurationModel currentLanguage;

        public string languageSelected = null;
        public string countrySelected = null;

        public LanguageCountryPage()
        {
            InitializeComponent();

            initializedList();

            if (App.MainViewModel.CurrentUsers.Role == "Lab User")
            {
                btnUserSetting.IsEnabled = false;
                //btnSettings.IsEnabled = false;
                //btnDeviceSetting.IsEnabled = false;
            }
            else
            {
                btnUserSetting.IsEnabled = true;
                //btnSettings.IsEnabled = true;
                //btnDeviceSetting.IsEnabled = true;
            }

            App.isLanguagePage = true;

            this.SizeChanged += MainWindow_SizeChanged;
        }

        public void initializedList()
        {
            String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();

            System.Windows.Controls.RadioButton radioButton;
            DockPanel dockPanel;
            System.Windows.Controls.Image image;
            TextBlock textBlock;

            currentCountry = App.MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "SystemSettings_Country").FirstOrDefault();
            currentLanguage = App.MainViewModel.ConfigurationModel.Where(x => x.ConfigurationKey == "SystemSettings_Language").FirstOrDefault();

            LanguageListView.Items.Clear();

            var languageList = MasterCodeDataContext.GetMasterCodeData("LanguageSelection");

            foreach ( var language in languageList )
            {
                radioButton = new System.Windows.Controls.RadioButton();
                dockPanel = new DockPanel();
                image = new System.Windows.Controls.Image();
                textBlock = new TextBlock() { Foreground = Brushes.Black };

                var uri = new Uri("pack://application:,,,/Content/Images/Icons/" + language.CodeID+".png");
                var bitmap = new BitmapImage(uri);

                if(language.CodeID == currentLanguage?.ConfigurationValue) { radioButton.IsChecked = true; languageSelected = currentLanguage.ConfigurationValue; }

                radioButton.Tag = language.CodeID;
                radioButton.Checked += new RoutedEventHandler(LanguageSelected_Checked);
                radioButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                dockPanel.Height = 34;
                dockPanel.Margin = new Thickness(0, -6, 0, 0);
                image.Source = bitmap;
                image.Margin = new Thickness(2);
                textBlock.Text = language.CodeName;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                textBlock.Margin = new Thickness(2,0,0,2);
                textBlock.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

                dockPanel.Children.Add(image);
                dockPanel.Children.Add(textBlock);
                radioButton.Content = dockPanel;

                LanguageListView.Items.Add(radioButton);
            }


            List<CountryModel> countryList = sContext.GetCountryData("");

            CountryListView.Items.Clear();

            foreach (CountryModel country in countryList)
            {
                MenuItem item = new MenuItem();
                item.Header = country.CountryName;
                item.Tag = country.CountryCode;
                item.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

                if (country.CountryCode == currentCountry?.ConfigurationValue) { CountryListView.SelectedItem = item; countrySelected = currentCountry.ConfigurationValue; }

                item.BorderThickness = new Thickness(0);
                item.Click += new RoutedEventHandler(CountrySelected_Click);
                CountryListView.Items.Add(item);
            }
        }

        private void LanguageSelected_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = (System.Windows.Controls.RadioButton)sender;

            languageSelected = radioButton.Tag.ToString();

            System.Globalization.CultureInfo sZHCultureNew = new(languageSelected);

            CultureResources.ChangeCulture(sZHCultureNew);

            App.TempChangeLanguageHandler(e, sender);

            LanguageListView.SelectedItems.Clear();

        }

        private void CountrySearchBar_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();
            string partialName = CountrySearchBar.Text;
            List<CountryModel> countryList = null;

            if (partialName.Length > 2)
            {
                CountryListView.Items.Clear();

                countryList = sContext.GetCountryData(partialName);
            }
            else
            {
                CountryListView.Items.Clear();

                countryList = sContext.GetCountryData("");
            }

            foreach (CountryModel country in countryList)
            {
                MenuItem item = new MenuItem();
                item.Header = country.CountryName;
                item.Tag = country.CountryCode;
                item.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

                if (country.CountryCode == countrySelected) { CountryListView.SelectedItem = item; }

                item.BorderThickness = new Thickness(0);
                item.Click += new RoutedEventHandler(CountrySelected_Click);
                CountryListView.Items.Add(item);
            }

        }

        private void CountrySelected_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;

            countrySelected = item.Tag.ToString();

            CountryListView.SelectedItem = item;

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentCountry?.ConfigurationValue == countrySelected && currentLanguage?.ConfigurationValue == languageSelected) 
                {
                    ErrorText.Text = Properties.Resources.General_Message_NoChanges;
                    ErrorText.Visibility = Visibility.Visible; 
                }
                else
                {
                    ErrorText.Visibility = Visibility.Hidden;

                    currentCountry.ConfigurationValueTemp = countrySelected;
                    currentLanguage.ConfigurationValueTemp = languageSelected;

                    App.MainViewModel.Origin = "ChangeLanguageCountry";

                    App.PopupHandler(e, sender);
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = Properties.Resources.General_Message_ErrorOccured;
                ErrorText.Visibility = Visibility.Visible;

                Logger slogger = new Logger();
                slogger.Error("Backend Error >>> ", ex);
            }
        }

        private void UserPage(object sender, RoutedEventArgs e)
        {
            App.GoToSettingUserPageHandler(e, sender);
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingDevicePageHandler(e, sender);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingConfigurationPageHandler(e, sender);
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingReportPageHandler(e, sender);
        }

        private void ClinicInfoPage(object sender, RoutedEventArgs e)
        {
            App.GoToClinicInfoPageHandler(e, sender);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            languageBorder.Height = e.NewSize.Height * 0.8915;
            countryBorder.Height = e.NewSize.Height * 0.8915;
        }
    }
}
