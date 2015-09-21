using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for all kinds of FBX geometry 
    /// </summary>
    public class Geometry : Object
    {
        public Geometry(ulong id, Element element, string name, Document doc)
        : base(id, element, name)
        {
            var conns = doc.GetConnectionsByDestinationSequenced(ID, "Deformer");
            foreach (var con in conns)
            {
                var sk = DocumentUtil.ProcessSimpleConnection<Skin>(con, false, "Skin -> Geometry", element);
                if (sk != null)
                {
                    skin = sk;
                    break;
                }
            }
        }

        private Skin skin;

        /// <summary>
        /// Get the Skin attached to this geometry or NULL
        /// </summary>
        public Skin DeformerSkin
        {
            get
            {
                return skin;
            }
        }
    }
}
