using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for generic FBX NoteAttribute blocks. NoteAttribute's just hold a property table,
    /// fixed members are added by deriving classes.
    /// </summary>
    public class NodeAttribute : Object
    {
        public NodeAttribute(ulong id, Element element, Document doc, string name)
            :base(id, element, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var classname = Parser.ParseTokenAsString(Parser.GetRequiredToken(element, 2));
            var isNullOrLimb = (classname == "Null") || (classname == "LimbNode");
            props = DocumentUtil.GetPropertyTable(doc, "NodeAttribute.Fbx" + classname, element, sc, isNullOrLimb);
        }

        public PropertyTable Props
        {
            get
            {
                Debug.Assert(props != null);
                return props;
            }
        }

        private PropertyTable props;
    }
}
