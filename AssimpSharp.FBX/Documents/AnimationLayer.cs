using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public class AnimationLayer : Object
    {
        /// <summary>
        /// Represents a FBX animation layer (i.e. a list of node animations)
        /// </summary>
        public AnimationLayer(ulong id, Element element, string name, Document doc)
        : base(id, element, name)
        {
            this.doc = doc;
            var sc = Parser.GetRequiredScope(element);

            // note: the props table here bears little importance and is usually absent
            this.props = DocumentUtil.GetPropertyTable(doc, "AnimationLayer.FbxAnimLayer", element, sc, true);
        }

        private PropertyTable props;
        private Document doc;
        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        /// <summary>
        /// the optional whitelist specifies a list of property names for which the caller
        /// wants animations for. Curves not matching this list will not be added to the
        /// animation layer.
        /// </summary>
        public List<AnimationCurveNode> Nodes(string[] targetPropWhitelist = null)
        {
            var conns = doc.GetConnectionsByDestinationSequenced(ID, "AnimationCurveNode");
            var nodes = new List<AnimationCurveNode>(conns.Count);
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
                    DocumentUtil.DOMWarning("failed to read source object for AnimationCurveNode->AnimationLayer link, ignoring", element);
                    continue;
                }
                var anim = ob as AnimationCurveNode;
                if (anim == null)
                {
                    DocumentUtil.DOMWarning("source object for ->AnimationLayer link is not an AnimationCurveNode", element);
                    continue;
                }
                if (targetPropWhitelist != null)
                {
                    var s = anim.TargetProperty;
                    var ok = false;
                    for (int i = 0; i < targetPropWhitelist.Length; i++)
                    {
                        if (s == targetPropWhitelist[i])
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        continue;
                    }
                }
                nodes.Add(anim);
            }
            return nodes; // pray for NRVO
        }
    }
}
