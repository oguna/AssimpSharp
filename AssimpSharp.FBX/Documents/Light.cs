using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for FBX lights attached to a node
    /// </summary>
    public class Light : NodeAttribute
    {
        public Light(ulong id, Element element, Document doc, string name)
            : base(id, element, doc, name)
        {
            Color = new SimpleProperty<Vector3>(Props, "Color", new Vector3(1, 1, 1));
            LightType = new SimpleProperty<Type>(Props, "LightType", (Type)0);
            CastLightOnObject = new SimpleProperty<bool>(Props, "CastLightOnObject", false);
            DrawVolumetricLight = new SimpleProperty<bool>(Props, "DrawVolumetricLight", true);
            DrawGroundProjection = new SimpleProperty<bool>(Props, "DrawGroundProjection", true);
            DrawFrontFacingVolumetricLight = new SimpleProperty<bool>(Props, "DrawFrontFacingVolumetricLight", false);
            Intensity = new SimpleProperty<float>(Props, "Intensity", 1.0f);
            InnerAngle = new SimpleProperty<float>(Props, "InnerAngle", 0.0f);
            OuterAngle = new SimpleProperty<float>(Props, "OuterAngle", 45.0f);
            Fog = new SimpleProperty<int>(Props, "Fog", 50);
            DecayType = new SimpleProperty<Decay>(Props, "DecayType", (Decay)0);
            DecayStart = new SimpleProperty<int>(Props, "DecayStart", 0);
            FileName = new SimpleProperty<string>(Props, "FileName", "");

            EnableNearAttenuation = new SimpleProperty<bool>(Props, "EnableNearAttenuation", false);
            NearAttenuationStart = new SimpleProperty<float>(Props, "NearAttenuationStart", 0f);
            NearAttenuationEnd = new SimpleProperty<float>(Props, "NearAttenuationEnd", 0f);
            EnableFarAttenuation = new SimpleProperty<bool>(Props, "EnableFarAttenuation", false);
            FarAttenuationStart = new SimpleProperty<float>(Props, "FarAttenuationStart", 0f);
            FarAttenuationEnd = new SimpleProperty<float>(Props, "FarAttenuationEnd", 0f);

            CastShadow = new SimpleProperty<bool>(Props, "CastShadows", true);
            ShadowColor = new SimpleProperty<Vector3>(Props, "ShadowColor", new Vector3(0, 0, 0));

            AreaLightShape = new SimpleProperty<int>(Props, "AreaLightShape", 0);

            LeftBarnDoor = new SimpleProperty<float>(Props, "LeftBarnDoor", 20.0f);
            RightBarnDoor = new SimpleProperty<float>(Props, "RightBarnDoor", 20.0f);
            TopBarnDoor = new SimpleProperty<float>(Props, "TopBarnDoor", 20.0f);
            BottomBarnDoor = new SimpleProperty<float>(Props, "BottomBarnDoor", 20.0f);
            EnableBarnDoor = new SimpleProperty<bool>(Props, "EnableBarnDoor", true);
        }

        public enum Type
        {
            Point,
            Directional,
            Spot,
            Area,
            Volume,
        };

        public enum Decay
        {
            None,
            Linear,
            Quadratic,
            Cubic,
        };

        public readonly SimpleProperty<Vector3> Color;
        public readonly SimpleProperty<Type> LightType;
        public readonly SimpleProperty<bool> CastLightOnObject;
        public readonly SimpleProperty<bool> DrawVolumetricLight;
        public readonly SimpleProperty<bool> DrawGroundProjection;
        public readonly SimpleProperty<bool> DrawFrontFacingVolumetricLight;
        public readonly SimpleProperty<float> Intensity;
        public readonly SimpleProperty<float> InnerAngle;
        public readonly SimpleProperty<float> OuterAngle;
        public readonly SimpleProperty<int> Fog;
        public readonly SimpleProperty<Decay> DecayType;
        public readonly SimpleProperty<int> DecayStart;
        public readonly SimpleProperty<string> FileName;

        public readonly SimpleProperty<bool> EnableNearAttenuation;
        public readonly SimpleProperty<float> NearAttenuationStart;
        public readonly SimpleProperty<float> NearAttenuationEnd;
        public readonly SimpleProperty<bool> EnableFarAttenuation;
        public readonly SimpleProperty<float> FarAttenuationStart;
        public readonly SimpleProperty<float> FarAttenuationEnd;

        public readonly SimpleProperty<bool> CastShadow;
        public readonly SimpleProperty<Vector3> ShadowColor;

        public readonly SimpleProperty<int> AreaLightShape;

        public readonly SimpleProperty<float> LeftBarnDoor;
        public readonly SimpleProperty<float> RightBarnDoor;
        public readonly SimpleProperty<float> TopBarnDoor;
        public readonly SimpleProperty<float> BottomBarnDoor;
        public readonly SimpleProperty<bool> EnableBarnDoor;
    }
}
