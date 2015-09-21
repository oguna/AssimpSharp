using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a FBX animation curve (i.e. a 1-dimensional set of keyframes and values therefor)
    /// </summary>
    public class AnimationCurve : Object
    {
        private List<int> keys;
        private List<float> values;
        private List<float> attributes;
        private List<uint> flags;

        /// <summary>
        /// list of keyframe positions (time).
        /// </summary>
        public List<int> Keys
        {
            get
            {
                return keys;
            }
        }

        /// <summary>
        /// list of keyframe values.
        /// </summary>
        public List<float> Values
        {
            get
            {
                return values;
            }
        }

        public List<float> Attributes
        {
            get
            {
                return attributes;
            }
        }

        public List<uint> Flags
        {
            get
            {
                return flags;
            }
        }

        public AnimationCurve(ulong id, Element element, string name, Document doc)
            : base(id, element, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var KeyTime = Parser.GetRequiredElement(sc, "KeyTime");
            var KeyValueFloat = Parser.GetRequiredElement(sc, "KeyValueFloat");

            Parser.ParseVectorDataArray(out this.keys, KeyTime);
            Parser.ParseVectorDataArray(out this.values, KeyValueFloat);

            if (keys.Count != values.Count)
            {
                DocumentUtil.DOMError("the number of key times does not match the number of keyframe values", KeyTime);
            }

            // check if the key times are well-ordered
            for (int i = 0; i < keys.Count - 1; i++)
            {
                if (keys[i] > keys[i + 1])
                {
                    DocumentUtil.DOMError("the keyframes are not in ascending order", KeyTime);
                }
            }

            var keyAttrDataFloat = sc["KeyAttrDataFloat"];
            if (keyAttrDataFloat != null)
            {
                Parser.ParseVectorDataArray(out attributes, keyAttrDataFloat);
            }

            var keyAttrFlags = sc["KeyAttrFlags"];
            if (keyAttrFlags != null)
            {
                Parser.ParseVectorDataArray(out flags, keyAttrFlags);
            }
        }
    }
}
