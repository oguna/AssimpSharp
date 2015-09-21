using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public class TokenizeException : Exception
    {
        public TokenizeException(string message, uint line, uint column)
        {
            throw (new Exception(Util.AddLineAndColumn("FBX-Tokenize", message, line, column)));
        }
    }
}
