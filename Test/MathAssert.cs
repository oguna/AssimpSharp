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
            Assert.AreEqual(a.A, b.Red);
            Assert.AreEqual(a.A, b.Green);
            Assert.AreEqual(a.A, b.Blue);
        }

        public static void AreEqual(Assimp.Vector2D a, SharpDX.Vector2 b)
        {
            Assert.AreEqual(a.X, b.X);
            Assert.AreEqual(a.Y, b.Y);
        }

        public static void AreEqual(Assimp.Vector3D a, SharpDX.Vector3 b)
        {
            AreEqual(a.X, b.X);
            AreEqual(a.Y, b.Y);
            AreEqual(a.Z, b.Z);
        }

        public static void AreEqual(Assimp.Vector3D[] a, SharpDX.Vector3[] b)
        {
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                AreEqual(a[i], b[i]);
            }
        }

        public static void AreEqual(float a, float b)
        {
            Assert.Less(Math.Abs(a - b), SharpDX.MathUtil.ZeroTolerance);
        }

        public static void AreEqual(Assimp.Matrix4x4 a, SharpDX.Matrix b)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Assert.AreEqual(a[column, row], b[column, row]);
                }
            }
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
            Assert.AreEqual(a.X, b.X);
            Assert.AreEqual(a.Y, b.Y);
            Assert.AreEqual(a.Z, b.Z);
            Assert.AreEqual(a.W, b.W);
        }

        public static void AreEqual(Color3D a, Vector3 b)
        {
            Assert.AreEqual(a.R, b.X);
            Assert.AreEqual(a.G, b.Y);
            Assert.AreEqual(a.B, b.Z);
        }
    }
}
