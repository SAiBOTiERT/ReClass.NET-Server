using ReClassNET_Server.FPGA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReClassNET_Server
{
    public class FPGAWrapper
    {
        public static FPGAWrapper instance;
        private Vmm _vmm;

        public FPGAWrapper()
        {
            instance = this;
            _vmm = new Vmm("-printf", "-v", "-device", "fpga");
        }

        public bool IsProcessValid(uint pid)
        {
            var pi = _vmm.ProcessGetInformation(pid);
            return pi.fValid;
        }

        public List<Vmm.PROCESS_INFORMATION> getAllProcesses()
        {
            var processes = new List<Vmm.PROCESS_INFORMATION>();
            foreach (var pid in _vmm.PidList())
            {
                var proc = _vmm.ProcessGetInformation(pid);
                if (proc.fValid)
                {
                    processes.Add(proc);
                }
            }
            return processes.OrderBy(x => x.dwPID).ToList();
        }

        public List<Vmm.MAP_MODULEENTRY> getAllModules(uint pid)
        {
            var modules = new List<Vmm.MAP_MODULEENTRY>();
            foreach (var module in _vmm.Map_GetModule(pid))
            {
                if (module.fValid)
                {
                    modules.Add(module);
                }
            }
            return modules.OrderBy(x => x.vaBase).ToList();
        }

        public Dictionary<UInt64, Windows.MEMORY_BASIC_INFORMATION> getAllVads(uint pid, out Dictionary<UInt64, String> imageNames)
        {
            imageNames = new Dictionary<ulong, String>();
            var sortedList = _vmm.Map_GetVad(pid).OrderBy(x => x.vaStart).ToList();
            var full = new Dictionary<UInt64, Windows.MEMORY_BASIC_INFORMATION>();
            UInt64 ptr = 0;
            while (sortedList.Any())
            {
                var nextVad = sortedList.ElementAt(0);
                var curr = new Windows.MEMORY_BASIC_INFORMATION();
                if (nextVad.vaStart == ptr)
                {
                    sortedList.RemoveAt(0);
                    curr.BaseAddress = (IntPtr)nextVad.vaStart;
                    curr.RegionSize = (IntPtr)nextVad.cbSize;
                    curr.Protect = Windows.AllocationProtectEnum.PageReadwrite;
                    curr.Type = getWin32Type(nextVad);
                    curr.State = 0;
                    full.Add(ptr, curr);
                    if (curr.Type == Windows.TypeEnum.MEM_IMAGE)
                    {
                        imageNames.Add(ptr, nextVad.wszText);
                    }
                    ptr += (UInt64)nextVad.cbSize;
                }
                else
                {
                    curr.BaseAddress = (IntPtr)ptr;
                    curr.RegionSize = (IntPtr)(nextVad.vaStart - (UInt64)curr.BaseAddress);
                    curr.Protect = Windows.AllocationProtectEnum.PageNoaccess;
                    curr.Type = 0;
                    curr.State = Windows.StateEnum.MEM_FREE;
                    full.Add(ptr, curr);
                    ptr += (UInt64)curr.RegionSize;
                }
            }
            return full;
        }

        private Windows.TypeEnum getWin32Type(Vmm.MAP_VADENTRY vad)
        {
            if (vad.fImage) return Windows.TypeEnum.MEM_IMAGE;
            if (vad.fPrivateMemory) return Windows.TypeEnum.MEM_PRIVATE;
            return Windows.TypeEnum.MEM_MAPPED;
        }

        public bool WPM(uint pid, ulong qwA, byte[] data)
        {
            return _vmm.MemWrite(pid, qwA, data);
        }


        public unsafe byte[] RPM(uint pid, ulong qwA, uint cb)
        {
            byte[] data = new byte[cb];
            fixed (byte* pb = data)
            {
                vmmi.VMMDLL_MemReadEx(_vmm.hVMM, pid, qwA, pb, cb, out _,
                    Vmm.FLAG_NOCACHE | Vmm.FLAG_NOCACHEPUT | Vmm.FLAG_ZEROPAD_ON_FAIL);
            }
            return data;
        }

        ~FPGAWrapper()
        {
            _vmm.Close();
        }

    }
}