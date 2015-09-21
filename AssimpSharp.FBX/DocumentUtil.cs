using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    public static class DocumentUtil
    {
        public static void DOMWarning(string message, Token token)
        {
            throw (new Exception());
        }

        public static void DOMWarning(string message, Element element = null)
        {
            throw (new Exception());
        }

        public static void DOMError(string message, Token token)
        {
            throw (new Exception());
        }

        public static void DOMError(string message, Element element = null)
        {
            throw (new Exception());
        }

        public static PropertyTable GetPropertyTable(Document doc, string templateName, Element element, Scope sc, bool noWarn = false)
        {
            Element properties70 = sc["Properties70"];
            PropertyTable templateProps = new PropertyTable();
            if (templateName.Length > 0)
            {
                PropertyTable it;
                if (doc.Templates().TryGetValue(templateName, out it))
                {
                    templateProps = it;
                }
            }
            if (properties70 == null)
            {
                if (!noWarn)
                {
                    Console.Error.WriteLine("property table (Properties70) not found");
                }
                if (templateProps != null)
                {
                    return templateProps;
                }
                else
                {
                    return new PropertyTable();
                }
            }
            return new PropertyTable(properties70, templateProps);
        }

        public static T ProcessSimpleConnection<T>(Connection con, bool isObjectPropertyConn, string name, Element element, string propNameOut = null)
            where T : Object
        {
            if (isObjectPropertyConn & string.IsNullOrEmpty(con.PropertyName))
            {
                throw (new Exception());
            }
            else if (!isObjectPropertyConn && !string.IsNullOrEmpty(con.PropertyName))
            {
                throw (new Exception());
            }

            if (isObjectPropertyConn && !string.IsNullOrEmpty(propNameOut))
            {
                propNameOut = con.PropertyName;
            }
            Object ob = con.SourceObject;
            if (ob == null)
            {
                throw (new Exception());
            }
            return (T)ob;
        }
    }
}
