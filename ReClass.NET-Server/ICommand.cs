using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReClassNET_Server
{
    public interface ICommand
    {
        BinaryReader reader { get; set; }
        BinaryWriter writer { get; set; }
        void ProcessData();

    }
}
