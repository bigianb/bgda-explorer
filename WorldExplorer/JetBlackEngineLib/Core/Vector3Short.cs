using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Core;

/// <summary>
/// A structure encapsulating three short values and provides hardware accelerated methods.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack=1)]
public struct Vector3Short : IEquatable<Vector3Short>, IFormattable
{
    #region Public Static Properties
    /// <summary>
    /// Returns the vector (0,0,0).
    /// </summary>
    public static Vector3Short Zero { get { return new Vector3Short(); } }
    /// <summary>
    /// Returns the vector (1,1,1).
    /// </summary>
    public static Vector3Short One { get { return new Vector3Short(1, 1, 1); } }
    /// <summary>
    /// Returns the vector (1,0,0).
    /// </summary>
    public static Vector3Short UnitX { get { return new Vector3Short(1, 0, 0); } }
    /// <summary>
    /// Returns the vector (0,1,0).
    /// </summary>
    public static Vector3Short UnitY { get { return new Vector3Short(0, 1, 0); } }
    /// <summary>
    /// Returns the vector (0,0,1).
    /// </summary>
    public static Vector3Short UnitZ { get { return new Vector3Short(0, 0, 1); } }
    #endregion Public Static Properties
        
    #region Constructors
    /// <summary>
    /// Constructs a vector whose elements are all the single specified value.
    /// </summary>
    /// <param name="value">The element to fill the vector with.</param>
    public Vector3Short(short value) : this(value, value, value) { }
 
    // /// <summary>
    // /// Constructs a Vector3 from the given Vector2 and a third value.
    // /// </summary>
    // /// <param name="value">The Vector to extract X and Y components from.</param>
    // /// <param name="z">The Z component.</param>
    // public Vector3Short(Vector2 value, short z) : this(value.X, value.Y, z) { }
 
    /// <summary>
    /// Constructs a vector with the given individual elements.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3Short(short x, short y, short z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    #endregion Constructors
 
    #region Public Instance Methods
    /// <summary>
    /// Copies the contents of the vector into the given array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(short[] array)
    {
        CopyTo(array, 0);
    }
 
    /// <summary>
    /// Copies the contents of the vector into the given array, starting from index.
    /// </summary>
    /// <exception cref="ArgumentNullException">If array is null.</exception>
    /// <exception cref="RankException">If array is multidimensional.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero.</exception>
    /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(short[] array, int index)
    {
        if (array == null)
        {
            // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
            throw new NullReferenceException();
        }
        if (index < 0 || index >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if ((array.Length - index) < 3)
        {
            throw new ArgumentException(nameof(index));
        }
        array[index] = X;
        array[index + 1] = Y;
        array[index + 2] = Z;
    }
 
    /// <summary>
    /// Returns a boolean indicating whether the given Vector3 is equal to this Vector3 instance.
    /// </summary>
    /// <param name="other">The Vector3 to compare this instance to.</param>
    /// <returns>True if the other Vector3 is equal to this instance; False otherwise.</returns>
    public bool Equals(Vector3Short other)
    {
        return X == other.X &&
               Y == other.Y &&
               Z == other.Z;
    }
    #endregion Public Instance Methods
 
    #region Public Static Methods
    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3Short vector1, Vector3Short vector2)
    {
        return vector1.X * vector2.X +
               vector1.Y * vector2.Y +
               vector1.Z * vector2.Z;
    }
 
    /// <summary>
    /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <returns>The minimized vector.</returns>
    public static Vector3Short Min(Vector3Short value1, Vector3Short value2)
    {
        return new Vector3Short(
            (value1.X < value2.X) ? value1.X : value2.X,
            (value1.Y < value2.Y) ? value1.Y : value2.Y,
            (value1.Z < value2.Z) ? value1.Z : value2.Z);
    }
 
    /// <summary>
    /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
    /// </summary>
    /// <param name="value1">The first source vector.</param>
    /// <param name="value2">The second source vector.</param>
    /// <returns>The maximized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Max(Vector3Short value1, Vector3Short value2)
    {
        return new Vector3Short(
            (value1.X > value2.X) ? value1.X : value2.X,
            (value1.Y > value2.Y) ? value1.Y : value2.Y,
            (value1.Z > value2.Z) ? value1.Z : value2.Z);
    }
 
