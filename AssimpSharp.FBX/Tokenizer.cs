using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public static class Tokenizer
    {
        private static void TokenizeError(string message, int line, int column)
        {
            throw new Exception("FBX-Tokenize");
        }

        private static void ProcessDataToken(List<Token> outputTokens, byte[] input, ref int start, ref int end, int line, int column, TokenType type = TokenType.Data, bool mustHaveToken = false)
        {
            if (start >= 0 && end >= 0)
            {
                bool inDoubleQuotes = false;
                for (int i = start; i != end + 1; i++)
                {
                    char c = (char)input[i];
                    if (c == '\"')
                    {
                        inDoubleQuotes = !inDoubleQuotes;
                    }
                    if (!inDoubleQuotes && (char.IsWhiteSpace(c) || c == '\n'))
                    {
                        TokenizeError("unexpected whitespace in token", line, column);
                    }
                }
                if (inDoubleQuotes)
                {
                    TokenizeError("non-terminated double quotes", line, column);
                }
                outputTokens.Add(new Token(input, start, end + 1, type, (uint)line, (uint)column));
            }
            else if (mustHaveToken)
            {
                TokenizeError("unexpected character, expected data token", line, column);
            }
            start = end = -1;
        }

        public static void Tokenize(out List<Token> outputTokens, byte[] input)
        {
            Debug.Assert(input != null);
            outputTokens = new List<Token>();

            // line and column numbers numbers are one-based
            int line = 1;
            int column = 1;

            bool comment = false;
            bool inDoubleQuotes = false;
            bool pendingDataToken = false;

            int tokenBegin = -1;
            int tokenEnd = -1;
            for (int cur = 0; input.Length > cur; column += (input[cur] == '\t' ? 1 : 1), cur++)
            {
                char c = (char)input[cur];
                if (c == '\n')
                {
                    comment = false;
                    column = 0;
                    line++;
                }

                if (comment)
                {
                    continue;
                }

                if (inDoubleQuotes)
                {
                    if (c == '\"')
                    {
                        inDoubleQuotes = false;
                        tokenEnd = cur;
                        ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column);
                        pendingDataToken = false;
                    }
                    continue;
                }

                switch (c)
                {
                    case '\"':
                        if (tokenBegin >= 0)
                        {
                            throw (new TokenizeException("unexpected double-quote", (uint)line, (uint)column));
                        }
                        tokenBegin = cur;
                        inDoubleQuotes = true;
                        continue;
                    case ';':
                        ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column);
                        comment = true;
                        continue;
                    case '{':
                        ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column);
                        outputTokens.Add(new Token(input, cur, cur + 1, TokenType.OpenBracket, (uint)line, (uint)column));
                        continue;
                    case '}':
                        ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column);
                        outputTokens.Add(new Token(input, cur, cur + 1, TokenType.CloseBracket, (uint)line, (uint)column));
                        continue;
                    case ',':
                        if (pendingDataToken)
                        {
                            ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column, TokenType.Data, true);
                        }
                        outputTokens.Add(new Token(input, cur, cur + 1, TokenType.Comma, (uint)line, (uint)column));
                        continue;
                    case ':':
                        if (pendingDataToken)
                        {
                            ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column, TokenType.Key, true);
                        }
                        else
                        {
                            throw (new TokenizeException("unexpected colon", (uint)line, (uint)column));
                        }
                        continue;
                }

                if (char.IsWhiteSpace(c) || c == '\n')
                {
                    if (tokenBegin >= 0)
                    {
                        // peek ahead and check if the next token is a colon in which
                        // case this counts as KEY token.
                        TokenType type = TokenType.Data;
                        for (int peek = cur; input.Length > peek && (char.IsWhiteSpace((char)input[peek]) || input[peek] == '\n'); ++peek)
                        {
                            if (input[peek] == ':')
                            {
                                type = TokenType.Key;
                                cur = peek;
                                break;
                            }
                        }
                        ProcessDataToken(outputTokens, input, ref tokenBegin, ref tokenEnd, line, column, type);
                    }
                    pendingDataToken = false;
                }
                else
                {
                    tokenEnd = cur;
                    if (tokenBegin < 0)
                    {
                        tokenBegin = cur;
                    }
                    pendingDataToken = true;
                }
            }

        }
    }
}
