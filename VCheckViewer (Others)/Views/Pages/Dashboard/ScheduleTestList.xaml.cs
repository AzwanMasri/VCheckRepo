using System.Windows;
using System.Windows.Controls;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;

namespace VCheckViewer_Others.Views.Pages.Dashboard
{
    /// <summary>
    /// Interaction logic for GeneralTestList.xaml
    /// </summary>
    public partial class ScheduleTestList : Page
    {
        public ScheduleTestList()
        {
            InitializeComponent();
            initializeData();
        }

        public void initializeData()
        {
            List<ScheduledTestModel> sScheduledList = VCheck.Lib.Data.ScheduledTestRepository.GetCurrentScheduledTestList(ConfigSettings.GetConfigurationSettings());

            if (sScheduledList != null && sScheduledList.Count > 0)
            {
                icScheduledTest.ItemsSource = sScheduledList.ToList();

                borderScheduledTest.Visibility = Visibility.Visible;
                borderNoScheduledTest.Visibility = Visibility.Collapsed;
            }
            else
            {
                borderScheduledTest.Visibility = Visibility.Collapsed;
                borderNoScheduledTest.Visibility = Visibility.Visible;
            }
        }
    }
}
