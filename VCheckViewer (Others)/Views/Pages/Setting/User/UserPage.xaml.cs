using System.Windows;
using System.Windows.Controls;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Views.Windows;

namespace VCheckViewer_Others.Views.Pages
{
    /// <summary>
    /// Interaction logic for UserPage.xaml
    /// </summary>
    public partial class UserPage : Page
    {
        UserDBContext sContext = App.GetService<UserDBContext>();

        public static event EventHandler? GoToAddUserPage;
        public static event EventHandler? GoToUpdateUserPage;
        public static event EventHandler? GoToViewUserPage;
        public static event EventHandler? DeleteUser;
        public static event EventHandler? GoToLanguageCountryPage;
        public int pageSize = 10;
        public int paginationSize = 3;
        public int totalUser = 0;
        public int startPagination = 1;
        public int endPagination = 5;
        public int currentPage = 1;

        public UserPage()
        {
            InitializeComponent();

            Main.InitializedUserPage += new EventHandler(initializedPage);
            pagination.ButtonNextControlClick += new EventHandler(PaginationNextButton_Click);
            pagination.ButtonPrevControlClick += new EventHandler(PaginationPrevButton_Click);
            pagination.ButtonPageControlClick += new EventHandler(PaginationNumButton_Click);

            initializedPage(null,null);

            //if (App.MainViewModel.CurrentUsers.Role == "Lab User")
            //{
            //    btnSettings.IsEnabled = false;
            //    btnDevice.IsEnabled = false;
            //}
            //else
            //{
            //    btnSettings.IsEnabled = true;
            //    btnDevice.IsEnabled = true;
            //}
        }

        public void initializedPage(object? sender, EventArgs? e)
        {
            reloadData(0, pageSize, true);
        }

        public void reloadData(int start, int end, bool reset)
        {
            dataGrid.ItemsSource = sContext.GetUserListByPage(start, end);
            App.MainViewModel.CurrentUserIndexStart = start + 1;

            if (reset)
            {
                pagination.iTotalRecords = sContext.GetTotalUser();
                pagination.iPaginationLimit = paginationSize;
                pagination.iPageSize = pageSize;
                currentPage = 1;
                pagination.GetPageCountByRecordCountWithLimit();
            }

            pagination.LoadPagingNumber();
        }

        protected void PaginationNumButton_Click(object? sender, EventArgs? e)
        {
            System.Windows.Controls.Button btnNum = sender as System.Windows.Controls.Button;
            int iNumberSelected = Convert.ToInt32(btnNum?.Tag);

            currentPage = iNumberSelected;

            pagination.iPageIndex = currentPage;

            if (pagination.iPaginationEnd == currentPage && pagination.iPaginationEnd != pagination.iLastPage) { pagination.iPaginationStart++; pagination.iPaginationEnd++; }
            else if (pagination.iPaginationStart == currentPage && currentPage != 1) { pagination.iPaginationStart--; pagination.iPaginationEnd--; }

            reloadData((currentPage - 1) * pageSize, pageSize, false);
        }

        protected void PaginationNextButton_Click(object? sender, EventArgs? e)
        {
            currentPage++;

            pagination.iPageIndex = currentPage;

            if (pagination.iPaginationEnd == currentPage && pagination.iPaginationEnd != pagination.iLastPage) { pagination.iPaginationStart++; pagination.iPaginationEnd++; }

            reloadData((currentPage - 1) * pageSize, pageSize, false);
        }

        protected void PaginationPrevButton_Click(object? sender, EventArgs? e)
        {
            currentPage--;

            pagination.iPageIndex = currentPage;

            if (pagination.iPaginationStart == currentPage && currentPage != 1) { pagination.iPaginationStart--; pagination.iPaginationEnd--; }

            reloadData((currentPage - 1) * pageSize, pageSize, false);
        }

        private void AddUserList_Click(object sender, RoutedEventArgs e)
        {
            GoToAddUserPageHandler(e, sender);
        }

        private void ViewUserButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Users = dataGrid.SelectedItem as UserModel;
            App.HideTopTabBar = false;

            GoToViewUserPageHandler(e, sender);
        }

        private void UpdateUserButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Users = dataGrid.SelectedItem as UserModel;

            GoToUpdateUserPageHandler(e, sender);
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainViewModel.Origin = "UserDeleteRow";

            App.MainViewModel.Users = (dataGrid.SelectedItem as UserModel);

            App.PopupHandler(e, sender);
        }

        private void LanguageCountry(object sender, RoutedEventArgs e)
        {
            GoToLanguageCountryPageHandler(e, sender);
        }

        private static void GoToAddUserPageHandler(EventArgs e, object sender)
        {
            if (GoToAddUserPage != null)
            {
                GoToAddUserPage(sender, e);
            }
        }

        private static void GoToViewUserPageHandler(EventArgs e, object sender)
        {
            if (GoToViewUserPage != null)
            {
                GoToViewUserPage(sender, e);
            }
        }

        private static void GoToUpdateUserPageHandler(EventArgs e, object sender)
        {
            if (GoToUpdateUserPage != null)
            {
                GoToUpdateUserPage(sender, e);
            }
        }

        private static void GoToLanguageCountryPageHandler(EventArgs e, object sender)
        {
            if (GoToLanguageCountryPage != null)
            {
                GoToLanguageCountryPage(sender, e);
            }
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
