using ReClassNET.Extensions;
using ReClassNET_Server;
using System;
using System.IO;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.Win
{
    internal class IsProcessValid : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            uint exitCode;
            bool isProcessValid = false;
            if (GetExitCodeProcess(reader.ReadIntPtr(), out exitCode))
            {
                isProcessValid = exitCode == STILL_ACTIVE;
            }
            writer.Write(isProcessValid);
            Console.WriteLine(isProcessValid ? "Valid" : "InValid");
        }
    }
}
