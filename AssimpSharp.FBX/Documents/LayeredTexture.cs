using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for layered FBX textures
    /// </summary>
    public class LayeredTexture : Object
    {
        public enum Blend
        {
            Translucent,
            Additive,
            Modulate,
            Modulate2,
            Over,
            Normal,
            Dissolve,
            Darken,
            ColorBurn,
            LinearBurn,
            DarkerColor,
            Lighten,
            Screen,
            ColorDodge,
            LinearDodge,
            LighterColor,
            SoftLight,
            HardLight,
            VividLight,
            LinearLight,
            PinLight,
            HardMix,
            Difference,
            Exclusion,
            Subtract,
            Divide,
            Hue,
            Saturation,
            Color,
            Luminosity,
            Overlay,
            BlendModeCount
        };

        private Texture texture;
        private Blend blendMode;
        private float alpha;

        public LayeredTexture(ulong id, Element element, Document doc, string name)
        : base(id, element, name)
        {
            texture = null;
            blendMode = Blend.Modulate;
            alpha = 1;

            var sc = Parser.GetRequiredScope(element);
            var BlendModes = sc["BlendModes"];
            var Alphas = sc["Alphas"];
            if (BlendModes != null)
            {
                blendMode = (Blend)Parser.ParseTokenAsInt(Parser.GetRequiredToken(BlendModes, 0));
            }
            if (Alphas != null)
            {
                alpha = Parser.ParseTokenAsFloat(Parser.GetRequiredToken(Alphas, 0));
            }

        }

        public void FillTexture(Document doc)
        {
            var conns = doc.GetConnectionsByDestinationSequenced(ID);
            for (int i = 0; i < conns.Count; i++)
            {
                var con = conns[i];
                var ob = con.SourceObject;
                if (ob == null)
                {
                    DocumentUtil.DOMWarning("failed to read source object for texture link, ignoring", element);
                    continue;
                }
                var tex = ob as Texture;
                texture = tex;
            }
        }

        public Texture Texture
        {
            get
            {
                return texture;
            }
        }

        public Blend BlendMode
        {
            get
            {
                return blendMode;
            }
        }

        public float Alpha
        {
            get
            {
                return alpha;
            }
        }
    }
}
