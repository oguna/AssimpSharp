using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AssimpSharp
{
    /// <summary>
    /// Represents a 3x3 Matrix ( contains only Scale and Rotation ).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Matrix3x3
    {
        /// <summary>
        /// The size of the <see cref="Matrix3x3"/> type, in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Matrix3x3));

        /// <summary>
        /// A <see cref="Matrix3x3"/> with all of its components set to zero.
        /// </summary>
        public static readonly Matrix3x3 Zero = new Matrix3x3();

        /// <summary>
        /// The identity <see cref="Matrix3x3"/>.
        /// </summary>
        public static readonly Matrix3x3 Identity = new Matrix3x3() { M11 = 1.0f, M22 = 1.0f, M33 = 1.0f };
        
        /// <summary>
        /// Value at row 1 column 1 of the Matrix3x3.
        /// </summary>
        public float M11;

        /// <summary>
        /// Value at row 1 column 2 of the Matrix3x3.
        /// </summary>
        public float M12;

        /// <summary>
        /// Value at row 1 column 3 of the Matrix3x3.
        /// </summary>
        public float M13;

        /// <summary>
        /// Value at row 2 column 1 of the Matrix3x3.
        /// </summary>
        public float M21;

        /// <summary>
        /// Value at row 2 column 2 of the Matrix3x3.
        /// </summary>
        public float M22;

        /// <summary>
        /// Value at row 2 column 3 of the Matrix3x3.
        /// </summary>
        public float M23;

        /// <summary>
        /// Value at row 3 column 1 of the Matrix3x3.
        /// </summary>
        public float M31;

        /// <summary>
        /// Value at row 3 column 2 of the Matrix3x3.
        /// </summary>
        public float M32;

        /// <summary>
        /// Value at row 3 column 3 of the Matrix3x3.
        /// </summary>
        public float M33;

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3x3"/> struct.
        /// </summary>
        /// <param name="value">The value that will be assigned to all components.</param>
        public Matrix3x3(float value)
        {
            M11 = M12 = M13 =
            M21 = M22 = M23 =
            M31 = M32 = M33 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3x3"/> struct.
        /// </summary>
        /// <param name="M11">The value to assign at row 1 column 1 of the Matrix3x3.</param>
        /// <param name="M12">The value to assign at row 1 column 2 of the Matrix3x3.</param>
        /// <param name="M13">The value to assign at row 1 column 3 of the Matrix3x3.</param>
        /// <param name="M21">The value to assign at row 2 column 1 of the Matrix3x3.</param>
        /// <param name="M22">The value to assign at row 2 column 2 of the Matrix3x3.</param>
        /// <param name="M23">The value to assign at row 2 column 3 of the Matrix3x3.</param>
        /// <param name="M31">The value to assign at row 3 column 1 of the Matrix3x3.</param>
        /// <param name="M32">The value to assign at row 3 column 2 of the Matrix3x3.</param>
        /// <param name="M33">The value to assign at row 3 column 3 of the Matrix3x3.</param>
        public Matrix3x3(float M11, float M12, float M13,
            float M21, float M22, float M23,
            float M31, float M32, float M33)
        {
            this.M11 = M11; this.M12 = M12; this.M13 = M13;
            this.M21 = M21; this.M22 = M22; this.M23 = M23;
            this.M31 = M31; this.M32 = M32; this.M33 = M33;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3x3"/> struct.
        /// </summary>
        /// <param name="values">The values to assign to the components of the Matrix3x3. This must be an array with sixteen elements.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="values"/> contains more or less than sixteen elements.</exception>
        public Matrix3x3(float[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (values.Length != 9)
                throw new ArgumentOutOfRangeException("values", "There must be sixteen and only sixteen input values for Matrix3x3.");

            M11 = values[0];
            M12 = values[1];
            M13 = values[2];

            M21 = values[3];
            M22 = values[4];
            M23 = values[5];

            M31 = values[6];
            M32 = values[7];
            M33 = values[8];
        }
    }
}
