using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// FBX data entity that consists of a key:value tuple.
    /// </summary>
    /// <example>
    /// <code>
    /// AnimationCurve: 23, "AnimCurve::", "" {
    /// [..]
    /// }
    /// </code>
    /// </example>
    public class Element
    {
        private Token keyToken;
        private List<Token> tokens = new List<Token>();
        private Scope compound;

        public Element(Token keyToken, Parser parser)
        {
            this.keyToken = keyToken;

            Token n;
            do
            {
                n = parser.AdvanceToNextToken();
                if (n == null)
                {
                    throw (new Exception("unexpected end of file, expected closing bracket"));
                }
                if (n.Type == TokenType.Data)
                {
                    tokens.Add(n);
                    n = parser.AdvanceToNextToken();
                    if (n == null)
                    {
                        throw (new Exception("unexpected end of file, expected bracket, comma or key"));
                    }
                    TokenType ty = n.Type;
                    if (ty != TokenType.OpenBracket && ty != TokenType.CloseBracket && ty != TokenType.Comma && ty != TokenType.Key)
                    {
                        throw (new Exception("unexpected token; expected bracket, comma or key"));
                    }
                }
                if (n.Type == TokenType.OpenBracket)
                {
                    compound = new Scope(parser);

                    // current token should be a TOK_CLOSE_BRACKET
                    n = parser.CurrentToken;
                    Debug.Assert(n != null);

                    if (n.Type != TokenType.CloseBracket)
                    {
                        throw (new Exception("expected closing bracket"));
                    }
                    parser.AdvanceToNextToken();
                    return;
                }
            }
            while (n.Type != TokenType.Key && n.Type != TokenType.CloseBracket);
        }

        public Scope Compound
        {
            get
            {
                return compound;
            }
        }

        public Token KeyToken
        {
            get
            {
                return keyToken;
            }
        }

        public List<Token> Tokens
        {
            get
            {
                return tokens;
            }
        }
    }
}
