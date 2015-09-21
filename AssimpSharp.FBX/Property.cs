using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a dynamic property. Type info added by deriving classes,
    /// see #TypedProperty.
    /// <code>
    /// P: "ShininessExponent", "double", "Number", "",0.5
    /// </code>
    /// </summary>
    public abstract class Property
    {
        public static Property ReadTypedProperty(Element elemenet)
        {
            Debug.Assert(elemenet.KeyToken.StringContents == "P");
            var tok = elemenet.Tokens;
            Debug.Assert(tok.Count >= 5);
            string s = Parser.ParseTokenAsString(tok[1]);
            if (s =="KString")
            {
                return new TypedProperty<string>(Parser.ParseTokenAsString(tok[4]));
            }
            else if (s == "bool" || s == "Bool")
            {
                return new TypedProperty<bool>(Parser.ParseTokenAsInt(tok[4]) != 0);
            }
            else if (s == "int" || s == "Int" || s == "enum" || s == "Enum")
            {
                return new TypedProperty<int>(Parser.ParseTokenAsInt(tok[4]));
            }
            else if (s == "ULongLong")
            {
                return new TypedProperty<ulong>(Parser.ParseTokenAsID(tok[4]));
            }
            else if (s == "KTime")
            {
                return new TypedProperty<long>(Parser.ParseTokenAsInt64(tok[4]));
            }
            else if (s == "Vector3D" || s == "ColorRGB" || s == "Vector" || s == "Color" || s == "Lcl Translation" || s == "Lcl Rotation" || s == "Lcl Scaling")
            {
                return new TypedProperty<Vector3>(new Vector3(Parser.ParseTokenAsFloat(tok[4]),Parser.ParseTokenAsFloat(tok[5]),Parser.ParseTokenAsFloat(tok[6])));
            }
            else if (s == "double" || s == "Number" || s == "Float" || s == "FieldOfView")
            {
                return new TypedProperty<float>(Parser.ParseTokenAsFloat(tok[4]));
            }
            return null;
        }

        public static string PeekPropertyName(Element element)
        {
            Debug.Assert(element.KeyToken.StringContents == "P");
            List<Token> tok = element.Tokens;
            if (tok.Count < 4)
            {
                return "";
            }
            return Parser.ParseTokenAsString(tok[0]);
        }

        protected Property()
        { }

        public T As<T>() where T : Property
        {
            return this as T;
        }
    }
}
