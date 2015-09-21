using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    /// <summary>
    /// A node in the imported hierarchy. 
    /// </summary>
    public class Node
    {
        public string Name;
        public Matrix Transformation = Matrix.Identity;
        public Node Parent;
        public readonly List<Node> Children = new List<Node>();
        public readonly List<int> Meshes = new List<int>();
        public readonly Metadata MetaData = new Metadata();
        public Node()
        {
            Name = string.Empty;
        }
        public Node(string name)
        {
            this.Name = name;
        }
        public Node FindNode(string name)
        {
            if (Name == name)
            {
                return this;
            }
            foreach(var i in Children)
            {
                Node n;
                if ((n = i.FindNode(name)) != null)
                {
                    return n;
                }
            }
            return null;
        }

    }
}
