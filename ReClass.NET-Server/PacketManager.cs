using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ReClassNET_Server
{

    public class PacketManager
    {

        private Dictionary<CommandType, Type> commandHandler = new Dictionary<CommandType, Type>();
        private Mode mode;

        public PacketManager(Mode mode)
        {
            this.mode = mode;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            
            foreach (var ct in Enum.GetValues(typeof(CommandType)).Cast<CommandType>())  
            {  
                var modeStr = mode.ToString();
                if(mode == Mode.x86 || mode == Mode.x64)
                {
                    modeStr = "Win";
                }
                var type = Type.GetType("ReClassNET_Server."+modeStr+"."+ct.ToString());
                if(type == null)
                {
                    throw new Exception("Class not found: \"ReClassNET_Server."+modeStr+"."+ct.ToString()+"\"");
                }
                RegisterCommand(ct, type);
            }
        } 

        private void RegisterCommand(CommandType commandType, Type classType)
        {
            if (commandHandler.ContainsKey(commandType))
                commandHandler.Remove(commandType);
            commandHandler.Add(commandType, classType);
        }

        public static byte[] getBytes(object str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        public void HandlePackage(NetworkStream stream)
        {
            var reader = new BinaryReader(stream);

            CommandType type = (CommandType)reader.ReadByte();

            Type classType;
            if(!commandHandler.TryGetValue(type, out classType))
            {
                Console.WriteLine("Missing Command Handler");
                return;
            }
            Console.WriteLine("Command: " + type.ToString() + " received");
 
            var command = (ICommand)Activator.CreateInstance(classType);
            var writer = new BinaryWriter(stream);
            command.reader = reader;
            command.writer = writer;
            command.Initialize();
            command.ProcessData();
            command.Unintialize();
            writer.Flush();
            
        }
    }
}
