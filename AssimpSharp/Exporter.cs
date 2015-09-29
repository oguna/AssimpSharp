using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public class Exporter
    {
        public delegate void ExportFunc(string file, IOSystem iosystem, Scene scene, ExportProperties propoerties);

        public struct ExportFormatEntry
        {
            public ExportFormatDesc Description;
            public ExportFunc ExportFunction;
            public int EnforcePP;
            public ExportFormatEntry(string id, string desc, string extension, ExportFunc function, int enforcePP = 0)
            {
                Description = new ExportFormatDesc()
                {
                    ID = id,
                    Description = desc,
                    FileExtension = extension,
                };
                ExportFunction = function;
                EnforcePP = enforcePP;
            }
        }

        public IOSystem IOHandler { get; set; }


    }
}
