using System.Net;
using System.Net.Sockets;

namespace VCheckViewer_Others.Services
{
    class DeviceChecker
    {
        TcpClient ourTcpClient = null;
        NetworkStream networkStream = null;
        bool isOnline = true;

        public async Task<bool> IsOnline(string ipAddress, int port)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(5000);

                    try
                    {
                        ipAddress = ipAddress.Replace(" ", "");
                        ourTcpClient = new TcpClient();

                        await ourTcpClient.ConnectAsync(IPAddress.Parse(ipAddress), port).WaitAsync(cts.Token);

                    }
                    catch (Exception ex)
                    {
                        isOnline = false;
                    }

                }
            }
            catch (Exception ex)
            {
                isOnline = false;
            }
            finally
            {
                //close the IO strem and the TCP connection
                networkStream?.Close();
                ourTcpClient?.Close();
            }

            return isOnline;
        }
    }
}
