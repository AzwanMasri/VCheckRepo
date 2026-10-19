using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Local_API.Lib.Utils
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
