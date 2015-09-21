using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color4D = SharpDX.Color4;
using Color3D = SharpDX.Color3;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Matrix4x4 = SharpDX.Matrix;
using VectorKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Vector3>;
using QuatKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Quaternion>;
using MatrixKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Matrix>;

namespace AssimpSharp.XFile
{
    /// <summary>
    /// Helper structure representing a XFile mesh face
    /// </summary>
    public struct Face
    {
        public List<uint> Indices;
    }

    /// <summary>
    /// Helper structure representing a texture filename inside a material and its potential source
    /// </summary>
    public struct TexEntry
    {
        public string Name;

        /// <summary>
        /// true if the texname was specified in a NormalmapFilename tag
        /// </summary>
        public bool IsNormalMap;

        public TexEntry(string name, bool isNormalMap = false)
        {
            Name = name;
            IsNormalMap = isNormalMap;
        }
    }

    /// <summary>
    /// Helper structure representing a XFile material
    /// </summary>
    public struct Material
    {
        public string Name;

        /// <summary>
        /// if true, mName holds a name by which the actual material can be found in the material list
        /// </summary>
        public bool IsReference;

        public Color4D Diffuse;

        public float SpecularExponent;

        public Color3D Specular;

        public Color3D Emissive;

        public List<TexEntry> Textures;

        /// <summary>
        /// the index under which it was stored in the scene's material list
        /// </summary>
        public int SceneIndex;
    }

    /// <summary>
    /// Helper structure to represent a bone weight
    /// </summary>
    public struct BoneWeight
    {
        public uint Vertex;

        public float Weight;
    }

    /// <summary>
    /// Helper structure to represent a bone in a mesh
    /// </summary>
    public class Bone
    {
        public string Name;

        public List<BoneWeight> Weights;

        public Matrix4x4 OffsetMatrix;

        public Bone()
        {
            Weights = new List<BoneWeight>();
        }
    }

    /// <summary>
    /// Helper structure to represent an XFile mesh
    /// </summary>
    public class Mesh
    {
        public String Name;

        public List<Vector3> Positions;

        public List<Face> PosFaces;

        public List<Vector3> Normals;

        public List<Face> NormalFaces;

        public uint NumTextures;

        public List<Vector2>[] TexCoords;

        public uint NumColorSets;

        public List<Color4D>[] Colors;

        public List<uint> FaceMaterials;

        public List<Material> Materials;

        public List<Bone> Bones;

        public Mesh(string name = "")
        {
            uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;
            uint AI_MAX_NUMBER_OF_COLOR_SETS = 4;
            Name = name;
            Positions = new List<Vector3>();
            PosFaces = new List<Face>();
            Normals = new List<Vector3>();
            NormalFaces = new List<Face>();
            TexCoords = new List<Vector2>[AI_MAX_NUMBER_OF_TEXTURECOORDS];
            Colors = new List<Color4D>[AI_MAX_NUMBER_OF_COLOR_SETS];
            FaceMaterials = new List<uint>();
            Materials = new List<Material>();
            Bones = new List<Bone>();
            NumTextures = 0;
            NumColorSets = 0;
        }
    }

    /// <summary>
    /// Helper structure to represent a XFile frame
    /// </summary>
    public class Node
    {
        public string Name;

        public Matrix4x4 TrafoMatrix;

        public Node Parent;

        public List<Node> Children = new List<Node>();

        public List<Mesh> Meshes = new List<Mesh>();

        public Node(Node parent = null)
        {
            this.Parent = parent;
        }
    }

    /// <summary>
    /// Helper structure representing a single animated bone in a XFile
    /// </summary>
    public class AnimBone
    {
        public string BoneName;

        /// <summary>
        /// either three separate key sequences for position, rotation, scaling
        /// </summary>
        public List<VectorKey> PosKeys = new List<VectorKey>();

        public List<QuatKey> RotKeys = new List<QuatKey>();

        public List<VectorKey> ScaleKeys = new List<VectorKey>();

        /// <summary>
        /// or a combined key sequence of transformation matrices.
        /// </summary>
        public List<MatrixKey> TrafoKeys = new List<MatrixKey>();
    }

    /// <summary>
    /// Helper structure to represent an animation set in a XFile
    /// </summary>
    public class Animation
    {
        public string Name;

        public List<AnimBone> Anims = new List<AnimBone>();
    }

    /// <summary>
    /// Helper structure analogue to aiScene
    /// </summary>
    public class Scene
    {
        public Node RootNode;

        /// <summary>
        /// global meshes found outside of any frames
        /// </summary>
        public List<Mesh> GlobalMeshes = new List<Mesh>();

        /// <summary>
        /// global materials found outside of any meshes.
        /// </summary>
        public List<Material> GlobalMaterial = new List<Material>();

        public List<Animation> Anims = new List<Animation>();

        public uint AnimTicksPerSecond;
    }
}