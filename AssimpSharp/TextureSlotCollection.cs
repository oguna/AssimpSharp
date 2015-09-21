using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public class TextureSlotCollection
    {
        public readonly Material Material;

        public TextureSlotCollection(Material material)
        {
            Material = material;
        }

        public TextureSlot this[TextureType type]
        {
            get
            {
                switch (type)
                {
                    case TextureType.Diffuse:
                        return Material.TextureDiffuse;
                    case TextureType.Ambient:
                        return Material.TextureAmbient;
                    case TextureType.Normals:
                        return Material.TextureNormals;
                    case TextureType.Emissive:
                        return Material.TextureEmmisive;
                    case TextureType.Specular:
                        return Material.TextureSpecular;
                    case TextureType.Height:
                        return Material.TextureHeight;
                    case TextureType.Displacement:
                        return Material.TextureDisplacement;
                    case TextureType.Opacity:
                        return Material.TextureOpacity;
                    case TextureType.Shininess:
                        return Material.TextureShininess;
                    default:
                        throw (new KeyNotFoundException());
                }
            }
            set
            {
                switch (type)
                {
                    case TextureType.Diffuse:
                        Material.TextureDiffuse = value;
                        break;
                    case TextureType.Ambient:
                        Material.TextureAmbient = value;
                        break;
                    case TextureType.Normals:
                        Material.TextureNormals = value;
                        break;
                    case TextureType.Emissive:
                        Material.TextureEmmisive = value;
                        break;
                    case TextureType.Specular:
                        Material.TextureSpecular = value;
                        break;
                    case TextureType.Height:
                        Material.TextureHeight = value;
                        break;
                    case TextureType.Displacement:
                        Material.TextureDisplacement = value;
                        break;
                    case TextureType.Opacity:
                        Material.TextureOpacity = value;
                        break;
                    case TextureType.Shininess:
                        Material.TextureShininess = value;
                        break;
                    default:
                        throw (new KeyNotFoundException());
                }
            }
        }
    }
}
