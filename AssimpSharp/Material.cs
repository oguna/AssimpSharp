using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp
{
    public enum TextureOp
    {
        Multiply,
        Add,
        Subtract,
        Divide,
        SmoothAdd,
        SignedAdd,
    }

    public enum TextureMapMode
    {
        Wrap = 0x0,
        Clamp = 0x1,
        Decal = 0x3,
        Mirror = 0x2,
    }

    public enum TextureMapping
    {
        UV,
        Sphere,
        Cylinder,
        Box,
        Plane,
        Other
    }

    public enum TextureType
    {
        None,
        Diffuse,
        Specular,
        Ambient,
        Emissive,
        Height,
        Normals,
        Shininess,
        Opacity,
        Displacement,
        LightMap,
        Reflection,
        Unknown
    }

    public enum ShadingMode
    {
        Flat = 0x1,
        Gouraund,
        Phong,
        Blinn,
        Toon,
        OrenNayar,
        Minnaert,
        CookTorrance,
        NoShading,
        Fresnel
    }

    [Flags]
    public enum TextureFlags
    {
        Invert = 0x1,
        UseAlpha = 0x2,
        IgnoreAlpha = 0x4,
    }

    public enum BlendMode
    {
        Default = 0x1,
        Additive,
    }

    public struct UVTransform
    {
        public Vector2 Translation;
        public Vector2 Scaling;
        public float Rotation;
        public static UVTransform Identity
        {
            get{
                return new UVTransform()
                {
                    Translation = Vector2.Zero,
                    Scaling = new Vector2(1, 1),
                    Rotation = 0f,
                };
            }
        }
    }

    public enum PropertyTypeInfo
    {
        Float = 0x1,
        String,
        Integer,
        Buffer
    }

    public class MaterialProperty
    {
        public string Key;
        public int Semantic;
        public int Index;
        public int DataLength;
        public PropertyTypeInfo Type;
        public Object Data;
    }

    /// <summary>
    /// Data structure for a material
    /// </summary>
    public class Material
    {

        /// <summary>
        /// Name for default materials (2nd is used if meshes have UV coords)
        /// </summary>
        public const string AI_DEFAULT_MATERIAL_NAME = "DefaultMaterial";

        //protected List<MaterialProperty> Properties = new List<MaterialProperty>();

        //protected int NumProperties = 0;

        //public void Get(string key, int type, int idx, out int[] result, int max)
        //{ }
        //public void Get(string key, int type, int idx, out float[] result, int max)
        //{ }

        //public void Get(string key, int type, int idx, out int result)
        //{ }
        //public void Get(string key, int type, int idx, out float result)
        //{ }
        //public void Get(string key, int type, int idx, out string result)
        //{ }
        //public void Get(string key, int type, int idx, out Color3 result)
        //{ }
        //public void Get(string key, int type, int idx, out Color4 result)
        //{ }
        //public void Get(string key, int type, int idx, out UVTransform result)
        //{ }

        //public void GetTexture(TextureType type, int index, string path, TextureMapping mappin = null, int uvindex = 0, float blend = 0, TextureOp op = null, TextureMapMode mapmpde = null)
        //{ }

        //public void AddBinaryProperty(byte[] input, int sizeInBytes, string key, int type, int index, PropertyTypeInfo info)
        //{ }

        //public void AddProperty(string input, string key, int type = 0, int index = 0)
        //{ }

        //public void AddProperty<T>(T input, int numValues, string key, int type = 0, int index = 0)
        //{ }

        //public void AddProperty(Vector3 input, int numValues, string key, int type = 0, int index = 0)
        //{ }

        //public void GetMaterialFloatArray(Material mat, string key, int type, int index, out float[] result, int max)
        //{
        //}

        public Color4? ColorDiffuse;
        public string Name;
        public Color4? ColorEmissive;
        public Color4? ColorAmbient;
        public Color4? ColorSpecular;
        public Color4? ColorReflective;
        public Color4? ColorTransparent;
        public float? Opacity;
        public float? Reflectivity;
        public float? Shininess;
        public float? ShininessStrength;
        public ShadingMode? ShadingMode;
        public BlendMode? BlendMode;
        public float? BumpScaling;

        public TextureSlot TextureHeight;
        public TextureSlot TextureNormals;
        public TextureSlot TextureSpecular;
        public TextureSlot TextureAmbient;
        public TextureSlot TextureEmmisive;
        public TextureSlot TextureDiffuse;
        public TextureSlot TextureDisplacement;
        public TextureSlot TextureOpacity;
        public TextureSlot TextureShininess;

        public readonly TextureSlotCollection TextureSlotCollection;

        public Material()
        {
            TextureSlotCollection = new TextureSlotCollection(this);
        }
    }
}
