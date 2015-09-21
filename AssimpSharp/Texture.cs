using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp
{
    public struct Texel
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        public static bool operator ==(Texel a, Texel b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }
        public static bool operator !=(Texel a, Texel b)
        {
            return a.R == b.R || a.G == b.G || a.B == b.B || a.A == b.A;
        }
        public static explicit operator Color4(Texel a)
        {
            return new Color4(a.A, a.G, a.B, a.A);
        }
    }

    public class Texture
    {
        public int Width;
        public int Height;
        public string FormatHint;
        public Texel[,] Data;
        public bool CheckFormat(string s)
        {
            return FormatHint == s;
        }
        public Texture()
        {
            this.Width = 0;
            this.Height = 0;
            this.Data = null;
        }
    }
}