    /// <summary>
    /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The absolute value vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Abs(Vector3Short value)
    {
        return new Vector3Short(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
    }
 
    /// <summary>
    /// Returns a vector whose elements are the square root of each of the source vector's elements.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The square root vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short SquareRoot(Vector3Short value)
    {
        return new Vector3Short((short)Math.Sqrt(value.X), (short)Math.Sqrt(value.Y), (short)Math.Sqrt(value.Z));
    }
    #endregion Public Static Methods
 
    #region Public Static Operators
    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator +(Vector3Short left, Vector3Short right)
    {
        return new Vector3Short((short)(left.X + right.X), (short)(left.Y + right.Y), (short)(left.Z + right.Z));
    }
 
    /// <summary>
    /// Subtracts the second vector from the first.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The difference vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator -(Vector3Short left, Vector3Short right)
    {
        return new Vector3Short((short)(left.X - right.X), (short)(left.Y - right.Y), (short)(left.Z - right.Z));
    }
 
    /// <summary>
    /// Multiplies two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The product vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator *(Vector3Short left, Vector3Short right)
    {
        return new Vector3Short((short)(left.X * right.X), (short)(left.Y * right.Y), (short)(left.Z * right.Z));
    }
 
    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="right">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator *(Vector3Short left, short right)
    {
        return left * new Vector3Short(right);
    }
 
    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator *(short left, Vector3Short right)
    {
        return new Vector3Short(left) * right;
    }
 
    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator /(Vector3Short left, Vector3Short right)
    {
        return new Vector3Short((short)(left.X / right.X), (short)(left.Y / right.Y), (short)(left.Z / right.Z));
    }
 
    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3F operator /(Vector3Short value1, short value2)
    {
        float invDiv = 1.0f / value2;
 
        return new Vector3F(
            value1.X * invDiv,
            value1.Y * invDiv,
            value1.Z * invDiv);
    }
        
    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>S
    /// <param name="value1">The source vector.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3F operator /(Vector3Short value1, float value2)
    {
        var invDiv = 1.0f / value2;
 
        return new Vector3F(
            value1.X * invDiv,
            value1.Y * invDiv,
            value1.Z * invDiv);
    }
        
    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="value2">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator /(Vector3Short value1, double value2)
    {
        var invDiv = 1.0 / value2;
 
        return new Vector3D(
            value1.X * invDiv,
            value1.Y * invDiv,
            value1.Z * invDiv);
    }
 
    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short operator -(Vector3Short value)
    {
        return Zero - value;
    }
 
    /// <summary>
    /// Returns a boolean indicating whether the two given vectors are equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns>True if the vectors are equal; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3Short left, Vector3Short right)
    {
        return (left.X == right.X &&
                left.Y == right.Y &&
                left.Z == right.Z);
    }
 
    /// <summary>
    /// Returns a boolean indicating whether the two given vectors are not equal.
    /// </summary>
    /// <param name="left">The first vector to compare.</param>
    /// <param name="right">The second vector to compare.</param>
    /// <returns>True if the vectors are not equal; False if they are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3Short left, Vector3Short right)
    {
        return (left.X != right.X ||
                left.Y != right.Y ||
                left.Z != right.Z);
    }
    #endregion Public Static Operators
        
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public short X;
    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public short Y;
    /// <summary>
    /// The Z component of the vector.
    /// </summary>
    public short Z;
 
    #region Public Instance Methods
 
    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return (X, Y, Z).GetHashCode();
    }
 
