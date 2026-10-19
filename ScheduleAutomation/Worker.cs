using log4net.Config;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ScheduleAutomation.Lib.Logic;
using ScheduleAutomation.Lib.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data.DBContext;
using VCheck.Lib.Data.Models;

namespace ScheduleAutomation
{
    public class Worker : BackgroundService
    {
        ScheduleAutomation.Log.Logger sLogger;

        static string schedulesString;
        static string clinicID;
        static string PMS;
        static bool TwoWayDeviceExist;
        static List<string> tempAnalyzers;
        static string tempAnalyzersListString;

        static List<DeviceTypeModel> twoWayCommDeviceType = new List<DeviceTypeModel>();
        static List<ScheduledTestModelExtended> schedulesExtended = new List<ScheduledTestModelExtended>();
        static VCheckAPI vCheckAPI = new VCheckAPI();

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

                    
                    twoWayCommDeviceType = ScheduleRepository.GetTwoWayCommunicationDeviceTypeList();
                    schedulesString = await vCheckAPI.GetScheduleListNotSent(clinicID);
                    schedulesExtended = string.IsNullOrEmpty(schedulesString) ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);
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
                                //targetDevice = ScheduleRepository.GetDeviceByAnalyzerList(targetDeviceTypes.Select(x => x.id).ToArray());

                                //if (targetDevice != null)
                                //{
                                //    var targetDeviceTypeName = targetDeviceTypes.FirstOrDefault(x => x.id == targetDevice.DeviceTypeID).TypeName;
                                //    if (targetDeviceTypeName != "H6")
                                //    {
                                //        await HL7.SendMessage(schedule.IDAnalyzers, schedule.Schedule, targetDevice, targetDeviceTypeName);
                                //    }
                                //}

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
                                await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                                //await vCheckAPI.UpdateScheduleAnalyzer("N/A", schedule.Schedule.ScheduleUniqueID);
                            }

                            tempAnalyzersListString = null;
                            twoWayCommDeviceType.Clear();
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
