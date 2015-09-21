using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a FBX animation stack (i.e. a list of animation layers)
    /// </summary>
    public class AnimationStack : Object
    {
        public AnimationStack(ulong id, Element element, string name, Document doc)
            : base(id, element, name)
        {
            var sc = Parser.GetRequiredScope(element);

            // note: we don't currently use any of these properties so we shouldn't bother if it is missing
            props = DocumentUtil.GetPropertyTable(doc, "AnimationStack.FbxAnimStack", element, sc, true);

            // resolve attached animation layers
            var conns = doc.GetConnectionsByDestinationSequenced(ID, "AnimationLayer");
            layers = new List<AnimationLayer>(conns.Count);
            foreach (var con in conns)
            {
                // link should not go to a property
                if (!string.IsNullOrEmpty(con.PropertyName))
                {
                    continue;
                }
                var ob = con.SourceObject;
                if (ob == null)
                {
                    DocumentUtil.DOMWarning("failed to read source object for AnimationLayer->AnimationStack link, ignoring", element);
                    continue;
                }
                var anim = ob as AnimationLayer;
                if (anim == null)
                {
                    DocumentUtil.DOMWarning("source object for ->AnimationStack link is not an AnimationLayer", element);
                    continue;
                }
                layers.Add(anim);
            }

            LocalStart = new SimpleProperty<long>(props, "LocalStart", 0);
            LocalStop = new SimpleProperty<long>(props, "LocalStop", 0);
            ReferenceStart = new SimpleProperty<long>(props, "ReferenceStart", 0);
            ReferenceStop = new SimpleProperty<long>(props, "ReferenceStop", 0);
        }

        public readonly SimpleProperty<long> LocalStart;
        public readonly SimpleProperty<long> LocalStop;
        public readonly SimpleProperty<long> ReferenceStart;
        public readonly SimpleProperty<long> ReferenceStop;

        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        public List<AnimationLayer> Layers
        {
            get
            {
                return layers;
            }
        }

        private PropertyTable props;
        private List<AnimationLayer> layers;
    }
}
