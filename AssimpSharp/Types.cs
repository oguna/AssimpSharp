using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public enum Return
    {
        Success = 0x0,
        Failure = -0x1,
        OutOfMemory = -0x3,
    }

    public enum Origin
    {
        Set = 0x0,
        Cur = 0x1,
        End = 0x2
    }
}
