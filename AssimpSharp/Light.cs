using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    /// <summary>
    /// Enumerates all supported types of light sources.
    /// </summary>
    public enum LightSourceType
    {
        Undefined = 0x0,
        /// <summary>
        /// A directional light source has a well-defined direction
        /// but is infinitely far away. That's quite a good 
        /// approximation for sun light.
        /// </summary>
        Directional = 0x1,
        /// <summary>
        /// A point light source has a well-defined position
        /// in space but no direction - it emits light in all
        /// directions. A normal bulb is a point light.
        /// </summary>
        Point = 0x2,
        /// <summary>
        /// A spot light source emits light in a specific 
        /// angle. It has a position and a direction it is pointing to.
        /// A good example for a spot light is a light spot in
        /// sport arenas.
        /// </summary>
        Spot = 0x3
    }

    /// <summary>
    /// Helper structure to describe a light source.
    /// </summary>
    public class Light
    {
        /// <summary>
        /// The name of the light source.
        /// </summary>
        public string Name;

        /// <summary>
        /// The type of the light source.
        /// </summary>
        public LightSourceType Type;

        /// <summary>
        /// Position of the light source in space. Relative to the
	    ///  transformation of the node corresponding to the light.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Direction of the light source in space. Relative to the
	    /// transformation of the node corresponding to the light.
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// Constant light attenuation factor.
        /// </summary>
        public float AttenuationConstant;

        /// <summary>
        /// Linear light attenuation factor.
        /// </summary>
        public float AttenuationLinear;

        /// <summary>
        /// Quadratic light attenuation factor.
        /// </summary>
        public float AttenuationQuadratic;

        /// <summary>
        /// Diffuse color of the light source
        /// </summary>
        public Vector3 ColorDiffuse;

        /// <summary>
        /// Specular color of the light source
        /// </summary>
        public Vector3 ColorSpecular;

        /// <summary>
        /// Ambient color of the light source
        /// </summary>
        public Vector3 ColorAmbient;

        /// <summary>
        /// Inner angle of a spot light's light cone.
        /// </summary>
        public float AngleInnerCone;

        /// <summary>
        /// Outer angle of a spot light's light cone.
        /// </summary>
        public float AngleOuterCone;
        public Light()
        {
            this.Type = LightSourceType.Undefined;
            this.AttenuationConstant = 0f;
            this.AttenuationLinear = 1f;
            this.AttenuationQuadratic = 0f;
            this.AngleInnerCone = MathUtil.TwoPi;
            this.AngleOuterCone = MathUtil.TwoPi;
        }
    }
}
