using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using NUnit.Framework;
using SharpDX;

namespace Test
{
    public static class MathAssert
    {


        public static void AreEqual(Assimp.Color4D a, SharpDX.Color4 b)
        {
            Assert.AreEqual(a.A, b.Alpha);
            Assert.AreEqual(a.R, b.Red);
            Assert.AreEqual(a.G, b.Green);
            Assert.AreEqual(a.B, b.Blue);
        }

        public static void AreEqual(Assimp.Vector2D a, SharpDX.Vector2 b)
        {
            Assert.AreEqual(a.X, b.X);
            Assert.AreEqual(a.Y, b.Y);
        }

        public static void AreEqual(Assimp.Vector3D a, SharpDX.Vector3 b)
        {
            AreNearEqual(a.X, b.X);
            AreNearEqual(a.Y, b.Y);
            AreNearEqual(a.Z, b.Z);
        }

        public static void AreEqual(Assimp.Vector3D[] a, SharpDX.Vector3[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                AreEqual(a[i], b[i]);
            }
        }

        public static void AreNearEqual(float a, float b)
        {
            if (!MathUtil.NearEqual(a, b))
            {
                throw (new AssertionException(string.Format("expected: {0}, but {1}", a, b)));
            }
        }

        public static void AreEqual(Assimp.Matrix4x4 a, SharpDX.Matrix b)
        {
            Vector3D transA;
            Vector3 transB;
            Vector3D scaleA;
            Vector3 scaleB;
            Assimp.Quaternion rotA;
            SharpDX.Quaternion rotB;
            a.Decompose(out scaleA, out rotA, out transA);
            b.Decompose(out scaleB, out rotB, out transB);
            AreEqual(transA, transB);
            AreEqual(rotA, rotB);
            AreEqual(scaleA, scaleB);
            //AreNearEqual(a.A1, b.M11);
            //AreNearEqual(a.A2, b.M12);
            //AreNearEqual(a.A3, b.M13);
            //AreNearEqual(a.A4, b.M14);
            //AreNearEqual(a.B1, b.M21);
            //AreNearEqual(a.B2, b.M22);
            //AreNearEqual(a.B3, b.M23);
            //AreNearEqual(a.B4, b.M24);
            //AreNearEqual(a.C1, b.M31);
            //AreNearEqual(a.C2, b.M32);
            //AreNearEqual(a.C3, b.M33);
            //AreNearEqual(a.C4, b.M34);
            //AreNearEqual(a.D1, b.M41);
            //AreNearEqual(a.D2, b.M42);
            //AreNearEqual(a.D3, b.M43);
            //AreNearEqual(a.D4, b.M44);
        }

        public static void AreEqual(List<Assimp.Vector3D> a, SharpDX.Vector3[] b)
        {
            Assert.AreEqual(a.Count, b.Length);
            for (int i = 0; i < a.Count; i++)
            {
                AreEqual(a[i], b[i]);
            }
        }

        public static void AreEqual(Assimp.Quaternion a, SharpDX.Quaternion b)
        {
            AreNearEqual(a.X, b.X);
            AreNearEqual(a.Y, b.Y);
            AreNearEqual(a.Z, b.Z);
            AreNearEqual(a.W, b.W);
        }

        public static void AreEqual(Color3D a, Vector3 b)
        {
            Assert.AreEqual(a.R, b.X);
            Assert.AreEqual(a.G, b.Y);
            Assert.AreEqual(a.B, b.Z);
        }

        internal static void AreEqual(List<Color4D> a, Color4[] b)
        {
            Assert.AreEqual(a.Count, b.Length);
            for(int i=0; i<a.Count; i++)
            {
                AreEqual(a[i], b[i]);
            }
        }
    }
}
