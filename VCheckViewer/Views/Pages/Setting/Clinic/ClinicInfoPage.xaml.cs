using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Lib.Function;

namespace VCheckViewer.Views.Pages.Setting.Clinic
{
    /// <summary>
    /// Interaction logic for ClinicInfoPage.xaml
    /// </summary>
    public partial class ClinicInfoPage : System.Windows.Controls.Page
    {

        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public ObservableCollection<ComboBoxItem> cbCountryPhoneNum { get; set; }
        public ObservableCollection<ComboBoxItem> cbCountryPhoneNumSearch { get; set; }
        public ComboBoxItem SelectedcbCountryPhoneNum { get; set; }
        public ComboBoxItem SelectedcbCountryPhoneNumSearch { get; set; }

        public (bool ClinicExist, string PhoneNo) ClinicExistInfo = (false, "");

        public ClinicInfoPage()
        {
            InitializeComponent();
            DataContext = this;

            LoadClinicInfo();

            this.SizeChanged += MainWindow_SizeChanged;

            btnUpdate.IsEnabled = false;
        }

        private void LoadClinicInfo()
        {
            var sClinicName = configDBContext.GetConfigurationData("ClinicName").FirstOrDefault();
            var sClinicAddress = configDBContext.GetConfigurationData("ClinicAddress").FirstOrDefault();
            var sClinicPhoneNum = configDBContext.GetConfigurationData("ClinicPhoneNum").FirstOrDefault();
            var sClinicCity = configDBContext.GetConfigurationData("ClinicCity").FirstOrDefault();
            var sClinicState = configDBContext.GetConfigurationData("ClinicState").FirstOrDefault();
            var sClinicContactName = configDBContext.GetConfigurationData("ClinicContactName").FirstOrDefault();
            var sClinicEmail = configDBContext.GetConfigurationData("ClinicEmail").FirstOrDefault();

            var phoneNumberObject = sClinicPhoneNum != null ? sClinicPhoneNum.ConfigurationValue.Split(" ") : Array.Empty<string>();
            var phoneNumber = phoneNumberObject.Count() == 0 ? "" : phoneNumberObject[1];
            var countryCode = phoneNumberObject.Count() == 0 ? "" : phoneNumberObject[0];

            ClinicName.Text = sClinicName != null ? sClinicName.ConfigurationValue : "";
            ClinicAddress.Text = sClinicAddress != null ? sClinicAddress.ConfigurationValue : "";
            ClinicCity.Text = sClinicCity != null ? sClinicCity.ConfigurationValue : "";
            ClinicState.Text = sClinicState != null ? sClinicState.ConfigurationValue : "";
            ClinicPhoneNum.Text = phoneNumber;
            ClinicContactName.Text = sClinicContactName != null ? sClinicContactName.ConfigurationValue : "";
            ClinicEmail.Text = sClinicEmail != null ? sClinicEmail.ConfigurationValue : "";

            cbCountryPhoneNum = App.MainViewModel.cbCountryPhoneNum;
            cbCountryPhoneNumSearch = App.MainViewModel.cbCountryPhoneNumSearch;
            SelectedcbCountryPhoneNum = cbCountryPhoneNum.Where(a => (string)a.Tag == countryCode).FirstOrDefault();
            ClinicCountryCodeSearch.SelectedIndex = 0;

            checker();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            List<ConfigurationModel> sConfigList = new List<ConfigurationModel>();

            ComboBoxItem selectedItem = (ComboBoxItem)ClinicCountryCode.SelectedItem;
            var clinicPhoneNo = selectedItem.Tag + " " + ClinicPhoneNum.Text;

            //if (ClinicExistInfo.ClinicExist && ClinicExistInfo.PhoneNo == clinicPhoneNo)
            //{
            //    sConfigList.Add(new ConfigurationModel
            //    {
            //        ConfigurationKey = "ClinicID",
            //        ConfigurationValue = clinicPhoneNo.Replace(" ", "").Replace("-", "")
            //    });
            //}

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicName",
                ConfigurationValue = ClinicName.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicAddress",
                ConfigurationValue = ClinicAddress.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicCity",
                ConfigurationValue = ClinicCity.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicState",
                ConfigurationValue = ClinicState.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicPhoneNum",
                ConfigurationValue = selectedItem.Tag + " " + ClinicPhoneNum.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicEmail",
                ConfigurationValue = ClinicEmail.Text
            });

