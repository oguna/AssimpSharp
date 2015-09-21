using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Data structure to store a mesh
    /// </summary>
    public class Mesh
    {
        public const uint NoMaterial = ~0u;
        public const uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;

        /// <summary>
        /// The name for the mesh
        /// </summary>
        public string Name;

        /// <summary>
        /// Array with pointer to all stored faces
        /// </summary>
        public List<Face> Faces;

        /// <summary>
        /// Assigned material
        /// </summary>
        public Material Material;

        /// <summary>
        /// Number of stored indices.
        /// </summary>
        public uint NumIndices;

        /// <summary>
        /// Number of UV
        /// </summary>
        public uint[] UVCoordinates;

        /// <summary>
        /// Material index.
        /// </summary>
        public uint MaterialIndex;

        /// <summary>
        /// True, if normals are stored.
        /// </summary>
        public bool HasNormals;

        /// <summary>
        /// Constructor
        /// </summary>
        public Mesh()
        {
            Material = null;
            NumIndices = 0;
            MaterialIndex = NoMaterial;
            HasNormals = false;
            UVCoordinates = new uint[AI_MAX_NUMBER_OF_TEXTURECOORDS];
            Faces = new List<Face>();
        }
    }
}
