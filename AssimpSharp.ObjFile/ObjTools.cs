using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AssimpSharp.ObjFile
{
    public static class ObjTools
    {
        public static bool IsEndOfBuffer(TextReader stream)
        {
            return stream.Peek() == -1;
        }

        public static bool IsSeparator(char token)
        {
            return (token == ' ' || token == '\n' || token == '\f' || token == '\r' || token == '\t');
        }

        public static bool IsNewLine(char token)
        {
            return token == '\n' || token == '\f' || token == '\r';
        }

        public static void GetNextWord(TextReader reader)
        {
            while (!IsEndOfBuffer(reader))
            {
                if (!IsSeparator((char)reader.Peek()) || IsNewLine((char)reader.Peek()))
                {
                    break;
                }
                reader.Read();
            }
        }

        public static void SkipLine(TextReader sr, ref uint line)
        {
            while (!IsEndOfBuffer(sr) && !IsNewLine((char)sr.Peek()))
            {
                sr.Read();
            }
            if (sr.Peek() != -1)
            {
                sr.Read();
                line++;
            }
            while ((sr.Peek() != -1) && (sr.Peek() == '\t' || sr.Peek() == ' '))
            {
                sr.Read();
            }
        }

        public static string CopyNextWord(TextReader sr)
        {
            StringBuilder sb = new StringBuilder();
            GetNextWord(sr);
            while (!IsSeparator((char)sr.Peek()) && !IsEndOfBuffer(sr))
            {
                sb.Append((char)sr.Read());
                if (sr.Peek() == -1)
                {
                    break;
                }
            }
            return sb.ToString();
        }

        public static void GetFloat(StreamReader sr, out float value)
        {
            string buffer = CopyNextWord(sr);
            value = float.Parse(buffer);
        }
    }
}
