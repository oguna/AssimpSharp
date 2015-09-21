using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    // 完成

    /// <summary>
    /// DOM class for deformers
    /// </summary>
    public class Deformer : Object
    {
        public Deformer(ulong id, Element element, Document doc, string name)
        : base(id, element, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var classname = Parser.ParseTokenAsString(Parser.GetRequiredToken(element, 2));
            Props = DocumentUtil.GetPropertyTable(doc, "Deformer.Fbx" + classname, element, sc, true);
        }

        public PropertyTable Props { get; private set; }
    }
}
