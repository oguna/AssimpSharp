using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for FBX limb node markers attached to a node
    /// </summary>
    public class LimbNode : NodeAttribute
    {
        public LimbNode(ulong id, Element element, Document doc, string name)
        : base(id, element, doc, name)
        {
        }
    }
}
