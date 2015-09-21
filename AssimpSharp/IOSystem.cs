using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AssimpSharp
{
    public abstract class IOSystem
    {
        public IOSystem()
        {
        }

        public abstract bool Exists(string file);

        public abstract Stream Open(string file, FileMode mode = FileMode.Open);

        public abstract void Close(Stream file);

        public virtual bool ComparePaths(string one, string second)
        { 
            return string.Compare(one, second) == 0;
        }
    }
}
