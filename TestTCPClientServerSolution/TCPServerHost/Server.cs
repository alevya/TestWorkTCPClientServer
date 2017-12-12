using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPServerHost
{
    internal class Server
    {
        public async Task Start(int listenPort)
        {
            var listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
            try
            {
                while (true)
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    await _accept(tcpClient);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task _accept(TcpClient tcpClient)
        {
            await Task.Yield();
            try
            {
                using (tcpClient)
                {
                    using (var stream = tcpClient.GetStream())
                    {
                        byte[] data = new byte[1];
                        await stream.ReadAsync(data, 0, data.Length);

                        if (data[0] == 0x1)
                        {
                            var rnd = new Random();
                            byte[] buffer = new byte[3];
                            rnd.NextBytes(buffer);
                            await stream.WriteAsync(buffer, 0, buffer.Length);
                        }
                        else if(data[0] == 0xA)
                        {
                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
