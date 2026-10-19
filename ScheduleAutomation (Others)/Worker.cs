using log4net.Config;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ScheduleAutomation_Others.Lib.Logic;
using ScheduleAutomation_Others.Lib.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace ScheduleAutomation_Others
{
    public class Worker : BackgroundService
    {
        ScheduleAutomation_Others.Log.Logger sLogger;

        static string schedulesString;
        static string clinicID;
        static string PMS;
        static bool TwoWayDeviceExist;
        static List<string> tempAnalyzers;
        static string tempAnalyzersListString;

        static List<DeviceTypeModel> twoWayCommDeviceType = new List<DeviceTypeModel>();
        static List<ScheduledTestModelExtended> schedulesExtended = new List<ScheduledTestModelExtended>();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            XmlConfigurator.Configure(log4net.LogManager.GetRepository(Assembly.GetEntryAssembly()),
                                      new FileInfo("log4Net.config"));
            sLogger = new Log.Logger();            
            

            while (true)
            {
                try
                {
                    PMS = ScheduleRepository.GetConfigurationInfoByKey("InterfaceSettingsPMS");

                    if(string.IsNullOrEmpty(PMS) || PMS == "None")
                    {
                        Thread.Sleep(TimeSpan.FromMinutes(5));
                        continue;
                    }

                    clinicID = ScheduleRepository.GetConfigurationInfoByKey("ClinicID");
                    ScheduledTestRepository.RefreshData();
                    twoWayCommDeviceType = ScheduleRepository.GetTwoWayCommunicationDeviceTypeList();
                    var schedulesExtended = ScheduledTestRepository.GetScheduleListByLocationNotSent(Host.CreateApplicationBuilder().Configuration, "Local");
                    TwoWayDeviceExist = ScheduleRepository.TwoWayDeviceExist();
                    //List<DeviceTypeModel> deviceTypes = ScheduleRepository.GetDeviceTypeList();

                    if (TwoWayDeviceExist && schedulesExtended.Any())
                    {
                        foreach (var schedule in schedulesExtended)
                        {
                            //var selectedDevice = ScheduleRepository.GetAnalyzerByParameterAllowed(schedule.Parameters);
                            //string[] deviceList = schedule.IDAnalyzers.FirstOrDefault().Analyzers.Split(",");

                            tempAnalyzers = schedule.IDAnalyzers.Select(x => x.Analyzers).ToList();

                            foreach (var analyzerType in tempAnalyzers)
                            {
                                var tempAnalyzerArray = analyzerType.Split(',');

                                foreach (var tempAnalyzer in tempAnalyzerArray)
                                {
                                    if(tempAnalyzer == "C10") { tempAnalyzersListString = string.IsNullOrEmpty(tempAnalyzersListString) ? tempAnalyzer : tempAnalyzersListString + "," + tempAnalyzer; }
                                }

                                Array.Clear(tempAnalyzerArray, 0, tempAnalyzerArray.Length);
                            }

                            string[] deviceList = tempAnalyzersListString.Split(",");
                            DeviceModel? targetDevice = null;
                            var targetDeviceTypes = twoWayCommDeviceType.AsEnumerable().Where(x => deviceList.Contains(x.TypeName));

                            if (targetDeviceTypes.Any())
                            {
                                foreach (var targetDeviceType in targetDeviceTypes)
                                {
                                    targetDevice = ScheduleRepository.GetDeviceByAnalyzerList(targetDeviceType.id);

                                    if (targetDevice != null)
                                    {
                                        var targetDeviceTypeName = targetDeviceType.TypeName;
                                        await HL7.SendMessage(schedule.IDAnalyzers.Where(x => x.Analyzers.Contains(targetDeviceTypeName)).ToList(), schedule.Schedule, targetDevice, targetDeviceTypeName);

                                        targetDeviceTypeName = null;
                                    }

                                    targetDevice = null;
                                }
                            }
                            else
                            {
                                ScheduledTestRepository.RefreshData();
                                var data = ScheduledTestRepository.GetScheduledTestByOrderID(Host.CreateApplicationBuilder().Configuration, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, schedule.Schedule.LocationID);

                                if (data != null && data.ScheduleTestStatus != 1)
                                {
                                    data.ScheduleTestStatus = 1;
                                    data.UpdatedBy = schedule.Schedule.CreatedBy;
                                    data.UpdatedDate = DateTime.Now;

                                    if (data.SentToAnalyzer == null || !data.SentToAnalyzer.Split(",").Contains("H6"))
                                    {
                                        data.SentToAnalyzer = data.SentToAnalyzer == null ? "H6" : data.SentToAnalyzer + "," + "H6";
                                    }

                                    ScheduledTestRepository.UpdateScheduledTestByUniqueID(Host.CreateApplicationBuilder().Configuration, data);
                                }
                            }

                            twoWayCommDeviceType.Clear();
                            tempAnalyzersListString = null;
                            Array.Clear(deviceList, 0, deviceList.Length);
                            targetDeviceTypes = null;

                        }
                    }

                    schedulesExtended.Clear();
                }
                catch (Exception e)
                {
                    sLogger.Error("Error >> ", e);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Thread.Sleep(TimeSpan.FromMinutes(1));
                //Thread.Sleep(TimeSpan.FromSeconds(5));

            }
        }
    }
}
