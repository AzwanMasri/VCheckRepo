using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VCheck.Lib.Data;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib;
using VCheckViewer_Others.Lib.Function;

namespace VCheckViewer_Others.Views.Pages.Setting.Device
{
    /// <summary>
    /// Interaction logic for DevicePage.xaml
    /// </summary>
    public partial class DevicePage : Page
    {
        private static List<string> twoWayDeviceTypesID;
        public ObservableCollection<ComboBoxItem> deviceComboList = new ObservableCollection<ComboBoxItem>();
        List<DeviceTypeModel> deviceTypeList = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings());

        public DevicePage()
        {
            InitializeComponent();

            twoWayDeviceTypesID = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings()).Where(x => x.TwoWayCommunication == 1).Select(y => y.id.ToString()).ToList();

            LoadDeviceTypeList();
            LoadDeviceListing();

            this.SizeChanged += MainWindow_SizeChanged;

            DataContext = this;

            generateView();
        }

        private void LoadDeviceTypeList()
        {
            ComboBoxItem selectedItem = new ComboBoxItem
            {
                Content = "--- Please select device type ---",
                Tag = "0"
            };

            deviceComboList.Add(selectedItem);

            List<DeviceTypeModel> deviceList = DeviceRepository.GetDeviceTypeList(ConfigSettings.GetConfigurationSettings());
            if (deviceList != null)
            {
                foreach(var d in deviceList)
                {
                    deviceComboList.Add(new ComboBoxItem
                    {
                        Content = d.TypeName,
                        Tag = d.id
                    });
                }
            }

            if (deviceComboList.Count > 0)
            {
                cboDeviceType.ItemsSource = deviceComboList;
                cboDeviceType.SelectedItem = selectedItem;
            }
        }

        private void LoadDeviceListing()
        {
            var deviceList = DeviceRepository.GetDeviceList(ConfigSettings.GetConfigurationSettings());
            //dgDevice.ItemsSource = deviceList;

            var deviceListString = JsonConvert.SerializeObject(deviceList);
            var deviceListExtended = JsonConvert.DeserializeObject<List<DeviceModelExtended>>(deviceListString);

            foreach (var device in deviceListExtended)
            {
                device.DeviceNameExtended = device.DeviceName + " (" + device.DeviceSerialNo + ")";
            }

            dgDevice.ItemsSource = deviceListExtended;

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            String sDefaultImagePath = "\\Storage\\Device\\Img_F200.png";
            String sSelectedDeviceType = "";
            int sSelectedDeviceTypeID = 0;
            string sIPAddress = "";

            if (cboDeviceType.SelectedItem != null)
            {
                var sSelectedItem = ((ComboBoxItem)cboDeviceType.SelectedItem);
                int iID = Convert.ToInt32(sSelectedItem.Tag);

                var sDeviceTypeObj = DeviceRepository.GetDeviceTypeByID(ConfigSettings.GetConfigurationSettings(), iID);
                if (sDeviceTypeObj != null)
                {
                    sSelectedDeviceType = sDeviceTypeObj.ImageSource;
                    sSelectedDeviceTypeID = sDeviceTypeObj.id;
                }

                if (twoWayDeviceTypesID.Contains(sSelectedItem.Tag.ToString()))
                {
                    sIPAddress = txtIPAddress.Text;
                }
            }
            else
            {
                sSelectedDeviceType = sDefaultImagePath;
            }

            DeviceModel sDeviceObj = new DeviceModel()
            {
                DeviceName = txtName.Text,
                DeviceIPAddress = sIPAddress,
                DeviceImagePath = sSelectedDeviceType,
                status = (int)DataDictionary.DeviceListStatus.Active,
                CreatedDate = DateTime.Now,
                CreatedBy = App.MainViewModel.CurrentUsers.FullName,
                DeviceSerialNo = txtSerialNo.Text,
                DeviceTypeID = sSelectedDeviceTypeID
            };

            Popup sConfirmPopup = new Popup();
            sConfirmPopup.IsOpen = true;

            App.MainViewModel.Origin = "DeviceAdd";
            App.MainViewModel.DeviceModel = sDeviceObj;

            App.PopupHandler(e, sender);
        }

        private void dgDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sRowData = ((DataGrid)sender).SelectedValue as DeviceModel;
            if (sRowData != null)
            {
                ShowHideBorder("View");

                var sDeviceType = DeviceRepository.GetDeviceTypeByID(ConfigSettings.GetConfigurationSettings(), sRowData.DeviceTypeID.Value);

                if (twoWayDeviceTypesID.Contains(sRowData.DeviceTypeID.Value.ToString()))
                {
                    labelIPAddress.Visibility = Visibility.Visible;
                    borderIPView.Visibility = Visibility.Visible;
                }
                else
                {
                    labelIPAddress.Visibility = Visibility.Collapsed;
                    borderIPView.Visibility = Visibility.Collapsed;
                }

                lbName.Text = sRowData.DeviceName;
                lbIPAddeess.Text = sRowData.DeviceIPAddress;
                lbSerialNo.Text = sRowData.DeviceSerialNo;
                lbDeviceType.Text = (sDeviceType != null) ? sDeviceType.TypeName : "";
                hidID.Text = sRowData.id.ToString();
            }
        }

        private void dgDeviceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MandatoryFieldValiation(null);

            var comboBoxSelectedItem = ((ComboBoxItem)((System.Windows.Controls.ComboBox)sender).SelectedItem);
            var sRowDataID = comboBoxSelectedItem != null ? comboBoxSelectedItem.Tag.ToString() : "0";

            if (twoWayDeviceTypesID.Contains(sRowDataID))
            {
                labelIPAddress.Visibility = Visibility.Visible;
                borderIPEdit.Visibility = Visibility.Visible;
            }
            else
            {
                labelIPAddress.Visibility = Visibility.Collapsed;
                borderIPEdit.Visibility = Visibility.Collapsed;
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            ShowHideBorder("Update");

            int iID = Convert.ToInt32(hidID.Text);

            var sDeviceObj = DeviceRepository.GetDeviceByID(iID, ConfigSettings.GetConfigurationSettings());
            if (sDeviceObj != null)
            {
                if (twoWayDeviceTypesID.Contains(sDeviceObj.DeviceTypeID.ToString()))
                {
                    labelIPAddress.Visibility = Visibility.Visible;
                    borderIPEdit.Visibility = Visibility.Visible;
                }
                else
                {
                    labelIPAddress.Visibility = Visibility.Collapsed;
                    borderIPEdit.Visibility = Visibility.Collapsed;
                }

                txtName.Text = sDeviceObj.DeviceName;
                lbName.Text = sDeviceObj.DeviceName;
                txtIPAddress.Text = sDeviceObj.DeviceIPAddress;
                lbIPAddeess.Text = sDeviceObj.DeviceIPAddress;
                txtSerialNo.Text = sDeviceObj.DeviceSerialNo;
                lbSerialNo.Text = sDeviceObj.DeviceSerialNo;
                hidID.Text = sDeviceObj.id.ToString();

                var selectedItem = cboDeviceType.Items.OfType<ComboBoxItem>().FirstOrDefault(x => Convert.ToInt32(x.Tag) == sDeviceObj.DeviceTypeID);
                cboDeviceType.SelectedItem = selectedItem;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(hidID.Text))
            {
                int iID = 0;
                int.TryParse(hidID.Text, out iID);

                var sDeviceObj = DeviceRepository.GetDeviceByID(iID, ConfigSettings.GetConfigurationSettings());
                if (sDeviceObj != null)
                {
                    sDeviceObj.status = 2;
                    sDeviceObj.UpdatedDate = DateTime.Now;
                    sDeviceObj.UpdatedBy = App.MainViewModel.CurrentUsers.FullName;
                }
                
                Popup sConfirmPopup = new Popup();
                sConfirmPopup.IsOpen = true;

                App.MainViewModel.Origin = "DeviceDelete";
                App.MainViewModel.DeviceModel = sDeviceObj;

                App.PopupHandler(e, sender);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ShowHideBorder("View");
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            String sSelectedDeviceType = "";
            String sOldDeviceName = "";
            int iTypeID = 0;

            if (!String.IsNullOrEmpty(hidID.Text))
            {
                int iID = 0;
                int.TryParse(hidID.Text, out iID);

                var sDeviceObj = DeviceRepository.GetDeviceByID(iID, ConfigSettings.GetConfigurationSettings());
                if (sDeviceObj != null)
                {
                    sOldDeviceName = sDeviceObj.DeviceName;

                    if (cboDeviceType.SelectedItem != null)
                    {
                        var sSelectedItem = ((ComboBoxItem)cboDeviceType.SelectedItem);
                        iTypeID = Convert.ToInt32(sSelectedItem.Tag);

                        var sDeviceTypeObj = DeviceRepository.GetDeviceTypeByID(ConfigSettings.GetConfigurationSettings(), iTypeID);
                        if (sDeviceTypeObj != null)
                        {
                            sSelectedDeviceType = sDeviceTypeObj.ImageSource;
                        }

                        if (twoWayDeviceTypesID.Contains(sSelectedItem.Tag.ToString()))
                        {
                            sDeviceObj.DeviceIPAddress = txtIPAddress.Text;
                        }
                    }

                    sDeviceObj.DeviceName = txtName.Text;
                    sDeviceObj.DeviceSerialNo = txtSerialNo.Text;
                    sDeviceObj.UpdatedDate = DateTime.Now;
                    sDeviceObj.UpdatedBy = App.MainViewModel.CurrentUsers.FullName;
                    sDeviceObj.DeviceTypeID = iTypeID;
                    sDeviceObj.DeviceImagePath = sSelectedDeviceType;
                }

                Popup sConfirmPopup = new Popup();
                sConfirmPopup.IsOpen = true;

                App.MainViewModel.Origin = "DeviceUpdate";
                App.MainViewModel.DeviceModel = sDeviceObj;
                App.MainViewModel.OldDeviceName = sOldDeviceName;

                App.PopupHandler(e, sender);
            }
        }

        private void ShowHideBorder(string sType)
        {
            var twoWayDevice = cboDeviceType.SelectedItem != null && twoWayDeviceTypesID.Contains((cboDeviceType.SelectedItem as ComboBoxItem).Tag.ToString());

            if (sType.ToLower() == "view")
            {
                borderNameEdit.Visibility = Visibility.Collapsed;
                borderNameView.Visibility = Visibility.Visible;

                borderSerialNoEdit.Visibility = Visibility.Collapsed;
                borderSerialNoView.Visibility = Visibility.Visible;

                if (twoWayDevice)
                {
                    borderIPEdit.Visibility = Visibility.Collapsed;
                    borderIPView.Visibility = Visibility.Visible;
                }
                else
                {
                    borderIPEdit.Visibility = Visibility.Collapsed;
                    borderIPView.Visibility = Visibility.Collapsed;

                }

                borderDeviceTypeEdit.Visibility = Visibility.Collapsed;
                borderDeviceTypeView.Visibility = Visibility.Visible;

                borderButtonAdd.Visibility = Visibility.Collapsed;
                borderButtonView.Visibility = Visibility.Visible;
                borderButtonUpdate.Visibility = Visibility.Collapsed;

                imgBack.Visibility = Visibility.Visible;
                btnBack.Visibility = Visibility.Visible;

                btnBack.Content = Properties.Resources.Device_Label_Button_BackDevice;
            }
            else if (sType.ToLower() == "update")
            {
                borderNameEdit.Visibility = Visibility.Visible;
                borderNameView.Visibility = Visibility.Collapsed;

                borderSerialNoEdit.Visibility = Visibility.Visible;
                borderSerialNoView.Visibility = Visibility.Collapsed;

                if (twoWayDevice)
                {
                    borderIPEdit.Visibility = Visibility.Visible;
                    borderIPView.Visibility = Visibility.Collapsed;
                }
                else
                {
                    borderIPEdit.Visibility = Visibility.Collapsed;
                    borderIPView.Visibility = Visibility.Collapsed;

                }

                borderDeviceTypeEdit.Visibility = Visibility.Visible;
                borderDeviceTypeView.Visibility = Visibility.Collapsed;

                borderButtonAdd.Visibility = Visibility.Collapsed;
                borderButtonView.Visibility = Visibility.Collapsed;
                borderButtonUpdate.Visibility = Visibility.Visible;

                imgBack.Visibility = Visibility.Visible;
                btnBack.Visibility = Visibility.Visible;
                btnBack.Content = Properties.Resources.Device_Label_Button_BackViewDevice;
            }
            else if (sType.ToLower() == "add")
            {
                cboDeviceType.SelectedItem = null;

                borderNameEdit.Visibility = Visibility.Visible;
                borderNameView.Visibility = Visibility.Collapsed;
                txtName.Text = String.Empty;

                labelIPAddress.Visibility = Visibility.Visible;
                borderIPEdit.Visibility = Visibility.Visible;
                borderIPView.Visibility = Visibility.Collapsed;
                txtIPAddress.Text = String.Empty;

                labelSerialNo.Visibility = Visibility.Visible;
                borderSerialNoEdit.Visibility = Visibility.Visible;
                borderSerialNoView.Visibility = Visibility.Collapsed;
                txtSerialNo.Text = String.Empty;

                borderDeviceTypeEdit.Visibility = Visibility.Visible;
                borderDeviceTypeView.Visibility = Visibility.Collapsed;

                borderButtonAdd.Visibility = Visibility.Visible;
                borderButtonView.Visibility = Visibility.Collapsed;
                borderButtonUpdate.Visibility = Visibility.Collapsed;

                imgBack.Visibility = Visibility.Collapsed;
                btnBack.Visibility = Visibility.Collapsed;

                btnBack.Content = Properties.Resources.Device_Label_Button_BackDevice;
            }
        }


        private void cboDeviceType_LostFocus(object sender, RoutedEventArgs e)
        {
            MandatoryFieldValiation(sender);
        }

        private void FieldsVal_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            MandatoryFieldValiation(sender);
        }

        private void MandatoryFieldValiation(object sender)
        {
            Boolean IsFieldEmpty = false;

            if (cboDeviceType.SelectedItem == null)
            {
                borderDeviceTypeEdit.BorderBrush = System.Windows.Media.Brushes.Red;
                borderDeviceTypeEdit.BorderThickness = new Thickness(1);
                borderDeviceTypeEdit.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;

                IsFieldEmpty = true;
            }
            else if ((cboDeviceType.SelectedItem as ComboBoxItem).Tag.ToString() == "0")
            {
                borderDeviceTypeEdit.BorderBrush = System.Windows.Media.Brushes.Red;
                borderDeviceTypeEdit.BorderThickness = new Thickness(1);
                borderDeviceTypeEdit.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;

                IsFieldEmpty = true;
            }
            else
            {
                borderDeviceTypeEdit.BorderBrush = System.Windows.Media.Brushes.Black;
                borderDeviceTypeEdit.ToolTip = "";
            }

            if (cboDeviceType.SelectedItem != null && (cboDeviceType.SelectedItem as ComboBoxItem).Tag.ToString() != "default" && twoWayDeviceTypesID.Contains((cboDeviceType.SelectedItem as ComboBoxItem).Tag.ToString()))
            {
                String sIPAddress = txtIPAddress.Text;
                if (sIPAddress.Replace(" ", "").Replace(".", "").Trim().Length == 0)
                {
                    borderIPEdit.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderIPEdit.BorderThickness = new Thickness(1);
                    borderIPEdit.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;

                    IsFieldEmpty = true;
                }
                else
                {
                    borderIPEdit.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderIPEdit.ToolTip = null;
                }

                IPAddress iIP;
                if (!IPAddress.TryParse(txtIPAddress.Text.Replace(" ", ""), out iIP) || txtIPAddress.Text.Split(".").Count() != 4)
                {
                    borderIPEdit.BorderBrush = System.Windows.Media.Brushes.Red;
                    borderIPEdit.BorderThickness = new Thickness(1);
                    borderIPEdit.ToolTip = Properties.Resources.Setting_ErrorMessage_InvalidIP;

                    IsFieldEmpty = true;
                }
                else
                {
                    borderIPEdit.BorderBrush = System.Windows.Media.Brushes.Black;
                    borderIPEdit.ToolTip = null;
                }

            }

            if (String.IsNullOrEmpty(txtSerialNo.Text))
            {
                borderSerialNoEdit.BorderBrush = System.Windows.Media.Brushes.Red;
                borderSerialNoEdit.BorderThickness = new Thickness(1);
                borderSerialNoEdit.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;

                IsFieldEmpty = true;
            }
            else
            {
                borderSerialNoEdit.BorderBrush = System.Windows.Media.Brushes.Black;
                borderSerialNoEdit.ToolTip = null;
            }

            var sTxtNameBorder = txtName.Parent as Border;
            if (String.IsNullOrEmpty(txtName.Text))
            {
                sTxtNameBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                sTxtNameBorder.BorderThickness = new Thickness(1);
                sTxtNameBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;

                IsFieldEmpty = true;
            }
            else
            {
                sTxtNameBorder.BorderBrush = System.Windows.Media.Brushes.Black;
                sTxtNameBorder.ToolTip = null;
            }

            String? sColor = System.Windows.Application.Current.Resources["Themes_ButtonColor"].ToString();

            if (IsFieldEmpty)
            {
                if (borderButtonAdd.Visibility != Visibility.Collapsed)
                {
                    btnAdd.IsEnabled = false;
                    btnAdd.Background = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                }
                if (borderButtonUpdate.Visibility != Visibility.Collapsed)
                {
                    btnUpdate.IsEnabled = false;
                }
            }
            else
            {
                if (borderButtonAdd.Visibility != Visibility.Collapsed)
                {
                    btnAdd.IsEnabled = true;
                }
                if (borderButtonUpdate.Visibility != Visibility.Collapsed)
                {
                    btnUpdate.IsEnabled = true;
                }
            };
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

        private void ClinicInfoPage(object sender, RoutedEventArgs e)
        {
            App.GoToClinicInfoPageHandler(e, sender);
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            App.GoToInformationPageHandler(e, sender);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackButton();
        }

        private void imgBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackButton();
        }

        private void NavigateBackButton()
        {
            if (borderNameEdit.Visibility == Visibility.Visible && borderButtonUpdate.Visibility == Visibility.Visible)
            {
                ShowHideBorder("View");
            }
            else if (borderNameEdit.Visibility == Visibility.Collapsed && borderButtonView.Visibility == Visibility.Visible)
            {
                dgDevice.SelectedItem = null;
                ShowHideBorder("Add");
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var windowHeight = App.WindowHeight;

            if (windowHeight > 1016) { deviceBorder.Height = 425; }
            else { deviceBorder.Height = windowHeight * 0.47; }
        }

        public class DeviceModelExtended : DeviceModel
        {
            public string DeviceNameExtended { get; set; }
        }

        private void DownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.UpdateLink) { UseShellExecute = true });
        }

        // Handle hyperlink click
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                //MessageBox.Show("Unable to open link.");
            }
            e.Handled = true;
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

            if (totalElement == 0)
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

        public async Task createElementUsingGridByDeviceType(int totalElementPerRow, int imageHeight, int borderHeight, int borderWidth, int margin, int totalRow, bool excess, int remainder)
        {
            List<DeviceTypeModel> ExistedDevice = new List<DeviceTypeModel>();
            List<DeviceTypeModel> NotExistedDevice = new List<DeviceTypeModel>();

            

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

                    StackPanel secondStackPanel = new StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical };

                    Border childBorder = new Border() { Height = 30, Width = 70, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Visibility = Visibility.Hidden };


                    System.Windows.Controls.Image image = new System.Windows.Controls.Image();
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

                    parentBorder.MouseLeftButtonDown += Border_MouseLeftButtonDown;

                    Grid.SetColumn(parentBorder, j);

                    testGrid.Children.Add(parentBorder);
                }

                if (i == 0) { responsiveView.Children.Clear(); }

                responsiveView.Children.Add(testGrid);
            }
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

            try
            {
                Process.Start(new ProcessStartInfo("https://drive.google.com/drive/u/0/folders/1rkL59xJTrxTEvpZQJy9oA3tT0i9uY-_v") { UseShellExecute = true });
            }
            catch
            {
                //MessageBox.Show("Unable to open link.");
            }
            e.Handled = true;

        }
    }
    
}
