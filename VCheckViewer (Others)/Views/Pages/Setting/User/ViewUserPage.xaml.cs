using System.Windows;
using System.Windows.Controls;
using VCheck.Lib.Data.Models;

namespace VCheckViewer_Others.Views.Pages.Setting.User
{
    /// <summary>
    /// Interaction logic for ViewUserPage.xaml
    /// </summary>
    public partial class ViewUserPage : Page
    {
        UserModel userInfoViewPage;

        public static event EventHandler GoToUpdateCurrentUserPage;

        public ViewUserPage()
        {
            InitializeComponent();

            userInfoViewPage = App.MainViewModel.Users;
            if (App.HideTopTabBar) { TopTabBar.Visibility = Visibility.Hidden; UserPage.Visibility = Visibility.Hidden; }

            Title.Text = userInfoViewPage.Title;
            FullName.Text = userInfoViewPage.FullName;
            StaffID.Text = userInfoViewPage.EmployeeID;
            RegistrationNo.Text = userInfoViewPage.RegistrationNo;
            Gender.Text = userInfoViewPage.Gender;
            DateOfBirth.Text = userInfoViewPage.DateOfBirth;
            Role.Text = userInfoViewPage.Role;
            EmailAddress.Text = userInfoViewPage.EmailAddress;
            Status.Text = userInfoViewPage.Status;
            LoginID.Text = userInfoViewPage.LoginID;

            if (App.MainViewModel.CurrentUsers.Role == "User")
            {
                //btnSettings.IsEnabled = false;
                //btnDeviceSetting.IsEnabled = false;

                UserPage.Visibility = Visibility.Collapsed;
            }
            else
            {
                //btnSettings.IsEnabled = true;
                //btnDeviceSetting.IsEnabled = true;

                UserPage.DataContext = App.MainViewModel;

                App.MainViewModel.BackButtonText = Properties.Resources.Setting_Label_UserBackButton;
            }
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoPreviousPageHandler(e, sender);
        }

        private void UpdateCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Users = userInfoViewPage;

            GoToUpdateUserPageHandler(e, sender);
        }

        private static void GoToUpdateUserPageHandler(EventArgs e, object sender)
        {
            if (GoToUpdateCurrentUserPage != null)
            {
                GoToUpdateCurrentUserPage(sender, e);
            }
        }

        private void LanguageCountry(object sender, RoutedEventArgs e)
        {
            App.GoToSettingLanguageCountryPageHandler(e, sender);
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
    }
}