            sConfigList.Add(new ConfigurationModel
            {
                ConfigurationKey = "ClinicContactName",
                ConfigurationValue = ClinicContactName.Text
            });

            App.MainViewModel.Origin = "ClinicInfoUpdate";
            App.MainViewModel.ConfigurationModel = sConfigList;

            App.PopupHandler(e, sender);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            GetClinicInfo();
        }

        private async Task GetClinicInfo()
        {
            var clinicInfoString = "";
            ComboBoxItem selectedItem = (ComboBoxItem)ClinicCountryCodeSearch.SelectedItem;
            var ClinicPhoneNo = selectedItem.Tag + " " + ClinicPhoneNumSearch.Text;
            var ClinicID = ClinicPhoneNo.Replace(" ", "").Replace("-", "");
            var sPMS = configDBContext.GetConfigurationData("InterfaceSettingsPMS").FirstOrDefault();
            VCheckAPI VcheckAPI = new VCheckAPI();
            bool isGreywind = false;

            //GreywindAPI sAPI = new GreywindAPI();
            //var PMSURL = await VcheckAPI.GetPMSUrl(2);

            //var clinicInfoString = await sAPI.GetClinicInfo(ClinicID, PMSURL);

            if (sPMS != null && sPMS.ConfigurationValue == "Greywind")
            {
                GreywindAPI sAPI = new GreywindAPI();
                var PMSURL = await VcheckAPI.GetPMSUrl(2);

                clinicInfoString = await sAPI.GetClinicInfo(ClinicID, PMSURL);
                isGreywind = true;
            }
            else
            {
                clinicInfoString = await VcheckAPI.GetLocation(ClinicID);
            }

            if (string.IsNullOrEmpty(clinicInfoString))
            {
                App.MainViewModel.Origin = "NoClinicFound";

                App.PopupHandler(null, null);
                ClinicExistInfo = (false,"");
            }
            else
            {
                if (isGreywind)
                {
                    var ClinicInfo = JsonConvert.DeserializeObject<VCheck.Interface.API.Greywind.RequestMessage.InsertLocationRequest>(clinicInfoString);

                    ClinicName.Text = ClinicInfo.name_1;
                    ClinicAddress.Text = ClinicInfo.address_1;
                    ClinicCity.Text = ClinicInfo.city;
                    ClinicState.Text = ClinicInfo.state;
                    ClinicPhoneNum.Text = ClinicPhoneNumSearch.Text;

                }
                else
                {
                    var ClinicInfo = JsonConvert.DeserializeObject<List<LocationResultObject>>(clinicInfoString).FirstOrDefault();

                    ClinicName.Text = ClinicInfo.name;
                    ClinicAddress.Text = ClinicInfo.address;
                    //ClinicCity.Text = ClinicInfo.city;
                    //ClinicState.Text = ClinicInfo.state;
                }

                ClinicPhoneNum.Text = ClinicPhoneNumSearch.Text;
                cbCountryPhoneNum = App.MainViewModel.cbCountryPhoneNum;
                ClinicCountryCode.SelectedItem = cbCountryPhoneNum.Where(a => (string)a.Tag == selectedItem.Tag.ToString()).FirstOrDefault();
                checker();

                ClinicExistInfo = (true, ClinicPhoneNo);
            }
        }


        private void FieldsVal_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            checker();
        }


        private void LanguageCountry(object sender, RoutedEventArgs e)
        {
            App.GoToSettingLanguageCountryPageHandler(e, sender);
        }

        private void UserPage(object sender, RoutedEventArgs e)
        {
            App.GoToSettingUserPageHandler(e, sender);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingConfigurationPageHandler(e, sender);
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingReportPageHandler(e, sender);
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingDevicePageHandler(e, sender);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClinicAddress.MaxHeight = e.NewSize.Height * 0.156;
        }

        private void ClinicCountryCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClinicPhoneNum != null)
            {
                checker();
            }
        }

        private void checker()
        {
            Boolean isFieldEmpty = false;

            if (String.IsNullOrEmpty(ClinicName.Text))
            {
                borderClinicName.BorderBrush = System.Windows.Media.Brushes.Red;
                borderClinicName.BorderThickness = new Thickness(1);
                borderClinicName.ToolTip = "This is a mandatory field.";

                isFieldEmpty = true;
            }
            else
            {
                borderClinicName.BorderBrush = System.Windows.Media.Brushes.Black;
                borderClinicName.ToolTip = null;
            }

            //if (String.IsNullOrEmpty(ClinicContactName.Text))
            //{
            //    borderClinicContactName.BorderBrush = System.Windows.Media.Brushes.Red;
            //    borderClinicContactName.BorderThickness = new Thickness(1);
            //    borderClinicContactName.ToolTip = "This is a mandatory field.";

            //    isFieldEmpty = true;
            //}
            //else
            //{
            //    borderClinicContactName.BorderBrush = System.Windows.Media.Brushes.Black;
            //    borderClinicContactName.ToolTip = null;
            //}

            if (String.IsNullOrEmpty(ClinicAddress.Text))
            {
                borderClinicAddress.BorderBrush = System.Windows.Media.Brushes.Red;
                borderClinicAddress.BorderThickness = new Thickness(1);
                borderClinicAddress.ToolTip = "This is a mandatory field.";

                isFieldEmpty = true;
            }
            else
            {
                borderClinicAddress.BorderBrush = System.Windows.Media.Brushes.Black;
                borderClinicAddress.ToolTip = null;
            }

            if (String.IsNullOrEmpty(ClinicPhoneNum.Text) || ClinicCountryCode.SelectedIndex == 0)
            {
                borderClinicPhoneNum.BorderBrush = System.Windows.Media.Brushes.Red;
                borderClinicPhoneNum.BorderThickness = new Thickness(1);
                borderClinicPhoneNum.ToolTip = "This is a mandatory field.";

                isFieldEmpty = true;
            }
            else
            {
                borderClinicPhoneNum.BorderBrush = System.Windows.Media.Brushes.Black;
                borderClinicPhoneNum.ToolTip = null;


                var selectedCountryCode = ClinicCountryCode.SelectedItem;
            }

            //if (String.IsNullOrEmpty(ClinicEmail.Text) || !ClinicEmail.Text.Contains("@") || !ClinicEmail.Text.Contains("."))
            //{
            //    borderClinicEmail.BorderBrush = System.Windows.Media.Brushes.Red;
            //    borderClinicEmail.BorderThickness = new Thickness(1);
            //    borderClinicEmail.ToolTip = "This is a mandatory field.";

            //    isFieldEmpty = true;
            //}
            //else
            //{
            //    borderClinicEmail.BorderBrush = System.Windows.Media.Brushes.Black;
            //    borderClinicEmail.ToolTip = null;
            //}


            if (isFieldEmpty)
            {
                btnUpdate.IsEnabled = false;
            }
            else
            {
                btnUpdate.IsEnabled = true;
            }
        }

        private bool IsTextNumeric(string text)
        {
            return Regex.IsMatch(text, @"^\d+$"); // Only digits allowed
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string pastedText = (string)e.DataObject.GetData(System.Windows.DataFormats.Text);
                if (!IsTextNumeric(pastedText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
