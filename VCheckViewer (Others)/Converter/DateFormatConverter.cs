using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using VCheck.Lib.Data.DBContext;
using VCheckViewer_Others.Lib.Function;

namespace VCheckViewer_Others.Converter
{
    class DateFormatConverter : IValueConverter
    {
        ConfigurationDBContext ConfigurationContext = new ConfigurationDBContext(ConfigSettings.GetConfigurationSettings());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sConfigObj = ConfigurationContext.GetConfigurationData("System_DateFormat").FirstOrDefault();

            if (sConfigObj != null)
            {
                return DateTime.Parse(value.ToString()).ToString(sConfigObj.ConfigurationValue + " HH:mm");
            }
            else
            {
                return DateTime.Parse(value.ToString()).ToString("dd/MM/yyyy HH:mm");
            }
        }

        public object ConvertSimpleDate(object value)
        {
            var sConfigObj = ConfigurationContext.GetConfigurationData("System_DateFormat").FirstOrDefault();

            if (sConfigObj != null)
            {
                return DateTime.Parse(value.ToString()).ToString(sConfigObj.ConfigurationValue);
            }
            else
            {
                return DateTime.Parse(value.ToString()).ToString("dd/MM/yyyy");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
