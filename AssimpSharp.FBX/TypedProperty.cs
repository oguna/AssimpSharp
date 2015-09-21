using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public class TypedProperty<T> : Property
    {
        private T value;
        public T Value
        {
            get
            {
                return value;
            }
        }

        public TypedProperty(T value)
        {
            this.value = value;
        }
    }
}
