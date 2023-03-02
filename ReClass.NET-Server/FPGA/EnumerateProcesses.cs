using ReClassNET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ReClassNET_Server.FPGA
{
    internal class EnumerateProcesses : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            var procs = FPGAWrapper.instance.getAllProcesses();
            writer.Write(procs.Count);
            foreach (var proc in procs)
            {
                var procData = new EnumerateProcessData
                {
                    Id = (IntPtr)proc.dwPID,
                    Name = proc.szName,
                    Path = proc.szNameLong
                };
                writer.Write(PacketManager.getBytes(procData));
            }
        }

    }
}
