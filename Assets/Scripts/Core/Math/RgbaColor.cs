#nullable enable
using System;
using System.Globalization;

namespace Singularidi.Visualization
{
    public readonly struct RgbaColor : IEquatable<RgbaColor>
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;

        public RgbaColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static RgbaColor FromBytes(byte r, byte g, byte b, byte a = 255)
            => new RgbaColor(r / 255f, g / 255f, b / 255f, a / 255f);

        public static RgbaColor FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex color string is empty.", nameof(hex));

            string s = hex[0] == '#' ? hex.Substring(1) : hex;

            byte a, r, g, b;
            if (s.Length == 6)
            {
                a = 255;
                r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            else if (s.Length == 8)
            {
                a = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                r = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                g = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                b = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new FormatException($"Hex color must be #RRGGBB or #AARRGGBB, got '{hex}'.");
            }

            return FromBytes(r, g, b, a);
        }

        public string ToHex()
        {
            byte a = ToByte(A);
            byte r = ToByte(R);
            byte g = ToByte(G);
            byte b = ToByte(B);
            return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        }

        private static byte ToByte(float v) => (byte)Math.Clamp((int)Math.Round(v * 255f), 0, 255);

        public byte ByteR => ToByte(R);
        public byte ByteG => ToByte(G);
        public byte ByteB => ToByte(B);
        public byte ByteA => ToByte(A);

        public bool Equals(RgbaColor other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object? obj) => obj is RgbaColor c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public static bool operator ==(RgbaColor a, RgbaColor b) => a.Equals(b);
        public static bool operator !=(RgbaColor a, RgbaColor b) => !a.Equals(b);

        public override string ToString() => ToHex();
    }
}
