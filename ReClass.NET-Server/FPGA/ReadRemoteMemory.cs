using ReClassNET.Extensions;
using System.IO;

namespace ReClassNET_Server.FPGA
{
    internal class ReadRemoteMemory : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            var process = reader.ReadIntPtr();
            var address = reader.ReadIntPtr();
            var size = reader.ReadInt32();
            var buf = FPGAWrapper.instance.RPM((uint)process, (ulong)address, (uint)size);
            writer.Write(true);
            writer.Write(buf);
        }
    }
}
