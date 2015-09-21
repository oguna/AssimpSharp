using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a FBX animation curve (i.e. a mapping from single animation curves to nodes)
    /// </summary>
    public class AnimationCurveNode : Object
    {
        private Object target;
        private PropertyTable props;
        private Dictionary<string, AnimationCurve> curves;
        private string prop;
        private Document doc;

        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        public Dictionary<string, AnimationCurve> Curves
        {
            get
            {
                if (curves.Count == 0)
                {
                    // resolve attached animation curves
                    var conns = doc.GetConnectionsByDestinationSequenced(ID, "AnimationCurve");

                    foreach(var con in conns)
                    {
                        // link should go for a property
                        if (string.IsNullOrEmpty(con.PropertyName))
                        {
                            continue;
                        }

                        var ob = con.SourceObject;
                        if (ob == null)
                        {
                            DocumentUtil.DOMWarning("failed to read source object for AnimationCurve->AnimationCurveNode link, ignoring", element);
                            continue;
                        }

                        var anim = ob as AnimationCurve;
                        if (anim == null)
                        {
                            DocumentUtil.DOMWarning("source object for ->AnimationCurveNode link is not an AnimationCurve", element);
                            continue;
                        }
                        curves[con.PropertyName] = anim;
                    }
                }
                return curves;
            }
        }

        public Object Target
        {
            get
            {
                return target;
            }
        }

        public Model TargetAsModel
        {
            get
            {
                return (Model)target;
            }
        }

        public NodeAttribute TargetAsNodeAttribute
        {
            get
            {
                return (NodeAttribute)target;
            }
        }

        public string TargetProperty
        {
            get
            {
                return this.prop;
            }
        }

        /// <summary>
        /// the optional whitelist specifies a list of property names for which the caller
        /// wants animations for. If the curve node does not match one of these, std::range_error
        /// will be thrown.
        /// </summary>
        public AnimationCurveNode(ulong id, Element element, string name, Document doc, string[] targetPropWhitelist = null)
        : base(id, element, name)
        {
            this.doc = doc;
            this.target = null;
            curves = new Dictionary<string, AnimationCurve>();

            var sc = Parser.GetRequiredScope(element);

            // find target node
            var whitelist = new[] { "Model", "NodeAttribute" };
            var conns = doc.GetConnectionsBySourceSequenced(ID, whitelist);

            foreach(var con in conns)
            {
                // link should go for a property
                if (string.IsNullOrEmpty(con.PropertyName))
                {
                    continue;
                }

                if (targetPropWhitelist != null)
                {
                    var s = con.PropertyName;
                    var ok = false;
                    for(int i=0; i<whitelist.Length; ++i)
                    {
                        if (s == targetPropWhitelist[i])
                        {
                            ok = true;
                            break;
                        }
                    }

                    if (!ok)
                    {
                        throw (new ArgumentOutOfRangeException("AnimationCurveNode target property is not in whitelist"));
                    }
                }

                var ob = con.DestinationObject;
                if (ob == null)
                {
                    DocumentUtil.DOMWarning("failed to read destination object for AnimationCurveNode->Model link, ignoring", element);
                    continue;
                }

                // XXX support constraints as DOM class
                //ai_assert(dynamic_cast<const Model*>(ob) || dynamic_cast<const NodeAttribute*>(ob));
                target = ob;
                if (target == null)
                {
                    continue;
                }

                prop = con.PropertyName;
                break;
            }

            if (target == null)
            {
                DocumentUtil.DOMWarning("failed to resolve target Model/NodeAttribute/Constraint for AnimationCurveNode", element);
            }

            props = DocumentUtil.GetPropertyTable(doc, "AnimationCurveNode.FbxAnimCurveNode", element, sc, false);
        }


    }
}
