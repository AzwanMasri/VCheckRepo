using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Converter;
using VCheckViewer_Others.Lib.Function;
using VCheckViewer_Others.UserControls;
using Button = System.Windows.Controls.Button;
using Hyperlink = System.Windows.Documents.Hyperlink;
using TextBlock = System.Windows.Controls.TextBlock;

namespace VCheckViewer_Others.Views.Pages.Notification
{
    /// <summary>
    /// Interaction logic for NotificationPage.xaml
    /// </summary>
    public partial class NotificationPage : Page
    {
        NotificationDBContext sContext = App.GetService<NotificationDBContext>();

        public int pageSize = 4;
        public int paginationSize = 5;
        public int totalNotification = 0;
        public int startPagination = 1;
        public int endPagination = 5;
        public int currentPage = 1;
        public string? currentNotificationType;
        public string? currentStartDate, currentEndDate, currentKeyword;
        public ConfigurationDBContext configDBContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public NotificationPage()
        {
            InitializeComponent();

            reloadData(0, pageSize, null, null, null, null, true);

            pagination.ButtonNextControlClick += new EventHandler(PaginationNextButton_Click);
            pagination.ButtonPrevControlClick += new EventHandler(PaginationPrevButton_Click);
            pagination.ButtonPageControlClick += new EventHandler(PaginationNumButton_Click);

            RangeDateStart.DateRangePicker_Calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            RangeDateEnd.DateRangePicker_Calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            RangeDateStart.DateRangePicker_TextBlock.Text = "Start Date";
            RangeDateEnd.DateRangePicker_TextBlock.Text = "End Date";

            RangeDateStart.DateRangePicker_Calendar.SelectedDatesChanged += RangeDate_SelectedDateChanged;
            RangeDateEnd.DateRangePicker_Calendar.SelectedDatesChanged += RangeDate_SelectedDateChanged;
            RangeDateStart.DateRangePicker_Calendar.LostFocus += CloseCalender;
            RangeDateEnd.DateRangePicker_Calendar.LostFocus += CloseCalender;
        }

        public async Task reloadData(int start, int end, string? notificationType, string? startDate, string? endDate, string? keyword, bool reset)
        {
            var currentUserCreatedDate = App.MainViewModel.CurrentUsers.CreatedDate;
            List<NotificationModel> notificationList = sContext.GetNotificationByPage(start, end, notificationType, startDate, endDate, keyword, currentUserCreatedDate);

            //if(string.IsNullOrEmpty(notificationType) || notificationType == "Schedule Error") { notificationList.AddRange(await GetErrorList(startDate, endDate)); }            

            createList(notificationList);

            if (reset)
            {
                pagination.iTotalRecords = sContext.GetTotalNotification(notificationType, startDate, endDate, keyword, currentUserCreatedDate);
                pagination.iPaginationLimit = paginationSize;
                pagination.iPageSize = pageSize;
                currentPage = 1;
                pagination.GetPageCountByRecordCountWithLimit();
            }

            //pagination.LoadPagingNumberWithLimit();
            pagination.LoadPagingNumber();

            NotificationSearch sSearchModel = new NotificationSearch();
            sSearchModel.SearchStart = start;
            sSearchModel.SearchEnd = end;
            if (notificationType == null)
            {
                sSearchModel.SearchType = "All";
            }
            else
            {
                sSearchModel.SearchType = notificationType;
            }
            
            sSearchModel.SearchStartDate = startDate;
            sSearchModel.SearchEndDate = endDate;
            sSearchModel.SearchKeyword = keyword;
            sSearchModel.SearchReset = reset;
            //sSearchModel.SearchSelectedDates = RangeDate.SelectedDates;

            App.MainViewModel.SearchModel = sSearchModel;

            App.RefreshUnreadNotificationHandler(null, null);
        }

