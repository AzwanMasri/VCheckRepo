using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer.Lib.Function;
using RadioButton = System.Windows.Controls.RadioButton;

namespace VCheckViewer.Views.Pages.Setting.Interface
{
    /// <summary>
    /// Interaction logic for ConfigurationPage.xaml
    /// </summary>
    public partial class ConfigurationPage : Page
    {
        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());
        public String sIPConfigKey = "InterfaceSettingsIP";
        public String sPortNoConfigKey = "InterfaceSettingsPortNo";
        public String sUsernameConfigKey = "InterfaceSettingsUsername";
        public String sPasswordConfigKey = "InterfaceSettingsPassword";
        public String sPMSConfigKey = "InterfaceSettingsPMS";
        public String sClinicIDConfigKey = "ClinicID";
        public String sClinicPhoneNumConfigKey = "ClinicPhoneNum";

        public string PMSURL = "";
        public string ClinicID = "";
        public string CurrentLIS = "";

        public static event EventHandler? ConnectionStatus;

        public ConfigurationPage()
        {
            InitializeComponent();
            GetPMSURLAsync(2);


            ConnectionStatus = null;
            ConnectionStatus += ConnectionStatusReload;
        }

        private void CheckPMSSelected()
        {
            var sInterfacePMS = configDBContext.GetConfigurationData(sPMSConfigKey).FirstOrDefault();
            if (sInterfacePMS != null)
            {
                CurrentLIS = sInterfacePMS.ConfigurationValue.ToString();

                if (CurrentLIS == "Greywind")
                {
                    Greywind.IsChecked = true;
                }
                else if (CurrentLIS == "Other")
                {
                    Other.IsChecked = true;
                }
                else
                {
                    None.IsChecked = true;
                }
            }
            else
            {
                None.IsChecked = true;
            }
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

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingReportPageHandler(e, sender);
        }

        private void ClinicInfoPage(object sender, RoutedEventArgs e)
        {
            App.GoToClinicInfoPageHandler(e, sender);
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            App.GoToInformationPageHandler(e, sender);
        }

        private void NumberValidationOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void FieldsVal_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Boolean isFieldEmpty = false;

            if (Other.IsChecked.GetValueOrDefault())
            {
                String sIP = txtIP.Text;
                if (sIP.Replace(" ", "").Replace(".", "").Trim().Length == 0)
                {
                    borderIP.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderIP.BorderThickness = new Thickness(1);
                    borderIP.ToolTip = "This is a mandatory field";

                    isFieldEmpty = true;
                }
                else
                {
                    borderIP.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderIP.ToolTip = null;
                }

                if (String.IsNullOrEmpty(txtPortNo.Text) || int.Parse(txtPortNo.Text) == 0)
                {
                    borderPortNo.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderPortNo.BorderThickness = new Thickness(1);
                    borderPortNo.ToolTip = "This is a mandatory field.";

                    isFieldEmpty = true;
                }
                else
                {
                    borderPortNo.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderPortNo.ToolTip = null;
                }

                if (String.IsNullOrEmpty(txtUsername.Text))
                {
                    borderUsername.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderUsername.BorderThickness = new Thickness(1);
                    borderUsername.ToolTip = "This is a mandatory field.";

                    isFieldEmpty = true;
                }
                else
                {
                    borderUsername.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderUsername.ToolTip = null;
                }

                if (String.IsNullOrEmpty(txtPassword.Password))
                {
                    borderPassword.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderPassword.BorderThickness = new Thickness(1);
                    borderPassword.ToolTip = "This is a mandatory field.";

                    isFieldEmpty = true;
                }
                else
                {
                    borderPassword.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderPassword.ToolTip = null;
                }

                if(CurrentLIS == "Other")
                {
                    btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
                    btnConnect.Tag = "Connected";
                    btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

                    btnUpdate.IsEnabled = false;
                }
                else
                {
                    btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                    btnConnect.Tag = "Not Connected";
                    btnConnect.Background = System.Windows.Media.Brushes.Gray;
                }
            }
            else if(Greywind.IsChecked.GetValueOrDefault())
            {
                if (txtIP.Text.Replace(" ", "").Replace(".", "").Trim().Length == 0)
                {
                    isFieldEmpty = true;
                }

                if (string.IsNullOrEmpty(txtClinicPhoneNum.Text))
                {
                    borderClinicPhoneNum.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderClinicPhoneNum.BorderThickness = new Thickness(1);
                    borderClinicPhoneNum.ToolTip = "Please create clinic information first.";
                    isFieldEmpty = true;
                }
                else
                {
                    borderClinicPhoneNum.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderClinicPhoneNum.ToolTip = null;
                }

                borderIP.BorderBrush = System.Windows.Media.Brushes.Black;
                borderIP.ToolTip = null;
                borderPortNo.BorderBrush = System.Windows.Media.Brushes.Black;
                borderPortNo.ToolTip = null;
                borderUsername.BorderBrush = System.Windows.Media.Brushes.Black;
                borderUsername.ToolTip = null;
                borderPassword.BorderBrush = System.Windows.Media.Brushes.Black;
                borderPassword.ToolTip = null;
            }
            else
            {
                btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                btnConnect.Tag = "Not Connected";
                btnConnect.Background = System.Windows.Media.Brushes.Gray;

                if (CurrentLIS == "None")
                {
                    isFieldEmpty = true;
                }
                else
                {
                    isFieldEmpty = false;
                }
            }

