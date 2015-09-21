using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// FBX data entity that consists of a 'scope', a collection
    /// of not necessarily unique #Element instances.
    /// </summary>
    public class Scope
    {
        private Dictionary<string, List<Element>> elements = new Dictionary<string, List<Element>>();
        public Scope(Parser parser, bool topLevel = false)
        {
            if (!topLevel)
            {
                Token t = parser.CurrentToken;
                if (t.Type != TokenType.OpenBracket)
                {
                    throw (new Exception("expected open bracket"));
                }
            }

            Token n = parser.AdvanceToNextToken();
            if (n == null)
            {
                throw( new Exception("unexpected end of file"));
            }
            while ( n.Type != TokenType.CloseBracket)
            {
                if (n.Type != TokenType.Key)
                {
                    throw (new Exception("unexpected token, expected TOK_KEY"));
                }

                string str = n.StringContents;
                if (!elements.ContainsKey(str))
                {
                    elements.Add(str, new List<Element>());
                }
                elements[str].Add(new Element(n, parser));

                n = parser.CurrentToken;
                if (n == null)
                {
                    if (topLevel)
                    {
                        return;
                    }
                    throw (new Exception("unexpected end of file"));
                }
            }

        }
        public Element this[string index]
        {
            get
            {
                List<Element> elem;
                if (elements.TryGetValue(index, out elem) && elem.Count > 0)
                {
                    return elem[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public List<Element> GetCollection(string index)
        {
            List<Element> result;
            elements.TryGetValue(index, out result);
            return result;
        }

        public Dictionary<string, List<Element>> Elements
        {
            get
            {
                return elements;
            }
        }
    }
}
