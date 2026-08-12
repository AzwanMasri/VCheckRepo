using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VCheck.Lib.Data.Models;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Brushes = System.Windows.Media.Brushes;

namespace VCheckViewer_Others.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddUserPage.xaml
    /// </summary>
    public partial class AddUserPage : Page
    {
        public ObservableCollection<ComboBoxItem> cbTitle { get; set; }
        public ObservableCollection<ComboBoxItem> cbGender { get; set; }
        public ObservableCollection<ComboBoxItem> cbRoles { get; set; }
        public ObservableCollection<ComboBoxItem> cbStatus { get; set; }
        public ComboBoxItem SelectedcbTitle { get; set; }
        public ComboBoxItem SelectedcbGender { get; set; }
        public ComboBoxItem SelectedcbRoles { get; set; }
        public ComboBoxItem SelectedcbStatus { get; set; }


        public AddUserPage()
        {
            InitializeComponent();
            DataContext = this;

            cbTitle = App.MainViewModel.cbTitle;
            cbGender = App.MainViewModel.cbGender;
            cbRoles = App.MainViewModel.cbRoles;
            cbStatus = App.MainViewModel.cbStatus;

            SelectedcbTitle = cbTitle.FirstOrDefault();
            SelectedcbGender = cbGender.FirstOrDefault();
            SelectedcbRoles = cbRoles.FirstOrDefault();
            SelectedcbStatus = cbStatus.Where(x => x.Content.ToString() == "Active").FirstOrDefault();

            UserPage.DataContext = App.MainViewModel;

            App.MainViewModel.BackButtonText = Properties.Resources.Setting_Label_UserBackButton;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoPreviousPageHandler(e, sender);
        }

        private void CheckValueExist(object sender, RoutedEventArgs e)
        {
            CheckValue(sender);
        }

        private void CheckValue_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CheckValue(sender);
        }

        private void CheckValue(Object element)
        {
            ComboBox comboBox = null;
            DatePicker datePicker = null;
            TextBox textBox = null;
            Border parent;
            DateTime temp;

            if (element.GetType() == typeof(ComboBox)) { comboBox = element as ComboBox; parent = comboBox.Parent as Border; }
            else if (element.GetType() == typeof(TextBox)) { textBox = element as TextBox; parent = textBox.Parent as Border; }
            else { datePicker = element as DatePicker; parent = datePicker.Parent as Border; }



            if ((textBox != null && textBox.Text == "") || (comboBox != null && comboBox.Text == "") || (datePicker != null && datePicker.Text == ""))
            {
                parent.BorderBrush = Brushes.Red;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;
                CheckAllValueExisted();
            }
            else if (textBox != null && textBox.Name == "EmailAddress" && (!textBox.Text.Contains("@") || !textBox.Text.Contains(".")))
            {
                parent.BorderBrush = Brushes.Red;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = Properties.Resources.Setting_ErrorMessage_EmailFormat;
                CheckAllValueExisted();
            }
            else if (textBox != null && textBox.Name == "FullName" && textBox.Text.Length < 2)
            {
                parent.BorderBrush = Brushes.Red;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = Properties.Resources.Setting_ErrorMessage_TwoCharMin;
                CheckAllValueExisted();
            }
            else if (textBox != null && (textBox.Name == "StaffID" || textBox.Name == "RegistrationNo" || textBox.Name == "LoginID") && textBox.Text.Length < 5)
            {
                parent.BorderBrush = Brushes.Red;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = Properties.Resources.Setting_ErrorMessage_FiveCharMin;
                CheckAllValueExisted();
            }
            else if (datePicker != null && !DateTime.TryParse(datePicker.Text.ToString(), out temp))
            {
                parent.BorderBrush = Brushes.Red;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = Properties.Resources.Setting_ErrorMessage_DateFormat;
                CheckAllValueExisted();
            }
            else
            {
                parent.BorderBrush = Brushes.Black;
                parent.BorderThickness = new Thickness(1);
                parent.ToolTip = "No issue";
                CheckAllValueExisted();
            }
        }

        private void CheckAllValueExisted()
        {
            DateTime temp;

            if (Convert.ToString((StaffID.Parent as Border).ToolTip) == "No issue" && Convert.ToString((Title.Parent as Border).ToolTip) == "No issue" && Convert.ToString((FullName.Parent as Border).ToolTip) == "No issue" && Convert.ToString((RegistrationNo.Parent as Border).ToolTip) == "No issue" && Convert.ToString((Gender.Parent as Border).ToolTip) == "No issue" && Convert.ToString((DateOfBirth.Parent as Border).ToolTip) == "No issue" && Convert.ToString((EmailAddress.Parent as Border).ToolTip) == "No issue" && Convert.ToString((Role.Parent as Border).ToolTip) == "No issue" && DateTime.TryParse(DateOfBirth.ToString(), out temp) && Convert.ToString((LoginID.Parent as Border).ToolTip) == "No issue")
            {
                Create.IsEnabled = true;
            }
            else
            {
                Create.IsEnabled = false;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            UserModel user = new UserModel()
            {
                EmployeeID = StaffID.Text,
                Title = Title.Text,
                FullName = FullName.Text,
                RegistrationNo = RegistrationNo.Text,
                Gender = ((ComboBoxItem)Gender.SelectedItem).Tag.ToString(),
                DateOfBirth = Convert.ToDateTime(DateOfBirth.Text).ToString("yyyy-MM-dd"),
                EmailAddress = EmailAddress.Text,
                StatusID = 1,
                RoleID = ((ComboBoxItem)Role.SelectedItem).Tag.ToString(),
                Role = Role.Text,
                LoginID = LoginID.Text
            };

            Popup popup = new Popup();
            popup.IsOpen = true;


            App.MainViewModel.Origin = "UserAddRow";

            App.MainViewModel.Users = user;

            App.newPassword = RandomPasswordGenerator();

            App.PopupHandler(e, sender);
        }

        private void LanguageCountry(object sender, RoutedEventArgs e)
        {
            App.GoToSettingLanguageCountryPageHandler(e, sender);
        }

        private void btnDevice_Click(object sender, RoutedEventArgs e)
        {
            App.GoToSettingDevicePageHandler(e, sender);
        }

        private string RandomPasswordGenerator()
        {
            Random res = new Random();

            // String that contain alphabets, numbers and special character
            String lowerCase = "abcdefghijklmnopqrstuvwxyz";
            String upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            String number = "0123456789";
            String specialChar = "!@#$%^&*()_+-={}|[];,./:<>?";

            // Initializing the empty string 
            String randomstring = "";

            randomstring += lowerCase[res.Next(lowerCase.Length)];
            randomstring += upperCase[res.Next(upperCase.Length)];
            randomstring += number[res.Next(number.Length)];
            randomstring += specialChar[res.Next(specialChar.Length)];
            randomstring += lowerCase[res.Next(lowerCase.Length)];
            randomstring += upperCase[res.Next(upperCase.Length)];
            randomstring += number[res.Next(number.Length)];
            randomstring += specialChar[res.Next(specialChar.Length)];

            char[] chars = randomstring.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                int randomIndex = res.Next(0, chars.Length);
                char temp = chars[randomIndex];
                chars[randomIndex] = chars[i];
                chars[i] = temp;
            }

            randomstring = string.Join("", chars);
            return randomstring;
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
    }
}
