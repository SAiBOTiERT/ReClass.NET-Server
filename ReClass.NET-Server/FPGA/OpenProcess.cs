using ReClassNET.Extensions;
using ReClassNET_Server;
using System;
using System.IO;

namespace ReClassNET_Server.FPGA
{
    internal class OpenProcess : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }
        public void ProcessData()
        {
            uint pid = (uint)reader.ReadIntPtr();
            uint desiredAccess = reader.ReadUInt32();
            writer.Write((IntPtr)pid);
        }
    }
}
