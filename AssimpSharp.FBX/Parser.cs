using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using SharpDX;

namespace AssimpSharp.FBX
{
    public class Parser
    {
        public Parser(List<Token> tokens, bool isBinary)
        {
            this.tokens = tokens;
            last = -1;
            current = -1;
            cursor = -1;
            this.isBinary = isBinary;
            root = new Scope(this, true);
        }

        public Scope RootScope
        {
            get
            {
                return root;
            }
        }

        public bool IsBinary { get { return isBinary; } }

        public Token AdvanceToNextToken()
        {
            last = current;
            if (cursor == tokens.Count)
            {
                current = -1;
            }
            else
            {
                current = ++cursor;
            }
            return CurrentToken;
        }

        public int LastToken
        {
            get
            {
                return last;
            }

        }

        public Token CurrentToken
        {
            get
            {
                if (tokens.Count > current)
                {
                    return tokens[current];
                }
                else
                {
                    return null;
                }
            }
        }

        private List<Token> tokens;
        private int last, current, cursor;
        private Scope root;
        private bool isBinary;

        #region token parsing - this happens when building the DOM out of the parse-tree

        public static ulong ParseTokenAsID(Token t, out string errOut)
        {
            errOut = null;
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return 0;
            }
            if (t.IsBinary)
            {
                var data = t.BinaryContents;
                if (data[0] != 'L')
                {
                    errOut = "failed to parse ID, unexpected data type, expected L(ong) (binary)";
                    return 0L;
                }
                ulong id = BitConverter.ToUInt64(data, 1);
                return id;
            }
            int length = t.End - t.Begin;
            Debug.Assert(length > 0);
            ulong result;
            if (ulong.TryParse(t.StringContents, out result))
            {
                return result;
            }
            else
            {
                errOut = "failed to parse ID (text)";
                return 0;
            }
        }
        public static ulong ParseTokenAsDim(Token t, out string errOut)
        {
            errOut = null;
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return 0;
            }
            if (t.IsBinary)
            {
                var data = t.BinaryContents;
                if (data[0] != 'L')
                {
                    errOut = "failed to parse ID, unexpected data type, expected L(ong) (binary)";
                    return 0L;
                }
                ulong id = BitConverter.ToUInt64(data, 1);
                return id;
            }
            if (t.StringContents[0] != '*')
            {
                errOut = "expected asterisk before array dimension";
                return 0;
            }
            uint length = (uint)t.End - (uint)t.Begin;
            if (length == 0)
            {
                errOut = "expected valid integer number after asterisk";
                return 0;
            }
            return ulong.Parse(t.StringContents.Substring(1));

        }
        public static float ParseTokenAsFloat(Token t, out string errOut)
        {
            errOut = "";
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return 0.0f;
            }

            if (t.IsBinary)
            {
                var data = t.BinaryContents;
                if (data[0] != 'F' && data[0] != 'D')
                {
                    errOut = "failed to parse F(loat) or D(ouble), unexpected data type (binary)";
                    return 0.0f;
                }

                if (data[0] == 'F')
                {
                    return BitConverter.ToSingle(data, 1);
                }
                else
                {
                    return (float)BitConverter.ToDouble(data, 1);
                }
            }
            return float.Parse(t.StringContents);
        }

        public static int ParseTokenAsInt(Token t, out string errOut)
        {
            errOut = null;
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return 0;
            }
            if (t.IsBinary)
            {
                byte[] data = t.BinaryContents;
                if (data[0] != 'I')
                {
                    errOut = "failed to parse I(nt), unexpected data type (binary)";
                    return 0;
                }
                int ival = BitConverter.ToInt32(data, 1);
                return ival;
            }

            Debug.Assert(t.End - t.Begin > 0);

            return int.Parse(t.StringContents);
        }

        public static long ParseTokenAsInt64(Token t, out string errOut)
        {
            errOut = null;
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return 0;
            }
            if (t.IsBinary)
            {
                byte[] data = t.BinaryContents;
                if (data[0] != 'L')
                {
                    errOut = "failed to parse Int64, unexpected data type";
                    return 0;
                }
                long id = BitConverter.ToInt64(data, 1);
                return id;
            }

            Debug.Assert(t.End - t.Begin > 0);

            return long.Parse(t.StringContents);
        }

        public static string ParseTokenAsString(Token t, out string errOut)
        {
            errOut = null;
            if (t.Type != TokenType.Data)
            {
                errOut = "expected TOK_DATA token";
                return "";
            }
            if (t.IsBinary)
            {
                var data = t.BinaryContents;
                if (data[0] != 'S')
                {
                    errOut = "failed to parse S(tring), unexpected data type (binary)";
                    return "";
                }

                // read string length
                int len = BitConverter.ToInt32(data, 1);

                Debug.Assert(t.End - t.Begin == 5 + len);
                return Encoding.Default.GetString(data, 5, len);
            }

            var length = t.End - t.Begin;
            if (length < 2)
            {
                errOut = "token is too short to hold a string";
                return "";
            }

            var s = t.BinaryContents;
            if (s[0] != '\"' || s[s.Length-1] != '\"')
            {
                errOut = "expected double quoted string";
                return "";
            }
            return Encoding.Default.GetString(s, 1, length - 2);
        }
        #endregion

        /// <summary>
        /// read the type code and element count of a binary data array and stop there
        /// </summary>
        private static void ReadBinaryDataArrayHead(byte[] data, ref int cursor, int end, out char type, out uint count, Element el)
        {
            if (end - cursor < 5)
            {
                throw (new ParseException("binary data array is too short, need five (5) bytes for type signature and element count", el));
            }
            type = (char)data[cursor];
            uint len = BitConverter.ToUInt32(data, cursor + 1);
            count = len;
            cursor += 5;
        }

        /// <summary>
        /// read binary data array, assume cursor points to the 'compression mode' field (i.e. behind the header)
        /// </summary>
        private static void ReadBinaryDataArray(char type, uint count, byte[] data, ref int cursor, int end, out byte[] buff, Element el)
        {
            uint encmode = BitConverter.ToUInt32(data, cursor);
            cursor += 4;

            // next comes the compressed length
            uint compLen = BitConverter.ToUInt32(data, cursor);
            cursor += 4;

            Debug.Assert(cursor + compLen == end);

            // determine the length of the uncompressed data by looking at the type signature
            uint stride = 3;
            switch (type)
            {
                case 'f':
                case 'i':
                    stride = 4;
                    break;
                case 'd':
                case 'l':
                    stride = 8;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            uint fullLength = stride * count;
            buff = new byte[fullLength];

            if (encmode == 0)
            {
                Debug.Assert(fullLength == compLen);

                // plain data, no compression
                Array.Copy(data, cursor, buff, 0, fullLength);
            }
            else if (encmode == 1)
            {
                // zlib/deflate, next comes ZIP head (0x78 0x01)
                // see http://www.ietf.org/rfc/rfc1950.txt

                var stream = new MemoryStream(data, cursor, data.Length);
                var zstream = new DeflateStream(stream, CompressionMode.Decompress);
                try {
                    int ret = zstream.Read(buff, 0, buff.Length);
                }
                    catch (Exception e)
                {
                    throw (new ParseException("failure decompressing compressed data section"));
                }
            }
            else
            {
                Debug.Assert(false);
            }

            cursor += (int)compLen;
            Debug.Assert(cursor == end);
        }

        #region wrapper around ParseTokenAsXXX() with DOMError handling

        public static UInt64 ParseTokenAsID(Token t)
        {
            string err;
            ulong i = ParseTokenAsID(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }
        public static ulong ParseTokenAsDim(Token t)
        {
            string err;
            ulong i = ParseTokenAsDim(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }
        public static float ParseTokenAsFloat(Token t)
        {
            string err;
            float i = ParseTokenAsFloat(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }
        public static int ParseTokenAsInt(Token t)
        {
            string err;
            int i = ParseTokenAsInt(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }
        public static long ParseTokenAsInt64(Token t)
        {
            string err;
            long i = ParseTokenAsInt64(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }

        /// <summary>
        /// wrapper around ParseTokenAsString() with ParseError handling
        /// </summary>
        public static string ParseTokenAsString(Token t)
        {
            string err;
            string i = ParseTokenAsString(t, out err);
            if (!string.IsNullOrEmpty(err))
            {
                throw (new ParseException(err, t));
            }
            return i;
        }
        #endregion

        #region read data arrays

        public static void ParseVectorDataArray(out List<Vector4> result, Element el)
        {
            result = new List<Vector4>();
            List<Token> tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                var data = tok[0].BinaryContents;
                var end = tok[0].End;
                char type;
                uint count;
                int cursor = 0;
                ReadBinaryDataArrayHead(data, ref cursor, end, out type, out count, el);

                if (count % 4 != 0)
                {
                    throw (new ParseException("number of floats is not a multiple of four (4) (binary)", el));
                }

                if (count == 0)
                {
                    return;
                }

                if (type != 'd' && type != 'f')
                {
                    throw (new ParseException("expected float or double array (binary)", el));
                }

                byte[] buff;
                ReadBinaryDataArray(type, count, data, ref cursor, end, out buff, el);

                Debug.Assert(cursor == end);
                Debug.Assert(buff.Length == count * (type == 'd' ? 8 : 4));

                uint count4 = count / 4;
                result = new List<Vector4>((int)count4);

                if (type == 'd')
                {
                    int d = 0;
                    for (int i = 0; i < count4; i++, d += 4 * 8)
                    {
                        result.Add(new Vector4(
                            (float)BitConverter.ToDouble(buff, 0 + d),
                            (float)BitConverter.ToDouble(buff, 8 + d),
                            (float)BitConverter.ToDouble(buff, 16 + d),
                            (float)BitConverter.ToDouble(buff, 20 + d)));
                    }
                }
                else if (type == 'f')
                {
                    int f = 0;
                    for (int i = 0; i < count4; i++, f += 4 * 4)
                    {
                        result.Add(new Vector4(
                            BitConverter.ToSingle(buff, 0 + f),
                            BitConverter.ToSingle(buff, 4 + f),
                            BitConverter.ToSingle(buff, 8 + f),
                            BitConverter.ToSingle(buff, 12 + f)));
                    }
                }
                return;
            }

            ulong dim = Parser.ParseTokenAsDim(tok[0]);
            result = new List<Vector4>((int)dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            if (a.Tokens.Count % 4 != 0)
            {
                throw (new ParseException("number of floats is not a multiple of four (4)", el));
            }
            for (int i = 0; i < a.Tokens.Count;)
            {
                Vector4 v = new Vector4();
                v.X = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.Y = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.Z = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.W = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                result.Add(v);
            }
        }

        /// <summary>
        /// read an array of float3 tuples
        /// </summary>
        public static void ParseVectorDataArray(out List<Vector3> result, Element el)
        {
            result = new List<Vector3>();
            List<Token> tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                var data = tok[0].BinaryContents;
                var end = tok[0].End;
                char type;
                uint count;
                int cursor = 0;
                ReadBinaryDataArrayHead(data,ref cursor , end, out type, out count, el);

                if (count % 4 != 0)
                {
                    throw (new ParseException("number of floats is not a multiple of four (4) (binary)", el));
                }

                if (count == 0)
                {
                    return;
                }

                if (type != 'd' && type != 'f')
                {
                    throw (new ParseException("expected float or double array (binary)", el));
                }

                byte[] buff;
                ReadBinaryDataArray(type, count, data, ref cursor, end, out buff, el);

                Debug.Assert(cursor == end);
                Debug.Assert(buff.Length == count * (type == 'd' ? 8 : 4));

                uint count3 = count / 3;
                result = new List<Vector3>((int)count3);

                if (type == 'd')
                {
                    int d = 0;
                    for(int i=0; i<count3; i++, d+=3*8)
                    {
                        result.Add(new Vector3((float)BitConverter.ToDouble(buff, 0+d), (float)BitConverter.ToDouble(buff, 8 + d), (float)BitConverter.ToDouble(buff, 16 + d)));
                    }
                }
                else if (type == 'f')
                {
                    int f = 0;
                    for (int i = 0; i < count3; i++, f += 3 * 4)
                    {
                        result.Add(new Vector3(BitConverter.ToSingle(buff, 0 + f), BitConverter.ToSingle(buff,4 + f), BitConverter.ToSingle(buff, 8 + f)));
                    }
                }
                return;
            }

            var dim = Parser.ParseTokenAsDim(tok[0]);
            result = new List<Vector3>((int)dim);
            var scope = Parser.GetRequiredScope(el);
            var a = Parser.GetRequiredElement(scope, "a", el);
            if (a.Tokens.Count % 3 != 0)
            {
                throw (new ParseException("number of floats is not a multiple of three (3)", el));
            }
            for (int i = 0; i < a.Tokens.Count;)
            {
                var v = new Vector3();
                v.X = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.Y = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.Z = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                result.Add(v);
            }
        }

        /// <summary>
        /// read an array of float2 tuples
        /// </summary>
        public static void ParseVectorDataArray(out List<Vector2> result, Element el)
        {
            result = new List<Vector2>();
            List<Token> tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                var data = tok[0].BinaryContents;
                var end = tok[0].End;
                char type;
                uint count;
                int cursor = 0;
                ReadBinaryDataArrayHead(data, ref cursor, end, out type, out count, el);

                if (count % 2 != 0)
                {
                    throw (new ParseException("number of floats is not a multiple of two (2) (binary)", el));
                }

                if (count == 0)
                {
                    return;
                }

                if (type != 'd' && type != 'f')
                {
                    throw (new ParseException("expected float or double array (binary)", el));
                }

                byte[] buff;
                ReadBinaryDataArray(type, count, data, ref cursor, end, out buff, el);

                Debug.Assert(cursor == end);
                Debug.Assert(buff.Length == count * (type == 'd' ? 8 : 4));

                uint count2 = count / 2;
                result = new List<Vector2>((int)count2);

                if (type == 'd')
                {
                    int d = 0;
                    for (int i = 0; i < count2; i++, d += 2 * 8)
                    {
                        result.Add(new Vector2((float)BitConverter.ToDouble(buff, 0 + d), (float)BitConverter.ToDouble(buff, 8 + d)));
                    }
                }
                else if (type == 'f')
                {
                    int f = 0;
                    for (int i = 0; i < count2; i++, f += 2 * 4)
                    {
                        result.Add(new Vector2(BitConverter.ToSingle(buff, 0 + f), BitConverter.ToSingle(buff, 4 + f)));
                    }
                }
                return;
            }

            ulong dim = Parser.ParseTokenAsDim(tok[0]);
            result = new List<Vector2>((int)dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            if (a.Tokens.Count % 2 != 0)
            {
                throw (new ParseException("number of floats is not a multiple of two (2)", el));
            }
            for (int i = 0; i < a.Tokens.Count;)
            {
                Vector2 v = new Vector2();
                v.X = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                v.Y = Parser.ParseTokenAsFloat(a.Tokens[i++]);
                result.Add(v);
            }
        }


        /// <summary>
        /// read an array of ints
        /// </summary>
        public static void ParseVectorDataArray(out List<int> result, Element el)
        {
            var tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                throw (new NotImplementedException());
            }
            int dim = (int)Parser.ParseTokenAsDim(tok[0]);

            result = new List<int>(dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            foreach (var it in a.Tokens)
            {
                int ival = Parser.ParseTokenAsInt(it);
                result.Add(ival);
            }
        }

        public static void ParseVectorDataArray(out List<float> result, Element el)
        {
            var tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                throw (new NotImplementedException());
            }
            int dim = (int)Parser.ParseTokenAsDim(tok[0]);

            result = new List<float>(dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            foreach (var it in a.Tokens)
            {
                float ival = Parser.ParseTokenAsFloat(it);
                result.Add(ival);
            }
        }

        public static void ParseVectorDataArray(out List<uint> result, Element el)
        {
            var tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                throw (new NotImplementedException());
            }
            int dim = (int)Parser.ParseTokenAsDim(tok[0]);

            result = new List<uint>(dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            foreach (var it in a.Tokens)
            {
                int ival = Parser.ParseTokenAsInt(it);
                if (ival < 0)
                {
                    throw (new ParseException("encountered negative integer index"));
                }
                result.Add((uint)ival);
            }
        }

        public static void ParseVectorDataArray(out List<ulong> result, Element el)
        {
            var tok = el.Tokens;
            if (tok.Count == 0)
            {
                throw (new ParseException("unexpected empty element", el));
            }
            if (tok[0].IsBinary)
            {
                throw (new NotImplementedException());
            }
            int dim = (int)Parser.ParseTokenAsDim(tok[0]);

            result = new List<ulong>(dim);
            Scope scope = Parser.GetRequiredScope(el);
            Element a = Parser.GetRequiredElement(scope, "a", el);
            foreach (var it in a.Tokens)
            {
                ulong ival = Parser.ParseTokenAsID(it);
                result.Add(ival);
            }
        }

        #endregion

        public static Element GetRequiredElement(Scope sc, string index, Element element = null)
        {
            Element el = sc[index];
            if (el == null)
            {
                throw (new Exception());
            }
            return el;
        }

        public static Scope GetRequiredScope(Element el)
        {
            Scope s = el.Compound;
            if (s == null)
            {
                throw (new Exception("expected compound scope"));
            }
            return s;
        }

        public static Token GetRequiredToken(Element el, int index)
        {
            List<Token> t = el.Tokens;
            if (index >= t.Count)
            {
                throw (new Exception("missing token at index "));
            }
            return t[index];
        }

        public static Matrix ReadMatrix(Element element)
        {
            List<float> values;
            Parser.ParseVectorDataArray(out values, element);
            if (values.Count != 16)
            {
                throw (new Exception("expected 16 matrix elements"));
            }
            Matrix result = new Matrix(values.ToArray());
            result.Transpose();
            return result;
        }
    }
}
