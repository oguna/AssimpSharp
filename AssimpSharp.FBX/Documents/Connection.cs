using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a link between two FBX objects.
    /// </summary>
    public class Connection : IComparer<Connection>,IComparable<Connection>
    {
        public readonly ulong insertionOrder;
        public readonly string prop;
        public readonly ulong src;
        public readonly ulong dest;
        public readonly Document doc;

        public Connection(ulong insertionOrder, ulong src, ulong dest,string prop, Document doc)
        {
            this.insertionOrder = insertionOrder;
            this.prop = prop;
            this.src = src;
            this.dest = dest;
            this.doc = doc;
        }

        public Object SourceObject
        {
            get
            {
                LazyObject lazy = doc.GetObject(src);
                Debug.Assert(lazy != null);
                return lazy.Get();
            }
        }

        public Object DestinationObject
        {
            get
            {
                LazyObject lazy = doc.GetObject(dest);
                Debug.Assert(lazy != null);
                return lazy.Get();
            }
        }

        public LazyObject LazySourceObject
        {
            get 
            {
                LazyObject lazy = doc.GetObject(src);
                Debug.Assert(lazy != null);
                return lazy;
            }
        }

        public LazyObject LazyDestinationObject
        {
            get
            {
                LazyObject lazy= doc.GetObject(dest);
                Debug.Assert(lazy != null);
                return lazy;
            }
        }


        public string PropertyName
        {
            get
            {
                return prop;
            }
        }

        public ulong InsertionOrder
        {
            get
            {
                return insertionOrder;
            }
        }

        public int CompareTo(Connection c)
        {
            if (InsertionOrder > c.InsertionOrder)
            {
                return 1;
            }
            else if (InsertionOrder < c.InsertionOrder){
                return -1;
            }
            return 0;
        }

        public bool Compare(Connection c)
        {
            return InsertionOrder < c.InsertionOrder;
        }

        public int Compare(Connection x, Connection y)
        {
            return (int)x.InsertionOrder - (int)y.InsertionOrder;
        }

        int IComparable<Connection>.CompareTo(Connection other)
        {
            return (int)InsertionOrder - (int)other.InsertionOrder;
        }
    }

}
