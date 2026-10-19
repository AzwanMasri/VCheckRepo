namespace LocalAPI.Lib.Util
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
