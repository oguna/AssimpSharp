using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;
using aiTextureType = AssimpSharp.TextureType;
using aiMesh = AssimpSharp.Mesh;
using aiNode = AssimpSharp.Node;
using aiLight = AssimpSharp.Light;
using aiCamera = AssimpSharp.Camera;
using aiBone = AssimpSharp.Bone;
using aiMaterial = AssimpSharp.Material;
using aiVertexWeight = AssimpSharp.VertexWeight;
using aiMetadata = AssimpSharp.Metadata;
using AssimpSharp.FBX;

using KeyValueList = System.Collections.Generic.List<float>;
using KeyTimeList = System.Collections.Generic.List<long>;
// using KeyFrameList = System.Tuple<KeyTimeList, KeyValueList, int>;
using KeyFrameList = System.Tuple<System.Collections.Generic.List<long>, System.Collections.Generic.List<float>, uint>;
// using KeyFrameListList = System.Collections.Generic.List<KeyFrameList>;
using KeyFrameListList = System.Collections.Generic.List<System.Tuple<System.Collections.Generic.List<long>, System.Collections.Generic.List<float>, uint>>;
using LayerMap = System.Collections.Generic.Dictionary<AssimpSharp.FBX.AnimationCurveNode, AssimpSharp.FBX.AnimationLayer>;
using NodeMap = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AssimpSharp.FBX.AnimationCurveNode>>;

namespace AssimpSharp.FBX
{
    public class FbxConverter
    {
        public const string MAGIC_NODE_TAG = "_$AssimpFbx$";

        double CONVERT_FBX_TIME(double time)
        {
            return (double)time / 46186158000L;
        }

        public enum TransformationComp
        {
            Translation = 0,
            RotationOffset,
            RotationPivot,
            PreRotation,
            Rotation,
            PostRotation,
            RotationPivotInverse,
            ScalingOffset,
            ScalingPivot,
            Scaling,
            ScalingPivotInverse,
            GeometricTranslation,
            GeometricRotation,
            GeometricScaling,
        }

        private int DefaultMaterialIndex;
        private List<AssimpSharp.Mesh> Meshes = new List<aiMesh>();
        private List<AssimpSharp.Material> Materials = new List<aiMaterial>();
        private List<AssimpSharp.Animation> Animations = new List<AssimpSharp.Animation>();
        private List<AssimpSharp.Light> Lights = new List<aiLight>();
        private List<AssimpSharp.Camera> Cameras = new List<aiCamera>();
        private Dictionary<Material, int> MaterialsConverted = new Dictionary<Material, int>();
        private Dictionary<Geometry, List<int>> MeshesConverted = new Dictionary<Geometry, List<int>>();
        private Dictionary<string, uint> NodeAnimChainBits = new Dictionary<string, uint>();
        private Dictionary<string, bool> NodeNames;
        private Dictionary<string, string> RenamedNodes;
        private double AnimFps;
        private AssimpSharp.Scene Result;
        private Document Doc;

        public FbxConverter(out AssimpSharp.Scene scene, Document doc)
        {
            this.Doc = doc;
            this.NodeNames = new Dictionary<string, bool>();
            this.RenamedNodes = new Dictionary<string, string>();
            this.Result = scene = new AssimpSharp.Scene();

            // animations need to be converted first since this will
            // populate the node_anim_chain_bits map, which is needed
            // to determine which nodes need to be generated.
            ConvertAnimations();
            ConvertRootNode();
            if (doc.Settings.ReadAllMaterials)
            {
                // unfortunately this means we have to evaluate all objects
                foreach(var v in doc.Objects)
                {
                    var ob = v.Value.Get();
                    if (ob == null)
                    {
                        continue;
                    }
                    var mat = ob as Material;
                    if (mat != null)
                    {
                        if (!MaterialsConverted.ContainsKey(mat))
                        {
                            ConvertMaterial(mat, null);
                        }
                    }
                }
            }
            TransferDataToScene();
        }

        /// <summary>
        /// find scene root and trigger recursive scene conversion
        /// </summary>
        private void ConvertRootNode()
        {
            Result.RootNode = new aiNode();
            Result.RootNode.Name = "RootNode";
            ConvertNodes(0, Result.RootNode);
        }

        private void ConvertNodes(ulong id, aiNode node)
        {
            ConvertNodes(id, node, Matrix.Identity);
        }

        private void ConvertNodes(ulong id, aiNode parent, Matrix parentTransform)
        {
            var conns = Doc.GetConnectionsByDestinationSequenced(id, "Model");
            var nodes = new List<aiNode>(conns.Count);
            var nodesChain = new List<aiNode>();

            foreach (var con in conns)
            {
                // ignore object-property links
                if (con.PropertyName.Length > 0)
                {
                    continue;
                }

                var obj = con.SourceObject;
                if (obj == null)
                {
                    FBXImporter.LogWarn("failed to convert source object for Model link");
                    continue;
                }

                var model = obj as Model;
                if (model != null)
                {
                    nodesChain.Clear();

                    var newAbsTransform = parentTransform;

                    // even though there is only a single input node, the design of
                    // assimp (or rather: the complicated transformation chain that
                    // is employed by fbx) means that we may need multiple aiNode's
                    // to represent a fbx node's transformation.
                    GenerateTransformationNodeChain(model, out nodesChain);

                    Debug.Assert(nodesChain.Count > 0);

                    var originalName = FixNodeName(model.Name);

                    // check if any of the nodes in the chain has the name the fbx node
                    // is supposed to have. If there is none, add another node to
                    // preserve the name - people might have scripts etc. that rely
                    // on specific node names.
                    aiNode nameCarrier = null;
                    foreach (var prenode in nodesChain)
                    {
                        if (prenode.Name != originalName)
                        {
                            nameCarrier = prenode;
                            break;
                        }
                    }

                    if (nameCarrier != null)
                    {
                        nodesChain.Add(new aiNode(originalName));
                        nameCarrier = nodesChain[nodesChain.Count - 1];
                    }

                    //setup metadata on newest node
                    SetupNodeMetadata(model, nodesChain[nodesChain.Count - 1]);

                    // link all nodes in a row
                    var lastParent = parent;
                    foreach (var prenode in nodesChain)
                    {
                        Debug.Assert(prenode != null);
                        if (lastParent != parent)
                        {
                            lastParent.Children.Add(prenode);
                        }
                        prenode.Parent = lastParent;
                        lastParent = prenode;

                        newAbsTransform = prenode.Transformation;
                    }

                    ConvertModel(model, nodesChain[nodesChain.Count - 1], newAbsTransform);

                    ConvertNodes(model.ID, nodesChain[nodesChain.Count - 1], newAbsTransform);

                    if (Doc.Settings.ReadLights)
                    {
                        ConvertLights(model);
                    }

                    if (Doc.Settings.ReadCameras)
                    {
                        ConvertCameras(model);
                    }

                    nodes.Add(nodesChain[0]);
                    nodesChain.Clear();
                }
            }

            if (nodes.Count > 0)
            {
                parent.Children.AddRange(nodes);
            }
        }

        private void ConvertLights(Model model)
        {
            var nodeAttrs = model.Attributes;
            foreach (var attr in nodeAttrs)
            {
                var light = attr as Light;
                if (light != null)
                {
                    ConvertLight(model, light);
                }
            }
        }

        private void ConvertCameras(Model model)
        {
            var nodeAttrs = model.Attributes;
            foreach (var attr in nodeAttrs)
            {
                var camera = attr as Camera;
                if (camera != null)
                {
                    ConvertCamera(model, camera);
                }
            }
        }

