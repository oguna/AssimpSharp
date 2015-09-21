using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for skin deformers
    /// </summary>
    public class Skin : Deformer
    {
        public Skin(ulong id, Element element, Document doc, string name)
        : base(id, element, doc, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var LinkDeformAcuracy = sc["Link_DeformAcuracy"];
            if (LinkDeformAcuracy != null)
            {
                DeformAccuracy = Parser.ParseTokenAsFloat(Parser.GetRequiredToken(LinkDeformAcuracy, 0));
            }
            var conns = doc.GetConnectionsByDestinationSequenced(ID, "Deformer");
            Clusters = new List<Cluster>(conns.Count);
            foreach (var con in conns)
            {
                var cluster = DocumentUtil.ProcessSimpleConnection<Cluster>(con, false, "Cluster -> Skin", element);
                if (cluster != null)
                {
                    Clusters.Add(cluster);
                    continue;
                }
            }
        }

        public float DeformAccuracy { get; private set; }

        public List<Cluster> Clusters { get; private set; }
    }
}
