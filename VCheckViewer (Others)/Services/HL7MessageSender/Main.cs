using DocumentFormat.OpenXml.InkML;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using VCheck.Interface.API;
using VCheck.Lib.Data;
using VCheck.Lib.Data.Models;
using VCheckViewer_Others.Lib.Function;

namespace VCheckViewer_Others.Services.HL7MessageSender
{

    public class Main
    {
        TcpClient ourTcpClient = null;
        NetworkStream networkStream = null;
        String receivedMessage = "";

        public async Task SendMessage(ScheduledTestModelExtended scheduleTest)
        {
            DeviceModel device = DeviceRepository.GetDeviceByID(App.AnalyzerID, ConfigSettings.GetConfigurationSettings());
            DeviceTypeModel deviceType = DeviceRepository.GetDeviceTypeByID(ConfigSettings.GetConfigurationSettings(), device.DeviceTypeID.Value);
            VCheckAPI VcheckAPI = new VCheckAPI();
            List<(string, string)> sContents = new List<(string, string)>();
            if(deviceType.TypeName == "C10")
            {
                List<(string, string)> testCodeName = new List<(string, string)>();
                foreach (var testInfo in scheduleTest.IDAnalyzers)
                {
                    var testResponseString = await VcheckAPI.GetTestByNameOrCode(null, testInfo.TestID);
                    var TestName = string.IsNullOrEmpty(testResponseString) ? "VCheck" : JsonConvert.DeserializeObject<VCheck.Lib.Data.Models.TestDataObject>(testResponseString).testname.Replace(" (C10)", "");

                    //testCodeName.Add((testInfo.TestID, TestName));


                    sContents.Add((TestName, MessageGenerator.GenerateOMLO33Message(new List<(string, string)>() { (testInfo.TestID, TestName) }, scheduleTest.Schedule)));
                }
            }
            else if (deviceType.TypeName == "H6")
            {
                sContents.Add(("", MessageGenerator.GenerateORMO01Message(scheduleTest.IDAnalyzers, scheduleTest.Schedule)));
            }

            int timeout = 60000;

            bool success = false;
            int successRate = 0;

            if (deviceType.TypeName == "C10")
            {
                foreach (var sContent in sContents)
                {
                    try
                    {
                        using (var cts = new CancellationTokenSource())
                        {
                            cts.CancelAfter(timeout); 
                            success = false;

                            try
                            {
                                var ipAddress = device.DeviceIPAddress.Replace(" ", "");

                                ourTcpClient = new TcpClient();
                                await ourTcpClient.ConnectAsync(IPAddress.Parse(ipAddress), 5067).WaitAsync(cts.Token);

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
                                        successRate = ProcessAckMessage(message) ? successRate + 1 : successRate;
                                    }
                                }

                            }
                            catch (TaskCanceledException cancelled)
                            {
                                App.MainViewModel.Origin = "CanceledSendToAnalyzer";
                            }
                            catch (Exception ex)
                            {
                                App.MainViewModel.Origin = "FailedToSendToAnalyzer";
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        //display any exceptions that occur to console
                        //Console.WriteLine(ex.Message);
                        App.MainViewModel.Origin = "FailedToSendToAnalyzer";
                    }
                    finally
                    {
                        //close the IO strem and the TCP connection
                        networkStream?.Close();
                        ourTcpClient?.Close();
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }


                if (successRate > 0)
                {
                    VCheckAPI vCheckAPI = new VCheckAPI();
                    var test = await vCheckAPI.UpdateScheduleStatus(scheduleTest.Schedule.LocationID, scheduleTest.Schedule.PatientID, scheduleTest.Schedule.ScheduleUniqueID.Split("-")[1], scheduleTest.Schedule.CreatedBy, 1);
                    App.MainViewModel.Origin = "SentToAnalyzer";
                }
                else
                {
                    App.MainViewModel.Origin = "FailedToSendToAnalyzer";
                }

            }
            else
            {
                App.MainViewModel.Origin = "CannotSendToAnalyzer";
            }
            
        }

        private bool ProcessAckMessage(string message)
        {
            int ix = message.IndexOf("MSA|");
            var statusCode = message.Substring(ix + 4, 2);

            if(statusCode == "AA")
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public async Task<Boolean> SendData(VCheck.Interface.API.Greywind.RequestMessage.UpdateResultRequest sResultRequest, System.Net.IPAddress ip, int port)
        {
            String sContent = System.Text.Json.JsonSerializer.Serialize(sResultRequest);
            int timeout = 5000;

            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);

                    try
                    {

                        ourTcpClient = new TcpClient();
                        await ourTcpClient.ConnectAsync(ip, port).WaitAsync(cts.Token);

                        //get the IO stream on this connection to write to
                        networkStream = ourTcpClient.GetStream();
                        var sendMessageByteBuffer = Encoding.UTF8.GetBytes(sContent);

                        if (networkStream.CanWrite)
                        {
                            networkStream.Write(sendMessageByteBuffer, 0, sendMessageByteBuffer.Length);
                            byte[] receiveMessageByteBuffer = new byte[ourTcpClient.ReceiveBufferSize];
                            var bytesReceivedFromServer = await networkStream.ReadAsync(receiveMessageByteBuffer, 0, receiveMessageByteBuffer.Length).WaitAsync(cts.Token);

                            if (bytesReceivedFromServer > 0 && networkStream.CanRead)
                            {
                                receivedMessage = Encoding.UTF8.GetString(receiveMessageByteBuffer);

                                var message = receivedMessage.Replace("\0", string.Empty);

                                return true;
                            }
                        }

                        return false;

                    }
                    catch (TaskCanceledException cancelled)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                //close the IO strem and the TCP connection
                networkStream?.Close();
                ourTcpClient?.Close();
            }
        }
    }
}
