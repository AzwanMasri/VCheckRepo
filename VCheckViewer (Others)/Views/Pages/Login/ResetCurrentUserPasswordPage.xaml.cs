using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using VCheckViewer_Others.Views.Windows;
using Brushes = System.Windows.Media.Brushes;

namespace VCheckViewer_Others.Views.Pages.Login
{
    /// <summary>
    /// Interaction logic for ResetCurrentUserPasswordPage.xaml
    /// </summary>
    public partial class ResetCurrentUserPasswordPage : Page
    {
        IdentityUser user;
        public ResetCurrentUserPasswordPage()
        {
            InitializeComponent();

            assignUser();

            Main.ResetCurrentPassword += new EventHandler(ProceedResetPassword);
        }

        private async void assignUser()
        {
            user = await App.UserManager.FindByIdAsync(App.MainViewModel.CurrentUsers.UserId.ToString());
        }

        private async void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if(NewPassword.Password == ConfirmPassword.Password)
            {
                if(user != null)
                {

                    var correctCurrentPassword = await App.UserManager.CheckPasswordAsync(user, OldPassword.Password);

                    if (!correctCurrentPassword) 
                    {
                        ErrorText.Text = Properties.Resources.Login_Message_WrongCurrentPassword;
                        ErrorText.Foreground = Brushes.Red;
                    }
                    else
                    {
                        string errorMessage = "";

                        if (ConfirmPassword.Password.Length < 8) { errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_PasswordLessEightChar; }
                        if (!Regex.IsMatch(ConfirmPassword.Password, "^(?=.*?[A-Z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoUpperCase; }
                        if (!Regex.IsMatch(ConfirmPassword.Password, "^(?=.*?[a-z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoLowerCase; }
                        if (!Regex.IsMatch(ConfirmPassword.Password, "^(?=.*?[0-9]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoNumberChar; }

                        if(errorMessage != "") {   ErrorText.Text = errorMessage; ErrorText.Foreground = Brushes.Red; }
                        else
                        {
                            App.MainViewModel.Origin = "ResetPassword";

                            App.PopupHandler(e, sender);
                        }
                    }

                    }
                else
                {
                    ErrorText.Text = Properties.Resources.General_Message_Error;
                    ErrorText.Foreground = Brushes.Red;
                }
            }
            else
            {
                ErrorText.Text = Properties.Resources.Login_Message_WrongNewPassword;
                ErrorText.Foreground = Brushes.Red;
            }
            
        }

        private async void ProceedResetPassword(object sender, EventArgs e)
        {
            var changePassword = await App.UserManager.ChangePasswordAsync(user, OldPassword.Password, ConfirmPassword.Password);
            if (changePassword.Succeeded)
            {
                ErrorText.Text = Properties.Resources.Login_Message_PasswordResetted;
                ErrorText.Foreground = Brushes.Green;
            }
            else
            {
                ErrorText.Text = Properties.Resources.General_Message_Error;
                ErrorText.Foreground = Brushes.Red;
            }

        }

        private async void ValueCheck(object sender, System.Windows.Input.KeyEventArgs e)
        {
            PasswordBox password = null;
            Border parentBorder;


            password = sender as PasswordBox; 
            parentBorder = password.Parent as Border;


            if ((password != null && password.Password == ""))
            {
                parentBorder.BorderBrush = Brushes.Red;
                parentBorder.ToolTip = Properties.Resources.Setting_ErrorMessage_MandatoryField;
            }
            else if (password != null && (password.Password.Length < 8 || !Regex.IsMatch(password.Password, "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9]).{0,}$")))
            {
                string errorMessage = "";
                parentBorder.BorderBrush = Brushes.Red;

                if (password.Password.Length < 8) { errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_PasswordLessEightChar; }
                if (!Regex.IsMatch(password.Password, "^(?=.*?[A-Z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoUpperCase; }
                if (!Regex.IsMatch(password.Password, "^(?=.*?[a-z]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoLowerCase; }
                if (!Regex.IsMatch(password.Password, "^(?=.*?[0-9]).{1,}$")) { if (errorMessage != "") { errorMessage = errorMessage + "\r\n"; } errorMessage = errorMessage + "- " + Properties.Resources.Login_ErrorMessage_NoNumberChar; }
                parentBorder.ToolTip = errorMessage;
            }
            else if (password != null && password.Name == "OldPassword" && !await App.UserManager.CheckPasswordAsync(user, OldPassword.Password))
            {
                parentBorder.BorderBrush = Brushes.Red;
                parentBorder.ToolTip = Properties.Resources.Login_Message_WrongCurrentPassword;
            }
            else if (password != null && password.Name == "ConfirmPassword" && password.Password != NewPassword.Password)
            {
                parentBorder.BorderBrush = Brushes.Red;
                parentBorder.ToolTip = Properties.Resources.Login_Message_WrongNewPassword;
            }
            else
            {
                parentBorder.BorderBrush = Brushes.Black;
                parentBorder.ToolTip = "No issue";
            }

            if (OldPassword.Password != "" && NewPassword.Password != "" && ConfirmPassword.Password != "") { ResetPassword.IsEnabled = true; }
            else { ResetPassword.IsEnabled = false; }
        }
    }
}