    /// <summary>
    /// Returns a boolean indicating whether the given Object is equal to this Vector3 instance.
    /// </summary>
    /// <param name="obj">The Object to compare against.</param>
    /// <returns>True if the Object is equal to this Vector3; False otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (!(obj is Vector3Short))
            return false;
        return Equals((Vector3Short)obj);
    }
 
    /// <summary>
    /// Returns a String representing this Vector3 instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString()
    {
        return ToString("G", CultureInfo.CurrentCulture);
    }
 
    /// <summary>
    /// Returns a String representing this Vector3 instance, using the specified format to format individual elements.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string format)
    {
        return ToString(format, CultureInfo.CurrentCulture);
    }
 
    /// <summary>
    /// Returns a String representing this Vector3 instance, using the specified format to format individual elements 
    /// and the given IFormatProvider.
    /// </summary>
    /// <param name="format">The format of individual elements.</param>
    /// <param name="formatProvider">The format provider to use when formatting elements.</param>
    /// <returns>The string representation.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new StringBuilder();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        sb.Append('<');
        sb.Append(((IFormattable)this.X).ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(((IFormattable)this.Y).ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(((IFormattable)this.Z).ToString(format, formatProvider));
        sb.Append('>');
        return sb.ToString();
    }
 
    /// <summary>
    /// Returns the length of the vector.
    /// </summary>
    /// <returns>The vector's length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        if (Vector.IsHardwareAccelerated)
        {
            var ls = Vector3Short.Dot(this, this);
            return (short)Math.Sqrt(ls);
        }
        else
        {
            var ls = X * X + Y * Y + Z * Z;
            return (short)Math.Sqrt(ls);
        }
    }

    /// <summary>
    /// Returns the length of the vector squared. This operation is cheaper than Length().
    /// </summary>
    /// <returns>The vector's length squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        if (Vector.IsHardwareAccelerated)
        {
            return Vector3Short.Dot(this, this);
        }
        else
        {
            return X * X + Y * Y + Z * Z;
        }
    }
    #endregion Public Instance Methods
 
    #region Public Static Methods
    /// <summary>
    /// Returns the Euclidean distance between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3Short value1, Vector3Short value2)
    {
        if (Vector.IsHardwareAccelerated)
        {
            Vector3Short difference = value1 - value2;
            var ls = Vector3Short.Dot(difference, difference);
            return (short)Math.Sqrt(ls);
        }
        else
        {
            var dx = value1.X - value2.X;
            var dy = value1.Y - value2.Y;
            var dz = value1.Z - value2.Z;
 
            var ls = dx * dx + dy * dy + dz * dz;
 
            return (short)Math.Sqrt((double)ls);
        }
    }
 
    /// <summary>
    /// Returns the Euclidean distance squared between the two given points.
    /// </summary>
    /// <param name="value1">The first point.</param>
    /// <param name="value2">The second point.</param>
    /// <returns>The distance squared.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector3Short value1, Vector3Short value2)
    {
        if (Vector.IsHardwareAccelerated)
        {
            Vector3Short difference = value1 - value2;
            return Vector3Short.Dot(difference, difference);
        }
        else
        {
            float dx = value1.X - value2.X;
            float dy = value1.Y - value2.Y;
            float dz = value1.Z - value2.Z;
 
            return dx * dx + dy * dy + dz * dz;
        }
    }

    /// <summary>
    /// Restricts a vector between a min and max value.
    /// </summary>
    /// <param name="value1">The source vector.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <returns>The restricted vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Clamp(Vector3Short value1, Vector3Short min, Vector3Short max)
    {
        // This compare order is very important!!!
        // We must follow HLSL behavior in the case user specified min value is bigger than max value.
 
        short x = value1.X;
        x = (x > max.X) ? max.X : x;
        x = (x < min.X) ? min.X : x;
 
        short y = value1.Y;
        y = (y > max.Y) ? max.Y : y;
        y = (y < min.Y) ? min.Y : y;
 
        short z = value1.Z;
        z = (z > max.Z) ? max.Z : z;
        z = (z < min.Z) ? min.Z : z;
 
        return new Vector3Short(x, y, z);
    }
    #endregion Public Static Methods
 
    #region Public operator methods
 
    // All these methods should be inlined as they are implemented
    // over JIT intrinsics
 
    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The summed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Add(Vector3Short left, Vector3Short right)
    {
        return left + right;
    }
 
    /// <summary>
    /// Subtracts the second vector from the first.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The difference vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Subtract(Vector3Short left, Vector3Short right)
    {
        return left - right;
    }
 
    /// <summary>
    /// Multiplies two vectors together.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The product vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Multiply(Vector3Short left, Vector3Short right)
    {
        return left * right;
    }
 
    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="right">The scalar value.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Multiply(Vector3Short left, short right)
    {
        return left * right;
    }
 
    /// <summary>
    /// Multiplies a vector by the given scalar.
    /// </summary>
    /// <param name="left">The scalar value.</param>
    /// <param name="right">The source vector.</param>
    /// <returns>The scaled vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Multiply(short left, Vector3Short right)
    {
        return left * right;
    }
 
    /// <summary>
    /// Divides the first vector by the second.
    /// </summary>
    /// <param name="left">The first source vector.</param>
    /// <param name="right">The second source vector.</param>
    /// <returns>The vector resulting from the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Divide(Vector3Short left, Vector3Short right)
    {
        return left / right;
    }
 
    /// <summary>
    /// Divides the vector by the given scalar.
    /// </summary>
    /// <param name="left">The source vector.</param>
    /// <param name="divisor">The scalar value.</param>
    /// <returns>The result of the division.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3F Divide(Vector3Short left, short divisor)
    {
        return left / divisor;
    }
 
    /// <summary>
    /// Negates a given vector.
    /// </summary>
    /// <param name="value">The source vector.</param>
    /// <returns>The negated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Short Negate(Vector3Short value)
    {
        return -value;
    }
    #endregion Public operator methods
}