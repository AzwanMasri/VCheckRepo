using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace ScheduleAutomation_Others.Lib.Logic
{
    public class ScheduleRepository
    {
        static ConfigurationDBContext configurationDBContext = new ConfigurationDBContext(GetConfigurationSettings());

        public static string GetConfigurationInfoByKey(string configurationKey)
        {
            var configuration = configurationDBContext.GetConfigurationData(configurationKey).FirstOrDefault(); ;

            return configuration != null ? configuration.ConfigurationValue : "";
        }

        public static bool TwoWayDeviceExist()
        {
            var device = DeviceRepository.GetTwoWayCommDevice(GetConfigurationSettings()).FirstOrDefault();

            return device != null;
        }

        public static List<DeviceTypeModel> GetTwoWayCommunicationDeviceTypeList()
        {
            var deviceTypes = DeviceRepository.GetDeviceTypeList(GetConfigurationSettings()).Where(x => x.TwoWayCommunication == 1).ToList();

            return deviceTypes;
        }

        public static string[] GetDeviceTypeParameterList(string analyzer)
        {
            var parameters = TestResultsRepository.GetAllParametersByAnalyzer(GetConfigurationSettings(), analyzer);

            return parameters;
        }

        //public static DeviceModel GetAnalyzerByParameterAllowed(string[] parameters)
        //{
        //    var deviceTypes = GetTwoWayCommunicationDeviceTypeList();
        //    var twoWaydevices = DeviceRepository.GetTwoWayCommDevice(GetConfigurationSettings());

        //    foreach (var device in deviceTypes)
        //    {
        //        var deviceParameters = TestResultsRepository.GetAllParametersByAnalyzer(GetConfigurationSettings(), device.ParameterType);

        //        if (!parameters.Except(deviceParameters).Any())
        //        {
        //            var selectDevice = twoWaydevices.FirstOrDefault(x => x.DeviceTypeID == device.id && x.Next == 0);

        //            if (selectDevice == null)
        //            {
        //                DeviceRepository.RevertTwoWayCommDevice(GetConfigurationSettings(), device.id);
        //                selectDevice = twoWaydevices.FirstOrDefault(x => x.DeviceTypeID == device.id);
        //            }

        //            NextUpdate(selectDevice.id);
        //            return selectDevice;
        //        }
        //    }

        //    return null;
        //}

        public static DeviceModel GetDeviceByAnalyzerList(int analyzersID)
        {
            return DeviceRepository.GetDeviceByDeviceTypeIDList(GetConfigurationSettings(), analyzersID);
        }

        public static void NextUpdate(int id)
        {
            DeviceRepository.NextUpdate(GetConfigurationSettings(), id);
        }

        /// <summary>
        /// Get Appsetting configuration
        /// </summary>
        /// <returns></returns>
        public static IConfiguration GetConfigurationSettings()
        {
            var iHost = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

            return iHost.Configuration;
        }
    }
}
