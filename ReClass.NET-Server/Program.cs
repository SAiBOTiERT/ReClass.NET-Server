using System;
using ReClassNET_Server;

namespace ReClassNET_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(Mode.x64);
            server.StartAsync().Wait();
        }
    }
}
