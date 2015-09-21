using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    // FBXDocument.h

    /// <summary>
    /// Base class for in-memory (DOM) representations of FBX objects
    /// </summary>
    public class Object
    {
        protected Element element;

        protected string name;

        protected ulong id;

        public Element SourceElement
        {
            get
            {
                return element;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public ulong ID
        {
            get
            {
                return id;
            }
        }

        public Object(ulong id, Element element, string name)
        {
            this.element = element;
            this.name = name;
            this.id = id;
        }
    }
}
