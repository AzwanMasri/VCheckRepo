using Microsoft.Extensions.Hosting;
using VCheckViewer_Others.Views.Windows;
using VCheckViewer_Others.Views.Pages.Login;
using VCheck.Lib.Data.DBContext;
using Microsoft.Extensions.Configuration;

namespace VCheckViewer_Others.Services
{
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly static UserDBContext usersContext = App.GetService<UserDBContext>();
        public static IConfiguration iConfig;

        public ApplicationHostService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            var firstUser = usersContext.FirstUser();

            if (firstUser)
            {
                LoginWindow LoginPage = new LoginWindow();
                LoginPage.Navigate(new RegisterPage());
                LoginPage.Show();
            }
            else
            {
                LoginWindow LoginPage = new LoginWindow();
                LoginPage.Navigate(new LoginPage());
                LoginPage.Show();
            }

            await Task.CompletedTask;
        }
    }
}
