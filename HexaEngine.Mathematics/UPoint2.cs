﻿namespace HexaEngine.Mathematics
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Numerics;

    public struct UPoint2 : IEquatable<UPoint2>
    {
        /// <summary>The X component of the point.</summary>
        public uint X;

        /// <summary>The Y component of the point.</summary>
        public uint Y;

        public static readonly UPoint2 Zero = new(0);

        public static readonly UPoint2 One = new(1);

        public static readonly UPoint2 UnitX = new(1, 0);

        public static readonly UPoint2 UnitY = new(0, 1);

        public UPoint2(uint value)
        {
            X = value;
            Y = value;
        }

        public UPoint2(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            return obj is UPoint2 point && Equals(point);
        }

        public readonly bool Equals(UPoint2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(UPoint2 left, UPoint2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UPoint2 left, UPoint2 right)
        {
            return !(left == right);
        }

        public static UPoint2 operator +(UPoint2 left, UPoint2 right)
        {
            return new UPoint2(left.X + right.X, left.Y + right.Y);
        }

        public static UPoint2 operator -(UPoint2 left, UPoint2 right)
        {
            return new UPoint2(left.X - right.X, left.Y - right.Y);
        }

        public static UPoint2 operator *(UPoint2 left, UPoint2 right)
        {
            return new UPoint2(left.X * right.X, left.Y * right.Y);
        }

        public static UPoint2 operator /(UPoint2 left, UPoint2 right)
        {
            return new UPoint2(left.X / right.X, left.Y / right.Y);
        }

        public static UPoint2 operator +(UPoint2 left, uint right)
        {
            return new UPoint2(left.X + right, left.Y + right);
        }

        public static UPoint2 operator -(UPoint2 left, uint right)
        {
            return new UPoint2(left.X - right, left.Y - right);
        }

        public static UPoint2 operator *(UPoint2 left, uint right)
        {
            return new UPoint2(left.X * right, left.Y * right);
        }

        public static UPoint2 operator /(UPoint2 left, uint right)
        {
            return new UPoint2(left.X / right, left.Y / right);
        }

        public static UPoint2 operator ++(UPoint2 point)
        {
            return new UPoint2(point.X++, point.Y++);
        }

        public static UPoint2 operator --(UPoint2 point)
        {
            return new UPoint2(point.X--, point.Y--);
        }

        public static implicit operator UPoint2(Vector2 vector) => new() { X = (uint)vector.X, Y = (uint)vector.Y };

        public static implicit operator Vector2(UPoint2 point) => new() { X = point.X, Y = point.Y };

        /// <summary>Returns the string representation of the current instance using default formatting.</summary>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        public override readonly string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="System.Globalization.NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
        }
    }
}