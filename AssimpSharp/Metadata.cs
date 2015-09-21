using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp
{
    public enum MetadataType
    {
        Bool,
        Int,
        Uint64,
        Float,
        String,
        Vector3
    }

    public struct MetadataEntry
    {
        public MetadataType Type;
        public Object Data;
    }

    public class Metadata
    {
        public int NumProperties;
        public string[] Keys;
        public MetadataEntry[] Values;
        public void Set<T>(int index, string key, T value)
        {
            Debug.Assert(index < NumProperties);
            Keys[index] = key;
            Values[index] = new MetadataEntry()
            {
                Type = GetType<T>(value),
                Data = value
            };
        }
        public bool Get<T>(int index, out T value)
        {
            Debug.Assert(index < NumProperties);
            if (GetType<T>(default(T)) != Values[index].Type)
            {
                value = default(T);
                return false;
            }
            value = (T)(Values[index].Data);
            return true;
        }
        public bool Get<T>(string key, out T value)
        {
            for(int i=0; i<NumProperties; i++)
            {
                if (Keys[i] == key)
                {
                    return Get<T>(i, out value);
                }
            }
            value = default(T);
            return false;
        }

        private MetadataType GetType<T>(T value)
        {
            if (value is bool){
                return MetadataType.Bool;
            } else if (value is int)
            {
                return MetadataType.Int;
            } else if (value is ulong)
            {
                return MetadataType.Uint64;
            } else if (value is float)
            {
                return MetadataType.Float;
            }
            else if (value is string)
            {
                return MetadataType.String;
            }
            else if (value is Vector3)
            {
                return MetadataType.Vector3;
            }
            throw(new Exception());
        }
    }
}
