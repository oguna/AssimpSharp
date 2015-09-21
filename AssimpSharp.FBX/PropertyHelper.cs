using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public static class PropertyHelper
    {
        public static T PropertyGet<T>(PropertyTable table, string name, T defalutValue)
        {
            Property prop = table[name];
            if (prop == null)
            {
                return defalutValue;
            }
            TypedProperty<T> tprop = prop.As<TypedProperty<T>>();
            if (tprop == null)
            {
                return defalutValue;
            }
            return tprop.Value;
        }

        public static T PropertyGet<T>(PropertyTable table, string name, out bool result)
        {
            Property prop = table[name];
            if (prop == null)
            {
                result = false;
                return default(T);
            }
            var tprop = prop.As<TypedProperty<T>>();
            if (tprop == null)
            {
                result = false;
                return default(T);
            }
            result = true;
            return tprop.Value;
        }
    }
}
