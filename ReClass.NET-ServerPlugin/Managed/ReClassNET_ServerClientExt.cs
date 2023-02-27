using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using ReClassNET.Core;
using ReClassNET.Debugger;
using ReClassNET.Extensions;
using ReClassNET.Plugins;

// The namespace name must equal the plugin name
namespace ReClassNET_ServerPlugin
{
    /// <summary>The class name must equal the namespace name + "Ext"</summary>
    public class ReClassNET_ServerPluginExt : Plugin, ICoreProcessFunctions
    {
        private IPluginHost host;
        private TcpClient client;
        private readonly object sync = new object();


        //public override Image Icon => Properties.Resources.icon;
        private T[] Read<T>(Byte[] buf, int count) where T : struct
        {
            var sz = Marshal.SizeOf<T>();
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                var result = new T[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + i * sz, typeof(T));
                }

                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        private T Read<T>(Byte[] buf) where T : struct => Read<T>(buf, 1)[0];

        private List<T> ReadFromStream<T>(BinaryReader reader) where T : struct
        {
            var result = new List<T>();
            var sz = Marshal.SizeOf<T>();
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var buffer = reader.ReadBytes(sz);
                result.Add(Read<T>(buffer));
            }
            return result;
        }

        public override bool Initialize(IPluginHost host)
        {
            Contract.Requires(host != null);

            this.host = host ?? throw new ArgumentNullException(nameof(host));

            var provider = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            host.Process.CoreFunctions.RegisterFunctions(provider, this);

            try
            {
                client = new TcpClient("localhost", 8080);
                host.Process.CoreFunctions.SetActiveFunctionsProvider(provider);
            }catch(Exception e){
                throw e;    
            }
            return true;
        }

        public override void Terminate()
        {
            host = null;
        }

        /// <summary>Opens a file browser dialog and reports the selected file.</summary>
        /// <param name="callbackProcess">The callback which gets called for the selected file.</param>
        public void EnumerateProcesses(EnumerateProcessCallback callbackProcess)
        {
            if (callbackProcess == null)
            {
                return;
            }
            lock (sync)
            {

                NetworkStream stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.EnumerateProcesses);
                writer.Flush();

                var procData = ReadFromStream<EnumerateProcessData>(reader);
                foreach (var proc in procData)
                {
                    var data = proc;
                    callbackProcess(ref data);
                }
            }
        }

        /// <summary>Queries if the file is valid.</summary>
        /// <param name="process">The file to check.</param>
        /// <returns>True if the file is valid, false if not.</returns>
        public bool IsProcessValid(IntPtr process)
        {
            lock (sync)
            {
                NetworkStream stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.IsProcessValid);
                writer.Write(process);
                writer.Flush();

                var isValid = reader.ReadBoolean();
                return isValid;
            }
        }

        /// <summary>Opens the file.</summary>
        /// <param name="id">The file id.</param>
        /// <param name="desiredAccess">The desired access. (ignored)</param>
        /// <returns>A plugin internal handle to the file.</returns>
        public IntPtr OpenRemoteProcess(IntPtr id, ProcessAccess desiredAccess)
        {
            lock (sync)
            {
                NetworkStream stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.OpenProcess);
                writer.Write(id);
                writer.Write((uint)desiredAccess);
                writer.Flush();

                return reader.ReadIntPtr();
            }
        }

        /// <summary>Closes the file.</summary>
        /// <param name="process">The file to close.</param>
        public void CloseRemoteProcess(IntPtr process)
        {
            lock (sync)
            {
                NetworkStream stream = client.GetStream();
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.CloseRemoteProcess);
                writer.Write(process);
                writer.Flush();
            }
        }

        /// <summary>Reads memory of the file.</summary>
        /// <param name="process">The process to read from.</param>
        /// <param name="address">The address to read from.</param>
        /// <param name="buffer">[out] The buffer to read into.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="size">The size of the memory to read.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        public bool ReadRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            lock (sync)
            {
                NetworkStream stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.ReadRemoteMemory);
                writer.Write(process);
                writer.Write(address);
                writer.Write(size);
                writer.Flush();

                var result = reader.ReadBoolean();
                if (result)
                {
                    var data = reader.ReadBytes(size);
                    data.CopyTo(buffer, offset);
                }
                return result;
            }
        }

        /// <summary>Not supported.</summary>
        /// <param name="process">The file to write to.</param>
        /// <param name="address">The address to write to.</param>
        /// <param name="buffer">[in] The memory to write.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="size">The size of the memory to write.</param>
        /// <returns>True if it succeeds, false if it fails.</returns>
        public bool WriteRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            // Not supported.

            return false;
        }

        /// <summary>Reports a single module and section for the loaded file.</summary>
        /// <param name="process">The process.</param>
        /// <param name="callbackSection">The callback which gets called for every section.</param>
        /// <param name="callbackModule">The callback which gets called for every module.</param>
        public void EnumerateRemoteSectionsAndModules(IntPtr process, EnumerateRemoteSectionCallback callbackSection, EnumerateRemoteModuleCallback callbackModule)
        {
            if (callbackSection == null && callbackModule == null)
            {
                return;
            }
            lock (sync)
            {

                NetworkStream stream = client.GetStream();
                var reader = new BinaryReader(stream);
                var writer = new BinaryWriter(stream);

                writer.Write((Byte)ReClassNET_Server.CommandType.EnumerateRemoteSectionsAndModules);
                writer.Write(process);
                writer.Flush();

                var sections = ReadFromStream<EnumerateRemoteSectionData>(reader);
                if (callbackSection != null)
                {
                    foreach (var section in sections)
                    {
                        var data = section;
                        callbackSection(ref data);
                    }
                }
                var modules = ReadFromStream<EnumerateRemoteModuleData>(reader);
                if (callbackModule != null)
                {
                    foreach (var module in modules)
                    {
                        var data = module;
                        callbackModule(ref data);
                    }
                }
            }
        }

        public void ControlRemoteProcess(IntPtr process, ControlRemoteProcessAction action)
        {
            // Not supported.
        }

        public bool AttachDebuggerToProcess(IntPtr id)
        {
            // Not supported.

            return false;
        }

        public void DetachDebuggerFromProcess(IntPtr id)
        {
            // Not supported.
        }

        public bool AwaitDebugEvent(ref DebugEvent evt, int timeoutInMilliseconds)
        {
            // Not supported.

            return false;
        }

        public void HandleDebugEvent(ref DebugEvent evt)
        {
            // Not supported.
        }

        public bool SetHardwareBreakpoint(IntPtr id, IntPtr address, HardwareBreakpointRegister register, HardwareBreakpointTrigger trigger, HardwareBreakpointSize size, bool set)
        {
            // Not supported.

            return false;
        }
    }

}
