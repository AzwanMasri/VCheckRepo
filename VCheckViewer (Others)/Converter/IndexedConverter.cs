using System.Globalization;
using System.Windows.Data;

namespace VCheckViewer_Others.Converter
{
    public class IndexedConverter : IValueConverter
    {
        public object Convert
            (object value
            , Type targetType
            , object parameter
            , CultureInfo culture
            )
        {
            int index = (System.Convert.ToInt32(value)) + App.MainViewModel.CurrentUserIndexStart;

            return index;

        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
