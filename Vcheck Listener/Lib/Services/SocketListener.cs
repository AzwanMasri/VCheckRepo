using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VCheck.Interface.API;
using Vcheck_Listener.Lib.Models;

namespace Vcheck_Listener.Lib.Services
{
    class SocketListener
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public Action<string>? OnClientConnect;
        public Action<string>? OnDataReceived;
        public Action<string>? OnClientDisconnect;
        public Action<string>? OnError;


        public static Socket? sListener;
        public String sSystemName = "VCheck Listener";
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



        public void Start(string sIPAddress, int sPort)
        {
            OnError?.Invoke("Listening for analyzer " + App.DeviceType);

            try
            {
                IPEndPoint sIPEndPoint = IPEndPoint.Parse(String.Concat(sIPAddress, ":", sPort));

                sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sListener.Bind(sIPEndPoint);
                sListener.Listen(backlog: 10);

                while (!_cts.Token.IsCancellationRequested)
                {
                    if(sIPAddress != App.IPAddress || App.reRrunSocketListener) { App.reRrunSocketListener = false; break; }

                    if (sListener.Poll(100000, SelectMode.SelectRead) && sIPAddress == App.IPAddress)
                    {
                        Socket sClient = sListener.Accept();

                        IPEndPoint remoteIpEndPoint = sClient.RemoteEndPoint as IPEndPoint;

                        var IpPort = remoteIpEndPoint.Address.ToString() + ":" + remoteIpEndPoint.Port;

                        Task.Run(() => ProcessMessage(sClient, IpPort));

                        if (App.DeviceType == "H6")
                        {
                            Task.Run(() => ProcessSchedule(sClient));
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                log.Error("Socket Error: ", ex);
                OnError?.Invoke("Socket error occur. (SocketExcpetion)");
            }
            catch (Exception ex)
            {
                log.Error("Start Error: ", ex);
                OnError?.Invoke("Error occur. (Excpetion)");
            }
            finally
            {
                if(sListener != null) { sListener.Dispose(); }
            }
        }

        private async Task ProcessMessage(Socket sClient, string sIpPort)
        {
            string tempData = "";
            bool continueLoop = true;
            if (!sIpPort.Contains(App.IPAddress))
            {
                OnClientConnect?.Invoke(sIpPort); // 🔔 trigger balloon tip
            }

            while (continueLoop)
            {
                bool isConnected = true;
                int byteLength = sClient.Available;
                while (isConnected)
                {
                    Thread.Sleep(1000);
                    var temp = sClient.Available;
                    if (temp == byteLength && temp != 0) { break; }
                    byteLength = temp;
                    isConnected = !(sClient.Poll(1000, SelectMode.SelectRead) && sClient.Available == 0);
                }

                if (!isConnected) { sClient.Close(); break; }

                byte[] bBuffer = new byte[byteLength];

                int s = sClient.Receive(bBuffer);
                String sDataTemp = Encoding.UTF8.GetString(bBuffer, 0, s);
                sDataTemp = sDataTemp.Replace("\u001c", "")
                             .Replace("\n", "\r");
                String sData = sDataTemp;

                while (sClient.Available != 0)
                {
                    s = sClient.Receive(bBuffer);
                    sDataTemp = Encoding.UTF8.GetString(bBuffer, 0, s);
                    sDataTemp = sDataTemp.Replace("\u001c", "")
                                 .Replace("\n", "\r");
                    if (string.IsNullOrEmpty(sDataTemp) || sDataTemp.Contains("MSH|")) { break; }
                    sData += sDataTemp;
                    Thread.Sleep(500);
                }

                if (!String.IsNullOrEmpty(sData) && sData != tempData)
                {
                    tempData = sData;

                    OnDataReceived?.Invoke(sData.Trim());

                    NHapi.Base.Parser.XMLParser sXMLParser = new NHapi.Base.Parser.DefaultXMLParser();
                    NHapi.Base.Parser.PipeParser sParser = new NHapi.Base.Parser.PipeParser() { ValidationContext = new CustomMessageValidation() };
                    NHapi.Base.Model.IMessage sIMessage = null;
                    bool continueProcessData = false;

                    try
                    {
                        if ((sData.Contains("ORU^R01") && sData.Contains("OBX")) || !sData.Contains("ORU^R01"))
                        {
                            continueProcessData = true;
                        }

                        var lines = sData.Split("\r", StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (!lines[i].Contains("|")) { lines[i - 1] = lines[i - 1] + lines[i]; lines[i] = ""; }
                        }

                        sData = string.Join("\r", lines);

                        sIMessage = sParser.Parse(sData.Trim());
                    }
                    catch (Exception ex)
                    {
                        log.Error("Message failed during parsing: ", ex);
                        OnError?.Invoke("Error parsing data.");
                    }

                    String sAckMessage = String.Empty;
                    String sFileName = "TestResult_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                    if (sIMessage != null)
                    {
                        sAckMessage = await SendAckMessage(sIMessage);
                        var sMessageByte = Encoding.UTF8.GetBytes(sAckMessage);
                        sClient.Send(sMessageByte, SocketFlags.None);

                        if (continueProcessData)
                        {
                            Task.Run(() => ProcessIMessage(sIMessage, sSystemName));

                            OutputMessage(sFileName, sData.Trim(), sAckMessage);
                        }
                    }
                    else
                    {
                        OutputMessage("ErrorTestResult_" + DateTime.Now.ToString("yyyyMMddHHmmss"), sData.Trim(), null);
                    }
                }
            }

            if (!sIpPort.Contains(App.IPAddress))
            {
                OnClientDisconnect?.Invoke(sIpPort);
            }
        }


        private async Task ProcessSchedule(Socket client)
        {
            VCheckAPI vCheckAPI = new VCheckAPI();
            var listenerConfig = App.db.GetCollection<ListenerConfig>("ListenerConfig");
            var result = listenerConfig.FindOne(x => x.Id == 1);
            var sClinic = result != null && !string.IsNullOrEmpty(result.ClinicID) ? result.ClinicID : "";
            var sDeviceType = result != null ? result.AnalyzerType : "";

            while (sClinic != null)
            {
                var schedulesString = await vCheckAPI.GetScheduleListNotSent(sClinic);
                var schedulesExtended = string.IsNullOrEmpty(schedulesString) ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);

                if (schedulesExtended.Count > 0)
                {
                    schedulesExtended = schedulesExtended.Where(x => x.IDAnalyzers.Where(y => y.Analyzers == "H6").Count() != 0).ToList();
                    var order = await Lib.Logics.HL7.V231.OrderRepository.GenerateOrderMessageORM(schedulesExtended);

                    if (!string.IsNullOrEmpty(order))
                    {
                        var testsMessageByte = Encoding.UTF8.GetBytes(order);
                        client.SendAsync(testsMessageByte, SocketFlags.None);

                        foreach (var schedule in schedulesExtended)
                        {
                            await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                            await vCheckAPI.UpdateScheduleAnalyzer(sDeviceType, schedule.Schedule.ScheduleUniqueID);
                        }
                    }

                }

                Thread.Sleep(5000);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        public bool isCancelled()
        {
            return _cts.Token.IsCancellationRequested;
        }

        /// <summary>
        /// Process HL7 Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <param name="sSystemName"></param>
        /// <returns></returns>
        private Boolean ProcessIMessage(NHapi.Base.Model.IMessage sIMessage, String sSystemName)
        {
            Boolean isCompleted = false;

            switch (sIMessage.Version)
            {
                case "2.6":
                    Logics.HL7.V26.HL7Repository.ProcessMessageAsync(sIMessage, sSystemName);
                    break;

                case "2.5.1":
                    Logics.HL7.V251.HL7Repository.ProcessMessage(sIMessage, sSystemName);
                    break;

                case "2.3.1":
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V231.Message.ORU_R01))
                    {
                        Logics.HL7.V231.HL7Repository.ProcessMessageAsync(sIMessage, sSystemName);
                    }
                    break;

                default:
                    break;
            }

            return isCompleted;
        }

        /// <summary>
        /// Generate Ack Message
        /// </summary>
        /// <param name="sIMessage"></param>
        /// <returns></returns>
        private async Task<String> SendAckMessage(NHapi.Base.Model.IMessage sIMessage)
        {
            String sMessage = String.Empty;

            switch (sIMessage.Version)
            {
                case "2.6":
                    sMessage = Logics.HL7.V26.AckRepository.GenerateAcknowlegeMessage(sIMessage);
                    break;

                case "2.5.1":
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V251.Message.QBP_Q11))
                    {
                        sMessage = Logics.HL7.V251.AckRepository.GenerateAcknowlegeMessageQBP(sIMessage);
                    }
                    else if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V251.Message.OUL_R22))
                    {
                        sMessage = Logics.HL7.V251.AckRepository.GenerateAcknowlegeMessage(sIMessage);
                    }

