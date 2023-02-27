using ReClassNET.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ReClassNET_Server.x64
{
    internal class EnumerateProcesses : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void Initialize()
        {
            
        }

        public void ProcessData()
        {
            Process[] procs = Process.GetProcesses();
            List<EnumerateProcessData> enumerateProcessData = new List<EnumerateProcessData>();
            foreach (Process proc in procs)
            {
                try
                {
                    var data = new EnumerateProcessData
                    {
                        Id = (IntPtr)proc.Id,
                        Name = Path.GetFileName(proc.MainModule.FileName),
                        Path = proc.MainModule.FileName
                    };

                    enumerateProcessData.Add(data);
                }
                catch (Win32Exception)
                {
                    continue;
                }
            }

            writer.Write(enumerateProcessData.Count);
            foreach (EnumerateProcessData procData in enumerateProcessData)
            {
                writer.Write(PacketManager.getBytes(procData));
            }
        }

        public void Unintialize()
        {
           
        }
    }
}
