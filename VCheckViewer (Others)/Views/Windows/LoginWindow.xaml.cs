using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using VCheckViewer_Others.Views.Pages;
using VCheckViewer_Others.Views.Pages.Login;
using Wpf.Ui.Appearance;

namespace VCheckViewer_Others.Views.Windows
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    //public partial class Login : INavigationWindow
    public partial class LoginWindow : Window
    {
        public static event EventHandler ResetPassword;
        public static event EventHandler GoToMainWindow;

        public LoginWindow()
        {
            SystemThemeWatcher.Watch(this);
            InitializeComponent();

            //WindowStyle = WindowStyle.None;
            //WindowState = WindowState.Maximized;
            //ResizeMode = ResizeMode.NoResize;
            //Topmost = true;

            LoginPage.GoToResetPasswordPage += new EventHandler(GoToResetPasswordPage);
            ResetPasswordPage.GoToLoginPage += new EventHandler(GoToLoginPage);
            PasswordRecoveryPage.GoToLoginPage += new EventHandler(GoToLoginPage);

            GoToMainWindow = null;
            GoToMainWindow += new EventHandler(GoToMainWindowProcess);

            //popup
            PasswordRecoveryPage.Popup += new EventHandler(OpenPopup);
            App.Popup += new EventHandler(GoToLoginPage);

        }


        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        public void Navigate(object sender) => LoginFrame.Content = sender;

        public void GoToResetPasswordPage(object sender, EventArgs e)
        {
            LoginFrame.Content = new PasswordRecoveryPage();
        }

        public void GoToRegisterPage(object sender, EventArgs e)
        {
            LoginFrame.Content = new RegisterPage();
        }

        public void GoToLoginPage(object sender, EventArgs e)
        {
            LoginFrame.Content = new LoginPage();
        }

        public void GoToMainWindowProcess(object sender, EventArgs e)
        {
            foreach (var window in System.Windows.Application.Current.Windows.OfType<Main>())
            {
                window.Close();
            }

            Main main = new Main();
            main.frameContent.Content = new DashboardPage();
            main.Show();

            foreach (var window in System.Windows.Application.Current.Windows.OfType<LoginWindow>())
            {
                window.Close();
            }
        }

        public void OpenPopup(object sender, EventArgs e)
        {
            OKButton.Visibility = Visibility.Collapsed;
            ContinueButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Visible;

            PopupBackground.Background = System.Windows.Media.Brushes.DimGray;
            PopupBackground.Opacity = 0.5;
            PopupContent.Text = Properties.Resources.Popup_Message_RecoverPassword;
            popup.IsOpen = true;
        }

        public void SentTemporaryPassword(object sender, EventArgs e)
        {
            if(popup.IsOpen == false)
            {
                OKButton.Visibility = Visibility.Visible;
                ContinueButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                PopupBackground.Background = System.Windows.Media.Brushes.DimGray;
                PopupBackground.Opacity = 0.5;
                popup.IsOpen = true;
            }
            else
            {
                OKButton.Visibility = Visibility.Visible;
                ContinueButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }

            Run bold = new Run();
            bold.Text = Properties.Resources.Popup_Message_PasswordRecoveredP2;

            bold.FontWeight = FontWeights.Bold;

            PopupContent.Text = "";

            var firstpartSection = (Properties.Resources.Popup_Message_PasswordRecoveredP1).Split("<nextline>");
            var combineFirstpartSection = firstpartSection.Count() == 1 ? firstpartSection[0] : firstpartSection[0] + "\r\n \r\n" + firstpartSection[1];

            PopupContent.Inlines.Add(combineFirstpartSection);
            PopupContent.Inlines.Add(bold);
            PopupContent.Inlines.Add(Properties.Resources.Popup_Message_PasswordRecoveredP3);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            SentTemporaryPassword(sender, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            // Enable main window
            this.IsEnabled = true;
            // Close the popup
            popup.IsOpen = false;

            PopupBackground.Background = null;

            if(button.Name == "OKButton")
            {
                LoginFrame.Content = new LoginPage();
                ResetPasswordHandler(e, sender);
            }
        }

        private static void ResetPasswordHandler(EventArgs e, object sender)
        {
            if (ResetPassword != null)
            {
                ResetPassword(sender, e);
            }
        }

        public static void GoToMainWindowHandler(EventArgs e, object sender)
        {
            if (GoToMainWindow != null)
            {
                GoToMainWindow(sender, e);
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }
    }
}