        private void ConvertLight(Model model, Light light)
        {
            var outLight = new AssimpSharp.Light();
            Lights.Add(outLight);
            outLight.Name = FixNodeName(model.Name);
            float intensity = light.Intensity.Value;
            Vector3 col = light.Color.Value;
            outLight.ColorDiffuse = new SharpDX.Vector3(col.X, col.Y, col.Z);
            outLight.ColorDiffuse.X *= intensity;
            outLight.ColorDiffuse.Y *= intensity;
            outLight.ColorDiffuse.Z *= intensity;
            outLight.ColorSpecular = outLight.ColorDiffuse;

            switch (light.LightType.Value)
            {
                case Light.Type.Point:
                    outLight.Type = AssimpSharp.LightSourceType.Point;
                    break;
                case Light.Type.Directional:
                    outLight.Type = AssimpSharp.LightSourceType.Directional;
                    break;
                case Light.Type.Spot:
                    outLight.Type = AssimpSharp.LightSourceType.Spot;
                    outLight.AngleOuterCone = SharpDX.MathUtil.DegreesToRadians(light.OuterAngle.Value);
                    outLight.AngleInnerCone = SharpDX.MathUtil.DegreesToRadians(light.InnerAngle.Value);
                    break;
                case Light.Type.Area:
                    outLight.Type = AssimpSharp.LightSourceType.Undefined;
                    break;
                case Light.Type.Volume:
                    outLight.Type = AssimpSharp.LightSourceType.Undefined;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
            switch (light.DecayType.Value)
            {
                case Light.Decay.None:
                    outLight.AttenuationConstant = 1.0f;
                    break;
                case Light.Decay.Linear:
                    outLight.AttenuationLinear = 1.0f;
                    break;
                case Light.Decay.Quadratic:
                    outLight.AttenuationQuadratic = 1.0f;
                    break;
                case Light.Decay.Cubic:
                    Debug.WriteLine("cannot represent cubic attenuation, set to Quadratic");
                    outLight.AttenuationQuadratic = 1.0f;
                    break;
            }
        }

        private void ConvertCamera(Model model, Camera cam)
        {
            var camera = new AssimpSharp.Camera()
            {
                Name = FixNodeName(model.Name),
                Aspect = cam.AspectWidth.Value / cam.AspectHeight.Value,
                Position = cam.Position.Value,
                LookAt = cam.InterestPosition.Value - cam.Position.Value,
                HorizontalFOV = SharpDX.MathUtil.DegreesToRadians(cam.FieldOfView.Value)
            };
            Cameras.Add(camera);
        }

        private string NameTransformationComp(TransformationComp comp)
        {
            switch (comp)
            {
                case TransformationComp.Translation:
                    return "Translation";
                case TransformationComp.RotationOffset:
                    return "RotationOffset";
                case TransformationComp.RotationPivot:
                    return "RotationPivot";
                case TransformationComp.PreRotation:
                    return "PreRotation";
                case TransformationComp.Rotation:
                    return "Rotation";
                case TransformationComp.PostRotation:
                    return "PostRotation";
                case TransformationComp.RotationPivotInverse:
                    return "RotationPivotInverse";
                case TransformationComp.ScalingOffset:
                    return "ScalingOffset";
                case TransformationComp.ScalingPivot:
                    return "ScalingPivot";
                case TransformationComp.Scaling:
                    return "Scaling";
                case TransformationComp.ScalingPivotInverse:
                    return "ScalingPivotInverse";
                case TransformationComp.GeometricScaling:
                    return "GeometricScaling";
                case TransformationComp.GeometricRotation:
                    return "GeometricRotation";
                case TransformationComp.GeometricTranslation:
                    return "GeometricTranslation";
                default:
                    break;
            }
            Debug.Assert(false);
            return null;
        }

        private string NameTransformationCompProperty(TransformationComp comp)
        {
            switch (comp)
            {
                case TransformationComp.Translation:
                    return "Lcl Translation";
                case TransformationComp.RotationOffset:
                    return "RotationOffset";
                case TransformationComp.RotationPivot:
                    return "RotationPivot";
                case TransformationComp.PreRotation:
                    return "PreRotation";
                case TransformationComp.Rotation:
                    return "Lcl Rotation";
                case TransformationComp.PostRotation:
                    return "PostRotation";
                case TransformationComp.RotationPivotInverse:
                    return "RotationPivotInverse";
                case TransformationComp.ScalingOffset:
                    return "ScalingOffset";
                case TransformationComp.ScalingPivot:
                    return "ScalingPivot";
                case TransformationComp.Scaling:
                    return "Lcl Scaling";
                case TransformationComp.ScalingPivotInverse:
                    return "ScalingPivotInverse";
                case TransformationComp.GeometricScaling:
                    return "GeometricScaling";
                case TransformationComp.GeometricRotation:
                    return "GeometricRotation";
                case TransformationComp.GeometricTranslation:
                    return "GeometricTranslation";
                default:
                    break;
            }
            Debug.Assert(false);
            return null;
        }

        private Vector3 TransformationCompDefaultValue(TransformationComp comp)
        {
            // XXX a neat way to solve the never-ending special cases for scaling 
            // would be to do everything in log space!
            return comp == TransformationComp.Scaling ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3();
        }

        private void GetRotationMatrix(Model.RotOrder mode, Vector3 rotation, out Matrix result)
        {
            if (mode == Model.RotOrder.SphericXYZ)
            {
                FBXImporter.LogError("Unsupported RotationMode: SphericXYZ");
                result = new Matrix();
                return;
            }
            const float angleEpsilon = 1e-6f;
            result = Matrix.Identity;
            var isId = new bool[3] { true, true, true };
            var temp = new Matrix[3];
            if (Math.Abs(rotation.Z) > angleEpsilon)
            {
                Matrix.RotationZ(MathUtil.DegreesToRadians(rotation.Z), out temp[2]);
                isId[2] = false;
            }
            if (Math.Abs(rotation.Y) > angleEpsilon)
            {
                Matrix.RotationY(MathUtil.DegreesToRadians(rotation.Y), out temp[1]);
                isId[1] = false;
            }
            if (Math.Abs(rotation.X) > angleEpsilon)
            {
                Matrix.RotationX(MathUtil.DegreesToRadians(rotation.X), out temp[0]);
                isId[0] = false;
            }
            var order = new int[3] { -1, -1, -1 };
            switch (mode)
            {
                case Model.RotOrder.EulerXYZ:
                    order[0] = 2;
                    order[1] = 1;
                    order[2] = 0;
                    break;

                case Model.RotOrder.EulerXZY:
                    order[0] = 1;
                    order[1] = 2;
                    order[2] = 0;
                    break;

                case Model.RotOrder.EulerYZX:
                    order[0] = 0;
                    order[1] = 2;
                    order[2] = 1;
                    break;

                case Model.RotOrder.EulerYXZ:
                    order[0] = 2;
                    order[1] = 0;
                    order[2] = 1;
                    break;

                case Model.RotOrder.EulerZXY:
                    order[0] = 1;
                    order[1] = 0;
                    order[2] = 2;
                    break;

                case Model.RotOrder.EulerZYX:
                    order[0] = 0;
                    order[1] = 1;
                    order[2] = 2;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            Debug.Assert((order[0] >= 0) && (order[0] <= 2));
            Debug.Assert((order[1] >= 0) && (order[1] <= 2));
            Debug.Assert((order[2] >= 0) && (order[2] <= 2));
            if (!isId[order[0]])
            {
                result = temp[order[0]];
            }
            if (!isId[order[1]])
            {
                result = result * temp[order[1]];
            }
            if (!isId[order[2]])
            {
                result = result * temp[order[2]];
            }
        }

        private bool NeedsComplexTransformationChain(Model model)
        {
            var props = model.Props;
            var zeroEpsilon = 1e-6f;
            bool ok = false;
            foreach (var i in Enum.GetValues(typeof(TransformationComp)))
            {
                var comp = (TransformationComp)i;
                if (comp == TransformationComp.Rotation || comp == TransformationComp.Scaling || comp == TransformationComp.Translation ||
                comp == TransformationComp.GeometricScaling || comp == TransformationComp.GeometricRotation || comp == TransformationComp.GeometricTranslation)
                {
                    continue;
                }
                Vector3 v = PropertyHelper.PropertyGet<Vector3>(props, NameTransformationCompProperty(comp), out ok);
                if (ok && v.LengthSquared() > zeroEpsilon)
                {
                    return true;
                }
            }
            return false;
        }

        private string NameTransformationChainNode(string name, TransformationComp comp)
        {
            return name + MAGIC_NODE_TAG + "_" + NameTransformationComp(comp);
        }

        /// <remarks>
        /// memory for output_nodes will be managed by the caller
        /// </remarks>
        private void GenerateTransformationNodeChain(Model model, out List<aiNode> outputNodes)
        {
            outputNodes = new List<aiNode>();
            var props = model.Props;
            var rot = model.RotationOrder.Value;
            bool ok;
            var chain = new Matrix[Enum.GetValues(typeof(TransformationComp)).Length];
            for(int i=0; i<chain.Length; i++)
            {
                chain[i] = Matrix.Identity;
            }
            float zeroEpsilon = 1e-6f;
            bool isComplex = false;

            Vector3 preRotation = PropertyHelper.PropertyGet<Vector3>(props, "PreRotation", out ok);
            if (ok && preRotation.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                GetRotationMatrix(rot, preRotation, out chain[(int)TransformationComp.PreRotation]);
            }

            Vector3 postRotation = PropertyHelper.PropertyGet<Vector3>(props, "PostRotation", out ok);
            if (ok && postRotation.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                GetRotationMatrix(rot, postRotation, out chain[(int)TransformationComp.PostRotation]);
            }

            Vector3 RotationPivot = PropertyHelper.PropertyGet<Vector3>(props, "RotationPivot", out ok);
            if (ok && RotationPivot.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                Matrix.Translation(ref RotationPivot, out chain[(int)TransformationComp.RotationPivot]);
                chain[(int)TransformationComp.RotationPivotInverse] = Matrix.Translation(-RotationPivot);
            }

            Vector3 RotationOffset = PropertyHelper.PropertyGet<Vector3>(props, "RotationOffset", out ok);
            if (ok && RotationOffset.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                Matrix.Translation(ref RotationOffset, out chain[(int)TransformationComp.RotationOffset]);
            }

            Vector3 ScalingOffset = PropertyHelper.PropertyGet<Vector3>(props, "ScalingOffset", out ok);
            if (ok && ScalingOffset.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                Matrix.Translation(ref ScalingOffset, out chain[(int)TransformationComp.ScalingOffset]);
            }

            Vector3 ScalingPivot = PropertyHelper.PropertyGet<Vector3>(props, "ScalingPivot", out ok);
            if (ok && ScalingPivot.LengthSquared() > zeroEpsilon)
            {
                isComplex = true;
                chain[(int)TransformationComp.ScalingPivot] = Matrix.Translation(ScalingPivot);
                chain[(int)TransformationComp.ScalingPivotInverse] = Matrix.Translation(-ScalingPivot);
            }

            Vector3 Translation = PropertyHelper.PropertyGet<Vector3>(props, "Lcl Translation", out ok);
            if (ok && Translation.LengthSquared() > zeroEpsilon)
            {
                Matrix.Translation(ref Translation, out chain[(int)TransformationComp.Translation]);
            }

            Vector3 Scaling = PropertyHelper.PropertyGet<Vector3>(props, "Lcl Scaling", out ok);
            if (ok && Math.Abs(Scaling.LengthSquared() - 1.0f) > zeroEpsilon)
            {
                Matrix.Scaling(ref Scaling, out chain[(int)TransformationComp.Scaling]);
            }

            Vector3 Rotation = PropertyHelper.PropertyGet<Vector3>(props, "Lcl Rotation", out ok);
            if (ok && Rotation.LengthSquared() > zeroEpsilon)
            {
                GetRotationMatrix(rot, Rotation, out chain[(int)TransformationComp.Rotation]);
            }

            Vector3 GeometricScaling = PropertyHelper.PropertyGet<Vector3>(props, "GeometricScaling", out ok);
            if (ok && Math.Abs(GeometricScaling.LengthSquared() - 1.0f) > zeroEpsilon)
            {
                Matrix.Scaling(ref GeometricScaling, out chain[(int)TransformationComp.GeometricScaling]);
            }

            Vector3 GeometricRotation = PropertyHelper.PropertyGet<Vector3>(props, "GeometricRotation", out ok);
            if (ok && GeometricRotation.LengthSquared() > zeroEpsilon)
            {
                GetRotationMatrix(rot, GeometricRotation, out chain[(int)TransformationComp.GeometricRotation]);
            }

            Vector3 GeometricTranslation = PropertyHelper.PropertyGet<Vector3>(props, "GeometricTranslation", out ok);
            if (ok && GeometricTranslation.LengthSquared() > zeroEpsilon)
            {
                Matrix.Translation(ref GeometricTranslation, out chain[(int)TransformationComp.GeometricTranslation]);
            }

            // is_complex needs to be consistent with NeedsComplexTransformationChain()
            // or the interplay between this code and the animation converter would
            // not be guaranteed.
            Debug.Assert(NeedsComplexTransformationChain(model) == isComplex);

            string name = FixNodeName(model.Name);

            // now, if we have more than just Translation, Scaling and Rotation,
            // we need to generate a full node chain to accommodate for assimp's
            // lack to express pivots and offsets.
            if (isComplex && this.Doc.Settings.PreservePivots)
            {
                FBXImporter.LogInfo("generating full transformation chain for node: " + name);

                // query the anim_chain_bits dictionary to find out which chain elements
                // have associated node animation channels. These can not be dropped 
                // even if they have identity transform in bind pose.
                uint animChainBitmask;
                if (!NodeAnimChainBits.TryGetValue(name, out animChainBitmask))
                {
                    animChainBitmask = 0;
                }

                uint bit = 0x1;
                for (int i = 0; i < Enum.GetValues(typeof(TransformationComp)).Length; ++i, bit <<= 1)
                {
                    TransformationComp comp = (TransformationComp)i;

                    if (chain[i].IsIdentity && (animChainBitmask & bit) == 0)
                    {
                        continue;
                    }

                    aiNode nd = new aiNode();
                    outputNodes.Add(nd);

                    nd.Name = NameTransformationChainNode(name, comp);
                    nd.Transformation = chain[i];
                }
                Debug.Assert(outputNodes.Count > 0);
                return;
            }

            // else, we can just multiply the matrices together
            aiNode nd_ = new aiNode();
            outputNodes.Add(nd_);

            nd_.Name = name;

            nd_.Transformation = Matrix.Identity;
            for (int i = 0; i < Enum.GetValues(typeof(TransformationComp)).Length; ++i)
            {
                nd_.Transformation = nd_.Transformation * chain[i];
            }
        }

        private void SetupNodeMetadata(Model model, aiNode nd)
        {
            var props = model.Props;
            var unparsedProperties = props.GetUnparsedProperties();

            // create metadata on node
            int numStaticMetaData = 2;
            var data = nd.MetaData;
            data.NumProperties = unparsedProperties.Count + numStaticMetaData;
            data.Keys = new string[data.NumProperties];
            data.Values = new AssimpSharp.MetadataEntry[data.NumProperties];
            int index = 0;

            // find user defined properties (3ds Max)
            data.Set<string>(index++, "UserProperties", PropertyHelper.PropertyGet<string>(props, "UDP3DSMAX", ""));
            unparsedProperties.Remove("UDP3DSMAX");
            data.Set(index++, "IsNull", model.IsNull ? true : false);

            foreach (var prop in unparsedProperties)
            {
                var interpreted = prop.Value.As<TypedProperty<bool>>();
                if (interpreted != null)
                {
                    data.Set(index++, prop.Key, interpreted.Value);
                }

            }
        }

        private void ConvertModel(Model model, aiNode nd, Matrix nodeGlobalTransform)
        {
            var geos = model.Geometry;

            var meshes = new List<int>(geos.Count);
            foreach (var geo in geos)
            {
                var mesh = geo as MeshGeometry;
                if (mesh != null)
                {
                    var indices = ConvertMesh(mesh, model, nodeGlobalTransform);
                    meshes.AddRange(indices);
                }
                else
                {
                    Debug.WriteLine("ignoring unrecognized geometry: " + geo.Name);
                }
            }
            if (meshes.Count > 0)
            {
                nd.Meshes.AddRange(meshes);
            }
        }

        #region mesh

        private int[] ConvertMesh(MeshGeometry mesh, Model model,
            Matrix nodeGlobalTransform)
        {
            List<int> temp;
            if (MeshesConverted.TryGetValue(mesh, out temp))
            {
                return temp.ToArray();
            }
            else
            {
                temp = new List<int>();
            }

            var vertices = mesh.Vertices;
            var faces = mesh.FaceIndexCounts;
            if (vertices.Count == 0 || faces.Count == 0)
            {
                Debug.WriteLine("ignore empty geometry: " + mesh.Name);
                return temp.ToArray();
            }

            var mindices = mesh.MaterialIndices;
            if (Doc.Settings.ReadMaterials && !(mindices.Count == 0))
            {
                var b = mindices[0];
                foreach (var index in mindices)
                {
                    if (index != b)
                    {
                        return ConvertMeshMultiMaterial(mesh, model, nodeGlobalTransform).ToArray();
                    }
                }
            }

            temp.Add(ConvertMeshSingleMaterial(mesh, model, nodeGlobalTransform));
            return temp.ToArray();
        }

        private aiMesh SetupEmptyMesh(MeshGeometry mesh)
        {
            var outMesh = new aiMesh();
            this.Meshes.Add(outMesh);
            if (MeshesConverted.ContainsKey(mesh))
            {
                this.MeshesConverted[mesh].Add(this.Meshes.Count - 1);
            }
            else
            {
                this.MeshesConverted[mesh] = new List<int>() {this.Meshes.Count - 1 };
            }

            // set name
            var name = mesh.Name;
            if (name.StartsWith("Geometry::"))
            {
                name = name.Substring(10);
            }

            if (!string.IsNullOrEmpty(name))
            {
                outMesh.Name = name;
            }

            return outMesh;
        }

        private int ConvertMeshSingleMaterial(MeshGeometry mesh, Model model, Matrix nodeGlobalTransform)
        {
            var mindices = mesh.MaterialIndices;
            var outMesh = SetupEmptyMesh(mesh);

            var vertices = mesh.Vertices;
            var faces = mesh.FaceIndexCounts;

            // copy vertices
            outMesh.NumVertices = vertices.Count;
            outMesh.Vertices = new Vector3[vertices.Count];
            vertices.CopyTo(outMesh.Vertices);

            // generate dummy faces
            outMesh.NumFaces = faces.Count;
            var fac = outMesh.Faces = new AssimpSharp.Face[faces.Count];
            for(int i=0; i<faces.Count; i++)
            {
                fac[i] = new AssimpSharp.Face();
            }

            int cursor = 0;
            var facIndex = 0;
            foreach(var pcount in faces)
            {
                var f = fac[facIndex++];
                f.Indices = new int[pcount];
                switch(pcount)
                {
                    case 1:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Point;
                        break;
                    case 2:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Line;
                        break;
                    case 3:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Triangle;
                        break;
                    default:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Polygon;
                        break;
                }
                for(int i=0; i<pcount; ++i)
                {
                    f.Indices[i] = cursor++;
                }
            }

            // copy normals
            var normals = mesh.Normals;
            if (normals.Count > 0)
            {
                Debug.Assert(normals.Count == vertices.Count);
                outMesh.Normals = new Vector3[vertices.Count];
                normals.CopyTo(outMesh.Normals);
            }

            // copy tangents - assimp requires both tangents and bitangents (binormals)
            // to be present, or neither of them. Compute binormals from normals
            // and tangents if needed.
            var tangents = mesh.Tangents;
            var binormals = mesh.Binormals;
            if (tangents.Count > 0)
            {
                var tempBinormals = new List<Vector3>();
                if (binormals.Count == 0)
                {
                    if (normals.Count > 0)
                    {
                        tempBinormals = new List<Vector3>(normals.Count);
                        for(int i=0; i<tangents.Count; i++)
                        {
                            tempBinormals.Add(Vector3.Cross(normals[i], tangents[i]));
                        }
                        binormals.Clear();
                        for(int i=0; i<tempBinormals.Count; i++)
                        {
                            binormals.Add(tempBinormals[i]);
                        }
                    }
                    else
                    {
                        binormals = null;
                    }
                }
                if (binormals != null)
                {
                    Debug.Assert(tangents.Count == vertices.Count);
                    Debug.Assert(binormals.Count == vertices.Count);

                    outMesh.Tangents = new Vector3[vertices.Count];
                    tangents.CopyTo(outMesh.Tangents);

                    outMesh.Bitangents = new Vector3[vertices.Count];
                    binormals.CopyTo(outMesh.Bitangents);
                }
            }

            // copy texture coords
            for (int i=0; i<Mesh.AI_MAX_NUMBER_OF_TEXTURECOORDS; i++)
            {
                var uvs = mesh.GetTextureCoords(i);
                if (uvs.Count == 0)
                {
                    break;
                }

                var outUv = outMesh.TextureCoords[i] = new Vector3[vertices.Count];
                for(int j=0; j<uvs.Count; j++)
                {
                    outUv[j] = new Vector3(uvs[j].X, uvs[j].Y, 0);
                }
            }

            // copy vertex colors
            for (int i = 0; i < aiMesh.AI_MAX_NUMBER_OF_COLOR_SETS; ++i)
            {
                var colors = mesh.GetVertexColors(i);
                if (colors.Count == 0)
                {
                    break;
                }
                outMesh.Colors[i] = new Color4[vertices.Count];
                colors.CopyTo(outMesh.Colors[i]);
            }

            if (!Doc.Settings.ReadMaterials || mindices.Count == 0)
            {
                //FBXImporter.LogError("no material assigned to mesh, setting default material");
                outMesh.MaterialIndex = GetDefaultMaterial();
            }
            else
            {
                ConvertMaterialForMesh(outMesh, model, mesh, mindices[0]);
            }

            if (Doc.Settings.ReadWeights && mesh.DeformerSkin != null)
            {
                ConvertWeights(outMesh, model, mesh, nodeGlobalTransform, NO_MATERIAL_SEPARATION);
            }

            return Meshes.Count - 1;
        }

        private List<int> ConvertMeshMultiMaterial(MeshGeometry mesh, Model model, Matrix nodeGlobalTransform)
        {
            var mindices = mesh.MaterialIndices;
            Debug.Assert(mindices.Count > 0);

            var had = new HashSet<int>();
            var indices = new List<int>();

            foreach (var index in mindices)
            {
                if (!had.Contains(index))
                {
                    indices.Add(ConvertMeshMultiMaterial(mesh, model, index, nodeGlobalTransform));
                    had.Add(index);
                }
            }
            return indices;
        }

        private int ConvertMeshMultiMaterial(MeshGeometry mesh, Model model, int index, Matrix nodeGlobalTransform)
        {
            var outMesh = SetupEmptyMesh(mesh);
            var mindices = mesh.MaterialIndices;
            var vertices = mesh.Vertices;
            var faces = mesh.FaceIndexCounts;
            var processWeights = Doc.Settings.ReadWeights && (mesh.DeformerSkin != null);

            int countFaces = 0;
            int countVertices = 0;

            // count faces
            var itf = faces.GetEnumerator();
            foreach(var it in mindices)
            {
                itf.MoveNext();
                if (it != index)
                {
                    continue;
                }
                ++countFaces;
                countVertices += (int)itf.Current;
            }

            Debug.Assert(countFaces > 0);
            Debug.Assert(countVertices > 0);

            // mapping from output indices to DOM indexing, needed to resolve weights
            var reverseMappigng = new int[0];
            if (processWeights)
            {
                reverseMappigng = new int[countVertices];
            }

            // allocate output data arrays, but don't fill them yet
            outMesh.Vertices = new Vector3[countVertices];
            var fac = outMesh.Faces = new AssimpSharp.Face[countFaces];

            // allocate normals
            var normals = mesh.Normals;
            if (normals.Count > 0)
            {
                Debug.Assert(normals.Count == vertices.Count);
                outMesh.Normals = new Vector3[vertices.Count];
            }

            // allocate tangents, binormals.
            var tangets = mesh.Tangents;
            var binormals = mesh.Binormals;
            if (tangets.Count > 0)
            {
                Vector3[] tempBinormals = new Vector3[0];
                if (binormals.Count == 0)
                {
                    if (normals.Count > 0)
                    {
                        // XXX this computes the binormals for the entire mesh, not only
                        // the part for which we need them.
                        tempBinormals = new Vector3[normals.Count];
                        for (int i = 0; i < tangets.Count; i++)
                        {
                            tempBinormals[i] = Vector3.Cross(normals[i], tangets[i]);
                        }
                        binormals.Clear();
                        for (int i = 0; i < tempBinormals.Length; i++)
                        {
                            binormals.Add(tempBinormals[i]);
                        }
                    }
                    else
                    {
                        binormals = null;
                    }
                }

                if (binormals.Count > 0)
                {
                    Debug.Assert(tangets.Count == vertices.Count);
                    Debug.Assert(binormals.Count == vertices.Count);

                    outMesh.Tangents = new Vector3[vertices.Count];
                    outMesh.Bitangents = new Vector3[vertices.Count];
                }
            }

            // allocate texture coords
            int numUvs = 0;
            for (int i = 0; i < AssimpSharp.Mesh.AI_MAX_NUMBER_OF_TEXTURECOORDS; i++, numUvs++)
            {
                var uvs = mesh.GetTextureCoords(i);
                if (uvs.Count == 0)
                {
                    break;
                }
                outMesh.TextureCoords[i] = new Vector3[vertices.Count];
                outMesh.NumUVComponents[i] = 2;
            }

            // allocate vertex colors
            int numVcs = 0;
            for (int i = 0; i < AssimpSharp.Mesh.AI_MAX_NUMBER_OF_COLOR_SETS; i++, numVcs++)
            {
                var colors = mesh.GetVertexColors(i);
                if (colors.Count == 0)
                {
                    break;
                }
                outMesh.Colors[i] = new Color4[vertices.Count];
            }

            int cursor = 0;
            int inCursor = 0;

            int facesIndex = 0;
            int facIndex = 0;
            foreach (var it in mindices)
            {
                int pcount = (int)faces[facesIndex];
                if (it != index)
                {
                    inCursor += pcount;
                    continue;
                }

                var f = fac[facIndex++] = new Face();
                f.Indices = new int[pcount];
                switch (pcount)
                {
                    case 1:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Point;
                        break;
                    case 2:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Line;
                        break;
                    case 3:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Triangle;
                        break;
                    default:
                        outMesh.PrimitiveTypes |= AssimpSharp.PrimitiveType.Polygon;
                        break;
                }
                for (int i = 0; i < pcount; ++i, ++cursor, ++inCursor)
                {
                    f.Indices[i] = cursor;
                    if (reverseMappigng.Length > 0)
                    {
                        reverseMappigng[cursor] = inCursor;
                    }
                    outMesh.Vertices[cursor] = vertices[inCursor];
                    if (outMesh.Normals.Length > 0)
                    {
                        outMesh.Normals[cursor] = normals[inCursor];
                    }
                    if (outMesh.Tangents.Length > 0)
                    {
                        outMesh.Tangents[cursor] = tangets[inCursor];
                        outMesh.Bitangents[cursor] = binormals[inCursor];
                    }
                    for (int j = 0; j < numUvs; j++)
                    {
                        var uvs = mesh.GetTextureCoords(j);
                        outMesh.TextureCoords[j][cursor] = new Vector3(uvs[inCursor], 0.0f);
                    }
                    for (int j = 0; j < numVcs; j++)
                    {
                        var cols = mesh.GetVertexColors(j);
                        outMesh.Colors[j][cursor] = cols[inCursor];
                    }
                }
            }

            ConvertMaterialForMesh(outMesh, model, mesh, index);

            if (processWeights)
            {
                ConvertWeights(outMesh, model, mesh, nodeGlobalTransform, index, reverseMappigng);
            }
            return Meshes.Count - 1;
        }

        private const int NO_MATERIAL_SEPARATION = -1;

        private void ConvertWeights(aiMesh result, Model model, MeshGeometry geo, Matrix nodeGlobalTransform, int materialIndex = FbxConverter.NO_MATERIAL_SEPARATION, int[] outputVertStartIndices = null)
        {
            Debug.Assert(geo.DeformerSkin != null);
            var outIndices = new List<int>();
            var indexOutIndices = new List<int>();
            var countOutIndices = new List<int>();

            var sk = geo.DeformerSkin;

            var bones = new List<aiBone>(sk.Clusters.Count);

            var noMatCheck = (materialIndex == FbxConverter.NO_MATERIAL_SEPARATION);
            Debug.Assert(noMatCheck || outputVertStartIndices != null);

            try
            {
                foreach (var cluster in sk.Clusters)
                {
                    Debug.Assert(cluster != null);

                    var indices = cluster.Indices;

                    if (indices.Count == 0)
                    {
                        continue;
                    }

                    var mats = geo.MaterialIndices;

                    var ok = false;

                    int noIndexSentine = int.MaxValue;

                    countOutIndices.Clear();
                    indexOutIndices.Clear();
                    outIndices.Clear();

                    foreach (var index in indices)
                    {
                        var outIdx = geo.ToOutputVertexIndex((uint)index);
                        var count = outIdx.Count;
                        // ToOutputVertexIndex only returns NULL if index is out of bounds
                        // which should never happen
                        Debug.Assert(outIdx.Count > 0);

                        indexOutIndices.Add(noIndexSentine);
                        countOutIndices.Add(0);

                        for (int i = 0; i < count; i++)
                        {
                            if (noMatCheck || mats[(int)geo.FaceForVertexIndex((uint)outIdx.Array[i+outIdx.Offset])] == materialIndex)
                            {
                                if (indexOutIndices[indexOutIndices.Count] == noIndexSentine)
                                {
                                    indexOutIndices[indexOutIndices.Count] = outIndices.Count;
                                }
                                if (noMatCheck)
                                {
                                    outIndices.Add((int)outIdx.Array[i+outIdx.Offset]);
                                }
                                else
                                {
                                    int it;
                                    for (it = 0; it < outputVertStartIndices.Length; it++)
                                    {
                                        if (outIdx.Array[i+outIdx.Offset] == outputVertStartIndices[it])
                                        {
                                            break;
                                        }
                                    }
                                    outIndices.Add(it);
                                }
                                countOutIndices[countOutIndices.Count] += 1;
                                ok = true;
                            }
                        }
                    }

                    // if we found at least one, generate the output bones
                    // XXX this could be heavily simplified by collecting the bone
                    // data in a single step.
                    if (ok)
                    {
                        ConvertCluster(bones, model, cluster, outIndices.ToArray(), indexOutIndices.ToArray(),
                            countOutIndices.ToArray(), nodeGlobalTransform);
                    }
                }
            }
            catch (Exception e)
            {
                bones.Clear();
                throw (e);
            }
            if (bones.Count == 0)
            {
                return;
            }

            result.Bones = bones.ToArray();
            result.NumBones = bones.Count;
        }

        private void ConvertCluster(List<aiBone> bones, Model model, Cluster cl, int[] outIndices, int[] indexOutIndices, int[] countOutIndices, Matrix nodeGlobalTransform)
        {
            var bone = new aiBone();
            bones.Add(bone);

            bone.Name = FixNodeName(cl.TargetNode.Name);

            bone.OffsetMatrix = cl.TransformLink;
            bone.OffsetMatrix.Invert();

            bone.OffsetMatrix = bone.OffsetMatrix * nodeGlobalTransform;

            bone.NumWeights = outIndices.Length;
            var cursor = bone.Weights = new aiVertexWeight[outIndices.Length];
            int cursor_index = 0;

            int noIndexSentinel = int.MaxValue;
            var weights = cl.Weights;

            int c = indexOutIndices.Length;
            for (int i = 0; i < c; i++)
            {
                int indexIndex = indexOutIndices[i];
                if (indexIndex == noIndexSentinel)
                {
                    continue;
                }
                int cc = countOutIndices[i];
                for (int j = 0; j < cc; j++)
                {
                    cursor[cursor_index].VertexId = outIndices[indexIndex + j];
                    cursor[cursor_index].Weight = weights[i];
                    j++;
                }
            }
        }

        #endregion

        #region material

        private void ConvertMaterialForMesh(aiMesh result, Model model, MeshGeometry geo, int materialIndex)
        {
            // locate source materials for this mesh
            var mats = model.Materials;
            if (materialIndex >= mats.Count || materialIndex < 0)
            {
                FBXImporter.LogError("material index out of bounds, setting default material");
                result.MaterialIndex = GetDefaultMaterial();
                return;
            }

            var mat = mats[materialIndex];
            int it;
            if (MaterialsConverted.TryGetValue(mat, out it))
            {
                result.MaterialIndex = it;
                return;
            }

            result.MaterialIndex = ConvertMaterial(mat, geo);
            this.MaterialsConverted[mat] = result.MaterialIndex;
        }

        private int GetDefaultMaterial()
        {
            if (DefaultMaterialIndex != 0)
            {
                return DefaultMaterialIndex - 1;
            }

            var outMat = new aiMaterial();
            Materials.Add(outMat);

            var diffuse = new Color(0.8f, 0.8f, 0.8f, 0f);
            outMat.ColorDiffuse = diffuse;
            outMat.Name = aiMaterial.AI_DEFAULT_MATERIAL_NAME;

            this.DefaultMaterialIndex = Materials.Count;
            return DefaultMaterialIndex - 1;
        }

        private int ConvertMaterial(Material material, MeshGeometry mesh)
        {
            var props = material.Props;

            // generate empty output material
            var outMat = new aiMaterial();
            this.MaterialsConverted[material] = this.Materials.Count;

            this.Materials.Add(outMat);

            // stip Material:: prefix
            string name = material.Name;
            if (name.Substring(0, 10) == "Material::")
            {
                name = name.Substring(10);
            }

            // set material name if not empty - this could happen
            // and there should be no key for it in this case.
            if (name.Length > 0)
            {
                outMat.Name = name;
            }

            // shading stuff and colors
            SetShadingPropertiesCommon(outMat, props);

            // texture assignments
            SetTextureProperties(outMat, material.Textures, mesh);
            SetTextureProperties(outMat, material.LayeredTextures, mesh);

            return this.Materials.Count - 1;
        }

        private void TrySetTextureProperties(aiMaterial outMat, Dictionary<string, Texture> textures,
            string propName, TextureType target, MeshGeometry mesh)
        {
            Texture tex;
            if (!textures.TryGetValue(propName, out tex))
            {
                return;
            }

            if (tex != null)
            {
                outMat.TextureSlotCollection[target] = new TextureSlot();

                var path = tex.RelativeFilename;
                outMat.TextureSlotCollection[target].TextureBase = path;

                var uvTrafo = new AssimpSharp.UVTransform();
                uvTrafo.Scaling = tex.UVScaling;
                uvTrafo.Translation = tex.UVTranslation;
                outMat.TextureSlotCollection[target].UVTransformBase = uvTrafo;

                var props = tex.Props;

                int uvIndex = 0;

                bool ok;
                var uvSet = PropertyHelper.PropertyGet<string>(props, "UVSet", out ok);
                if (ok)
                {
                    if (uvSet != "default" && uvSet.Length > 0)
                    {
                        int matIndex = Materials.IndexOf(outMat);

                        uvIndex = -1;
                        if (mesh == null)
                        {
                            foreach (var v in this.MeshesConverted)
                            {
                                var mesh_ = v.Key;
                                if (mesh_ != null)
                                {
                                    continue;
                                }

                                var mats = mesh.MaterialIndices;
                                if (!mats.Contains(matIndex))
                                {
                                    continue;
                                }

                                int index = -1;
                                for (int i = 0; i < 4; i++)
                                {
                                    if (mesh.GetTextureCoords(i).Count == 0)
                                    {
                                        break;
                                    }
                                    var name = mesh.GetTextureCoordChannelName(i);
                                    if (name == uvSet)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                                if (index == -1)
                                {
                                    FBXImporter.LogWarn("did not find UV channel named " + uvSet + " in a mesh using this material");
                                    continue;
                                }
                                if (uvIndex == -1)
                                {
                                    uvIndex = index;
                                }
                                else
                                {
                                    FBXImporter.LogWarn("the UV channel named " + uvSet +
                                  " appears at different positions in meshes, results will be wrong");
                                }
                            }
                        }
                        else
                        {
                            int index = -1;
                            for (int i = 0; i < 4; i++)
                            {
                                if (mesh.GetTextureCoords(i).Count == 0)
                                {
                                    break;
                                }
                                var name = mesh.GetTextureCoordChannelName(i);
                                if (name == uvSet)
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if (index == -1)
                            {
                                FBXImporter.LogWarn("did not find UV channel named " + uvSet + " in a mesh using this material");
                            }
                            if (uvIndex == -1)
                            {
                                uvIndex = index;
                            }
                        }
                        if (uvIndex == -1)
                        {
                            FBXImporter.LogWarn("failed to resolve UV channel " + uvSet + ", using first UV channel");
                            uvIndex = 0;
                        }
                    }
                }
                outMat.TextureSlotCollection[target].UVWSrcBase = uvIndex;
            }
        }

        private void TrySetTextureProperties(aiMaterial outMat, Dictionary<string, LayeredTexture> layeredTextures,
            string propName,
            aiTextureType target, MeshGeometry mesh)
        {
            LayeredTexture it;
            if (!layeredTextures.TryGetValue(propName, out it))
            {
                return;
            }
            var tex = it.Texture;

            var path = tex.RelativeFilename;
            outMat.TextureSlotCollection[target].TextureBase = path;

            var uvTrafo = new AssimpSharp.UVTransform();
            uvTrafo.Scaling = tex.UVScaling;
            uvTrafo.Translation = tex.UVTranslation;
            outMat.TextureSlotCollection[target].UVTransformBase = uvTrafo;

            var props = tex.Props;
            var uvIndex = 0;
            bool ok;
            var uvSet = PropertyHelper.PropertyGet<string>(props, "UVSet", out ok);
            if (ok)
            {
                if (uvSet != "default" && !string.IsNullOrEmpty(uvSet))
                {
                    var matIndex = Materials.IndexOf(outMat);
                    uvIndex = -1;
                    if (mesh == null)
                    {
                        foreach (var v in MeshesConverted)
                        {
                            var mesh_ = v.Key as MeshGeometry;
                            if (mesh_ == null)
                            {
                                continue;
                            }
                            var mats = mesh_.MaterialIndices;
                            if (!mats.Contains(matIndex))
                            {
                                continue;
                            }
                            int index = -1;
                            for (int i = 0; i < aiMesh.AI_MAX_NUMBER_OF_TEXTURECOORDS; i++)
                            {
                                if (mesh.GetTextureCoords(i).Count == 0)
                                {
                                    break;
                                }
                                var name = mesh.GetTextureCoordChannelName(i);
                                if (name == uvSet)
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if (index == -1)
                            {
                                FBXImporter.LogWarn("did not find UV channel named " + uvSet + " in a mesh using this material");
                                continue;
                            }
                            if (uvIndex == -1)
                            {
                                uvIndex = index;
                            }
                            else
                            {
                                FBXImporter.LogWarn("the UV channel named " + uvSet +
                                " appears at different positions in meshes, results will be wrong");
                            }
                        }
                    }
                    else
                    {
                        int index = -1;
                        for (int i = 0; i < aiMesh.AI_MAX_NUMBER_OF_TEXTURECOORDS; i++)
                        {
                            if (mesh.GetTextureCoords(i).Count == 0)
                            {
                                break;
                            }
                            var name = mesh.GetTextureCoordChannelName(i);
                            if (name == uvSet)
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index == -1)
                        {
                            FBXImporter.LogWarn("did not find UV channel named " + uvSet + " in a mesh using this material");
                        }
                        if (uvIndex == -1)
                        {
                            uvIndex = index;
                        }
                    }
                    if (uvIndex == -1)
                    {
                        FBXImporter.LogWarn("failed to resolve UV channel " + uvSet + ", using first UV channel");
                        uvIndex = 0;
                    }
                }
            }
            outMat.TextureSlotCollection[target].UVWSrcBase = uvIndex;
        }

        private void SetTextureProperties(aiMaterial outMat, Dictionary<string, Texture> textures, MeshGeometry mesh)
        {
            TrySetTextureProperties(outMat, textures, "DiffuseColor", aiTextureType.Diffuse, mesh);
            TrySetTextureProperties(outMat, textures, "AmbientColor", aiTextureType.Ambient, mesh);
            TrySetTextureProperties(outMat, textures, "EmissiveColor", aiTextureType.Emissive, mesh);
            TrySetTextureProperties(outMat, textures, "SpecularColor", aiTextureType.Specular, mesh);
            TrySetTextureProperties(outMat, textures, "TransparentColor", aiTextureType.Opacity, mesh);
            TrySetTextureProperties(outMat, textures, "ReflectionColor", aiTextureType.Reflection, mesh);
            TrySetTextureProperties(outMat, textures, "DisplacementColor", aiTextureType.Displacement, mesh);
            TrySetTextureProperties(outMat, textures, "NormalMap", aiTextureType.Normals, mesh);
            TrySetTextureProperties(outMat, textures, "Bump", aiTextureType.Height, mesh);
            TrySetTextureProperties(outMat, textures, "ShininessExponent", aiTextureType.Shininess, mesh);
        }

        private void SetTextureProperties(aiMaterial outMat, Dictionary<string, LayeredTexture> layeredTextures, MeshGeometry mesh)
        {
            TrySetTextureProperties(outMat, layeredTextures, "DiffuseColor", aiTextureType.Diffuse, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "AmbientColor", aiTextureType.Ambient, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "EmissiveColor", aiTextureType.Emissive, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "SpecularColor", aiTextureType.Specular, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "TransparentColor", aiTextureType.Opacity, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "ReflectionColor", aiTextureType.Reflection, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "DisplacementColor", aiTextureType.Displacement, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "NormalMap", aiTextureType.Normals, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "Bump", aiTextureType.Height, mesh);
            TrySetTextureProperties(outMat, layeredTextures, "ShininessExponent", aiTextureType.Shininess, mesh);
        }

        private Color3 GetColorPropertyFromMaterial(PropertyTable props, string baseName, out bool result)
        {
            result = true;

            bool ok;
            var diffuse = PropertyHelper.PropertyGet<Vector3>(props, baseName, out ok);
            if (ok)
            {
                return new Color3(diffuse.X, diffuse.Y, diffuse.Z);
            }
            else
            {
                Vector3 diffuseColor = PropertyHelper.PropertyGet<Vector3>(props, baseName + "Color", out ok);
                if (ok)
                {
                    float diffuseFactor = PropertyHelper.PropertyGet<float>(props, baseName + "Factor", out ok);
                    if (ok)
                    {
                        diffuseColor *= diffuseFactor;
                    }

                    return new Color3(diffuseColor.X, diffuseColor.Y, diffuseColor.Z);
                }
            }

            result = false;
            return new Color3(0, 0, 0);
        }

        private void SetShadingPropertiesCommon(aiMaterial outMat, PropertyTable props)
        {
            bool ok;
            var diffuse = GetColorPropertyFromMaterial(props, "Diffuse", out ok);
            if (ok)
            {
                outMat.ColorDiffuse = new Color4(diffuse, 1.0f);
            }
            var emissive = GetColorPropertyFromMaterial(props, "Emissive", out ok);
            if (ok)
            {
                outMat.ColorEmissive = new Color4(emissive, 1.0f);
            }
            var ambient = GetColorPropertyFromMaterial(props, "Ambient", out ok);
            if (ok)
            {
                outMat.ColorAmbient = new Color4(ambient, 1.0f);
            }
            var specular = GetColorPropertyFromMaterial(props, "Specular", out ok);
            if (ok)
            {
                outMat.ColorSpecular = new Color4(specular, 1.0f);
            }
            float opacity = PropertyHelper.PropertyGet<float>(props, "Opacity", out ok);
            if (ok)
            {
                outMat.Opacity = opacity;
            }
            float reflectivity = PropertyHelper.PropertyGet<float>(props, "Reflectivity", out ok);
            if (ok)
            {
                outMat.Reflectivity = reflectivity;
            }
            float shininess = PropertyHelper.PropertyGet<float>(props, "Shininess", out ok);
            if (ok)
            {
                outMat.Shininess = shininess;
            }
            float shininessExponent = PropertyHelper.PropertyGet<float>(props, "ShininessExponent", out ok);
            if (ok)
            {
                outMat.ShininessStrength = shininessExponent;
            }
        }

        #endregion

        #region animation

        /// <summary>
        /// get the number of fps for a FrameRate enumerated value
        /// </summary>
        private static double FrameRateToDouble(FileGlobalSettings.FrameRate fp, double customFPSVal = -1)
        {
            switch (fp)
            {
                case FileGlobalSettings.FrameRate.FrameRate_DEFAULT:
                    return 1.0;
                case FileGlobalSettings.FrameRate.FrameRate_120:
                    return 120.0;
                case FileGlobalSettings.FrameRate.FrameRate_100:
                    return 100.0;
                case FileGlobalSettings.FrameRate.FrameRate_60:
                    return 60.0;
                case FileGlobalSettings.FrameRate.FrameRate_50:
                    return 50.0;
                case FileGlobalSettings.FrameRate.FrameRate_48:
                    return 48.0;
                case FileGlobalSettings.FrameRate.FrameRate_30:
                case FileGlobalSettings.FrameRate.FrameRate_30_DROP:
                    return 30.0;
                case FileGlobalSettings.FrameRate.FrameRate_NTSC_DROP_FRAME:
                case FileGlobalSettings.FrameRate.FrameRate_NTSC_FULL_FRAME:
                    return 29.9700262;
                case FileGlobalSettings.FrameRate.FrameRate_PAL:
                    return 25.0;
                case FileGlobalSettings.FrameRate.FrameRate_CINEMA:
                    return 24.0;
                case FileGlobalSettings.FrameRate.FrameRate_1000:
                    return 1000.0;
                case FileGlobalSettings.FrameRate.FrameRate_CINEMA_ND:
                    return 23.976;
                case FileGlobalSettings.FrameRate.FrameRate_CUSTOM:
                    return customFPSVal;
            }
            Debug.Assert(false);
            return -1.0f;
        }

        /// <summary>
        /// convert animation data to aiAnimation et al
        /// </summary>
        private void ConvertAnimations()
        {
            var fps = Doc.GlobalSettings.TimeMode.Value;
            var custom = Doc.GlobalSettings.CustomFrameRate.Value;
            AnimFps = FrameRateToDouble(fps, custom);
            var animations = Doc.AnimationStacks;
            foreach (var stack in animations)
            {
                ConvertAnimationStack(stack);
            }
        }

        private void RenameNode(string fixedName, string newName)
        {
            Debug.Assert(NodeNames.ContainsKey(fixedName));
            Debug.Assert(!NodeNames.ContainsKey(newName));

            RenamedNodes[fixedName] = newName;

            string fn = fixedName;

            foreach (var cam in Cameras)
            {
                if (cam.Name == fn)
                {
                    cam.Name = newName;
                    break;
                }
            }

            foreach (var light in Lights)
            {
                if (light.Name == fn)
                {
                    light.Name = newName;
                    break;
                }
            }

            foreach (var anim in Animations)
            {
                for (int i = 0; i < anim.Channels.Length; i++)
                {
                    var na = anim.Channels[i];
                    if (na.NodeName == fn)
                    {
                        na.NodeName = newName;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// takes a fbx node name and returns the identifier to be used in the assimp output scene.
        /// the function is guaranteed to provide consistent results over multiple invocations
        /// UNLESS RenameNode() is called for a particular node name.
        /// </summary>
        private string FixNodeName(string name)
        {
            // strip Model:: prefix, avoiding ambiguities (i.e. don't strip if
            // this causes ambiguities, well possible between empty identifiers,
            // such as "Model::" and ""). Make sure the behaviour is consistent
            // across multiple calls to FixNodeName().
            if (name.StartsWith("Model::"))
            {
                var temp = name.Substring(7);
                bool it;
                if (NodeNames.TryGetValue(name, out it))
                {
                    if (!it)
                    {
                        return FixNodeName(name + "_");
                    }
                }
                NodeNames[temp] = true;

                string rit;
                if (RenamedNodes.TryGetValue(temp, out rit))
                {
                    return rit;
                }
                else
                {
                    return temp;
                }
            }

            bool it_;
            if (NodeNames.TryGetValue(name, out it_))
            {
                if (it_)
                {
                    return FixNodeName(name + "_");
                }
            }
            NodeNames[name] = false;

            string rit_;
            if (!RenamedNodes.TryGetValue(name, out rit_))
            {
                return name;
            }
            else
            {
                return rit_;
            }
        }

        private void ConvertAnimationStack(AnimationStack st)
        {
            var layers = st.Layers;
            if (layers.Count == 0)
            {
                return;
            }

            var anim = new AssimpSharp.Animation();
            Animations.Add(anim);

            // strip AnimationStack:: prefix
            var name = st.Name;
            if (name.StartsWith("AnimationStack::"))
            {
                name = name.Substring(16);
            }
            else if (name.StartsWith("AnimStack::"))
            {
                name = name.Substring(11);
            }

            anim.Name = name;

            // need to find all nodes for which we need to generate node animations -
            // it may happen that we need to merge multiple layers, though.
            var nodeMap = new NodeMap();

            // reverse mapping from curves to layers, much faster than querying
            // the FBX DOM for it.
            var layerMap = new LayerMap();

            var propWhitelist = new string[] { "Lcl Scaling", "Lcl Rotation", "Lcl Translation" };
            foreach (var layer in layers)
            {
                Debug.Assert(layer != null);
                var nodes = layer.Nodes(propWhitelist);
                foreach (var node in nodes)
                {
                    var model = node.Target as Model;
                    // this can happen - it could also be a NodeAttribute (i.e. for camera animations)
                    if (model == null)
                    {
                        continue;
                    }

                    var name_ = FixNodeName(model.Name);
                    if (nodeMap.ContainsKey(name))
                    {
                        nodeMap[name].Add(node);
                    }
                    else
                    {
                        nodeMap[name] = new List<AnimationCurveNode>() { node };
                    }

                    layerMap[node] = layer;
                }
            }

            // generate node animations
            var nodeAnims = new List<AssimpSharp.NodeAnim>();

            double minTime = 1e10;
            double maxTime = -1e10;

            long startTime = st.LocalStart.Value;
            long stopTime = st.LocalStop.Value;
            double startTimeF = CONVERT_FBX_TIME(startTime);
            double stopTimeF = CONVERT_FBX_TIME(stopTime);

            try
            {
                foreach (var kv in nodeMap)
                {
                    GenerateNodeAnimations(nodeAnims, kv.Key, kv.Value, layerMap, startTime, stopTime, ref maxTime, ref minTime);
                }
            }
            catch (Exception e)
            {
                nodeAnims.Clear();
                throw e;
            }

            if (nodeAnims.Count > 0)
            {
                anim.Channels = nodeAnims.ToArray();
            }
            else
            {
                // empty animations would fail validation, so drop them
                Animations.RemoveAt(Animations.Count - 1);
                FBXImporter.LogInfo("ignoring empty AnimationStack (using IK?): " + name);
                return;
            }

            //adjust relative timing for animation
            {
                var startFps = startTimeF * AnimFps;
                for (int c = 0; c < anim.Channels.Length; c++)
                {
                    var channel = anim.Channels[c];
                    for (int i = 0; i < channel.PositionKeys.Length; i++)
                    {
                        channel.PositionKeys[i].Time -= startFps;
                    }
                    for (int i = 0; i < channel.RotationKeys.Length; i++)
                    {
                        channel.RotationKeys[i].Time -= startFps;
                    }
                    for (int i = 0; i < channel.ScalingKeys.Length; i++)
                    {
                        channel.ScalingKeys[i].Time -= startFps;
                    }
                }
                maxTime -= minTime;
            }

            // for some mysterious reason, mDuration is simply the maximum key -- the
            // validator always assumes animations to start at zero.
            anim.Duration = (stopTimeF - startTimeF) * AnimFps;
            anim.TicksPreSecond = AnimFps;
        }

        private void GenerateNodeAnimations(List<AssimpSharp.NodeAnim> nodeAnims, string fixedName, List<AnimationCurveNode> curves, LayerMap layerMap, long start, long stop, ref double maxTime, ref double minTime)
        {
            var nodePropertyMap = new NodeMap();
            Debug.Assert(curves.Count > 0);

            // sanity check whether the input is ok
#if DEBUG
            {
                AssimpSharp.FBX.Object target_ = null;
                foreach (var node in curves)
                {
                    if (target_ == null)
                    {
                        target_ = node.Target;
                    }
                    Debug.Assert(node.Target == target_);
                }
            }
#endif

            AnimationCurveNode curveNode = null;
            foreach (var node in curves)
            {
                Debug.Assert(node != null);
                if (node.TargetProperty.Length == 0)
                {
                    FBXImporter.LogWarn("target property for animation curve not set: " + node.Name);
                    continue;
                }

                curveNode = node;
                if (node.Curves.Count == 0)
                {
                    FBXImporter.LogWarn("no animation curves assigned to AnimationCurveNode: " + node.Name);
                    continue;
                }

                if (nodePropertyMap.ContainsKey(node.TargetProperty))
                {
                    nodePropertyMap[node.TargetProperty].Add(node);
                }
                else
                {
                    nodePropertyMap[node.TargetProperty] = new List<AnimationCurveNode>() { node };
                }
            }

            Debug.Assert(curveNode != null);
            Debug.Assert(curveNode.TargetAsModel != null);

            var target = curveNode.TargetAsModel;

            var chain = new List<AnimationCurveNode>[Enum.GetValues(typeof(TransformationComp)).Length];

            bool hasAny = false;
            bool hasComplex = false;

            for (int i = 0; i < Enum.GetValues(typeof(TransformationComp)).Length; i++)
            {
                var comp = (TransformationComp)i;

                // inverse pivots don't exist in the input, we just generate them
                if (comp == TransformationComp.RotationPivotInverse || comp == TransformationComp.ScalingPivotInverse)
                {
                    chain[i] = null;
                    continue;
                }
                
                nodePropertyMap.TryGetValue(NameTransformationCompProperty(comp), out chain[i]);
                if (chain[i] != null)
                {
                    // check if this curves contains redundant information by looking
                    // up the corresponding node's transformation chain.
                    if (Doc.Settings.OptimizeEmptyAnimationCurves && IsRedundantAnimationData(target, comp, chain[i]))
                    {
                        FBXImporter.LogWarn("dropping redundant animation channel for node " + target.Name);
                        continue;
                    }

                    hasAny = true;

                    if (comp != TransformationComp.Rotation && comp != TransformationComp.Scaling && comp != TransformationComp.Translation &&
                        comp != TransformationComp.GeometricScaling && comp != TransformationComp.GeometricRotation && comp != TransformationComp.GeometricTranslation)
                    {
                        hasComplex = true;
                    }
                }
            }

            if (!hasAny)
            {
                FBXImporter.LogWarn("ignoring node animation, did not find any transformation key frames");
                return;
            }

            // this needs to play nicely with GenerateTransformationNodeChain() which will
            // be invoked _later_ (animations come first). If this node has only rotation,
            // scaling and translation _and_ there are no animated other components either,
            // we can use a single node and also a single node animation channel.
            if (!hasComplex && !NeedsComplexTransformationChain(target))
            {

                var nd = GenerateSimpleNodeAnim(fixedName, target, chain,
                     layerMap,
                     start, stop,
                     ref maxTime,
                     ref minTime,
                     true // input is TRS order, assimp is SRT
                     );
                Debug.Assert(nd != null);
                if (nd.PositionKeys.Length == 0 && nd.RotationKeys.Length == 0 && nd.ScalingKeys.Length == 0)
                {
                }
                else
                {
                    nodeAnims.Add(nd);
                }
                return;
            }

            // otherwise, things get gruesome and we need separate animation channels
            // for each part of the transformation chain. Remember which channels
            // we generated and pass this information to the node conversion
            // code to avoid nodes that have identity transform, but non-identity
            // animations, being dropped.
            uint flags = 0;
            uint bit = 0x1;
            for (int i = 0; i < Enum.GetValues(typeof(TransformationComp)).Length; i++, bit <<= 1)
            {
                var comp = (TransformationComp)i;
                if (chain[i] != nodePropertyMap.Values.ToList()[nodePropertyMap.Count-1])
                {
                    flags |= bit;

                    Debug.Assert(comp != TransformationComp.RotationPivotInverse);
                    Debug.Assert(comp != TransformationComp.ScalingPivotInverse);

                    var chainName = NameTransformationChainNode(fixedName, comp);

                    NodeAnim na = null;
                    switch(comp)
                    {
                        case TransformationComp.Rotation:
                        case TransformationComp.PreRotation:
                        case TransformationComp.PostRotation:
                        case TransformationComp.GeometricRotation:
                            na = GenerateRotationNodeAnim(chainName, target, chain[i], layerMap, start, stop, ref maxTime, ref minTime);
                            break;

                        case TransformationComp.RotationOffset:
                        case TransformationComp.RotationPivot:
                        case TransformationComp.ScalingOffset:
                        case TransformationComp.ScalingPivot:
                        case TransformationComp.Translation:
                        case TransformationComp.GeometricTranslation:
                            na = GenerateTranslationNodeAnim(chainName, target, chain[i], layerMap, start, stop, maxTime, minTime);
                            // pivoting requires us to generate an implicit inverse channel to undo the pivot translation
                            if (comp == TransformationComp.RotationPivot)
                            {
                                var invName = NameTransformationChainNode(fixedName, TransformationComp.RotationPivotInverse);
                                var inv = GenerateTranslationNodeAnim(invName, target, chain[i], layerMap, start, stop, maxTime, minTime, true);

                                Debug.Assert(inv != null);

                                if (inv.PositionKeys.Length == 0 && inv.RotationKeys.Length == 0 && inv.ScalingKeys.Length == 0)
                                {
                                }
                                else
                                {
                                    nodeAnims.Add(inv);
                                }
                                Debug.Assert((int)TransformationComp.RotationPivotInverse > i);
                                flags |= bit << (int)(TransformationComp.RotationPivotInverse - i);
                            }
                            else if (comp == TransformationComp.ScalingPivot)
                            {
                                var invName = NameTransformationChainNode(fixedName, TransformationComp.ScalingPivotInverse);
                                var inv = GenerateTranslationNodeAnim(invName, target, chain[i], layerMap, start, stop, maxTime, minTime, true);

                                Debug.Assert(inv != null);

                                if (inv.PositionKeys.Length == 0 && inv.RotationKeys.Length == 0 && inv.ScalingKeys.Length == 0)
                                {
                                }
                                else
                                {
                                    nodeAnims.Add(inv);
                                }
                                Debug.Assert((int)TransformationComp.RotationPivotInverse > i);
                                flags |= bit << (int)(TransformationComp.RotationPivotInverse - i);
                            }
                            break;
                        case TransformationComp.Scaling:
                        case TransformationComp.GeometricScaling:
                            na = GenerateTranslationNodeAnim(chainName, target, chain[i], layerMap, start, stop, maxTime, minTime);
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }
                    Debug.Assert(na != null);
                    if (na.PositionKeys.Length == 0 && na.RotationKeys.Length == 0 && na.ScalingKeys.Length == 0)
                    {
                    }
                    else
                    {
                        nodeAnims.Add(na);
                    }
                    continue;
                }
            }
            NodeAnimChainBits[fixedName] = flags;
        }

        private bool IsRedundantAnimationData(Model target, TransformationComp comp, List<AnimationCurveNode> curves)
        {
            Debug.Assert(curves.Count > 0);

            // look for animation nodes with
            //  * sub channels for all relevant components set
            //  * one key/value pair per component
            //  * combined values match up the corresponding value in the bind pose node transformation
            // only such nodes are 'redundant' for this function.

            if (curves.Count > 1)
            {
                return false;
            }

            var nd = curves[0];
            var subCurves = nd.Curves;

            AnimationCurve dx;
            AnimationCurve dy;
            AnimationCurve dz;

            subCurves.TryGetValue("d|X", out dx);
            subCurves.TryGetValue("d|Y", out dy);
            subCurves.TryGetValue("d|Z", out dz);

            if (dx == null || dy == null || dz == null)
            {
                return false;
            }

            var vx = dx.Values;
            var vy = dy.Values;
            var vz = dz.Values;

            if (vx.Count != 1 || vy.Count != 1 || vz.Count != 1)
            {
                return false;
            }

            var dynVal = new Vector3(vx[0], vy[0], vz[0]);
            var staticVal = PropertyHelper.PropertyGet<Vector3>(target.Props,
                NameTransformationCompProperty(comp),
                TransformationCompDefaultValue(comp));

            const float epsilon = 1e-6f;
            return (dynVal - staticVal).LengthSquared() < epsilon;
        }

        private AssimpSharp.NodeAnim GenerateRotationNodeAnim(string name, Model target, List<AnimationCurveNode> curves, LayerMap layerMap, long start, long stop, ref double maxTime, ref double minTime)
        {
            var na = new AssimpSharp.NodeAnim();
            na.NodeName = name;

            ConvertRotationKeys(na, curves, layerMap, start, stop, ref maxTime, ref minTime, target.RotationOrder.Value);

            // dummy scaling key
            na.ScalingKeys = new AssimpSharp.VectorKey[1];

            na.ScalingKeys[0].Time = 0;
            na.ScalingKeys[0].Value = new Vector3(1.0f, 1.0f, 1.0f);

            // dummy position key
            na.PositionKeys = new AssimpSharp.VectorKey[1];

            na.PositionKeys[0].Time = 0;
            na.PositionKeys[0].Value = new Vector3();

            return na;
        }

        private AssimpSharp.NodeAnim GenerateScalingNodeAnim(string name, Model target, List<AnimationCurveNode> curves, LayerMap layerMap,
            long start, long stop,
            double maxTime, double minTime)
        {
            var na = new AssimpSharp.NodeAnim();
            na.NodeName = name;

            ConvertScaleKeys(na, curves, layerMap, start, stop, ref maxTime, ref minTime);

            // dummy rotation key
            na.RotationKeys = new AssimpSharp.QuatKey[1];
            na.RotationKeys[0] = new AssimpSharp.QuatKey(0, new Quaternion());

            // dummy position key
            na.PositionKeys = new AssimpSharp.VectorKey[1];
            na.PositionKeys[0] = new AssimpSharp.VectorKey(0, new Vector3());

            return na;
        }

        private AssimpSharp.NodeAnim GenerateTranslationNodeAnim(string name, Model target, List<AnimationCurveNode> curves, LayerMap layerMap,
            long start, long stop, double maxTime, double minTime, bool inverse = false)
        {
            var na = new AssimpSharp.NodeAnim();
            na.NodeName = name;

            ConvertRotationKeys(na, curves, layerMap, start, stop, ref maxTime, ref minTime, target.RotationOrder.Value);

            if (inverse)
            {
                for (int i = 0; i < na.PositionKeys.Length; i++)
                {
                    na.PositionKeys[i].Value *= -1.0f;
                }
            }

            // dummy scaling key
            na.ScalingKeys = new AssimpSharp.VectorKey[1];
            na.ScalingKeys[0].Time = 0;
            na.ScalingKeys[0].Value = new Vector3(1, 1, 1);

            // dummy rotation key
            na.RotationKeys = new AssimpSharp.QuatKey[1];
            na.RotationKeys[0].Time = 0;
            na.RotationKeys[0].Value = Quaternion.Identity;

            return na;
        }

        /// <summary>
        /// generate node anim, extracting only Rotation, Scaling and Translation from the given chain
        /// </summary>
        private AssimpSharp.NodeAnim GenerateSimpleNodeAnim(string name,
            Model target,
            List<AnimationCurveNode>[] chain,
            LayerMap layerMap,
            long start,
            long stop,
            ref double maxTime,
            ref double minTime,
            bool reverseOrder = false)
        {
            var na = new AssimpSharp.NodeAnim();
            na.NodeName = name;
            var props = target.Props;

            // need to convert from TRS order to SRT?
            if (reverseOrder)
            {
                var defScale = Vector3.Zero;
                var defTranslate = Vector3.Zero;
                var defRot = Quaternion.Identity;
                var scaling = new KeyFrameListList();
                var translation = new KeyFrameListList();
                var rotation = new KeyFrameListList();
                if (chain[(int)TransformationComp.Scaling] != null)
                {
                    scaling = GetKeyframeList(chain[(int)TransformationComp.Scaling], start, stop);
                }
                else
                {
                    defScale = PropertyHelper.PropertyGet(props, "Lcl Scaling", new Vector3(1, 1, 1));
                }
                if (chain[(int)TransformationComp.Translation] != null)
                {
                    translation = GetKeyframeList(chain[(int)TransformationComp.Translation], start, stop);
                }
                else
                {
                    defTranslate = PropertyHelper.PropertyGet(props, "Lcl Translation", new Vector3(0, 0, 0));
                }
                if (chain[(int)TransformationComp.Rotation] != null)
                {
                    rotation = GetKeyframeList(chain[(int)TransformationComp.Rotation], start, stop);
                }
                else
                {
                    defRot = EulerToQuaternion(PropertyHelper.PropertyGet(props, "Lcl Rotation", new Vector3(0, 0, 0)), target.RotationOrder.Value);
                }

                var joined = new KeyFrameListList();
                joined.AddRange(scaling);
                joined.AddRange(translation);
                joined.AddRange(rotation);

                var times = GetKeyTimeList(joined);

                var outQuat = new AssimpSharp.QuatKey[times.Count];
                var outScale = new AssimpSharp.VectorKey[times.Count];
                var outTranslation = new AssimpSharp.VectorKey[times.Count];

                if (times.Count > 0)
                {
                    ConvertTransformOrder_TRStoSRT(outQuat, outScale, outTranslation,
                        scaling, translation, rotation, times, ref maxTime, ref minTime, target.RotationOrder.Value, defScale, defTranslate, defRot);
                }

                // XXX remove duplicates / redundant keys which this operation did
                // likely produce if not all three channels were equally dense.

                na.ScalingKeys = outScale;
                na.RotationKeys = outQuat;
                na.PositionKeys = outTranslation;
            }
            else
            {
                // if a particular transformation is not given, grab it from
                // the corresponding node to meet the semantics of aiNodeAnim,
                // which requires all of rotation, scaling and translation
                // to be set.

                if (chain[(int)TransformationComp.Scaling] != null)
                {
                    ConvertScaleKeys(na, chain[(int)TransformationComp.Scaling],
                        layerMap, start, stop, ref maxTime, ref minTime);
                }
                else
                {
                    na.ScalingKeys = new AssimpSharp.VectorKey[1];
                    na.ScalingKeys[0].Time = 0;
                    na.ScalingKeys[0].Value = PropertyHelper.PropertyGet(props, "Lcl Scaling", new Vector3(1, 1, 1));
                }
                if (chain[(int)TransformationComp.Rotation] != null)
                {
                    ConvertRotationKeys(na, chain[(int)TransformationComp.Rotation],
                        layerMap, start, stop, ref maxTime, ref minTime, target.RotationOrder.Value);
                }
                else
                {
                    na.RotationKeys = new AssimpSharp.QuatKey[1];
                    na.RotationKeys[0].Time = 0;
                    na.RotationKeys[0].Value = EulerToQuaternion(PropertyHelper.PropertyGet(props, "Lcl Rotation", new Vector3(0, 0, 0)), target.RotationOrder.Value);
                }
                if (chain[(int)TransformationComp.Translation] != null)
                {
                    ConvertTranslationKeys(na, chain[(int)TransformationComp.Translation],
                        layerMap, start, stop, ref maxTime, ref minTime);
                }
                else
                {
                    na.PositionKeys = new AssimpSharp.VectorKey[1];
                    na.PositionKeys[0].Time = 0;
                    na.PositionKeys[0].Value = PropertyHelper.PropertyGet(props, "Lcl Translation", new Vector3(0, 0, 0));
                }
            }

            return na;
        }

        private KeyFrameListList GetKeyframeList(List<AnimationCurveNode> nodes, long start, long stop)
        {
            var inputs = new KeyFrameListList(nodes.Count * 3);

            //give some breathing room for rounding errors
            long adjStart = start - 10000;
            long adjeStop = stop + 10000;

            foreach(var node in nodes)
            {
                Debug.Assert(node != null);

                var curves = node.Curves;
                foreach(var kv in curves)
                {
                    uint mapto;
                    if (kv.Key == "d|X")
                    {
                        mapto = 0;
                    }
                    else if (kv.Key == "d|Y")
                    {
                        mapto = 1;
                    }
                    else if (kv.Key == "d|Z")
                    {
                        mapto = 2;
                    }
                    else
                    {
                        FBXImporter.LogWarn("ignoring scale animation curve, did not recognize target component");
                        continue;
                    }

                    var curve = kv.Value;
                    Debug.Assert(curve.Keys.Count == curve.Values.Count && curve.Keys.Count > 0);

                    //get values within the start/stop time window
                    int count = curve.Keys.Count;
                    var keys = new KeyTimeList(count);
                    var values = new KeyValueList(count);
                    for (int n = 0; n < count; n++)
                    {
                        long k = curve.Keys[n];
                        if (k >= adjStart && k <= adjStart)
                        {
                            keys.Add(k);
                            values.Add(curve.Values[n]);
                        }
                    }
                    inputs.Add(new KeyFrameList(keys, values, mapto));
                }
            }
            return inputs;
        }

        private KeyTimeList GetKeyTimeList(KeyFrameListList inputs)
        {
            Debug.Assert(inputs.Count > 0);

            // reserve some space upfront - it is likely that the keyframe lists
            // have matching time values, so max(of all keyframe lists) should
            // be a good estimate.
            int estimate = 0;
            foreach (var kfl in inputs)
            {
                estimate = Math.Max(estimate, kfl.Item1.Count);
            }

            var keys = new KeyTimeList(estimate);

            var nextPos = new uint[inputs.Count];

            var count = inputs.Count;
            while (true)
            {
                long minTick = long.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    var kfl = inputs[i];
                    if (kfl.Item1.Count > nextPos[i] && kfl.Item1[(int)nextPos[i]] < minTick)
                    {
                        minTick = (long)kfl.Item1[(int)nextPos[i]];
                    }
                }

                if (minTick == long.MaxValue)
                {
                    break;
                }
                keys.Add(minTick);

                for (int i = 0; i < count; i++)
                {
                    var kfl = inputs[i];
                    while (kfl.Item1.Count > nextPos[i] && kfl.Item1[(int)nextPos[i]] == minTick)
                    {
                        ++nextPos[i];
                    }
                }
            }
            return keys;
        }

        private void InterpolateKeys(AssimpSharp.VectorKey[] valOut, KeyTimeList keys, KeyFrameListList inputs,
            bool geom,
            ref double maxTime,
            ref double minTime)
        {
            Debug.Assert(keys.Count > 0);
            Debug.Assert(valOut != null);

            var count = inputs.Count;
            var nextPos = new List<int>(count);

            int valOutIndex = 0;
            foreach (var time in keys)
            {
                var result = new float[] { 0, 0, 0 };
                if (geom)
                {
                    result[0] = result[1] = result[2] = 1.0f;
                }

                for (int i = 0; i < count; i++)
                {
                    var kfl = inputs[i];
                    var ksize = kfl.Item1.Count;
                    if (ksize > nextPos[i] && kfl.Item1[nextPos[i]] == time)
                    {
                        ++nextPos[i];
                    }

                    var id0 = nextPos[i] > 0 ? nextPos[i] - 1 : 0;
                    var id1 = nextPos[i] == ksize ? ksize - 1 : nextPos[i];

                    // use lerp for interpolation
                    var valueA = kfl.Item2[id0];
                    var valueB = kfl.Item2[id1];
                    var timeA = kfl.Item1[id0];
                    var timeB = kfl.Item1[id1];

                    // do the actual interpolation in double-precision arithmetics
                    // because it is a bit sensitive to rounding errors.
                    var factor = timeB == timeA ? 0.0 : (double)((time - timeA) / (timeB - timeA));
                    var interpValue = (float)(valueA + (valueB - valueA) * factor);

                    if (geom)
                    {
                        result[kfl.Item3] *= interpValue;
                    }
                    else
                    {
                        result[kfl.Item3] += interpValue;
                    }
                }

                // magic value to convert fbx times to seconds
                valOut[valOutIndex].Time = CONVERT_FBX_TIME(time) * AnimFps;

                minTime = Math.Min(minTime, valOut[valOutIndex].Time);
                maxTime = Math.Max(maxTime, valOut[valOutIndex].Time);

                valOut[valOutIndex].Value.X = result[0];
                valOut[valOutIndex].Value.Y = result[1];
                valOut[valOutIndex].Value.Z = result[2];

                valOutIndex++;
            }
        }

        private void InterpolateKeys(AssimpSharp.QuatKey[] valOut, KeyTimeList keys, KeyFrameListList inputs,
            bool geom, ref double maxTime, ref double minTime, Model.RotOrder order)
        {
            Debug.Assert(keys.Count > 0);
            Debug.Assert(valOut != null);

            var temp = new AssimpSharp.VectorKey[keys.Count];
            InterpolateKeys(temp, keys, inputs, geom, ref maxTime, ref minTime);

            Matrix m;
            Quaternion lastq = Quaternion.Identity;
            for (int i = 0; i < keys.Count; i++)
            {
                valOut[i].Time = temp[i].Time;
                GetRotationMatrix(order, temp[i].Value, out m);
                var quat = Quaternion.RotationMatrix(m);
                if (Quaternion.Dot(quat, lastq) < 0)
                {
                    quat = -quat;
                }
                lastq = quat;
                valOut[i].Value = quat;
            }
        }

        private void ConvertTransformOrder_TRStoSRT(
            AssimpSharp.QuatKey[] outQuat,
            AssimpSharp.VectorKey[] outScale,
            AssimpSharp.VectorKey[] outTranslation,
            KeyFrameListList scaling,
            KeyFrameListList translation,
            KeyFrameListList rotation,
            KeyTimeList times,
            ref double maxTime,
            ref double minTime,
            Model.RotOrder order,
            Vector3 defScasle,
            Vector3 defTranslate,
            Quaternion defRotation)
        {
            if (rotation.Count > 0)
            {
                InterpolateKeys(outQuat, times, rotation, false, ref maxTime, ref minTime, order);
            }
            else
            {
                for (int i = 0; i < times.Count; i++)
                {
                    outQuat[i].Time = CONVERT_FBX_TIME(times[i]) * AnimFps;
                    outQuat[i].Value = defRotation;
                }
            }

            if (scaling.Count > 0)
            {
                InterpolateKeys(outScale, times, scaling, true, ref maxTime, ref minTime);
            }
            else
            {
                for (int i = 0; i < times.Count; i++)
                {
                    outScale[i].Time = CONVERT_FBX_TIME(times[i]) * AnimFps;
                    outScale[i].Value = defScasle;
                }
            }

            if (translation.Count > 0)
            {
                InterpolateKeys(outTranslation, times, translation, false, ref maxTime, ref minTime);
            }
            else
            {
                for (int i = 0; i < times.Count; i++)
                {
                    outTranslation[i].Time = CONVERT_FBX_TIME(times[i]) * AnimFps;
                    outTranslation[i].Value = defTranslate;
                }
            }

            var count = times.Count;
            for (int i = 0; i < count; i++)
            {
                var r = outQuat[i].Value;
                var s = outScale[i].Value;
                var t = outTranslation[i].Value;

                Matrix mat;

                Matrix.Translation(ref t, out mat);
                mat *= Matrix.RotationQuaternion(r);
                mat *= Matrix.Scaling(s);
                mat.Decompose(out s, out r, out t);
                outQuat[i].Value = r;
                outScale[i].Value = s;
                outTranslation[i].Value = t;
            }

        }

        private Quaternion EulerToQuaternion(Vector3 rot, Model.RotOrder order)
        {
            Matrix m;
            GetRotationMatrix(order, rot, out m);
            return Quaternion.RotationMatrix(m);
        }

        private void ConvertScaleKeys(AssimpSharp.NodeAnim na, List<AnimationCurveNode> nodes, LayerMap layers,
            long start, long stop,
            ref double maxTime,
            ref double minTime)
        {
            Debug.Assert(nodes.Count > 0);
            var inputs = GetKeyframeList(nodes, start, stop);
            var keys = GetKeyTimeList(inputs);
            na.ScalingKeys = new AssimpSharp.VectorKey[keys.Count];
            InterpolateKeys(na.ScalingKeys, keys, inputs, true, ref maxTime, ref minTime);
        }

        private void ConvertTranslationKeys(AssimpSharp.NodeAnim na, List<AnimationCurveNode> nodes,
            LayerMap layers,
            long start, long stop,
            ref double maxTime,
            ref double minTime)
        {
            Debug.Assert(nodes.Count > 0);
            var inputs = GetKeyframeList(nodes, start, stop);
            var keys = GetKeyTimeList(inputs);
            na.PositionKeys = new AssimpSharp.VectorKey[keys.Count];
            InterpolateKeys(na.PositionKeys, keys, inputs, false, ref maxTime, ref minTime);
        }

        private void ConvertRotationKeys(AssimpSharp.NodeAnim na, List<AnimationCurveNode> nodes, LayerMap layers,
            long start, long stop, ref double maxTime, ref double minTime, Model.RotOrder order)
        {
            Debug.Assert(nodes.Count > 0);
            var inputs = GetKeyframeList(nodes, start, stop);
            var keys = GetKeyTimeList(inputs);
            na.RotationKeys = new AssimpSharp.QuatKey[keys.Count];
            InterpolateKeys(na.RotationKeys, keys, inputs, false, ref maxTime, ref minTime, order);
        }

        #endregion

        private void TransferDataToScene()
        {
            this.Result.Meshes.AddRange(Meshes);
            this.Result.Materials.AddRange(Materials);
            this.Result.Animations.AddRange(Animations);
            this.Result.Lights.AddRange(Lights);
            this.Result.Cameras.AddRange(Cameras);
        }

        public static void ConvertToScene(out AssimpSharp.Scene result, Document doc)
        {
            new FbxConverter(out result, doc);
        }
    }
}
