using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public static class Util
    {
        private static string TokenTypeString(TokenType t)
        {
            switch (t)
            {
                case TokenType.OpenBracket:
                    return "TOK_OPEN_BRACKET";

                case TokenType.CloseBracket:
                    return "TOK_CLOSE_BRACKET";

                case TokenType.Data:
                    return "TOK_DATA";

                case TokenType.Comma:
                    return "TOK_COMMA";

                case TokenType.Key:
                    return "TOK_KEY";

                case TokenType.BinaryData:
                    return "TOK_BINARY_DATA";
            }
            Debug.Assert(false);
            return "";
        }

        public static string AddLineAndColumn(string prefix, string text, uint line, uint column)
        {
            return string.Format(prefix + " (line " + line + ", col " + column + ") " + text);
        }

        public static string AddTokenText(string prefix, string text, Token tok)
        {
            if (tok.IsBinary)
            {
                return string.Format("{0} ({1}, offset 0x{2}) {3}", prefix, TokenTypeString(tok.Type), tok.Offset.ToString("X"), text);
            }
            return string.Format("{0} ({1}, line {2}, col {3} ) {4}", prefix, TokenTypeString(tok.Type), tok.Line, tok.Column, text);
        }
    }
}
