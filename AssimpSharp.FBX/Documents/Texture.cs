using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for generic FBX textures
    /// </summary>
    public class Texture : Object
    {
        private Vector2 uvTrans;
        private Vector2 uvScaling;
        private string type;
        private string relativeFileName;
        private string fileName;
        private string alphaSource;
        private PropertyTable props;
        private Tuple<uint, uint, uint, uint> crop;

        public Texture(ulong id, Element element, Document doc, string name)
            : base(id, element, name)
        {
            uvScaling = new Vector2(1, 1);

            var sc = Parser.GetRequiredScope(element);
            var Type = sc["Type"];
            var FileName = sc["FileName"];
            var RelativeFilename = sc["RelativeFilename"];
            var ModelUVTranslation = sc["ModelUVTranslation"];
            var ModelUVScaling = sc["ModelUVScaling"];
            var Texture_Alpha_Source = sc["Texture_Alpha_Source"];
            var Cropping = sc["Cropping"];

            if (Type != null)
            {
                type = Parser.ParseTokenAsString(Parser.GetRequiredToken(Type, 0));
            }
            if (FileName != null)
            {
                fileName = Parser.ParseTokenAsString(Parser.GetRequiredToken(FileName, 0));
            }
            if (RelativeFilename != null)
            {
                relativeFileName = Parser.ParseTokenAsString(Parser.GetRequiredToken(RelativeFilename, 0));
            }
            if (ModelUVTranslation != null)
            {
                uvTrans = new Vector2(Parser.ParseTokenAsFloat(Parser.GetRequiredToken(ModelUVTranslation, 0)), Parser.ParseTokenAsFloat(Parser.GetRequiredToken(ModelUVTranslation, 1)));
            }
            if (ModelUVScaling != null)
            {
                uvTrans = new Vector2(Parser.ParseTokenAsFloat(Parser.GetRequiredToken(ModelUVScaling, 0)), Parser.ParseTokenAsFloat(Parser.GetRequiredToken(ModelUVScaling, 1)));
            }
            if (Cropping != null)
            {
                uint i1 = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Cropping, 0));
                uint i2 = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Cropping, 1));
                uint i3 = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Cropping, 2));
                uint i4 = (uint)Parser.ParseTokenAsInt(Parser.GetRequiredToken(Cropping, 3));
                crop = new Tuple<uint, uint, uint, uint>(i1, i2, i3, i4);
            }
            else
            {
                crop = new Tuple<uint, uint, uint, uint>(0, 0, 0, 0);
            }
            if (Texture_Alpha_Source != null)
            {
                alphaSource = Parser.ParseTokenAsString(Parser.GetRequiredToken(Texture_Alpha_Source, 0));
            }
            props = DocumentUtil.GetPropertyTable(doc, "Texture.FbxFileTexture", element, sc);
        }

        public string Type
        {
            get
            {
                return type;
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public string RelativeFilename
        {
            get
            {
                return relativeFileName;
            }
        }

        public string AlphaSource
        {
            get
            {
                return alphaSource;
            }
        }

        public Vector2 UVTranslation
        {
            get
            {
                return uvTrans;
            }
        }

        public Vector2 UVScaling
        {
            get
            {
                return uvScaling;
            }
        }

        public PropertyTable Props
        {
            get
            {
                Debug.Assert(props != null);
                return props;
            }
        }

        public Tuple<uint, uint, uint, uint> Crop
        {
            get
            {
                return crop;
            }
        }
    }
}
