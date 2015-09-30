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
                return PropertyHelper.PropertyGet(props, name, defaultValue);
            }
        }
    }
}
