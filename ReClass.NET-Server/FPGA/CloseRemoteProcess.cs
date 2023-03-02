using ReClassNET.Extensions;
using ReClassNET_Server;
using System.IO;

namespace ReClassNET_Server.FPGA
{
    internal class CloseRemoteProcess : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            var hObject = reader.ReadIntPtr();
        }
    }
}