            if (btnConnect.Tag.ToString() == "Connected") { isFieldEmpty = true; }


            if (isFieldEmpty)
            {
                btnUpdate.IsEnabled = false;
            }
            else
            {
                btnUpdate.IsEnabled = true;
            }
        }

        private async void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            //if ((Greywind.IsChecked.GetValueOrDefault() && await ConnectToPIMS(e, null)) || Other.IsChecked.GetValueOrDefault())
            //{
            //    try
            //    {
            //        List<ConfigurationModel> sConfigList = new List<ConfigurationModel>();

            //        if (!Greywind.IsChecked.GetValueOrDefault())
            //        {
            //            sConfigList.Add(new ConfigurationModel
            //            {
            //                ConfigurationKey = sIPConfigKey,
            //                ConfigurationValue = txtIP.Text
            //            });

            //            sConfigList.Add(new ConfigurationModel
            //            {
            //                ConfigurationKey = sPortNoConfigKey,
            //                ConfigurationValue = int.Parse(txtPortNo.Text).ToString()
            //            });

            //            sConfigList.Add(new ConfigurationModel
            //            {
            //                ConfigurationKey = sUsernameConfigKey,
            //                ConfigurationValue = txtUsername.Text
            //            });

            //            sConfigList.Add(new ConfigurationModel
            //            {
            //                ConfigurationKey = sPasswordConfigKey,
            //                ConfigurationValue = txtPassword.Password
            //            });
            //        }

            //        var selectedPMS = gridPMSSelection.Children.OfType<RadioButton>().FirstOrDefault(x => x.IsChecked.GetValueOrDefault()).Name;
            //        sConfigList.Add(new ConfigurationModel
            //        {
            //            ConfigurationKey = sPMSConfigKey,
            //            ConfigurationValue = selectedPMS
            //        });

            //        Popup sConfirmPopup = new Popup();
            //        sConfirmPopup.IsOpen = true;

            //        //App.MainViewModel.Origin = "SettingsUpdate";
            //        App.MainViewModel.Origin = "LISSettingsUpdate";
            //        App.MainViewModel.ConfigurationModel = sConfigList;

            //        App.PopupHandler(e, sender);

            //        //btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
            //        //btnConnect.Tag = "Connected";
            //        //btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

            //        btnUpdate.IsEnabled = false;
            //    }
            //    catch (Exception ex)
            //    {
            //        App.MainViewModel.Origin = "FailedUpdateLIS";
            //        App.PopupHandler(e, sender);

            //        btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
            //        btnConnect.Tag = "Not Connected";
            //        btnConnect.Background = System.Windows.Media.Brushes.Gray;
            //    }
            //}

            try
            {
                List<ConfigurationModel> sConfigList = new List<ConfigurationModel>();

                if (Other.IsChecked.GetValueOrDefault())
                {
                    sConfigList.Add(new ConfigurationModel
                    {
                        ConfigurationKey = sIPConfigKey,
                        ConfigurationValue = txtIP.Text
                    });

                    sConfigList.Add(new ConfigurationModel
                    {
                        ConfigurationKey = sPortNoConfigKey,
                        ConfigurationValue = int.Parse(txtPortNo.Text).ToString()
                    });

                    sConfigList.Add(new ConfigurationModel
                    {
                        ConfigurationKey = sUsernameConfigKey,
                        ConfigurationValue = txtUsername.Text
                    });

                    sConfigList.Add(new ConfigurationModel
                    {
                        ConfigurationKey = sPasswordConfigKey,
                        ConfigurationValue = txtPassword.Password
                    });
                }

                var selectedPMS = gridPMSSelection.Children.OfType<RadioButton>().FirstOrDefault(x => x.IsChecked.GetValueOrDefault()).Name;
                sConfigList.Add(new ConfigurationModel
                {
                    ConfigurationKey = sPMSConfigKey,
                    ConfigurationValue = selectedPMS
                });

                Popup sConfirmPopup = new Popup();
                sConfirmPopup.IsOpen = true;

                //App.MainViewModel.Origin = "SettingsUpdate";
                App.MainViewModel.Origin = "LISSettingsUpdate";
                App.MainViewModel.ConfigurationModel = sConfigList;

                App.PopupHandler(e, sender);
            }
            catch (Exception ex)
            {
                App.MainViewModel.Origin = "FailedUpdateLIS";
                App.PopupHandler(e, sender);
            }

        }

        private async void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton selected = sender as RadioButton;

            if (selected.Name == "Greywind")
            {
                btnConnect.Visibility = Visibility.Visible;
                borderIPLabel.Visibility = Visibility.Visible;
                borderIP.Visibility = Visibility.Visible;
                borderPortNoLabel.Visibility = Visibility.Visible;
                borderPortNo.Visibility = Visibility.Visible;
                ClinicPhoneNumLabel.Visibility = Visibility.Visible;
                UsernameLabel.Visibility = Visibility.Collapsed;
                PasswordLabel.Visibility = Visibility.Collapsed;
                borderClinicPhoneNum.Visibility = Visibility.Visible;
                borderUsername.Visibility = Visibility.Collapsed;
                borderPassword.Visibility = Visibility.Collapsed;
                NoneLabel.Visibility = Visibility.Collapsed;

                if (CurrentLIS == "Greywind")
                {
                    await CheckGreywindConnection();
                }
                else
                {
                    btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                    btnConnect.Tag = "Not Connected";
                    btnConnect.Background = System.Windows.Media.Brushes.Gray;
                }

                var sClinicPhoneNum = configDBContext.GetConfigurationData(sClinicPhoneNumConfigKey).FirstOrDefault();
                if (sClinicPhoneNum != null)
                {
                    txtClinicPhoneNum.Text = "+" + sClinicPhoneNum.ConfigurationValue.ToString();
                }
                else { txtClinicPhoneNum.Text = ""; }

                txtIP.Text = PMSURL;
                txtPortNo.Text = "443";
                txtUsername.Text = "svrtech";
                txtPassword.Password = "V7FZ+fu9sQ9*";
                txtIP.IsEnabled = false;
                txtPortNo.IsEnabled = false;
            }
            else if(selected.Name == "Other")
            {
                btnConnect.Visibility = Visibility.Visible;
                borderIPLabel.Visibility = Visibility.Visible;
                borderIP.Visibility = Visibility.Visible;
                borderPortNoLabel.Visibility = Visibility.Visible;
                borderPortNo.Visibility = Visibility.Visible;
                txtIP.IsEnabled = true;
                txtPortNo.IsEnabled = true;
                ClinicPhoneNumLabel.Visibility = Visibility.Collapsed;
                UsernameLabel.Visibility = Visibility.Visible;
                PasswordLabel.Visibility = Visibility.Visible;
                borderClinicPhoneNum.Visibility = Visibility.Collapsed;
                borderUsername.Visibility = Visibility.Visible;
                borderPassword.Visibility = Visibility.Visible;
                NoneLabel.Visibility = Visibility.Collapsed;

                var sInterfaceIP = configDBContext.GetConfigurationData(sIPConfigKey).FirstOrDefault();
                if (sInterfaceIP != null)
                {
                    txtIP.Text = sInterfaceIP.ConfigurationValue.ToString();
                }
                else { txtIP.Text = ""; }

                var sInterfacePortNo = configDBContext.GetConfigurationData(sPortNoConfigKey).FirstOrDefault();
                if (sInterfacePortNo != null)
                {
                    txtPortNo.Text = sInterfacePortNo.ConfigurationValue.ToString();
                }
                else { txtPortNo.Text = ""; }

                var sInterfaceUsername = configDBContext.GetConfigurationData(sUsernameConfigKey).FirstOrDefault();
                if (sInterfaceUsername != null)
                {
                    txtUsername.Text = sInterfaceUsername.ConfigurationValue.ToString();
                }
                else { txtUsername.Text = ""; }

                var sInterfacePassword = configDBContext.GetConfigurationData(sPasswordConfigKey).FirstOrDefault();
                if (sInterfacePassword != null)
                {
                    txtPassword.Password = sInterfacePassword.ConfigurationValue.ToString();
                }
                else { txtPassword.Password = ""; }
            }
            else
            {
                btnConnect.Visibility = Visibility.Collapsed;
                borderIPLabel.Visibility = Visibility.Collapsed;
                borderIP.Visibility = Visibility.Collapsed;
                borderPortNoLabel.Visibility = Visibility.Collapsed;
                borderPortNo.Visibility = Visibility.Collapsed;
                ClinicPhoneNumLabel.Visibility = Visibility.Collapsed;
                UsernameLabel.Visibility = Visibility.Collapsed;
                PasswordLabel.Visibility = Visibility.Collapsed;
                borderClinicPhoneNum.Visibility = Visibility.Collapsed;
                borderUsername.Visibility = Visibility.Collapsed;
                borderPassword.Visibility = Visibility.Collapsed;
                NoneLabel.Visibility = Visibility.Collapsed;
                NoneLabel.Visibility = Visibility.Visible;
            }

            FieldsVal_KeyUp(null, null);
        }

        public async Task GetPMSURLAsync(int clientID)
        {
            VCheckAPI VcheckAPI = new VCheckAPI();
            PMSURL =  await VcheckAPI.GetPMSUrl(clientID);
            

            if (string.IsNullOrEmpty(PMSURL)) { Greywind.IsEnabled = false; }
            else { Greywind.IsEnabled = true; txtIP.Text = PMSURL; }

            CheckPMSSelected();
        }

        private async Task<bool> ConnectToPIMS(object sender, RoutedEventArgs? e)
        {
            var success = await CreateClinicInfoPMS();

            if (success)
            {
                App.MainViewModel.Origin = "GreywindConnected";
                App.PopupHandler(e, sender);

                btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
                btnConnect.Tag = "Connected";
                btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

                btnUpdate.IsEnabled = false;
            }
            else
            {
                App.MainViewModel.Origin = "FailedGreywindConnect";
                App.PopupHandler(e, sender);

                btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                btnConnect.Tag = "Not Connected";
                btnConnect.Background = System.Windows.Media.Brushes.Gray;
            }

            return success;
        }

        private async Task CheckGreywindConnection()
        {
            var sClinicID = configDBContext.GetConfigurationData(sClinicIDConfigKey).FirstOrDefault();
            var sClinicPhoneNum = configDBContext.GetConfigurationData(sClinicPhoneNumConfigKey).FirstOrDefault();
            var hasClinicID = sClinicID != null && !string.IsNullOrEmpty(sClinicID.ConfigurationValue);
            var noPhoneNumber = sClinicPhoneNum == null;
            bool disableConnect = false;
            bool cannotConnect = false;

            if (hasClinicID)
            {
                ClinicID = sClinicID.ConfigurationValue.ToString();

                disableConnect = await CheckGreywindClinic();
            }
            else if(noPhoneNumber)
            {
                cannotConnect = true;
            }

            if (cannotConnect)
            {
                btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                btnConnect.Tag = "Not Connected";
                btnConnect.Background = System.Windows.Media.Brushes.Gray;
            }
            else if (disableConnect)
            {
                btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
                btnConnect.Tag = "Connected";
                btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

                btnUpdate.IsEnabled = false;
            }
        }

        private async Task<bool> CheckGreywindClinic()
        {
            GreywindAPI sAPI = new GreywindAPI();

            return await sAPI.ClinicExist(ClinicID, PMSURL);
        }

        private async Task<bool> CreateClinicInfoPMS()
        {
            var sSettingsObj = configDBContext.GetConfigurationData("");
            var clinic = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicID");
            var ClinicIDNotExist = true;
            var success = false;

            if (clinic != null)
            {
                ClinicID = clinic.ConfigurationValue;
                ClinicIDNotExist = false;
                success = true;
            }
            else
            {
                ClinicID = await CreateClinicInfoVcheck(sSettingsObj);
            }

            if (string.IsNullOrEmpty(ClinicID))
            {
                return false;
            }
            else
            {
                if (ClinicIDNotExist)
                {
                    configDBContext.AddConfiguration("ClinicID", ClinicID);
                }

                if (Greywind.IsChecked.GetValueOrDefault())
                {
                    success = await CheckGreywindClinic();
                }
                else { return true; }

                if (success)
                {
                    return true;
                }
                else
                {
                    if (!ClinicIDNotExist)
                    {
                        ClinicID = await CreateClinicInfoVcheck(sSettingsObj, ClinicID);

                        success = await CheckGreywindClinic();
                    }

                    if (!string.IsNullOrEmpty(ClinicID) && !success)
                    {
                        success = await CreateClinicInfoGW(sSettingsObj);
                    }
                    else if (!string.IsNullOrEmpty(ClinicID) && success)
                    {
                        configDBContext.UpdateConfiguration("ClinicID", ClinicID);
                    }

                    return success;
                }
            }
        }

        public async Task<String> CreateClinicInfoVcheck(List<ConfigurationModel> sSettingsObj, string ClinicID = "")
        {
            VCheckAPI vcheckAPI = new VCheckAPI();

            var name = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicName").ConfigurationValue;
            var address = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicAddress").ConfigurationValue;
            var city = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicCity").ConfigurationValue;
            var state = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicState").ConfigurationValue;
            var phoneNum = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicPhoneNum").ConfigurationValue;

            VCheck.Interface.API.Lib.General.LocationModel location = new VCheck.Interface.API.Lib.General.LocationModel()
            {
                ID = ClinicID,
                Name = name,
                Address = address,
                ContactName = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicContactName").ConfigurationValue,
                PhoneNum = phoneNum,
                Email = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicEmail").ConfigurationValue,
                Description = "Clinic",
                Status = 1,
                CreatedBy = Greywind.IsChecked.GetValueOrDefault() ? "Greywind" : "Others"
            };

            return await vcheckAPI.UpdateLocation(location);
        }

        public async Task<bool> CreateClinicInfoGW(List<ConfigurationModel> sSettingsObj)
        {
            GreywindAPI greywindAPI = new GreywindAPI();

            var name = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicName").ConfigurationValue;
            var address = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicAddress").ConfigurationValue;
            var city = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicCity").ConfigurationValue;
            var state = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicState").ConfigurationValue;
            var phoneNum = sSettingsObj.FirstOrDefault(x => x.ConfigurationKey == "ClinicPhoneNum").ConfigurationValue;

            VCheck.Interface.API.Greywind.RequestMessage.InsertLocationRequest insertLocation = new VCheck.Interface.API.Greywind.RequestMessage.InsertLocationRequest()
            {
                clinic_id = ClinicID,
                name = name,
                address_1 = address,
                city = city,
                state = state,
                phone = phoneNum,
                api_access = "1"
            };

            return await greywindAPI.InsertClinicInfo(insertLocation, PMSURL);
        }

        public static void ConnectionStatusHandler(EventArgs e, object sender)
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus(sender, e);
            }
        }

        public async void ConnectionStatusReload(object sender, EventArgs e)
        {
            if (Greywind.IsChecked.GetValueOrDefault() || Other.IsChecked.GetValueOrDefault())
            {
                if (await ConnectToPIMS(sender, null))
                {
                    btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
                    btnConnect.Tag = "Connected";
                    btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

                    btnUpdate.IsEnabled = false;
                }
                else
                {
                    btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                    btnConnect.Tag = "Not Connected";
                    btnConnect.Background = System.Windows.Media.Brushes.Gray;
                }

                CurrentLIS = Greywind.IsChecked.GetValueOrDefault() ? "Greywind" : "Other";
            }
            //else if (Other.IsChecked.GetValueOrDefault())
            //{
            //    App.MainViewModel.Origin = "GreywindConnected";
            //    App.PopupHandler(e, sender);

            //    btnConnect.Content = Properties.Resources.Maintenance_Label_Connected;
            //    btnConnect.Tag = "Connected";
            //    btnConnect.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#0ed145");

            //    btnUpdate.IsEnabled = false;

            //    CurrentLIS = "Other";
            //}
            else
            {
                //App.MainViewModel.Origin = "SettingsUpdateCompleted";
                //App.PopupHandler(e, sender);

                btnConnect.Content = Properties.Resources.Maintenance_Label_NotConnected;
                btnConnect.Tag = "Not Connected";
                btnConnect.Background = System.Windows.Media.Brushes.Gray;

                btnUpdate.IsEnabled = false;

                CurrentLIS = "None";
            }

        }

        private void DownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.UpdateLink) { UseShellExecute = true });
        }
    }
}
