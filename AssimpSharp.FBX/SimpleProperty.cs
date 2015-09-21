using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public class SimpleProperty<T>
    {
        private PropertyTable props;
        private string name;
        private T defaultValue;
        public SimpleProperty(PropertyTable props, string name, T defaultValue)
        {
            this.props = props;
            this.name = name;
            this.defaultValue = defaultValue;
        }
        public T Value
        {
            get
            {
                Property prop = props[name];
                if (prop == null)
                {
                    return defaultValue;
                }
                //TypedProperty<T> tprop = prop.As<TypedProperty<T>>();
                dynamic v = prop;
                var tprop = new TypedProperty<T>((T)v.Value);
                if (tprop == null)
                {
                    return defaultValue;
                }
                return tprop.Value;
            }
        }
    }
}
