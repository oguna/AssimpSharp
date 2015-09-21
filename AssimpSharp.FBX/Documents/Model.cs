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
    /// DOM base class for FBX models (even though its semantics are more "node" than "model"
    /// </summary>
    public class Model : Object
    {
        public Model(ulong id, Element element, Document doc, string name)
        : base(id, element, name)
        {
            shading = "Y";
            var sc = Parser.GetRequiredScope(element);
            var Shading = sc["Shading"];
            var Culling = sc["Culling"];
            if (Shading != null)
            {
                shading = Parser.GetRequiredToken(Shading, 0).StringContents;
            }
            if (Culling != null)
            {
                culling = Parser.ParseTokenAsString(Parser.GetRequiredToken(Culling, 0));
            }
            props = DocumentUtil.GetPropertyTable(doc, "Model.FbxNode", element, sc);
            ResolveLink(element, doc);

            this.QuaternionInterpolate = new SimpleProperty<int>(this.Props, "QuaternionInterpolate", 0);

            this.RotationOrder = new SimpleProperty<RotOrder>(this.Props, "RotationOrder", (RotOrder)0);
            this.RotationOffset = new SimpleProperty<Vector3>(this.Props, "RotationOffset", new Vector3());
            this.RotationPivot = new SimpleProperty<Vector3>(this.Props, "RotationPivot", new Vector3());
            this.ScalingOffset = new SimpleProperty<Vector3>(this.Props, "ScalingOffset", new Vector3());
            this.ScalingPivot = new SimpleProperty<Vector3>(this.Props, "ScalingPivot", new Vector3());
            this.TranslationActive = new SimpleProperty<bool>(this.Props, "TranslationActive", false);

            this.TranslationMin = new SimpleProperty<Vector3>(this.Props, "TranslationMin", new Vector3());
            this.TranslationMax = new SimpleProperty<Vector3>(this.Props, "TranslationMax", new Vector3());

            this.TranslationMinX = new SimpleProperty<bool>(this.Props, "TranslationMinX", false);
            this.TranslationMaxX = new SimpleProperty<bool>(this.Props, "TranslationMaxX", false);
            this.TranslationMinY = new SimpleProperty<bool>(this.Props, "TranslationMinY", false);
            this.TranslationMaxY = new SimpleProperty<bool>(this.Props, "TranslationMaxY", false);
            this.TranslationMinZ = new SimpleProperty<bool>(this.Props, "TranslationMinZ", false);
            this.TranslationMaxZ = new SimpleProperty<bool>(this.Props, "TranslationMaxZ", false);

            this.RotationOrder = new SimpleProperty<RotOrder>(this.Props, "RotationOrder", (RotOrder)0);
            this.RotationSpaceForLimitOnly = new SimpleProperty<bool>(this.Props, "RotationSpaceForLimitOnly", false);
            this.RotationStiffnessX = new SimpleProperty<float>(this.Props, "RotationStiffnessX", 0.0f);
            this.RotationStiffnessY = new SimpleProperty<float>(this.Props, "RotationStiffnessY", 0.0f);
            this.RotationStiffnessZ = new SimpleProperty<float>(this.Props, "RotationStiffnessZ", 0.0f);
            this.AxisLen = new SimpleProperty<float>(this.Props, "AxisLen", 0.0f);

            this.PreRotation = new SimpleProperty<Vector3>(this.Props, "PreRotation", new Vector3());
            this.PostRotation = new SimpleProperty<Vector3>(this.Props, "PostRotation", new Vector3());
            this.RotationActive = new SimpleProperty<bool>(this.Props, "RotationActive", false);

            this.RotationMin = new SimpleProperty<Vector3>(this.Props, "RotationMin", new Vector3());
            this.RotationMax = new SimpleProperty<Vector3>(this.Props, "RotationMax", new Vector3());

            this.RotationMinX = new SimpleProperty<bool>(this.Props, "RotationMinX", false);
            this.RotationMaxX = new SimpleProperty<bool>(this.Props, "RotationMaxX", false);
            this.RotationMinY = new SimpleProperty<bool>(this.Props, "RotationMinY", false);
            this.RotationMaxY = new SimpleProperty<bool>(this.Props, "RotationMaxY", false);
            this.RotationMinZ = new SimpleProperty<bool>(this.Props, "RotationMinZ", false);
            this.RotationMaxZ = new SimpleProperty<bool>(this.Props, "RotationMaxZ", false);
            this.InheritType = new SimpleProperty<TransformInheritance>(this.Props, "InheritType", (TransformInheritance)0);

            this.ScalingActive = new SimpleProperty<bool>(this.Props, "ScalingActive", false);
            this.ScalingMin = new SimpleProperty<Vector3>(this.Props, "ScalingMin", new Vector3());
            this.ScalingMax = new SimpleProperty<Vector3>(this.Props, "ScalingMax", new Vector3(1.0f, 1.0f, 1.0f));
            this.ScalingMinX = new SimpleProperty<bool>(this.Props, "ScalingMinX", false);
            this.ScalingMaxX = new SimpleProperty<bool>(this.Props, "ScalingMaxX", false);
            this.ScalingMinY = new SimpleProperty<bool>(this.Props, "ScalingMinY", false);
            this.ScalingMaxY = new SimpleProperty<bool>(this.Props, "ScalingMaxY", false);
            this.ScalingMinZ = new SimpleProperty<bool>(this.Props, "ScalingMinZ", false);
            this.ScalingMaxZ = new SimpleProperty<bool>(this.Props, "ScalingMaxZ", false);

            this.GeometricTranslation = new SimpleProperty<Vector3>(this.Props, "GeometricTranslation", new Vector3());
            this.GeometricRotation = new SimpleProperty<Vector3>(this.Props, "GeometricRotation", new Vector3());
            this.GeometricScaling = new SimpleProperty<Vector3>(this.Props, "GeometricScaling", new Vector3(1.0f, 1.0f, 1.0f));

            this.MinDampRangeX = new SimpleProperty<float>(this.Props, "MinDampRangeX", 0.0f);
            this.MinDampRangeY = new SimpleProperty<float>(this.Props, "MinDampRangeY", 0.0f);
            this.MinDampRangeZ = new SimpleProperty<float>(this.Props, "MinDampRangeZ", 0.0f);
            this.MaxDampRangeX = new SimpleProperty<float>(this.Props, "MaxDampRangeX", 0.0f);
            this.MaxDampRangeY = new SimpleProperty<float>(this.Props, "MaxDampRangeY", 0.0f);
            this.MaxDampRangeZ = new SimpleProperty<float>(this.Props, "MaxDampRangeZ", 0.0f);

            this.MinDampStrengthX = new SimpleProperty<float>(this.Props, "MinDampStrengthX", 0.0f);
            this.MinDampStrengthY = new SimpleProperty<float>(this.Props, "MinDampStrengthY", 0.0f);
            this.MinDampStrengthZ = new SimpleProperty<float>(this.Props, "MinDampStrengthZ", 0.0f);
            this.MaxDampStrengthX = new SimpleProperty<float>(this.Props, "MaxDampStrengthX", 0.0f);
            this.MaxDampStrengthY = new SimpleProperty<float>(this.Props, "MaxDampStrengthY", 0.0f);
            this.MaxDampStrengthZ = new SimpleProperty<float>(this.Props, "MaxDampStrengthZ", 0.0f);

            this.PreferredAngleX = new SimpleProperty<float>(this.Props, "PreferredAngleX", 0.0f);
            this.PreferredAngleY = new SimpleProperty<float>(this.Props, "PreferredAngleY", 0.0f);
            this.PreferredAngleZ = new SimpleProperty<float>(this.Props, "PreferredAngleZ", 0.0f);

            this.Show = new SimpleProperty<bool>(this.Props, "Show", true);
            this.LODBox = new SimpleProperty<bool>(this.Props, "LODBox", false);
            this.Freeze = new SimpleProperty<bool>(this.Props, "Freeze", false);
        }

        public enum RotOrder
        {
            EulerXYZ = 0,
            EulerXZY,
            EulerYZX,
            EulerYXZ,
            EulerZXY,
            EulerZYX,
            SphericXYZ,
        };

        public enum TransformInheritance
        {
            RrSs = 0,
            RSrs,
            Rrs,
        };

        private List<Material> materials;
        private List<Geometry> geometry;
        private List<NodeAttribute> attributes;
        private string shading;
        private string culling;
        private PropertyTable props;

        public string Shading
        {
            get
            {
                return shading;
            }
        }

        public string Culling
        {
            get
            {
                return culling;
            }
        }

        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        public List<Material> Materials
        {
            get
            {
                return materials;
            }
        }

        public List<Geometry> Geometry
        {
            get
            {
                return geometry;
            }
        }

        public List<NodeAttribute> Attributes
        {
            get
            {
                return attributes;
            }
        }

        public bool IsNull
        {
            get
            {
                var attrs = Attributes;
                foreach (var att in attrs)
                {
                    Null nullTag = att as Null;
                    if (nullTag != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void ResolveLink(Element element, Document doc)
        {
            var arr = new[] { "Geometry", "Material", "NodeAttribute" };
            var conns = doc.GetConnectionsByDestinationSequenced(ID, arr);
            materials = new List<Material>(conns.Count);
            geometry = new List<Geometry>(conns.Count);
            attributes = new List<NodeAttribute>(conns.Count);
            foreach (var con in conns)
            {
                if (con.PropertyName.Length > 0)
                {
                    continue;
                }
                Object ob = con.SourceObject;
                if (ob == null)
                {
                    Debug.WriteLine("failed to read source object for incoming Model link, ignoring");
                    continue;
                }
                Material mat = ob as Material;
                if (mat != null)
                {
                    materials.Add(mat);
                    continue;
                }
                Geometry geo = ob as Geometry;
                if (geo != null)
                {
                    geometry.Add(geo);
                    continue;
                }
                NodeAttribute att = ob as NodeAttribute;
                if (att != null)
                {
                    attributes.Add(att);
                    continue;
                }
                Debug.WriteLine("source object for model link is neither Material, NodeAttribute nor Geometry, ignoring");
                continue;
            }
        }

        public readonly SimpleProperty<int> QuaternionInterpolate;

        public readonly SimpleProperty<Vector3> RotationOffset;
        public readonly SimpleProperty<Vector3> RotationPivot;
        public readonly SimpleProperty<Vector3> ScalingOffset;
        public readonly SimpleProperty<Vector3> ScalingPivot;
        public readonly SimpleProperty<bool> TranslationActive;

        public readonly SimpleProperty<Vector3> TranslationMin;
        public readonly SimpleProperty<Vector3> TranslationMax;

        public readonly SimpleProperty<bool> TranslationMinX;
        public readonly SimpleProperty<bool> TranslationMaxX;
        public readonly SimpleProperty<bool> TranslationMinY;
        public readonly SimpleProperty<bool> TranslationMaxY;
        public readonly SimpleProperty<bool> TranslationMinZ;
        public readonly SimpleProperty<bool> TranslationMaxZ;

        public readonly SimpleProperty<RotOrder> RotationOrder;
        public readonly SimpleProperty<bool> RotationSpaceForLimitOnly;
        public readonly SimpleProperty<float> RotationStiffnessX;
        public readonly SimpleProperty<float> RotationStiffnessY;
        public readonly SimpleProperty<float> RotationStiffnessZ;
        public readonly SimpleProperty<float> AxisLen;

        public readonly SimpleProperty<Vector3> PreRotation;
        public readonly SimpleProperty<Vector3> PostRotation;
        public readonly SimpleProperty<bool> RotationActive;

        public readonly SimpleProperty<Vector3> RotationMin;
        public readonly SimpleProperty<Vector3> RotationMax;

        public readonly SimpleProperty<bool> RotationMinX;
        public readonly SimpleProperty<bool> RotationMaxX;
        public readonly SimpleProperty<bool> RotationMinY;
        public readonly SimpleProperty<bool> RotationMaxY;
        public readonly SimpleProperty<bool> RotationMinZ;
        public readonly SimpleProperty<bool> RotationMaxZ;
        public readonly SimpleProperty<TransformInheritance> InheritType;

        public readonly SimpleProperty<bool> ScalingActive;
        public readonly SimpleProperty<Vector3> ScalingMin;
        public readonly SimpleProperty<Vector3> ScalingMax;
        public readonly SimpleProperty<bool> ScalingMinX;
        public readonly SimpleProperty<bool> ScalingMaxX;
        public readonly SimpleProperty<bool> ScalingMinY;
        public readonly SimpleProperty<bool> ScalingMaxY;
        public readonly SimpleProperty<bool> ScalingMinZ;
        public readonly SimpleProperty<bool> ScalingMaxZ;

        public readonly SimpleProperty<Vector3> GeometricTranslation;
        public readonly SimpleProperty<Vector3> GeometricRotation;
        public readonly SimpleProperty<Vector3> GeometricScaling;

        public readonly SimpleProperty<float> MinDampRangeX;
        public readonly SimpleProperty<float> MinDampRangeY;
        public readonly SimpleProperty<float> MinDampRangeZ;
        public readonly SimpleProperty<float> MaxDampRangeX;
        public readonly SimpleProperty<float> MaxDampRangeY;
        public readonly SimpleProperty<float> MaxDampRangeZ;

        public readonly SimpleProperty<float> MinDampStrengthX;
        public readonly SimpleProperty<float> MinDampStrengthY;
        public readonly SimpleProperty<float> MinDampStrengthZ;
        public readonly SimpleProperty<float> MaxDampStrengthX;
        public readonly SimpleProperty<float> MaxDampStrengthY;
        public readonly SimpleProperty<float> MaxDampStrengthZ;

        public readonly SimpleProperty<float> PreferredAngleX;
        public readonly SimpleProperty<float> PreferredAngleY;
        public readonly SimpleProperty<float> PreferredAngleZ;

        public readonly SimpleProperty<bool> Show;
        public readonly SimpleProperty<bool> LODBox;
        public readonly SimpleProperty<bool> Freeze;
    }
}
