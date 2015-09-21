using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// Represents a property table as can be found in the newer FBX files (Properties60, Properties70)
    /// </summary>
    public  class PropertyTable
    {

        private Dictionary<string, Element> lazyProps = new Dictionary<string, Element>();
        private Dictionary<string, Property> props = new Dictionary<string, Property>();
        private PropertyTable templateProps;
        private Element element;

        public Property this[string name]
        {
            get
            {
                Property it;
                if (!props.TryGetValue(name, out it))
                {
                    // hasn't been parsed yet?
                    Element lit;
                    if (lazyProps.TryGetValue(name, out lit))
                    {
                        props[name] = Property.ReadTypedProperty(lit);
                        it = props[name];
                        Debug.Assert(it != null);
                    }
                    if (it == null)
                    {
                        if (templateProps != null)
                        {
                            return templateProps[name];
                        }
                        return null;
                    }
                }
                return it;
            }
        }

        public Element Element
        {
            get
            {
                return element;
            }
        }

        public PropertyTable TemplateProps
        {
            get
            {
                return templateProps;
            }
        }

        public Dictionary<string, Property> GetUnparsedProperties()
        {
            var result = new Dictionary<string, Property>();

            // Loop through all the lazy properties (which is all the properties)
            foreach(var element in lazyProps)
            {
                // Skip parsed properties
                if (props.ContainsKey(element.Key))
                {
                    continue;
                }

                var prop = Property.ReadTypedProperty(element.Value);
                if (prop == null)
                {
                    continue;
                }

                result[element.Key] = prop;
            }
            return result;
        }


        public static string PeekPropertyName(Element element)
        {
            List<Token> tok = element.Tokens;
            if (tok.Count < 4)
            {
                return "";
            }
            return Parser.ParseTokenAsString(tok[0]); 
        }

        public PropertyTable()
        {
            this.templateProps = null;
            this.element = null;
        }

        public PropertyTable(Element element, PropertyTable templateProps)
        {
            this.templateProps = templateProps;
            this.element = element;

            Scope scope = Parser.GetRequiredScope(element);
            foreach(var i in scope.Elements)
            {
                if (i.Key != "P")
                {
                    Console.Error.WriteLine("expected only P elements in property table");
                    continue;
                }
                foreach (var j in i.Value)
                {
                    string name = PeekPropertyName(j);
                    if (name.Length == 0)
                    {
                        Console.Error.WriteLine("could not read property name");
                        continue;
                    }
                    if (lazyProps.ContainsKey(name))
                    {
                        Console.Error.WriteLine("duplicate property name, will hide previous value: " + name);
                        continue;
                    }
                    lazyProps[name] = j;
                }
            }
        }
    }
}
