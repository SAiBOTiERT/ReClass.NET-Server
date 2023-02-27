using System;

namespace ReClassNET_Server
{
    public enum CommandType : Byte
    {
        EnumerateProcesses = 0,
        IsProcessValid = 1,
        OpenProcess = 2,
        CloseRemoteProcess = 3,
        ReadRemoteMemory = 4,
        EnumerateRemoteSectionsAndModules = 5
    }
}
