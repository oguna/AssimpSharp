using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for FBX null markers attached to a node
    /// </summary>
    public class Null : NodeAttribute
    {
        public Null(ulong id, Element element, Document doc, string name)
            : base(id, element, doc, name)
        {
        }
    }
}
