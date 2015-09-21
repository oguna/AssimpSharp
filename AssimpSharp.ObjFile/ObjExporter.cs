using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpDX;

namespace AssimpSharp.ObjFile
{
    /// <summary>
    /// Helper class to export a given scene to an OBJ file.
    /// </summary>
    public class ObjExporter
    {
        struct FaceVertex
        {
            public int VP;
            public int VN;
            public int VT;
        }

        struct Face
        {
            public char Kind;
            public List<FaceVertex> Indices;
        }

        struct MeshInstance
        {
            public string Name;
            public string Matname;
            public List<Face> Faces;
        }

        string Filename;

        Scene Scene;

        List<Vector3> Vp;
        List<Vector3> Vn;
        List<Vector3> Vt;

        public ObjExporter(string filename, Scene scene)
        {
            Filename = filename;
            Scene = scene;
            Endl = "\n";
        }

        string Endl;

        public string MaterialLibName;
        public string MaterialLibFileName;
        public StreamWriter output, outputMat;

    }
}
