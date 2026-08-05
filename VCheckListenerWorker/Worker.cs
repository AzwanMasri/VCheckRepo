using log4net.Config;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Mysqlx.Crud;
using Newtonsoft.Json;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Base.Validation.Implementation;
using NHapi.Model.V23.Segment;
using NHapiTools.Model.V26.Segment;
using Org.BouncyCastle.Asn1;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using VCheck.Interface.API;
using VCheck.Lib.Data.Models;
using VCheckListenerWorker.Lib.Logic;
using VCheckListenerWorker.Lib.Logic.HL7.V231;
using VCheckListenerWorker.Lib.Models;
using VCheckListenerWorker.Lib.ValidationContext;

namespace VCheckListenerWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        System.Net.Sockets.Socket sListener;
        VCheckListenerWorker.Lib.Util.Logger sLogger;

        public String sSystemName = "VCheckViewer Listener";
        public string[] ConstantConnectionAnalyzer = { "H6", "U3", "V200", "V2400" };

        public static MainModel MainModel { get; } = new MainModel();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Main Logic process the data
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var test = Guid.NewGuid().ToString();

                    XmlConfigurator.Configure(log4net.LogManager.GetRepository(Assembly.GetEntryAssembly()),
                                              new FileInfo("log4Net.config"));
                    sLogger = new Lib.Util.Logger();

                    var configBuilder = Host.CreateApplicationBuilder();

                    Socket sClient = sListener.Accept();

                    string deviceType;
                    IPEndPoint remoteIpEndPoint = sClient.RemoteEndPoint as IPEndPoint;

                    string clientIpAddress = remoteIpEndPoint.Address.ToString();
                    string clientIpPort = remoteIpEndPoint.Port.ToString();
                    Console.WriteLine("Client connected from " + clientIpAddress + ":" + clientIpPort + ".");
                    var device = TestResultRepository.GetDeviceByIPSerialNo(clientIpAddress, null, out deviceType);
                    var clinicID = TestResultRepository.GetConfigurationByKey("ClinicID");

                    var childSocket = new Thread(async () =>
                    {
                        string tempData = "";
                        MainModel.DeviceSerialNum = device != null && device.id != 0 ? device.DeviceSerialNo : null;
                        MainModel.DeviceType = deviceType;
                        bool continueLoop = true;
                        NHapi.Base.Model.IMessage sIMessage = null;

                        //while (clinicID != null && continueLoop)
                        while (continueLoop)
                        {
                            
                            bool isConnected = true;
                            int byteLength = sClient.Available;
                            Console.WriteLine($"Continue listening....{byteLength}");
                            while (isConnected)
                            {
                                Thread.Sleep(1000);
                                var temp = sClient.Available;
                                if (temp == byteLength && temp != 0) { break; }
                                byteLength = temp;
                                isConnected = !(sClient.Poll(1000, SelectMode.SelectRead) && sClient.Available == 0);
                            }

                            if (!isConnected)
                            {
                                Console.WriteLine("Client disconnected from " + clientIpAddress + ":" + clientIpPort + ".");
                                break;
                            }

                            byte[] bBuffer = new byte[byteLength];

                            int s = sClient.Receive(bBuffer);
                            String sDataTemp = Encoding.UTF8.GetString(bBuffer, 0, s);
                            sDataTemp = sDataTemp.Replace("\u001c", "")
                                         .Replace("\n", "\r");
                            String sData = sDataTemp;

                            //Console.WriteLine($"Receiving data with byte length {byteLength}....");

                            while (sClient.Available != 0)
                            {
                                s = sClient.Receive(bBuffer);
                                sDataTemp = Encoding.UTF8.GetString(bBuffer, 0, s);
                                sDataTemp = sDataTemp.Replace("\u001c", "")
                                             .Replace("\n", "\r");
                                //if (string.IsNullOrEmpty(sDataTemp) || sDataTemp.Contains("MSH|")) { break; }
                                if (string.IsNullOrEmpty(sDataTemp)) { break; }
                                sData += sDataTemp;
                                Thread.Sleep(500);
                            }

                            if (!String.IsNullOrEmpty(sData) && sData != tempData)
                            {
                                tempData = sData;

                                NHapi.Base.Parser.XMLParser sXMLParser = new NHapi.Base.Parser.DefaultXMLParser();
                                NHapi.Base.Parser.PipeParser sParser = new NHapi.Base.Parser.PipeParser() { ValidationContext = new CustomMessageValidation() };
                                bool continueProcessData = false;

                                try
                                {
                                    if (((sData.Contains("ORU^R01") && sData.Contains("OBX")) || !sData.Contains("ORU^R01")) && !sData.Contains("ORU^R02"))
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
                                catch (Exception e)
                                {
                                    _logger.LogError("Message failed during parsing: " + e.Message);
                                    Console.WriteLine("Message failed during parsing: " + e.Message);
                                }

                                String sXMLMessage = String.Empty;
                                String sAckMessage = String.Empty;
                                String sFileName = "TestResult_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                                //OutputMessage(configBuilder, "TestResultTest_" + DateTime.Now.ToString("yyyyMMddHHmmss"), sData, null, null);

                                if (sIMessage != null)
                                {
                                    sAckMessage = await SendAckMessage(sIMessage, sData.Trim());
                                    var sMessageByte = Encoding.UTF8.GetBytes(sAckMessage);
                                    sClient.Send(sMessageByte, SocketFlags.None);
                                    OutputMessage(configBuilder, sFileName, null, null, sAckMessage);

                                    //if (!ConstantConnectionAnalyzer.Contains(deviceType))
                                    //{
                                    //    sClient.Close();
                                    //    Console.WriteLine("[one-way] Client disconnected from " + clientIpAddress + ":" + clientIpPort + ".");
                                    //    continueLoop = false;
                                    //}

                                    if (continueProcessData)
                                    {
                                        Task.Run(() => ProcessIMessage(sIMessage, sSystemName));
                                        sXMLMessage = sXMLParser.Encode(sIMessage);
                                        OutputMessage(configBuilder, sFileName, sData, sXMLMessage, null);
                                    }
                                }
                                else
                                {
                                    sFileName = "ErrorTestResult_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                                    OutputMessage(configBuilder, sFileName, sData, null, null);
                                }

                                Console.WriteLine("---------------------------------------------------------------------------------");
                            }
                        }

                        //if (ConstantConnectionAnalyzer.Contains(deviceType))
                        //{
                        //    sClient.Close();
                        //}

                        sClient.Close();
                        //Console.WriteLine("Client disconnected from " + clientIpAddress + ":" + clientIpPort + ".");
                    });

                    childSocket.Start();

                    if (deviceType == "H6")
                    {
                        var childSocket2 = new Thread(async () =>
                        {
                            VCheckAPI vCheckAPI = new VCheckAPI();

                            while (clinicID != null)
                            {
                                var schedulesString = await vCheckAPI.GetScheduleListNotSent(clinicID.ConfigurationValue);
                                var schedulesExtended = string.IsNullOrEmpty(schedulesString) ? new List<ScheduledTestModelExtended>() : JsonConvert.DeserializeObject<List<ScheduledTestModelExtended>>(schedulesString);

                                if (schedulesExtended.Count > 0)
                                {
                                    schedulesExtended = schedulesExtended.Where(x => x.IDAnalyzers.Where(y => y.Analyzers == "H6").Count() != 0).ToList();
                                    var order = await OrderRepository.GenerateOrderMessageORM(schedulesExtended);

                                    if (!string.IsNullOrEmpty(order))
                                    {
                                        var testsMessageByte = Encoding.UTF8.GetBytes(order);
                                        sClient.SendAsync(testsMessageByte, SocketFlags.None);

                                        foreach (var schedule in schedulesExtended)
                                        {
                                            await vCheckAPI.UpdateScheduleStatus(schedule.Schedule.LocationID, schedule.Schedule.PatientID, schedule.Schedule.ScheduleUniqueID.Split("-")[1], schedule.Schedule.CreatedBy, 1);
                                            await vCheckAPI.UpdateScheduleAnalyzer("H6", schedule.Schedule.ScheduleUniqueID);
                                        }
                                    }

                                }

                                //Thread.Sleep(TimeSpan.FromMinutes(5));
                                Thread.Sleep(5000);
                            }
                        });

                        childSocket2.Start();
                    }

                }
            }
            catch (Exception e)
            {
                _logger.LogError("Listener Error: " + e.Message);
            }
        }

        /// <summary>
        /// When program start execution
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            String sHostIP = "";
            int iPortNo = 0;
            var maxLoop = 5;

            Console.WriteLine("Searching Available IP Address...");

            while (maxLoop != 0)
            {
                try
                {

                    //var builder = Host.CreateApplicationBuilder();
                    //sHostIP = builder.Configuration.GetSection("Listener:HostIP").Value;                
                    //iPortNo = Convert.ToInt32(builder.Configuration.GetSection("Listener:Port").Value);

                    //sHostIP = "169.254.98.184";
                    iPortNo = 8585;
                    //iPortNo = 1001;

                    sHostIP = GetAssignedIPAddress();
                    //sHostIP = GetIPAddressFromDatabase(out iPortNo);
                    //sHostIP = GetIPAddressAccordingToType();

                    if (!string.IsNullOrEmpty(sHostIP))
                    {
                        Console.WriteLine("Starting Listener...");

                        //IPEndPoint sIPEndPoint = new IPEndPoint(IPAddress.Any, iPortNo);
                        System.Net.IPEndPoint sIPEndPoint = System.Net.IPEndPoint.Parse(String.Concat(sHostIP, ":", iPortNo));

                        sListener = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                        sListener.Bind(sIPEndPoint);
                        sListener.Listen(3);

                        //Console.WriteLine(Guid.NewGuid().ToString());

                        Console.WriteLine("Listener Start Successful.");
                        Console.WriteLine("IP Address : " + sHostIP);
                        Console.WriteLine("Port No : " + iPortNo);
                        Console.WriteLine("-------------------------------------------------------------------");

                        return base.StartAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("StartAsync >>> " + ex.ToString());
                    Environment.Exit(1);
                    return base.StopAsync(cancellationToken);
                }

                maxLoop--;
                Thread.Sleep(5000);
            }

            Console.WriteLine("No Available IP Address Found.");
            Environment.Exit(1);
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// When program stopped
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Initiated Stop Listener connection.");

                sListener.Disconnect(true);
                sListener.Dispose();

                //_logger.LogInformation("Listener connection closed.");
                Console.WriteLine("Listener connection closed.");

            }
            catch (Exception ex)
            {
                _logger.LogError("StopAsync >>>> " + ex.ToString());
                sLogger.Error("StopAsync >>>> " + ex.ToString());
            }

            return base.StopAsync(cancellationToken);
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
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V26.Message.ORU_R01))
                    {
                        Lib.Logic.HL7.V26.HL7Repository.ProcessMessageAsync(sIMessage, sSystemName);
                    }
                    break;

                case "2.5.1":
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V251.Message.OUL_R22))
                    {
                        Lib.Logic.HL7.V251.HL7Repository.ProcessMessage(sIMessage, sSystemName);
                        //Lib.Logic.HL7.V251.tempHL7.ProcessMessage(sIMessage, sSystemName);
                        Console.WriteLine("Data processed...");
                    }
                    else
                    {
                        Console.WriteLine("Data processed normally...");
                    }
                    break;

                case "2.3.1":
                    if (sIMessage.Message.Message.GetType() == typeof(NHapi.Model.V231.Message.ORU_R01))
                    {
                        Lib.Logic.HL7.V231.HL7Repository.ProcessMessageAsync(sIMessage, sSystemName);
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
        private async Task<String> SendAckMessage(NHapi.Base.Model.IMessage sIMessage, string HL7)
        {
            String sMessage = String.Empty;
            var type = sIMessage.Message.Message.GetType();
            var hl7Split = HL7.Split('\r');

            switch (sIMessage.Version)
            {
                case "2.6":
                    var messageType = hl7Split.First(x => x.StartsWith("MSH")).Split('|')[8];

                    if (messageType == "QBP^Z01^QBP_Z01")
                    {
                        sMessage = await Lib.Logic.HL7.V26.OrderRepository.ProcessMessageAsync(hl7Split);
                        sMessage.Replace("\n", "");
                    }
                    else
                    {
                        sMessage = Lib.Logic.HL7.V26.AckRepository.GenerateAcknowlegeMessage(sIMessage);
                    }

                    break;

                case "2.5.1":
                    messageType = hl7Split.First(x => x.StartsWith("MSH")).Split('|')[8];

                    if (type == typeof(NHapi.Model.V251.Message.QBP_Q11))
                    {
                        sMessage = Lib.Logic.HL7.V251.AckRepository.GenerateAcknowlegeMessageQBP(sIMessage);
                    }
                    else if(type == typeof(NHapi.Model.V251.Message.OUL_R22))
                    {
                        sMessage = Lib.Logic.HL7.V251.AckRepository.GenerateAcknowlegeMessage(sIMessage);
                    }
                    else if (messageType == "ORU^R02")
                    {
                        var messageCount = hl7Split.First(x => x.StartsWith("ORC")).Split('|')[2];
                        sMessage = await Lib.Logic.HL7.V251.OrderRepository.ProcessMessageAsync(messageCount);
                        sMessage.Replace("\n", "");
                    }

                    break;

                case "2.3.1":
                    if (type == typeof(NHapi.Model.V231.Message.ORU_R01))
                    {
                        sMessage = Lib.Logic.HL7.V231.AckRepository.GenerateAcknowlegeMessageORU(sIMessage);
                    }
                    else if (type == typeof(NHapi.Model.V231.Message.QRY_Q02))
                    {
                        Lib.Logic.HL7.V231.AckRepository ackRepository = new Lib.Logic.HL7.V231.AckRepository();
                        sMessage = await ackRepository.GenerateAcknowlegeMessageQRY(sIMessage);
                    }

                    break;

                default:
                    break;
            }

            return sMessage;
        }

        /// <summary>
        /// Output data to file
        /// </summary>
        /// <param name="sBuilder"></param>
        /// <param name="sFileName"></param>
        /// <param name="sData"></param>
        /// <param name="sXMLMessage"></param>
        /// <param name="sAckMessage"></param>
        public void OutputMessage(HostApplicationBuilder sBuilder, String sFileName, String sData, String sXMLMessage, String sAckMessage)
        {
            try
            {
                String sOutputPathHL7 = sBuilder.Configuration.GetSection("FileOutput:HL7").Value;
                String sOutputPathXML = sBuilder.Configuration.GetSection("FileOutput:XML").Value;
                String sOutputPathACK = sBuilder.Configuration.GetSection("FileOutput:ACK").Value;

                if (!String.IsNullOrEmpty(sOutputPathHL7) && sData != null)
                {
                    String outputPathHL7 = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sOutputPathHL7);
                    if (!Directory.Exists(outputPathHL7))
                    {
                        Directory.CreateDirectory(outputPathHL7);
                    }
                    File.WriteAllText(outputPathHL7 + sFileName + ".hl7", sData, System.Text.Encoding.ASCII);

                    Console.WriteLine("Saved HL7 Message.");
                }

                // Output to XML file
                if (!String.IsNullOrEmpty(sOutputPathXML) && sXMLMessage != null)
                {
                    String outputPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sOutputPathXML);
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }
                    File.WriteAllText(outputPath + sFileName + ".xml", sXMLMessage, System.Text.Encoding.ASCII);
                }

                if (!String.IsNullOrEmpty(sOutputPathACK) && sAckMessage != null)
                {
                    String outputPathACK = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), sOutputPathACK);
                    if (!Directory.Exists(outputPathACK))
                    {
                        Directory.CreateDirectory(outputPathACK);
                    }
                    File.WriteAllText(outputPathACK + sFileName + ".hl7", sAckMessage, System.Text.Encoding.ASCII);

                    Console.WriteLine("Saved ACK Message.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Function OutputMessage >>> " + ex.ToString());
            }

        }

        /// <summary>
        /// Get current IP Address
        /// </summary>
        public static string GetAssignedIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                {
                    return ip.ToString();
                }
            }
            //throw new Exception("No network adapters with an IPv4 address in the system!");
            return "";
        }

        /// <summary>
        /// Get IP Address from database
        /// </summary>
        public static string GetIPAddressFromDatabase(out int port)
        {
            string ipAddress = "";
            port = 0;
            bool hasPort = false;

            try
            {
                ipAddress = TestResultRepository.GetConfigurationByKey("InterfaceSettingsIP").ConfigurationValue.Replace(" ", "");
                hasPort = int.TryParse(TestResultRepository.GetConfigurationByKey("InterfaceSettingsPortNo").ConfigurationValue, out port);
            }
            catch
            {

            }

            return ipAddress;
        }

        public static string GetIPAddressAccordingToType()
        {
            var connectionType = TestResultRepository.GetConfigurationByKey("Connection_Type");
            string selectedIpAddress = "";
            var NetworkType = connectionType != null && connectionType.ConfigurationValue == "ethernet" ? NetworkInterfaceType.Ethernet : NetworkInterfaceType.Wireless80211;

            try
            {
                // Get all network interfaces
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Skip interfaces that are not up
                    if (ni.OperationalStatus != OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        continue;

                    // Filter only Ethernet or Wireless interfaces
                    if (ni.NetworkInterfaceType != NetworkType)
                        continue;

                    // Get IPv4 addresses for this interface
                    var ipProps = ni.GetIPProperties();
                    var ipv4Addrs = ipProps.UnicastAddresses
                        .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                    if (ipv4Addrs != null)
                    {
                        selectedIpAddress = ipv4Addrs.Address.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return selectedIpAddress;
        }
    }
}
