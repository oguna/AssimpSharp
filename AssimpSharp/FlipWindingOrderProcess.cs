using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public class FlipWindingOrderProcess
    {
        public bool IsActive(int flags)
        {
            throw (new NotImplementedException());
        }

        public void Execute(Scene scene)
        {
            foreach (var mesh in scene.Meshes)
            {
                ProcessMesh(mesh);
            }
        }

        void ProcessMesh(Mesh mesh)
        {
            foreach(var face in mesh.Faces)
            {
                for(int b = 0; b < face.Indices.Length / 2; b++)
                {
                    var tmp = face.Indices[b];
                    face.Indices[b] = face.Indices[face.Indices.Length - 1 - b];
                    face.Indices[face.Indices.Length - 1 - b] = tmp;
                }
            }
        }
    }
}
