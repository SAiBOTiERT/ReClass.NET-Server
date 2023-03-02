using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace ReClassNET_Server
{
    class Server
    {

        private TcpListener tcpListener;
        private CancellationTokenSource tokenSource;
        private CancellationToken token;
        private PacketManager pm;
        private FPGAWrapper fpga = null;

        public Server(Mode mode, ushort port = 8080)
        {
            if(mode == Mode.FPGA)
            {
                fpga = new FPGAWrapper();
            }
            tcpListener = new TcpListener(IPAddress.Any, port);
            pm = new PacketManager(mode);
        }

        private void HandleReceivedClient(TcpClient client)
        {
            while (true)
            {
                try
                {
                    pm.HandlePackage(client.GetStream());
                }
                catch (EndOfStreamException)
                {
                    client.Close();
                    break;
                }
                catch (Exception e)
                {
                    var w32ex = e as Win32Exception;
                    if (w32ex == null)
                    {
                        w32ex = e.InnerException as Win32Exception;
                    }
                    if (w32ex == null || w32ex.ErrorCode != 10054)
                    {
                        Console.WriteLine(e + ": " + e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    Console.WriteLine("Client disconnected");
                    client.Close();
                    break;
                }
            }
        }

        public async Task StartAsync()
        {
            token = new CancellationToken();
            tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            tcpListener.Start();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var tcpClientTask = tcpListener.AcceptTcpClientAsync();
                    var result = await tcpClientTask;
                    Console.WriteLine("New client");
                    _ = Task.Run(() =>
                      {
                          HandleReceivedClient(result);
                      }, token);
                }
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        public void Stop()
        {
            tokenSource?.Cancel();
        }

        public void Dispose()
        {
            Stop();
        }

    }
}