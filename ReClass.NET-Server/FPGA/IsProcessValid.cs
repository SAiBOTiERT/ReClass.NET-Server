using ReClassNET.Extensions;
using System;
using System.IO;

namespace ReClassNET_Server.FPGA
{
    internal class IsProcessValid : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }
        public void ProcessData()
        {
            var pid = (uint)reader.ReadIntPtr();
            var isValid = FPGAWrapper.instance.IsProcessValid(pid);
            writer.Write(isValid);
            Console.WriteLine(isValid ? "Valid" : "InValid");
        }
    }
}
