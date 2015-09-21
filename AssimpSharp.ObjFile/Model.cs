using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Data structure to store all obj-specific model datas
    /// </summary>
    public class Model
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string ModelName;

        /// <summary>
        /// List ob assigned objects
        /// </summary>
        public List<Object> Objects;

        /// <summary>
        /// Pointer to current object
        /// </summary>
        public Object Current;

        /// <summary>
        /// Pointer to current material
        /// </summary>
        public Material CurrentMaterial;

        /// <summary>
        /// Pointer to default material
        /// </summary>
        public Material DefaultMaterial;

        /// <summary>
        /// Vector with all generated materials
        /// </summary>
        public List<string> MaterialLib;

        /// <summary>
        /// Vector with all generated group
        /// </summary>
        public List<string> GroupLib;

        /// <summary>
        /// Vector with all generated vertices
        /// </summary>
        public List<Vector3> Vertices;

        /// <summary>
        /// vector with all generated normals
        /// </summary>
        public List<Vector3> Normals;

        /// <summary>
        /// Group map
        /// </summary>
        public Dictionary<string, List<uint>> Groups;

        /// <summary>
        /// Group to face id assignment
        /// </summary>
        public List<uint> GroupFaceIDs;

        /// <summary>
        /// Active group
        /// </summary>
        public string ActiveGroup;

        /// <summary>
        /// Vector with generated texture coordinates
        /// </summary>
        public List<Vector2> TextureCoord;

        /// <summary>
        /// Current mesh instance
        /// </summary>
        public Mesh CurrentMesh;

        /// <summary>
        /// Vector with stored meshes
        /// </summary>
        public List<Mesh> Meshes;

        /// <summary>
        /// Material map
        /// </summary>
        public Dictionary<string, Material> MaterialMap;

        /// <summary>
        /// The default class constructor
        /// </summary>
        public Model()
        {
            ModelName = "";
            Current = null;
            CurrentMaterial = null;
            DefaultMaterial = null;
            GroupFaceIDs = null;
            ActiveGroup = "";
            CurrentMesh = null;
            Objects = new List<Object>();
            MaterialLib = new List<string>();
            GroupLib = new List<string>();
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            Groups = new Dictionary<string, List<uint>>();
            GroupFaceIDs = new List<uint>();
            TextureCoord = new List<Vector2>();
            Meshes = new List<Mesh>();
            MaterialMap = new Dictionary<string, Material>();
        }
    }
}
