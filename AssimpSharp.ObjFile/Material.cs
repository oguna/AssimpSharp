using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Data structure to store all material specific data
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Name of material description
        /// </summary>
        public string MaterialName;

        public string Texture;
        public string TextureSpecular;
        public string TextureAmbient;
        public string TextureEmissive;
        public string TextureBump;
        public string TextureNormal;
        public string TextureSpecularity;
        public string TextureOpacity;
        public string TextureDisp;

        public enum TextureType : int
        {
            DiffuseType = 0,
            SpecularType,
            AmbientType,
            EmmisiveType,
            BumpType,
            NormalType,
            SpecularityType,
            OpacityType,
            DispType
        }

        public bool[] Clamp;

        /// <summary>
        /// Ambient color
        /// </summary>
        public Color3 Ambient;

        /// <summary>
        /// Diffuse color
        /// </summary>
        public Color3 Diffuse;

        /// <summary>
        /// Specular color
        /// </summary>
        public Color3 Specular;

        /// <summary>
        /// Alpha value
        /// </summary>
        public float Alpha;

        /// <summary>
        /// Shineness factor
        /// </summary>
        public float Shineness;

        /// <summary>
        /// Illumination model
        /// </summary>
        public int IlluminationModel;

        /// <summary>
        /// Index of refrection
        /// </summary>
        public float IOR;
        internal Color3 Emissive;

        public Material()
        {
            Diffuse = new Color3(0.6f, 0.6f, 0.6f);
            Alpha = 1f;
            Shineness = 0f;
            IlluminationModel = 1;
            IOR = 1f;
            Clamp = new bool[Enum.GetValues(typeof(TextureType)).Length];
        }
    }
}
