using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReClassNET_Server
{
    internal static class Syscalls
    {
        private static IntPtr _ntdllBaseAddress = IntPtr.Zero;

        /// <summary>
        /// Gets the base address of ntdll.dll
        /// </summary>
        public static IntPtr NtDllBaseAddress
        {
            get
            {
                if (_ntdllBaseAddress == IntPtr.Zero)
                    _ntdllBaseAddress = GetNtdllBaseAddress();
                return _ntdllBaseAddress;
            }
        }


        private static readonly byte[] Shellcode =
        {
            0x4C, 0x8B, 0xD1, // mov r10, rcx
            0xB8, 0x00, 0x00, 0x00, 0x00, // mov eax, 0x00
            0x0F, 0x05, // syscall
            0xC3 // ret
        };

        public static Windows.Ntstatus ZwReadVirtualMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
            int dwSize, out IntPtr lpNumberOfBytesRead)
        {
            // dynamically resolve the syscall
            Shellcode[4] = GetSysCallId("NtReadVirtualMemory");


            unsafe
            {
                fixed (byte* ptr = Shellcode)
                {
                    var memoryAddress = (IntPtr)ptr;

                    if (!Windows.VirtualProtect(memoryAddress, (UIntPtr)Shellcode.Length,
                            (uint)Windows.AllocationProtectEnum.PageExecuteReadwrite, out _))
                    {
                        throw new Win32Exception();
                    }

                    var assembledFunction =
                        (Delegates.ZwReadVirtualMemory)Marshal.GetDelegateForFunctionPointer(memoryAddress,
                            typeof(Delegates.ZwReadVirtualMemory));

                    return assembledFunction(hProcess, lpBaseAddress, lpBuffer, dwSize, out lpNumberOfBytesRead);
                }
            }
        }


        private static IntPtr GetNtdllBaseAddress()
        {
            var hProc = Process.GetCurrentProcess();

            foreach (ProcessModule m in hProc.Modules)
            {
                if (m.ModuleName != null && m.ModuleName.ToUpper().Equals("NTDLL.DLL"))
                    return m.BaseAddress;
            }

            // we can't find the base address
            return IntPtr.Zero;
        }

        public static byte GetSysCallId(string functionName)
        {
            // first get the proc address
            var funcAddress = Windows.GetProcAddress(NtDllBaseAddress, functionName);

            byte count = 0;

            // loop until we find an unhooked function
            while (true)
            {
                // is the function hooked - we are looking for the 0x4C, 0x8B, 0xD1, instructions - this is the start of a syscall
                var hooked = false;

                var instructions = new byte[5];

                Marshal.Copy(funcAddress, instructions, 0, 5);
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(
                        new[] { instructions[0], instructions[1], instructions[2] }, new byte[] { 0x4C, 0x8B, 0xD1 }))
                    hooked = true;

                if (!hooked)
                    return (byte)(instructions[4] - count);

                funcAddress = (IntPtr)((ulong)funcAddress + 32);
                count++;
            }
        }

        private struct Delegates
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate
                // ReSharper disable once MemberHidesStaticFromOuterClass
                Windows.Ntstatus ZwReadVirtualMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer,
                    int dwSize, out IntPtr lpNumberOfBytesRead);
        };
    }
}