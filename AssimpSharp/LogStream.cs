using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public abstract class LogStream
    {
        protected LogStream()
        { }

        public abstract void Write(string message);

        public LogStream CreateDefaultStream(DefaultLogStream stream, string name = "AssimpLog.txt", IOSystem io = null)
        {
            throw (new NotImplementedException());
        }
    }
}
