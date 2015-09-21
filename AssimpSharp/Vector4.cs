using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AssimpSharp
{
    /// <summary>
    /// Represents a four dimensional mathematical vector.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vector4
    {
        /// <summary>
        /// The size of the <see cref="Vector4"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Vector4));

        /// <summary>
        /// A <see cref="Vector4"/> with all of its components set to zero.
        /// </summary>
        public static readonly Vector4 Zero = new Vector4();

        /// <summary>
        /// The X unit <see cref="Vector4"/> (1, 0, 0, 0).
        /// </summary>
        public static readonly Vector4 UnitX = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The Y unit <see cref="Vector4"/> (0, 1, 0, 0).
        /// </summary>
        public static readonly Vector4 UnitY = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        /// <summary>
        /// The Z unit <see cref="Vector4"/> (0, 0, 1, 0).
        /// </summary>
        public static readonly Vector4 UnitZ = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        /// <summary>
        /// The W unit <see cref="Vector4"/> (0, 0, 0, 1).
        /// </summary>
        public static readonly Vector4 UnitW = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// A <see cref="Vector4"/> with all of its components set to one.
        /// </summary>
        public static readonly Vector4 One = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The X component of the vector.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z component of the vector.
        /// </summary>
        public float Z;

        /// <summary>
        /// The W component of the vector.
        /// </summary>
        public float W;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Vector4(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4"/> struct.
        /// </summary>
        /// <param name="x">Initial value for the X component of the vector.</param>
        /// <param name="y">Initial value for the Y component of the vector.</param>
        /// <param name="z">Initial value for the Z component of the vector.</param>
        /// <param name="w">Initial value for the W component of the vector.</param>
        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X, Y, and Z components.</param>
        /// <param name="w">Initial value for the W component of the vector.</param>
        public Vector4(Vector3 value, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4"/> struct.
        /// </summary>
        /// <param name="value">A vector containing the values with which to initialize the X and Y components.</param>
        /// <param name="z">Initial value for the Z component of the vector.</param>
        /// <param name="w">Initial value for the W component of the vector.</param>
        public Vector4(Vector2 value, float z, float w)
        {
            X = value.X;
            Y = value.Y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X, Y, Z, and W components of the vector. This must be an array with four elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Vector4(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be four and only four input values for Vector4.");

            X = values[0];
            Y = values[1];
            Z = values[2];
            W = values[3];
        }
    }
}
