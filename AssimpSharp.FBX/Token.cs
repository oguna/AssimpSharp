using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public class Token
    {
        private const uint BinaryMarker = uint.MaxValue;

        public Token(byte[] input, int begin, int end, TokenType type, uint line, uint column)
        {
            this.input = input;
            this.begin = begin;
            this.end = end;
            this.type = type;
            this.line = line;
            this.offset = 0;
            this.column = column;
            Debug.Assert(begin > 0);
            Debug.Assert(end > 0);
            Debug.Assert(end >= begin);
        }

        public Token(byte[] input, int begin, int end, TokenType type, uint offset)
        {
            this.input = input;
            this.begin = begin;
            this.end = end;
            this.type = type;
            this.offset = offset;
            this.column = BinaryMarker;
            this.line = 0;
            Debug.Assert(begin > 0);
            Debug.Assert(end > 0);
            Debug.Assert(end >= begin);
        }


        public string StringContents
        {
            get
            {
                return Encoding.Default.GetString(input, begin, end - begin);
            }
        }

        public byte[] BinaryContents
        {
            get
            {
                var bytes = new byte[end - begin];
                Array.Copy(input, begin, bytes, 0, end - begin);
                return bytes;
            }
        }

        public bool IsBinary
        {
            get
            {
                return column == BinaryMarker;
            }
        }

        public int Begin
        {
            get
            {
                return begin;
            }
        }

        public int End
        {
            get
            {
                return end;
            }
        }

        public TokenType Type
        {
            get
            {
                return type;
            }
        }

        public uint Offset
        {
            get
            {
                Debug.Assert(IsBinary);
                return offset;
            }
        }

        public uint Line
        {
            get
            {
                Debug.Assert(!IsBinary);
                return line;
            }
        }

        public uint Column
        {
            get
           {
                Debug.Assert(!IsBinary);
                return column;
            }
        }
        
        private int begin;
        private int end;
        private byte[] input;
        private TokenType type;
        private uint line;
        private uint offset;
        private uint column;
    }
}
