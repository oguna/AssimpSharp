using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    public static class BinaryTokenizer
    {
        static void TokenizeError(string message, int offset = 0)
        {
            throw (new Exception(message));
        }

        static int Offset(int begin, int cursor)
        {
            Debug.Assert(begin <= cursor);
            return (cursor - begin);
        }

        static void TokenizeError(string message, int begin, int cursor)
        {
            TokenizeError(message, Offset(begin, cursor));
        }

        static uint ReadWord(byte[] input, ref int cursor, int end)
        {
            if (Offset(cursor, end) < 4)
            {
                TokenizeError("cannot ReadWord, out of bounds", 0, cursor);
            }
            uint word = BitConverter.ToUInt32(input, cursor);
            cursor += 4;
            return word;
        }

        static byte ReadByte(byte[] input, ref int cursor, int end)
        {
            if (Offset(cursor, end) < 1)
            {
                TokenizeError("cannot ReadByte, out of bounds", 0, cursor);
            }
            byte word = input[cursor];
            ++cursor;
            return word;
        }

        static int ReadString(out int sbeginOut, out int sendOut, byte[] input, ref int cursor, int end, bool longLength = false, bool allowNull = false)
        {
            int lenLen = longLength ? 4 : 1;
            if (Offset(cursor, end) < lenLen)
            {
                TokenizeError("cannot ReadString, out of bounds reading length", 0, cursor);
            }

            int length = longLength ? (int)ReadWord(input, ref cursor, end) : ReadByte(input, ref cursor, end);
            if (Offset(cursor, end) < length)
            {
                TokenizeError("cannot ReadString, length is out of bounds", 0, cursor);
            }

            sbeginOut = cursor;
            cursor += length;

            sendOut = cursor;

            if (!allowNull)
            {
                for(int i=0; i<length; i++)
                {
                    if (input[sbeginOut+i] == '\0')
                    {
                        TokenizeError("failed ReadString, unexpected NUL character in string", 0, cursor);
                    }
                }
            }

            return length;
        }

        static void ReadData(out int sbeginOut, out int sendOut, byte[] input, ref int cursor, int end)
        {
            if (Offset(cursor, end) < 1)
            {
                TokenizeError("cannot ReadData, out of bounds reading length", 0, cursor);
            }

            char type = (char)input[cursor];
            sbeginOut = cursor++;
            
            switch (type)
            {
                case 'Y':
                    cursor += 2;
                    break;
                case 'C':
                    cursor += 1;
                    break;
                case 'I':
                case 'F':
                    cursor += 4;
                    break;
                case 'D':
                    cursor += 8;
                    break;
                case 'L':
                    cursor += 8;
                    break;
                case 'R':
                    {
                        int length = (int)ReadWord(input, ref cursor, end);
                        cursor += length;
                        break;
                    }
                case 'b':
                    cursor = end;
                    break;
                case 'f':
                case 'd':
                case 'l':
                case 'i':
                    {
                        int length = (int)ReadWord(input, ref cursor, end);
                        int encoding = (int)ReadWord(input, ref cursor, end);
                        int compLen = (int)ReadWord(input, ref cursor, end);
                        if (encoding == 0)
                        {
                            int stride = 0;
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
                            Debug.Assert(stride > 0);
                            if (length * stride != compLen)
                            {
                                TokenizeError("cannot ReadData, calculated data stride differs from what the file claims", 0, cursor);
                            }
                        }
                        // zip/deflate algorithm (encoding==1)? take given length. anything else? die
                        else if (encoding != 1)
                        {
                            TokenizeError("cannot ReadData, unknown encoding", 0, cursor);
                        }
                        cursor += compLen;
                        break;
                    }
                case 'S':
                    {
                        int sb, se;
                        ReadString(out sb, out se, input, ref cursor, end, true, true);
                        break;
                    }
                default:
                    TokenizeError("cannot ReadData, unexpected type code: " + type, 0, cursor);
                    break;
            }

            if (cursor > end)
            {
                TokenizeError("cannot ReadData, the remaining size is too small for the data type: " + type, 0, cursor);
            }

            // the type code is contained in the returned range
            sendOut = cursor;
        }

        private static bool ReadScope(List<Token> outputTokens, byte[] input, ref int cursor, int end)
        {
            // the first word contains the offset at which this block ends
            uint endOffset = ReadWord(input, ref cursor, end);

            // we may get 0 if reading reached the end of the file -
            // fbx files have a mysterious extra footer which I don't know
            // how to extract any information from, but at least it always
            // starts with a 0.

            if (endOffset == 0)
            {
                return false;
            }

            if (endOffset > Offset(0, end))
            {
                TokenizeError("block offset is out of range", 0, cursor);
            }
            else if (endOffset < Offset(0, cursor))
            {
                TokenizeError("block offset is negative out of range", 0, cursor);
            }

            // the second data word contains the number of properties in the scope
            uint propCount = ReadWord(input, ref cursor, end);

            // the third data word contains the length of the property list
            uint propLength = ReadWord(input, ref cursor, end);

            // now comes the name of the scope/key
            int sbeg, send;
            ReadString(out sbeg, out send, input, ref cursor, end);

            outputTokens.Add(new Token(input, sbeg, send, TokenType.Key, (uint)Offset(0, cursor)));

            // now come the individual properties
            int beginCursor = cursor;
            for(int i=0; i<propCount; i++)
            {
                ReadData(out sbeg, out send, input, ref cursor, (int)(beginCursor + propLength));

                outputTokens.Add(new Token(input, sbeg, send, TokenType.Data, (uint)Offset(0, cursor)));

                if (i != propCount - 1)
                {
                    outputTokens.Add(new Token(input, cursor, cursor + 1, TokenType.Comma, (uint)Offset(0, cursor)));
                }
            }

            if (Offset(beginCursor, cursor) != propLength)
            {
                TokenizeError("property length not reached, something is wrong", 0, cursor);
            }

            // at the end of each nested block, there is a NUL record to indicate
            // that the sub-scope exists (i.e. to distinguish between P: and P : {})
            // this NUL record is 13 bytes long.
            const int BLOCK_SENTINEL_LENGTH = 13;

            if (Offset(0, cursor) < endOffset)
            {
                if (endOffset - Offset(0, cursor) < BLOCK_SENTINEL_LENGTH)
                {
                    TokenizeError("insufficient padding bytes at block end", 0, cursor);
                }

                outputTokens.Add(new Token(input, cursor, cursor + 1, TokenType.OpenBracket, (uint)Offset(0, cursor)));

                // XXX this is vulnerable to stack overflowing ..
                while (Offset(0, cursor) < endOffset - BLOCK_SENTINEL_LENGTH)
                {
                    ReadScope(outputTokens, input, ref cursor, (int)(endOffset - BLOCK_SENTINEL_LENGTH));
                }
                outputTokens.Add(new Token(input, cursor, cursor + 1, TokenType.CloseBracket, (uint)Offset(0, cursor)));

                for (int i = 0; i < BLOCK_SENTINEL_LENGTH; i++)
                {
                    if (input[cursor+i] != '\0')
                    {
                        TokenizeError("failed to read nested block sentinel, expected all bytes to be 0", 0, cursor);
                    }
                }
                cursor += BLOCK_SENTINEL_LENGTH;
            }

            if (Offset(0, cursor) != endOffset)
            {
                TokenizeError("scope length not reached, something is wrong", 0, cursor);
            }


            return true;

        }


        public static void TokenizeBinary(out List<Token> outputTokens, byte[] input, int length)
        {
            Debug.Assert(input != null);
            outputTokens = new List<Token>();

            if (length < 0x1b)
            {
                TokenizeError("file is too short", 0);
            }

            if (Encoding.Default.GetString(input, 0, 18) != "Kaydara FBX Binary")
            {
                TokenizeError("magic bytes not found", 0);
            }

            int cursor = 0x1b;

            while (cursor < length)
            {
                if (!ReadScope(outputTokens, input, ref cursor, length))
                {
                    break;
                }
            }
        }
    }
}
