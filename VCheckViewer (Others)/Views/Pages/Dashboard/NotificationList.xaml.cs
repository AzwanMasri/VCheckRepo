using System.Windows;
using System.Windows.Controls;
using VCheck.Lib.Data.DBContext;

namespace VCheckViewer_Others.Views.Pages.Dashboard
{
    /// <summary>
    /// Interaction logic for NotificationList.xaml
    /// </summary>
    public partial class NotificationList : Page
    {
        NotificationDBContext sContext = App.GetService<NotificationDBContext>();

        public NotificationList()
        {
            InitializeComponent();
            initializeData();
        }

        public void initializeData()
        {
            var currentUserCreatedDate = App.MainViewModel.CurrentUsers.CreatedDate;
            var notificationList = sContext.GetNotificationByPage(0, 5, null, null, null, null, currentUserCreatedDate);

            if (notificationList != null && notificationList.Count > 0)
            {
                icNotification.ItemsSource = notificationList.ToList();

                borderNotification.Visibility = Visibility.Visible;
                borderNoNotification.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderNotification.Visibility = Visibility.Collapsed;
                borderNoNotification.Visibility = Visibility.Visible;
            }
        }
    }
}
