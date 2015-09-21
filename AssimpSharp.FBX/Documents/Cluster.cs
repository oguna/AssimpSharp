using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for skin deformer clusters (aka subdeformers)
    /// </summary>
    public class Cluster : Deformer
    {
        public Cluster(ulong id, Element element, Document doc, string name)
        : base(id, element, doc, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var Indexes = sc["Indexes"];
            var Weights = sc["Weights"];
            var Transform = Parser.GetRequiredElement(sc, "Transform", element);
            var TransformLink = Parser.GetRequiredElement(sc, "Transform", element);
            this.Transform = Parser.ReadMatrix(Transform);
            this.TransformLink = Parser.ReadMatrix(TransformLink);

            // it is actually possible that there be Deformer's with no weights
            if ((Indexes == null) != (Weights == null))
            {
                throw (new DomException("either Indexes or Weights are missing from Cluster", element));
            }
            if (Indexes != null)
            {
                List<uint> indices;
                List<float> weights;
                Parser.ParseVectorDataArray(out indices, Indexes);
                Parser.ParseVectorDataArray(out weights, Weights);
                this.Indices = indices;
                this.Weights = weights;
            }
            if (this.Indices.Count != this.Weights.Count)
            {
                throw (new DomException("sizes of index and weight array don't match up", element));
            }

            // read assigned node
            var conns = doc.GetConnectionsByDestinationSequenced(ID, "Model");
            foreach (var con in conns)
            {
                var mod = DocumentUtil.ProcessSimpleConnection<Model>(con, false, "Model -> Cluster", element);
                if (mod != null)
                {
                    this.TargetNode = mod;
                    break;
                }
            }
            if (this.TargetNode == null)
            {
                throw (new DomException("failed to read target Node for Cluster", element));
            }
        }

        public List<float> Weights { get; private set; }

        public List<uint> Indices { get; private set; }

        public Matrix Transform { get; private set; }

        public Matrix TransformLink { get; private set; }

        public Model TargetNode { get; private set; }
    }
}
