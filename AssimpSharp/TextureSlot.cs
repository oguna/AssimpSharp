using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public class TextureSlot
    {
        public string TextureBase;
        public UVTransform UVTransformBase;
        public int UVWSrcBase;
        public TextureType TextureType;
        public int TextureIndex;
        public TextureMapping Mapping;
        public int UVIndex;
        public float BlendFactor;
        public TextureOp Operation;
        public TextureMapMode MappingModeU;
        public TextureMapMode MappingModeV;
        public int Flags;
    }
}
