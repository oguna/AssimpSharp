using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AssimpSharp
{
    /// <summary>
    /// Represents a four dimensional mathematical quaternion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Quaternion
    {
        /// <summary>
        /// A <see cref="Quaternion"/> with all of its components set to zero.
        /// </summary>
        public static readonly Quaternion Zero = new Quaternion();

        /// <summary>
        /// A <see cref="Quaternion"/> with all of its components set to one.
        /// </summary>
        public static readonly Quaternion One = new Quaternion(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        /// The X component of the quaternion.
        /// </summary>
        public float X;

        /// <summary>
        /// The Y component of the quaternion.
        /// </summary>
        public float Y;

        /// <summary>
        /// The Z component of the quaternion.
        /// </summary>
        public float Z;

        /// <summary>
        /// The W component of the quaternion.
        /// </summary>
        public float W;

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Quaternion(float value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> struct.
        /// </summary>
        /// <param name="x">Initial value for the X component of the quaternion.</param>
        /// <param name="y">Initial value for the Y component of the quaternion.</param>
        /// <param name="z">Initial value for the Z component of the quaternion.</param>
        /// <param name="w">Initial value for the W component of the quaternion.</param>
        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the X, Y, Z, and W components of the quaternion. This must be an array with four elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than four elements.</exception>
        public Quaternion(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 4)
                throw new ArgumentOutOfRangeException("values", "There must be four and only four input values for Quaternion.");

            X = values[0];
            Y = values[1];
            Z = values[2];
            W = values[3];
        }
    }
}
