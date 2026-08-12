using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using VCheck.Helper;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.ViewModels.Windows;
using Brushes = System.Windows.Media.Brushes;
using TextBox = System.Windows.Controls.TextBox;

namespace VCheckViewer_Others.Views.Pages.Login
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        ConfigurationDBContext ConfigurationContext = App.GetService<ConfigurationDBContext>();
        static MasterCodeDataDBContext sContext = App.GetService<MasterCodeDataDBContext>();
        static RolesDBContext rolesContext = App.GetService<RolesDBContext>();
        static UserDBContext usersContext = App.GetService<UserDBContext>();
        TemplateDBContext TemplateContext = App.GetService<TemplateDBContext>();
        NotificationDBContext NotificationContext = App.GetService<NotificationDBContext>();

        public RegisterPage()
        {
            InitializeComponent();
            CheckThemesSettings();
            UpdatePlaceholderVisibility("");

            var Login_Label_LeftMain_array = Properties.Resources.Login_Label_LeftMain.Split("<nextline>");
            Login_Label_LeftMain.Text = Login_Label_LeftMain_array[0] + "\r\n" + ((Login_Label_LeftMain_array.Length > 1) ? Login_Label_LeftMain_array[1] : "");

            //PasswordPlaceholder.Text = Properties.Resources.Login_Label_MainPassword;
            //ConfirmPasswordPlaceholder.Text = Properties.Resources.Login_Label_ConfirmPassword;

        }

        private void CheckValue(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textBox = null;
            System.Windows.Controls.Border parentBorder;
            Grid parentGrid;
            bool noIssue = true;

            textBox = sender as TextBox; parentGrid = textBox.Parent as Grid;

            parentBorder = parentGrid.Parent as System.Windows.Controls.Border;


            if ((textBox != null && textBox.Text == ""))
            {
                noIssue = false;
                parentBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;
            }
            else if (textBox != null && textBox.Name == "Username" && textBox.Text.Length < 5)
            {
                noIssue = false;
                parentBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_FiveCharMin;
            }
            else if (textBox != null && textBox.Name == "Email")
            {
                if (!textBox.Text.Contains("@") || !textBox.Text.Contains("."))
                {
                    noIssue = false;
                    parentBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_EmailFormat;
                }
            }

            if (noIssue)
            {
                parentBorder.BorderBrush = Brushes.Black;
                parentBorder.ToolTip = "No issue";
            }
            else
            {
                parentBorder.BorderBrush = Brushes.Red;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var username = (Username.Parent as Grid).Parent as System.Windows.Controls.Border;
                var email = (Email.Parent as Grid).Parent as System.Windows.Controls.Border;
                var password = (Password.Parent as Grid).Parent as System.Windows.Controls.Border;

                if (string.IsNullOrEmpty(Username.Text))
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = Properties.Resources.Popup_Message_InputUsername;
                }
                else if (username.ToolTip.ToString() != "No issue")
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = Properties.Resources.Popup_Message_CheckUsername;
                }
                else if (string.IsNullOrEmpty(Email.Text))
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = "Input email.";
                }
                else if (email.ToolTip.ToString() != "No issue")
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = "Check email.";
                }
                else if (string.IsNullOrEmpty(Password.Password))
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = Properties.Resources.Popup_Message_InputPassword;
                }
                else if (password.ToolTip.ToString() != "No issue")
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = Properties.Resources.Popup_Message_CheckPassword;
                }
                else if (ConfirmPassword.Password != Password.Password)
                {
                    ErrorText.Visibility = Visibility.Visible;
                    ErrorText.Text = Properties.Resources.Popup_Message_ConfirmPasswordDoNotMatch;
                }
                else
                {
                    var role = rolesContext.GetRoles().FirstOrDefault(x => x.RoleName == "Superadmin");

                    UserModel userInfo = new UserModel()
                    {
                        EmployeeID = "NA",
                        Title = "",
                        FullName = "Superadmin",
                        RegistrationNo = "NA",
                        Gender = "M",
                        DateOfBirth = DateTime.Now.ToString("yyyy-MM-dd"),
                        EmailAddress = Email.Text,
                        StatusID = 1,
                        RoleID = role.RoleID,
                        Role = role.RoleName,
                        LoginID = Username.Text
                    };

                    App.MainViewModel.Users = userInfo;

                    App.newPassword = ConfirmPassword.Password;

                    Register(userInfo);

                    App.PopupHandler(e, sender);
                }

            }
            catch(Exception ex)
            {
                App.log.Error("Register User Error >>> ", ex);
            }
        }

        private async void Register(UserModel userInfo)
        {
            try
            {
                var user = Activator.CreateInstance<IdentityUser>();

                var emailStore = (IUserEmailStore<IdentityUser>)App.UserStore;

                await App.UserStore.SetUserNameAsync(user, userInfo.LoginID, CancellationToken.None);
                if (userInfo.EmailAddress != "")
                {
                    await emailStore.SetEmailAsync(user, userInfo.EmailAddress, CancellationToken.None);
                }
                var result = await App.UserManager.CreateAsync(user, App.newPassword);

                ConfigurationModel sLangCode = ConfigurationContext.GetConfigurationData("SystemSettings_Language").FirstOrDefault();

                if (result.Succeeded)
                {
                    userInfo.UserId = user.Id;
                    userInfo.CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    userInfo.CreatedBy = userInfo.LoginID;

                    if (usersContext.InsertUser(userInfo))
                    {
                        var roleResult = await App.UserManager.AddToRoleAsync(user, userInfo.Role);

                        if (roleResult.Succeeded)
                        {

                        }
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                App.log.Error("Register User Error >>> ", ex);
            }
        }

        private void CheckThemesSettings()
        {
            String? sTheme = "Light";

            var sConfigData = ConfigurationContext.GetConfigurationData("SystemSettings_Themes");
            if (sConfigData != null && sConfigData.Count > 0)
            {
                sTheme = sConfigData[0].ConfigurationValue;
            }

            String sThemeName = sTheme + ".xaml";
            AppTheme.ChangeTheme(new Uri("Themes/" + sThemeName, UriKind.Relative));
            if (sTheme.ToLower() == "light")
            {
                btnDarkTheme.Background = System.Windows.Media.Brushes.Transparent;

                System.Windows.Media.Color sBlue = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e76fb");
                btnLightTheme.Background = new SolidColorBrush(sBlue);
            }
            else if (sTheme.ToLower() == "dark")
            {
                btnLightTheme.Background = System.Windows.Media.Brushes.Transparent;

                System.Windows.Media.Color sBlue = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e76fb");
                btnDarkTheme.Background = new SolidColorBrush(sBlue);
            }
        }

        private void btnDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            var sButton = (System.Windows.Controls.Button)sender;
            ConfigurationContext.UpdateConfiguration("SystemSettings_Themes", (sButton != null) ? sButton.Tag.ToString() : "");

            CheckThemesSettings();
        }

        private void btnLightTheme_Click(object sender, RoutedEventArgs e)
        {
            var sButton = (System.Windows.Controls.Button)sender;
            ConfigurationContext.UpdateConfiguration("SystemSettings_Themes", (sButton != null) ? sButton.Tag.ToString() : "");

            CheckThemesSettings();
        }

        private void UpdatePlaceholderVisibility(string name)
        {
            if (name == "ConfirmPassword")
            {
                ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (name == "Password")
            {
                PasswordPlaceholder.Visibility = string.IsNullOrEmpty(Password.Password) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
                PasswordPlaceholder.Visibility = string.IsNullOrEmpty(Password.Password) ? Visibility.Visible : Visibility.Collapsed;
            }

        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox password = sender as PasswordBox;
            UpdatePlaceholderVisibility(password.Name);

            PasswordBox passwordBox = null;
            System.Windows.Controls.Border parentBorder;
            Grid parentGrid;

            passwordBox = sender as PasswordBox; parentGrid = passwordBox.Parent as Grid;

            parentBorder = parentGrid.Parent as System.Windows.Controls.Border;

            string errorMessage = "";
            bool hasError = false;
            if (Password.Password.Length < 8) { errorMessage = errorMessage + Properties.Resources.Login_ErrorMessage_PasswordLessEightChar; hasError = true; }
            if (!Regex.IsMatch(Password.Password, "^(?=.*?[A-Z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + Properties.Resources.Login_ErrorMessage_NoUpperCase; hasError = true; }
            if (!Regex.IsMatch(Password.Password, "^(?=.*?[a-z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + Properties.Resources.Login_ErrorMessage_NoLowerCase; hasError = true; }
            if (!Regex.IsMatch(Password.Password, "^(?=.*?[0-9]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + Properties.Resources.Login_ErrorMessage_NoNumberChar; hasError = true; }
            if (!Password.Password.Any(ch => !char.IsLetterOrDigit(ch))) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "At least one special character."; hasError = true; }



            if ((passwordBox != null && passwordBox.Password == ""))
            {
                parentBorder.BorderBrush = Brushes.Red;
                parentBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;
            }
            else if (hasError)
            {
                parentBorder.BorderBrush = Brushes.Red;
                //parentBorder.ToolTip = errorMessage;
                ErrorTextPassword.Text = errorMessage;
                ErrorTextPassword.Visibility = Visibility.Visible;
            }
            else
            {
                parentBorder.BorderBrush = Brushes.Black;
                parentBorder.ToolTip = "No issue";
                ErrorTextPassword.Visibility = Visibility.Collapsed;
            }

            if(passwordBox != null && passwordBox.Name == "ConfirmPassword")
            {
                if(Password.Password != passwordBox.Password)
                {
                    parentBorder.BorderBrush = Brushes.Red;
                    ErrorTextConfirmPassword.Text = Properties.Resources.Popup_Message_ConfirmPasswordDoNotMatch;
                    ErrorTextConfirmPassword.Visibility = Visibility.Visible;
                }
                else
                {
                    parentBorder.BorderBrush = Brushes.Black;
                    ErrorTextConfirmPassword.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox password = sender as PasswordBox;
            if (password.Name == "ConfirmPassword")
            {
                ConfirmPasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
            else if (password.Name == "Password")
            {
                PasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox password = sender as PasswordBox;
            UpdatePlaceholderVisibility(password.Name);
        }

        private void Placeholder_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox placeholder = sender as TextBox;

            if(placeholder.Name == "PasswordPlaceholder")
            {
                Password.Focus();
            }
            else
            {
                ConfirmPassword.Focus();
            }
        }

        private void passwordShow_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (passwordShow.Text == "") { PasswordPlaceholder.Visibility = Visibility.Visible; }
            else { PasswordPlaceholder.Visibility = Visibility.Collapsed; Password.Password = passwordShow.Text; }
        }

        private void confirmPasswordShow_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (confirmPasswordShow.Text == "") { ConfirmPasswordPlaceholder.Visibility = Visibility.Visible; }
            else { ConfirmPasswordPlaceholder.Visibility = Visibility.Collapsed; ConfirmPassword.Password = confirmPasswordShow.Text; }
        }

        private void PasswordVisible_Click(object sender, RoutedEventArgs e)
        {
            string iconPath;
            if (passwordVisibleIcon.Tag.ToString() == "hide") { iconPath = "see password"; passwordVisibleIcon.Tag = "show"; passwordShow.Visibility = Visibility.Visible; Password.Visibility = Visibility.Collapsed; passwordShow.Text = Password.Password; }
            else { iconPath = "hide password"; passwordVisibleIcon.Tag = "hide"; passwordShow.Visibility = Visibility.Collapsed; Password.Visibility = Visibility.Visible; Password.Password = passwordShow.Text; }

            var bitmap = new BitmapImage();
            Uri uri = new Uri("pack://application:,,,/Content/Images/" + iconPath + ".png");
            bitmap = new BitmapImage(uri);
            passwordVisibleIcon.Source = bitmap;
        }

        private void ConfirmPasswordVisible_Click(object sender, RoutedEventArgs e)
        {
            string iconPath;
            if (confirmPasswordVisibleIcon.Tag.ToString() == "hide") { iconPath = "see password"; confirmPasswordVisibleIcon.Tag = "show"; confirmPasswordShow.Visibility = Visibility.Visible; ConfirmPassword.Visibility = Visibility.Collapsed; confirmPasswordShow.Text = ConfirmPassword.Password; }
            else { iconPath = "hide password"; confirmPasswordVisibleIcon.Tag = "hide"; confirmPasswordShow.Visibility = Visibility.Collapsed; ConfirmPassword.Visibility = Visibility.Visible; ConfirmPassword.Password = confirmPasswordShow.Text; }

            var bitmap = new BitmapImage();
            Uri uri = new Uri("pack://application:,,,/Content/Images/" + iconPath + ".png");
            bitmap = new BitmapImage(uri);
            confirmPasswordVisibleIcon.Source = bitmap;
        }

        private void ConfirmPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RegisterButton_Click(null, null);
            }
        }
    }
}
