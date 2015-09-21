using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for FBX cameras attached to a node
    /// </summary>
    public class Camera : NodeAttribute
    {
        public Camera(ulong id, Element element, Document doc, string name)
        :base(id, element, doc, name)
        {
            Position = new SimpleProperty<Vector3>(Props, "Position", new Vector3(0, 0, 0));
            UpVector = new SimpleProperty<Vector3>(Props, "UpVector", new Vector3(0, 1, 0));
            InterestPosition = new SimpleProperty<Vector3>(Props, "InterestPosition", new Vector3(0, 0, 0));
            AspectWidth = new SimpleProperty<float>(Props, "AspectWidth", 1.0f);
            AspectHeight = new SimpleProperty<float>(Props, "AspectHeight", 1.0f);
            FilmWidth = new SimpleProperty<float>(Props, "FilmWidth", 1.0f);
            FilmHeight = new SimpleProperty<float>(Props, "FilmHeight", 1.0f);
            FilmAspectRatio = new SimpleProperty<float>(Props, "FilmAspectRatio", 1.0f);
            ApertureMode = new SimpleProperty<int>(Props, "ApertureMode", 0);
            FieldOfView = new SimpleProperty<float>(Props, "FieldOfView", 1.0f);
            FocalLength = new SimpleProperty<float>(Props, "FocalLength", 1.0f);
        }

        public readonly SimpleProperty<Vector3> Position;
        public readonly SimpleProperty<Vector3> UpVector;
        public readonly SimpleProperty<Vector3> InterestPosition;
        public readonly SimpleProperty<float> AspectWidth;
        public readonly SimpleProperty<float> AspectHeight;
        public readonly SimpleProperty<float> FilmWidth;
        public readonly SimpleProperty<float> FilmHeight;
        public readonly SimpleProperty<float> FilmAspectRatio;
        public readonly SimpleProperty<int> ApertureMode;
        public readonly SimpleProperty<float> FieldOfView;
        public readonly SimpleProperty<float> FocalLength;
    }
}
