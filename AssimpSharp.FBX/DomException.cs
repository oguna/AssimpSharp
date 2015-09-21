using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    // FBXDocumentUtil.h

    public class DomException : Exception
    {
        public DomException(string message, Token token)
            : base(message)
        {
            throw (new Exception(Util.AddTokenText("FBX-DOM", message, token)));
        }

        public DomException(string message, Element element = null)
            : base(message)
        {
            if (element != null)
            {
                throw (new DomException(message, element.KeyToken));
            }
            throw (new Exception("FBX-DOM " + message));
        }
    }
}
