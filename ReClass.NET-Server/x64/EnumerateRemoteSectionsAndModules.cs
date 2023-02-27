using ReClassNET.Core;
using ReClassNET.Extensions;
using ReClassNET.Memory;
using ReClassNET_Server;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static ReClassNET_Server.Windows;

namespace ReClassNET_Server.x64
{
    internal class EnumerateRemoteSectionsAndModules : ICommand
    {
        public BinaryReader reader { get; set; }
        public BinaryWriter writer { get; set; }

        public void Initialize()
        {

        }

        public void ProcessData()
        {
            var process = reader.ReadIntPtr();

            long currentAddress = 0;
            int ret = 0;
            var sections = new List<EnumerateRemoteSectionData>();
            var modules = new List<EnumerateRemoteModuleData>();
            while (true)
            {
                MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();
                ret = VirtualQueryEx(process, (IntPtr)currentAddress, out mbi, (uint)Marshal.SizeOf(mbi));
                if (ret == 0)
                    break;
                currentAddress += (long)mbi.RegionSize;

                if (mbi.State != StateEnum.MEM_COMMIT) continue;

                var section = new EnumerateRemoteSectionData();
                section.BaseAddress = mbi.BaseAddress;
                section.Size = mbi.RegionSize;
                if (mbi.Type == TypeEnum.MEM_MAPPED)
                {
                    section.Type = SectionType.Mapped;
                }
                else if (mbi.Type == TypeEnum.MEM_PRIVATE)
                {
                    section.Type = SectionType.Private;
                }
                else if (mbi.Type == TypeEnum.MEM_IMAGE)
                {
                    section.Type = SectionType.Image;
                }
                else
                {
                    section.Type = SectionType.Unknown;
                }
                section.Category = section.Type == SectionType.Private ? SectionCategory.HEAP : SectionCategory.Unknown;
                section.Protection = SectionProtection.NoAccess;
                if ((mbi.Protect & AllocationProtectEnum.PageExecute) == AllocationProtectEnum.PageExecute)
                {
                    section.Protection |= SectionProtection.Execute;
                }
                if ((mbi.Protect & AllocationProtectEnum.PageExecuteRead) == AllocationProtectEnum.PageExecuteRead) section.Protection |= SectionProtection.Execute | SectionProtection.Read;
                if ((mbi.Protect & AllocationProtectEnum.PageExecuteReadwrite) == AllocationProtectEnum.PageExecuteReadwrite) section.Protection |= SectionProtection.Execute | SectionProtection.Read | SectionProtection.Write;
                if ((mbi.Protect & AllocationProtectEnum.PageExecuteWritecopy) == AllocationProtectEnum.PageExecuteWritecopy) section.Protection |= SectionProtection.Execute | SectionProtection.Read | SectionProtection.CopyOnWrite;
                if ((mbi.Protect & AllocationProtectEnum.PageReadonly) == AllocationProtectEnum.PageReadonly) section.Protection |= SectionProtection.Read;
                if ((mbi.Protect & AllocationProtectEnum.PageReadwrite) == AllocationProtectEnum.PageReadwrite) section.Protection |= SectionProtection.Read | SectionProtection.Write;
                if ((mbi.Protect & AllocationProtectEnum.PageWritecopy) == AllocationProtectEnum.PageWritecopy) section.Protection |= SectionProtection.Read | SectionProtection.CopyOnWrite;
                if ((mbi.Protect & AllocationProtectEnum.PageGuard) == AllocationProtectEnum.PageGuard) section.Protection |= SectionProtection.Guard;

                section.Name = "";
                section.ModulePath = "";

                sections.Add(section);
            }

            writer.Write(sections.Count);
            if (sections.Count > 0)
            {
                foreach (var section in sections)
                {
                    writer.Write(PacketManager.getBytes(section));
                }
            }

            var pid = GetProcessId(process);

            var handle = CreateToolhelp32Snapshot(SnapshotFlags.Module, pid);
            if (handle != (IntPtr)(-1))
            {
                var moduleEntry = new MODULEENTRY32();
                moduleEntry.dwSize = (uint)Marshal.SizeOf(moduleEntry);
                var result = Module32First(handle, ref moduleEntry);
                if (result)
                {
                    do
                    {
                        var module = new EnumerateRemoteModuleData();
                        module.BaseAddress = moduleEntry.modBaseAddr;
                        module.Size = (IntPtr)moduleEntry.modBaseSize;
                        module.Path = moduleEntry.szExePath;
                        modules.Add(module);

                    } while (Module32Next(handle, ref moduleEntry));
                }
            }

            writer.Write(modules.Count);
            if (modules.Count > 0)
            {
                foreach (var module in modules)
                {
                    writer.Write(PacketManager.getBytes(module));
                }
            }
        }

        public void Unintialize()
        {

        }
    }
}
