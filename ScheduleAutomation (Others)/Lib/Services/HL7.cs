using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ScheduleAutomation_Others.Lib.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using VCheck.Lib.Data;
using VCheck.Lib.Data.Models;

namespace ScheduleAutomation_Others.Lib.Services
{
    public class HL7
    {
        static TcpClient ourTcpClient = null;
        static NetworkStream networkStream = null;
        static String receivedMessage = "";
        static List<(string, string)> sContents = new List<(string, string)>();
        //static List<(string, string)> testCodeName = new List<(string, string)>();

        public static async Task SendMessage(List<TestIDAnalyzers> testID, ScheduledTestModel scheduleTest, DeviceModel device, string DeviceTypeName)
        {

            if (DeviceTypeName == "C10")
            {
                foreach (var testInfo in testID)
                {
                    var sTest = TestResultsRepository.GetTestByUniqueID(Host.CreateApplicationBuilder().Configuration, testInfo.TestID);
                    var TestName = sTest != null ? sTest.TestName : "VCheck";

                    sContents.Add((TestName, MessageGenerator.GenerateOMLO33Message(new List<(string, string)>() { (testInfo.TestID, TestName) }, scheduleTest)));
                }
            }
            
            int timeout = 6000;

            //String sContent = "";
            //int timeout = 5000;

            bool success = false;
            int successRate = 0;

            var ipAddress = device.DeviceIPAddress.Replace(" ", "");

            foreach (var sContent in sContents)
            {
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        cts.CancelAfter(timeout);

                        try
                        {
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

                                    successRate = ProcessAckMessage(message) ? successRate + 1 : successRate;
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
