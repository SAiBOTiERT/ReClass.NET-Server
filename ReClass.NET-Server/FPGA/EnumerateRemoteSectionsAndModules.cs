using ReClassNET.Core;
using ReClassNET.Extensions;
using ReClassNET.Memory;
using ReClassNET_Server;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.FPGA
{
    internal class EnumerateRemoteSectionsAndModules : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void ProcessData()
        {
            var process = reader.ReadIntPtr();

            var sections = new List<EnumerateRemoteSectionData>();
            var modules = new List<EnumerateRemoteModuleData>();
            var imageNames = new Dictionary<UInt64, String>();
            var fpgavads = FPGAWrapper.instance.getAllVads((uint)process, out imageNames);
            var fpgamodules = FPGAWrapper.instance.getAllModules((uint)process);
            writer.Write(fpgavads.Count);
            foreach (var vad in fpgavads)
            {
                var section = new EnumerateRemoteSectionData();
                section.BaseAddress = vad.Value.BaseAddress;
                section.Size = vad.Value.RegionSize;
                if (vad.Value.Type == TypeEnum.MEM_MAPPED)
                {
                    section.Type = SectionType.Mapped;
                }
                else if (vad.Value.Type == TypeEnum.MEM_PRIVATE)
                {
                    section.Type = SectionType.Private;
                }
                else if (vad.Value.Type == TypeEnum.MEM_IMAGE)
                {
                    section.Type = SectionType.Image;
                }
                else
                {
                    section.Type = SectionType.Unknown;
                }
                section.Category = section.Type == SectionType.Private ? SectionCategory.HEAP : SectionCategory.Unknown;
                section.Protection = SectionProtection.NoAccess;
                if ((vad.Value.Protect & AllocationProtectEnum.PageExecute) == AllocationProtectEnum.PageExecute)
                {
                    section.Protection |= SectionProtection.Execute;
                }
                if ((vad.Value.Protect & AllocationProtectEnum.PageExecuteRead) == AllocationProtectEnum.PageExecuteRead) section.Protection |= SectionProtection.Execute | SectionProtection.Read;
                if ((vad.Value.Protect & AllocationProtectEnum.PageExecuteReadwrite) == AllocationProtectEnum.PageExecuteReadwrite) section.Protection |= SectionProtection.Execute | SectionProtection.Read | SectionProtection.Write;
                if ((vad.Value.Protect & AllocationProtectEnum.PageExecuteWritecopy) == AllocationProtectEnum.PageExecuteWritecopy) section.Protection |= SectionProtection.Execute | SectionProtection.Read | SectionProtection.CopyOnWrite;
                if ((vad.Value.Protect & AllocationProtectEnum.PageReadonly) == AllocationProtectEnum.PageReadonly) section.Protection |= SectionProtection.Read;
                if ((vad.Value.Protect & AllocationProtectEnum.PageReadwrite) == AllocationProtectEnum.PageReadwrite) section.Protection |= SectionProtection.Read | SectionProtection.Write;
                if ((vad.Value.Protect & AllocationProtectEnum.PageWritecopy) == AllocationProtectEnum.PageWritecopy) section.Protection |= SectionProtection.Read | SectionProtection.CopyOnWrite;
                if ((vad.Value.Protect & AllocationProtectEnum.PageGuard) == AllocationProtectEnum.PageGuard) section.Protection |= SectionProtection.Guard;

                section.Name = "";
                section.ModulePath = "";

                if (vad.Value.Type == TypeEnum.MEM_IMAGE && imageNames.ContainsKey(vad.Key))
                {
                    section.ModulePath = imageNames[vad.Key];
                }

                writer.Write(PacketManager.getBytes(section));
            }

            writer.Write(fpgamodules.Count);
            foreach (var mod in fpgamodules)
            {
                var module = new EnumerateRemoteModuleData();
                module.BaseAddress = (IntPtr)mod.vaBase;
                module.Size = (IntPtr)mod.cbImageSize;
                module.Path = mod.wszFullName;
                modules.Add(module);
                writer.Write(PacketManager.getBytes(module));
            }
        }
    }
}
