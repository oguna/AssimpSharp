using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public class ParseException :Exception
    {
        public ParseException(string message, Token token)
            :base(Util.AddTokenText("FBX-Parsre",message,token))
        {
        }
        public ParseException(string message, Element element)
            : base(Util.AddTokenText("FBX-Parsre", message, element.KeyToken))
        {
        }
        public ParseException(string message)
            : base("FBX-Parser"+message)
        {
        }
    }
}
