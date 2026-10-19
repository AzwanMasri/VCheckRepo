using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckListenerWorker
{
    public class Worker2 : BackgroundService
    {
        private readonly ILogger<Worker2> _logger;
        private String strIP = "192.168.0.58";
        private int iPortNo = 80;

        System.Net.Sockets.Socket sListener;
        public Worker2(ILogger<Worker2> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //if (_logger.IsEnabled(LogLevel.Information))
                //{
                //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //}
                //await Task.Delay(1000, stoppingToken);
                while (true)
                {
                    System.Net.Sockets.Socket sClient = sListener.Accept();
                    Console.WriteLine("Connection Accepted.");

                    byte[] bBuffer = new byte[4096];

                    var childSocket = new Thread(() =>
                    {
                        int s = sClient.Receive(bBuffer);
                        Console.WriteLine("Received Data.");

                        String sData = System.Text.Encoding.ASCII.GetString(bBuffer, 0, s);
                        sData = sData.Replace("\u001c", "")
                                     .Replace("\n", "\r");

                        if (!String.IsNullOrEmpty(sData))
                        {
                            Console.WriteLine(sData);

                            NHapi.Base.Parser.PipeParser sParser = new NHapi.Base.Parser.PipeParser();
                            NHapi.Base.Model.IMessage sIMessage = sParser.Parse(sData.Trim());

                            NHapi.Model.V26.Message.ORU_R01 sRU_R01 = (NHapi.Model.V26.Message.ORU_R01)sIMessage;
                            NHapi.Base.Parser.XMLParser sXMLParser = new NHapi.Base.Parser.DefaultXMLParser();
                            String sXMLMessage = sXMLParser.Encode(sIMessage);
                            Console.WriteLine(sXMLMessage);

                            String sAckMessage = ListenerSampleProgra_.Utils.CreateResponseMessage(sRU_R01.MSH.MessageControlID.Value);
                            var sMessageByte = System.Text.Encoding.UTF8.GetBytes(sAckMessage);
                            sClient.SendAsync(sMessageByte, System.Net.Sockets.SocketFlags.None);

                            Console.WriteLine();
                        }

                        sClient.Close();
                    });

                    childSocket.Start();
                }
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test 1");

            //IPAddress sIPAddress = IPAddress.Parse(strIP);
            System.Net.IPEndPoint sIPEndPoint = System.Net.IPEndPoint.Parse(String.Concat(strIP, ":", iPortNo));

            sListener = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                                                                                System.Net.Sockets.SocketType.Stream,
                                                                                System.Net.Sockets.ProtocolType.Tcp);
            sListener.Bind(sIPEndPoint);
            sListener.Listen(3);

            Console.WriteLine("Listener Start.");
            Console.WriteLine("IP Address :" + strIP);
            Console.WriteLine("Port No :" + iPortNo);


            // ---------------------------------------------------- //
            //System.Net.IPEndPoint sIPEndPoint1 = System.Net.IPEndPoint.Parse(String.Concat(strIP, ":", 80));


            //sListener2 = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
            //                                                                    System.Net.Sockets.SocketType.Stream,
            //                                                                   System.Net.Sockets.ProtocolType.Tcp);
            //sListener2.Bind(sIPEndPoint1);
            //sListener2.Listen(3);

            //Console.WriteLine("Listener Start.");
            //Console.WriteLine("IP Address :" + strIP);
            //Console.WriteLine("Port No :" + 90);

            return base.StartAsync(cancellationToken);
        }
        //public override Task StartAsync(CancellationToken cancellationToken)
        //{
        //    _logger.LogInformation("Test 1");

        //    //IPAddress sIPAddress = IPAddress.Parse(strIP);
        //    System.Net.IPEndPoint sIPEndPoint = System.Net.IPEndPoint.Parse(String.Concat(strIP, ":", iPortNo));

        //    sListener = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
        //                                                                        System.Net.Sockets.SocketType.Stream,
        //                                                                        System.Net.Sockets.ProtocolType.Tcp);
        //    sListener.Bind(sIPEndPoint);
        //    sListener.Listen(3);

        //    Console.WriteLine("Listener Start.");
        //    Console.WriteLine("IP Address :" + strIP);
        //    Console.WriteLine("Port No :" + iPortNo);

        //    return base.StartAsync(cancellationToken);
        //}

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test 2");

            sListener.Disconnect(true);
            sListener.Dispose();

            return base.StopAsync(cancellationToken);
        }
    }

}
