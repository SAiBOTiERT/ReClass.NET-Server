using ReClassNET.Extensions;
using ReClassNET_Server;
using System.IO;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.Win
{
    internal class OpenProcess : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            uint pid = (uint)reader.ReadIntPtr();
            uint desiredAccess = reader.ReadUInt32();
            var res = OpenProcess((uint)ProcessAccessFlags.VirtualMemoryRead | (uint)ProcessAccessFlags.QueryLimitedInformation, false, pid);
            writer.Write(res);
        }
    }
}
