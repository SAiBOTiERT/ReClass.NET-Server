namespace ReClassNET_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(Mode.Win);
            server.StartAsync().Wait();
        }
    }
}
