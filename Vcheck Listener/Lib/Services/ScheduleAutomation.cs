using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using Vcheck_Listener.Lib.Models;

namespace Vcheck_Listener.Lib.Services
{
    class ScheduleAutomation
    {
        static TcpClient ourTcpClient = null;
        static NetworkStream networkStream = null;
        static String receivedMessage = "";
        static bool hasRun = false;
        static List<TestIDAnalyzers> testID;
        static ScheduledTestModel scheduleTest;
        static string ipAddress;
        static string DeviceTypeName;

        public Action<string>? OnError;
        public Action<bool>? OnScheduleAutomationProcessUpdate;

        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task Start()
        {
            while (App.runScheduleAutomation)
            {
                if (!hasRun)
                {
                    hasRun = true;
                    OnScheduleAutomationProcessUpdate?.Invoke(hasRun);
                }

                try
                {
                    var listenerConfig = App.db.GetCollection<ListenerConfig>("ListenerConfig");
                    var result = listenerConfig.FindOne(x => x.Id == 1);
                    var clinicID = result != null && !string.IsNullOrEmpty(result.ClinicID) ? result.ClinicID : "";
                    var deviceType = result != null && !string.IsNullOrEmpty(result.AnalyzerType) ? result.AnalyzerType : "";
                    var deviceIP = result != null && !string.IsNullOrEmpty(result.AnalyzerIP) ? result.AnalyzerIP : "";

                    VCheckAPI vCheckAPI = new VCheckAPI();

                    var schedulesString = await vCheckAPI.GetScheduleListNotSent(clinicID);
                    var schedulesExtended = string.IsNullOrEmpty(schedulesString) || schedulesString == "null" ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);

                    if (schedulesExtended.Any() && deviceType == "C10")
                    {
                        foreach (var schedule in schedulesExtended)
                        {
                            bool CanSendSchedule = false;
                            foreach(var devices in schedule.IDAnalyzers)
                            {
                                CanSendSchedule = devices.Analyzers.Split(",").Contains(deviceType);
                                if (CanSendSchedule) { break; }
                            }

                            if (CanSendSchedule)
                            {
                                testID = schedule.IDAnalyzers;
                                scheduleTest = schedule.Schedule;
                                ipAddress = deviceIP;
                                DeviceTypeName = deviceType;

                                await SendMessage();
                            }
                            else
                            {
                                await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error("Schedule Automation Error >> ", e);
                    OnError?.Invoke("Error processing schedule automation.");
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
                //Thread.Sleep(TimeSpan.FromMinutes(5));

            }

            if (hasRun)
            {
                hasRun = false;
                OnScheduleAutomationProcessUpdate?.Invoke(hasRun);
            }
        }

        public static async Task SendMessage()
        {
            List<(string, string)> sContents = new List<(string, string)>();
            VCheckAPI VcheckAPI = new VCheckAPI();

            if (DeviceTypeName == "C10")
            {
                List<(string, string)> testCodeName = new List<(string, string)>();
                foreach (var testInfo in testID)
                {
                    var testResponseString = await VcheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                    var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                    //testCodeName.Add((testInfo.TestID, TestName));

                    sContents.Add((TestName, MessageGenerator.GenerateOMLO33Message(new List<(string, string)>() { (testInfo.TestID, TestName) }, scheduleTest)));
                }

                //sContent = MessageGenerator.GenerateOMLO33Message(testCodeName, scheduleTest);
            }

            await Send(sContents);
        }

        private async static Task Send(List<(string, string)> sContents) 
        {
            int timeout = 6000;
            bool success = false;
            int successRate = 0;

            foreach (var sContent in sContents)
            {
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        cts.CancelAfter(timeout);

                        try
                        {
                            ipAddress = ipAddress.Replace(" ", "");

                            ourTcpClient = new TcpClient();
                            await ourTcpClient.ConnectAsync(IPAddress.Parse(ipAddress), 5067).WaitAsync(cts.Token);

                            //get the IO stream on this connection to write to
                            networkStream = ourTcpClient.GetStream();
                            var sendMessageByteBuffer = Encoding.UTF8.GetBytes(sContent.Item2);

                            if (networkStream.CanWrite)
                            {
                                networkStream.Write(sendMessageByteBuffer, 0, sendMessageByteBuffer.Length);
                                byte[] receiveMessageByteBuffer = new byte[ourTcpClient.ReceiveBufferSize];
                                var bytesReceivedFromServer = await networkStream.ReadAsync(receiveMessageByteBuffer, 0, receiveMessageByteBuffer.Length).WaitAsync(cts.Token);

                                if (bytesReceivedFromServer > 0 && networkStream.CanRead)
                                {
                                    receivedMessage = Encoding.UTF8.GetString(receiveMessageByteBuffer);

                                    var message = receivedMessage.Replace("\0", string.Empty);

                                    //success = ProcessAckMessage(message);

                                    successRate = ProcessAckMessage(message) ? successRate++ : successRate;
                                }
                            }

                        }
                        catch (TaskCanceledException cancelled)
                        {

                        }
                        catch (Exception ex)
                        {

                        }

                    }
                }
                catch (Exception ex)
                {
                    //display any exceptions that occur to console
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    //close the IO strem and the TCP connection
                    networkStream?.Close();
                    ourTcpClient?.Close();
                }
            }

            if (successRate > 0)
            {
                VCheckAPI vCheckAPI = new VCheckAPI();
                var UpdateStatus = await vCheckAPI.UpdateScheduleStatus(scheduleTest.LocationID, scheduleTest.PatientID, scheduleTest.ScheduleUniqueID.Split("-")[1], scheduleTest.CreatedBy, 1);
                var updateAnalyzer = await vCheckAPI.UpdateScheduleAnalyzer(DeviceTypeName, scheduleTest.ScheduleUniqueID);
            }

        }

        private static bool ProcessAckMessage(string message)
        {
            int ix = message.IndexOf("MSA|");
            var statusCode = message.Substring(ix + 4, 2);

            if (statusCode == "AA")
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
