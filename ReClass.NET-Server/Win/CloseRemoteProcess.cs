using ReClassNET.Extensions;
using ReClassNET_Server;
using System.IO;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.Win
{
    internal class CloseRemoteProcess : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            var hObject = reader.ReadIntPtr();
            CloseHandle(hObject);
        }
    }
}
