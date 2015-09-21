using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Stores all objects of an objfile object definition
    /// </summary>
    public class Object
    {
        public enum ObjectType
        {
            ObjType,
            GroupType
        }

        /// <summary>
        /// Object name
        /// </summary>
        public string ObjName;

        /// <summary>
        /// Transformation matrix, stored in OpenGL format
        /// </summary>
        public Matrix Transformation;

        /// <summary>
        /// All sub-objects referenced by this object
        /// </summary>
        public List<Object> SubObjects;

        /// <summary>
        /// Assigned meshes
        /// </summary>
        public List<uint> Meshes;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Object()
        {
            ObjName = "";
            SubObjects = new List<Object>();
            Meshes = new List<uint>();
        }
    }
}
