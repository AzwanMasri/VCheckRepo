using System.Globalization;
using System.Windows.Data;

namespace VCheckViewer_Others.Converter
{
    public class HeightConverter : IValueConverter
    {
        public double Portion { get; set; } = 0.5; // Default to 50%

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double mainWindowHeight = App.Current.MainWindow.Height;

            return mainWindowHeight * Portion;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