                    break;

                case "2.3.1":
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V231.Message.ORU_R01))
                    {
                        sMessage = Logics.HL7.V231.AckRepository.GenerateAcknowlegeMessageORU(sIMessage);
                    }
                    else if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V231.Message.QRY_Q02))
                    {
                        Logics.HL7.V231.AckRepository ackRepository = new Lib.Logics.HL7.V231.AckRepository();
                        sMessage = await ackRepository.GenerateAcknowlegeMessageQRY(sIMessage);
                    }
                    break;

                default:
                    break;
            }

            return sMessage;
        }

        public void OutputMessage(String sFileName, String sData, String sAckMessage)
        {
            try
            {
                var sBuilder = Host.CreateApplicationBuilder();

                String sOutputPathHL7 = sBuilder.Configuration.GetSection("FileOutput:HL7").Value;
                String sOutputPathACK = sBuilder.Configuration.GetSection("FileOutput:ACK").Value;

                if (!String.IsNullOrEmpty(sOutputPathHL7) && sData != null)
                {
                    String outputPathHL7 = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sOutputPathHL7);
                    if (!Directory.Exists(outputPathHL7))
                    {
                        Directory.CreateDirectory(outputPathHL7);
                    }
                    File.WriteAllText(outputPathHL7 + sFileName + ".hl7", sData, Encoding.ASCII);
                }

                if (!String.IsNullOrEmpty(sOutputPathACK))
                {
                    String outputPathACK = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sOutputPathACK);
                    if (!Directory.Exists(outputPathACK))
                    {
                        Directory.CreateDirectory(outputPathACK);
                    }
                    File.WriteAllText(outputPathACK + sFileName + ".hl7", sAckMessage, Encoding.ASCII);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client error: " + ex.Message);
            }

        }
    }
}
