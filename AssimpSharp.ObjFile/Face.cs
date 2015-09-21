using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Data structure for a simple obj-face, describes discredit,l.ation and materials
    /// </summary>
    public class Face
    {
        /// <summary>
        /// Primitive type
        /// </summary>
        public PrimitiveType PrimitiveType;

        /// <summary>
        /// Vertex indices
        /// </summary>
        public List<uint> Vertices;

        /// <summary>
        /// Normal indices
        /// </summary>
        public List<uint> Normals;

        /// <summary>
        /// Texture coordinates indices
        /// </summary>
        public List<uint> TextureCoords;

        /// <summary>
        /// Assigned material
        /// </summary>
        public Material Material;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="vertices">Assigned vertex indexbuffer</param>
        /// <param name="normals">Assigned normals indexbuffer</param>
        /// <param name="texCoords">Assigned texture indexbuffer</param>
        public Face(List<uint> vertices, List<uint> normals, List<uint> texCoords, PrimitiveType pt = PrimitiveType.Polygon)
        {
            PrimitiveType = pt;
            Vertices = vertices;
            Normals = normals;
            TextureCoords = texCoords;
            Material = null;
        }
    }
}
