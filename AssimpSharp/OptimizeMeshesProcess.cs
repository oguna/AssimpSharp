using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    /// <summary>
    /// Postprocessing step to optimize mesh usage
    /// </summary>
    class OptimizeMeshesProcess
    {
        struct MeshInfo
        {
            public int InstanceCount;
            public int VertexFormat;
            public int OutputId;
        }

        Scene Scene;

        List<MeshInfo> Meshes;

        List<Mesh> Output;

        bool Pts;

        int MaxVerts;

        int MaxFaces;

        List<Mesh> MergeList;

        public bool IsActive(int flags)
        {
            throw (new NotImplementedException());
        }

        public void Execute(Scene scene)
        { }

        public void SetupProperties(BaseImporter importer)
        { }

        public void EnablePrimitiveTypeSorting(bool enable)
        {
            Pts = enable;
        }

        public bool IsPrimitiveTypeSortingEnabled()
        {
            return Pts;
        }

        protected void ProcessNode(Node node)
        {

        }

        protected void CanJoin(int a, int b, int verts, int faces)
        {

        }

        protected void FindInstancedMesh(Node node)
        {

        }
    }
}