        public void createList(List<NotificationModel> notificationList)
        {
            String? sColor = System.Windows.Application.Current.Resources["Themes_FontColor"].ToString();
            String? sSeparatorColor = System.Windows.Application.Current.Resources["Themes_GridNotifRowLinesColor"].ToString();
            SolidColorBrush sBrushSeparator = new BrushConverter().ConvertFrom(sSeparatorColor) as SolidColorBrush;

            NotificationViewList.Children.Clear();

            if(notificationList.Count > 0)
            {
                foreach (NotificationModel notification in notificationList)
                {
                    Grid grid = new Grid();
                    Ellipse ellipse = new Ellipse() { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, StrokeThickness = 0, Fill = System.Windows.Media.Brushes.Gray, Visibility = notification.Read == 0 ? Visibility.Visible : Visibility.Hidden };
                    DockPanel mainPanel = new DockPanel() { HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, Margin = new Thickness(10, 10, 10, 10) };
                    DockPanel panel1 = new DockPanel();
                    DockPanel panel2 = new DockPanel();
                    TextBlock title = new TextBlock();
                    TextBlock date = new TextBlock();
                    TextBlock content = new TextBlock();
                    TextBlock NotificationID = new TextBlock() { Text = notification.NotificationID.ToString(), Visibility = Visibility.Collapsed };
                    Border borderLeft = new Border() { Child = ellipse, Width = 10, Margin = notification.Read == 0 ? new Thickness(10, 10, 10, 10) : new Thickness(0, 10, 10, 10) };
                    Border borderRight = new Border() { Width = 10, Margin = new Thickness(10, 10, 0, 10) };
                    Border borderWhole = new Border() { Tag = "BorderWhole", CornerRadius = new CornerRadius(5), HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

                    grid.MouseDown += Grid_MouseDown;
                    grid.MouseEnter += Grid_MouseEnter;
                    grid.MouseLeave += Grid_MouseLeave;

                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("auto") });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("*") });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)new GridLengthConverter().ConvertFromString("auto") });
                    Grid.SetColumn(borderLeft, 0);
                    Grid.SetColumn(NotificationID, 0);
                    Grid.SetColumn(borderRight, 2);
                    Grid.SetColumn(borderWhole, 0);
                    Grid.SetColumnSpan(borderWhole, 3);
                    grid.Children.Add(borderLeft);
                    grid.Children.Add(NotificationID);
                    grid.Children.Add(borderRight);
                    grid.Children.Add(borderWhole);

                    System.Windows.Controls.Panel.SetZIndex(borderLeft, 5);


                    title.Text = notification.NotificationTitle;
                    title.TextAlignment = System.Windows.TextAlignment.Left;
                    title.FontWeight = FontWeights.Bold;
                    title.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                    date.Text = notification.CreatedDate;
                    date.TextAlignment = System.Windows.TextAlignment.Right;
                    date.FontWeight = FontWeights.Bold;
                    date.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                    content.Text = notification.NotificationContent;
                    content.TextWrapping = TextWrapping.Wrap;
                    content.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;

                    if (notification.NotificationTitle == "Reminder for Software Update")
                    {
                        var contentArray = notification.NotificationContent.Split("###<link>###");

                        content.Inlines.Clear();
                        content.Inlines.Add(contentArray[0]);
                        Hyperlink hyperLink = new Hyperlink()
                        {
                            NavigateUri = new Uri(App.UpdateLink)
                        };
                        hyperLink.Inlines.Add(App.UpdateLink);
                        hyperLink.RequestNavigate += new RequestNavigateEventHandler(delegate (object sender, RequestNavigateEventArgs e)
                        {
                            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                            e.Handled = true;
                        });
                        content.Inlines.Add(hyperLink);
                        content.Inlines.Add(contentArray[1]);
                    }


                    DockPanel.SetDock(panel1, Dock.Top);
                    DockPanel.SetDock(title, Dock.Left);
                    DockPanel.SetDock(date, Dock.Right);
                    DockPanel.SetDock(panel2, Dock.Bottom);
                    DockPanel.SetDock(content, Dock.Left);

                    panel1.Children.Add(title);
                    panel1.Children.Add(date);
                    panel2.Margin = new Thickness(0, 10, 0, 0);
                    panel2.Children.Add(content);

                    if (notification != notificationList.LastOrDefault()) { mainPanel.Margin = new Thickness(0, 10, 0, 10); }
                    else { mainPanel.Margin = new Thickness(0, 10, 0, 0); }

                    mainPanel.Height = 80;
                    mainPanel.Children.Add(panel1);
                    mainPanel.Children.Add(panel2);

                    Grid.SetColumn(mainPanel, 1);
                    grid.Children.Add(mainPanel);

                    //NotificationViewList.Children.Add(mainPanel);
                    NotificationViewList.Children.Add(grid);
                    if (notification != notificationList.LastOrDefault() || notificationList.Count == 1) { NotificationViewList.Children.Add(new Separator() { BorderBrush = sBrushSeparator, BorderThickness = new Thickness(0.5) }); }
                }
            }
            else
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = Properties.Resources.General_Message_NoData;
                textBlock.Foreground = new BrushConverter().ConvertFrom(sColor) as SolidColorBrush;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextAlignment = System.Windows.TextAlignment.Center;
                NotificationViewList.Children.Add(textBlock);
            }
        }
        
        protected void PaginationNumButton_Click(object? sender, EventArgs? e)
        {
            Button btnNum = sender as Button;
            int iNumberSelected = Convert.ToInt32(btnNum?.Tag);

            currentPage = iNumberSelected;

            pagination.iPageIndex = currentPage;

            if (pagination.iPaginationEnd == currentPage && pagination.iPaginationEnd != pagination.iLastPage) { pagination.iPaginationStart++; pagination.iPaginationEnd++; }
            else if (pagination.iPaginationStart == currentPage && currentPage != 1) { pagination.iPaginationStart--; pagination.iPaginationEnd--; }

            reloadData((currentPage - 1) * pageSize, pageSize, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, false);
        }               

        protected void PaginationNextButton_Click(object? sender, EventArgs? e)
        {
            currentPage++;

            pagination.iPageIndex = currentPage;

            if(pagination.iPaginationEnd == currentPage && pagination.iPaginationEnd != pagination.iLastPage) { pagination.iPaginationStart++; pagination.iPaginationEnd++; }

            reloadData((currentPage - 1) * pageSize, pageSize, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, false);
        }

        protected void PaginationPrevButton_Click(object? sender, EventArgs? e)
        {
            currentPage--;

            pagination.iPageIndex = currentPage;

            if (pagination.iPaginationStart == currentPage && currentPage != 1) { pagination.iPaginationStart--; pagination.iPaginationEnd--; }

            reloadData((currentPage - 1) * pageSize, pageSize, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, false);
        }

        private void NotificationKeywordSearch(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CloseCalenderPopup(null, null);
        }

        private void ChangeNotificationType(object sender, RoutedEventArgs e)
        {
            CloseCalenderPopup(null, null);
        }

        private void UpdateFilter(object sender, RoutedEventArgs e)
        {
            currentStartDate = null;
            currentEndDate = null;
            currentKeyword = null;

            //if (RangeDate.SelectedDates.Count > 0)
            //{
            //    currentStartDate = RangeDate.SelectedDates.FirstOrDefault().ToString("yyyy-MM-dd");
            //    currentEndDate = RangeDate.SelectedDates.LastOrDefault().AddDays(1).ToString("yyyy-MM-dd");
            //}

            if (RangeDateStart.SelectedDates.Count() == 1)
            {
                currentStartDate = RangeDateStart.SelectedDates.FirstOrDefault().ToString("yyyy-MM-dd");
            }

            if (RangeDateEnd.SelectedDates.Count() == 1)
            {
                currentEndDate = RangeDateEnd.SelectedDates.FirstOrDefault().AddDays(1).AddMinutes(-1).ToString("yyyy-MM-dd");
            }
            else if (!string.IsNullOrEmpty(currentStartDate))
            {
                currentEndDate = RangeDateStart.SelectedDates.FirstOrDefault().AddDays(1).AddMinutes(-1).ToString("yyyy-MM-dd");
            }

            if (KeywordSearchBar.Text.Length > 0) { currentKeyword = KeywordSearchBar.Text; }

            var notificationTypeBtn = ((ComboBoxItem)NotificationType.SelectedItem).Tag;

            if (notificationTypeBtn != null) { currentNotificationType = notificationTypeBtn.ToString(); }
            else { currentNotificationType = null; }

            reloadData(0, pageSize, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, true);

            CloseCalenderPopup(null, null);

        }

        private void RefreshFilter(object sender, RoutedEventArgs e)
        {
            KeywordSearchBar.Text = string.Empty;
            RangeDateStart.SelectedDates = new ObservableCollection<DateTime>();
            RangeDateStart.DateRangePicker_TextBox.Text = string.Empty;
            RangeDateEnd.SelectedDates = new ObservableCollection<DateTime>();
            RangeDateEnd.DateRangePicker_TextBox.Text = string.Empty;
            NotificationType.SelectedItem = NotificationType.Items.OfType<ComboBoxItem>().Where(x => x.Tag == null).FirstOrDefault();

            UpdateFilter(null, null);

        }

        private void CloseCalenderPopup(object? sender, MouseButtonEventArgs? e)
        {
            RangeDateStart.DateRangePicker_Popup.IsOpen = false;
            RangeDateEnd.DateRangePicker_Popup.IsOpen = false;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var grid = (Grid)sender;
            var NotificationID = grid.Children.OfType<TextBlock>().FirstOrDefault().Text.ToString();
            var currentSearch = App.MainViewModel.SearchModel;

            sContext.MarkReadByID(NotificationID);
            reloadData(currentSearch.SearchStart, currentSearch.SearchEnd, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, false);
        }

        private void MarkAsRead_Click(object sender, RoutedEventArgs e)
        {
            var currentSearch = App.MainViewModel.SearchModel;
            sContext.MarkAllAsRead();
            reloadData(currentSearch.SearchStart, currentSearch.SearchEnd, currentNotificationType, currentStartDate, currentEndDate, currentKeyword, false);
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = (Grid)sender;
            var border = grid.Children.OfType<Border>().FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == "BorderWhole");
            border.SetResourceReference(Border.BackgroundProperty, "Themes_HighlightColor");
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var grid = (Grid)sender;
            var border = grid.Children.OfType<Border>().FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == "BorderWhole");
            border.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void CloseCalender(object sender, RoutedEventArgs e)
        {
            var focusedControl = Keyboard.FocusedElement as FrameworkElement;

            if (focusedControl == null || focusedControl.GetType() == typeof(CalendarDayButton) || focusedControl.GetType() == typeof(CalendarButton) ||
                focusedControl.GetType() == typeof(Calendar))
            {
                return;
            }

            if (focusedControl.GetType() == typeof(ScrollViewer))
            {
                focusedControl.LostFocus += CloseCalender;

                return;
            }


            DateRangePicker calender = sender as DateRangePicker;

            if (calender == RangeDateStart || RangeDateStart.DateRangePicker_Popup.IsOpen)
            {
                RangeDateStart.DateRangePicker_Popup.IsOpen = false;
            }
            else if (calender == RangeDateEnd || RangeDateEnd.DateRangePicker_Popup.IsOpen)
            {
                RangeDateEnd.DateRangePicker_Popup.IsOpen = false;
            }
        }

        private void RangeDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            RangeDateStart.SelectedDates = RangeDateStart.DateRangePicker_Calendar.SelectedDates;
            RangeDateEnd.SelectedDates = RangeDateEnd.DateRangePicker_Calendar.SelectedDates;

            if (RangeDateStart.SelectedDates.Count() == 1 && RangeDateEnd.SelectedDates.Count() == 0)
            {
                RangeDateEnd.SelectedDates = RangeDateStart.SelectedDates;
            }
            else if (RangeDateEnd.SelectedDates.Count() == 1 && RangeDateStart.SelectedDates.Count() == 0)
            {
                RangeDateStart.SelectedDates = RangeDateEnd.SelectedDates;
            }
            else if (RangeDateEnd.SelectedDates.FirstOrDefault() < RangeDateStart.SelectedDates.FirstOrDefault())
            {
                RangeDateEnd.SelectedDates = RangeDateStart.SelectedDates;
                RangeDateEnd.DateRangePicker_Calendar.SelectedDate = RangeDateStart.DateRangePicker_Calendar.SelectedDate;
            }

            RangeDateEnd.DateRangePicker_Calendar.DisplayDateStart = RangeDateStart.DateRangePicker_Calendar.SelectedDate;

            DateFormatConverter dateFormatConverter = new DateFormatConverter();

            RangeDateStart.DateRangePicker_TextBox.Text = dateFormatConverter.ConvertSimpleDate(RangeDateStart.SelectedDates.FirstOrDefault()).ToString();
            RangeDateEnd.DateRangePicker_TextBox.Text = dateFormatConverter.ConvertSimpleDate(RangeDateEnd.SelectedDates.FirstOrDefault()).ToString();

            CloseCalenderPopup(null, null);
        }
    }
}
