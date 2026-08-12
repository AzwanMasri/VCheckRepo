using Microsoft.Extensions.Configuration;

namespace VCheckViewer_Others.Lib.Function
{
    public class ConfigSettings
    {
        public static IConfiguration GetConfigurationSettings()
        {
            var iHost = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            return iHost.Configuration;
        }

    }
}
