using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    public class Face
    {
        public int[] Indices;
    }

    public struct VertexWeight
    {
        public int VertexId;
        public float Weight;
        public VertexWeight(int id, float weight)
        {
            this.VertexId = id;
            this.Weight = weight;
        }
    }

    public class Bone
    {
        public string Name;
        public int NumWeights;
        public VertexWeight[] Weights;
        public Matrix OffsetMatrix;
        public Bone()
        { }
        public Bone(Bone other)
        {
            this.Name = other.Name;
            this.NumWeights = other.NumWeights;
            this.Weights = new VertexWeight[other.Weights.Length];
            Array.Copy(other.Weights, this.Weights, other.Weights.Length);
            this.OffsetMatrix = other.OffsetMatrix;
        }
    }

    /// <summary>
    /// Enumerates the types of geometric primitives supported by Assimp.
    /// </summary>
    [Flags]
    public enum PrimitiveType
    {
        /// <summary>
        /// A point primitive.
        /// </summary>
        Point = 0x1,
        /// <summary>
        /// A point primitive. 
        /// </summary>
        Line = 0x2,
        /// <summary>
        /// A triangular primitive.
        /// </summary>
        Triangle = 0x4,
        /// <summary>
        /// A higher-level polygon with more than 3 edges.
        /// </summary>
        Polygon = 0x8
    }

    public class AnimMesh
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector3[] Tangents;
        public Vector3[] Bitangents;
        public Color4[][] Colors = new Color4[4][];
        public Vector3[][] TextureCoords = new Vector3[4][];
        public int NumVertices;
        public bool HasPosition
        {
            get
            {
                return Vertices == null || Vertices.Length == 0;
            }
        }
        public bool HasNormals
        {
            get
            {
                return Normals == null || Normals.Length == 0;
            }
        }
        public bool HasTangentsAndBitangets
        {
            get
            {
                return Tangents == null || Tangents.Length == 0;
            }
        }
        public bool HasVertexColors(int index)
        {
            if (index >= 4)
            {
                return false;
            }
            else
            {
                return Colors[index] != null;
            }
        }
        public bool HasTextureCoords(int index)
        {
            if (index >= 4)
            {
                return false;
            }
            else
            {
                return TextureCoords[index] != null;
            }
        }
    }

    /// <summary>
    /// A mesh represents a geometry or model with a single material. 
    /// </summary>
    public class Mesh
    {
        public const int AI_MAX_NUMBER_OF_COLOR_SETS = 8;
        public const int AI_MAX_NUMBER_OF_TEXTURECOORDS = 8;

        public PrimitiveType PrimitiveTypes;
        public int NumVertices;
        public int NumFaces;
        public Vector3[] Vertices = new Vector3[0];
        public Vector3[] Normals = new Vector3[0];
        public Vector3[] Tangents = new Vector3[0];
        public Vector3[] Bitangents = new Vector3[0];
        public Color4[][] Colors = new Color4[AI_MAX_NUMBER_OF_COLOR_SETS][];
        public Vector3[][] TextureCoords = new Vector3[AI_MAX_NUMBER_OF_TEXTURECOORDS][];
        public int[] NumUVComponents = new int[AI_MAX_NUMBER_OF_TEXTURECOORDS];
        public Face[] Faces;
        public int NumBones;
        public Bone[] Bones = new Bone[0];
        public int MaterialIndex;
        public string Name = string.Empty;
        public int NumAnimationMeshes;
        public AnimMesh[] AnimMeshes;
        public bool HasPosition
        {
            get
            {
                return Vertices != null && Vertices.Length != 0;
            }
        }
        public bool HasNormals
        {
            get
            {
                return Normals != null && Normals.Length != 0;
            }
        }
        public bool HasTangentsAndBitangets
        {
            get
            {
                return Tangents != null && Tangents.Length != 0;
            }
        }
        public bool HasVertexColors(int index)
        {
            if (index >= AI_MAX_NUMBER_OF_COLOR_SETS)
            {
                return false;
            }
            else
            {
                return Colors[index] != null && Colors[index].Length > 0 && NumVertices > 0;
            }
        }
        public bool HasTextureCoords(int index)
        {
            if (index >= AI_MAX_NUMBER_OF_TEXTURECOORDS)
            {
                return false;
            }
            else
            {
                return TextureCoords[index] != null && NumVertices > 0;
            }
        }
        public int GetNumUVChannels()
        {
            throw (new NotImplementedException());
        }
        public bool HasBones()
        {
            return Bones != null && NumBones > 0;
        }
    }
}
