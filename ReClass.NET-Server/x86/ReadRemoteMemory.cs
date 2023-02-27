using ReClassNET.Extensions;
using ReClassNET_Server;
using System.IO;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.x86
{
    internal class ReadRemoteMemory : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void Initialize()
        {

        }

        public void ProcessData()
        {
            var process = reader.ReadIntPtr();
            var address = reader.ReadIntPtr();
            var size = reader.ReadInt32();
            byte[] buf = new byte[size];
            var res = Rpm(process, address, buf, size);
            writer.Write(res);
            if (res)
            {
                writer.Write(buf);
            }
        }

        public void Unintialize()
        {

        }
    }
}
