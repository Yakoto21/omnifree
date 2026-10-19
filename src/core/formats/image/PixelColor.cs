using System;
using System.Globalization;

namespace Lodino.Core.Formats.Image
{
    public struct PixelColor : IEquatable<PixelColor>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public PixelColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static PixelColor FromRgb(byte r, byte g, byte b)
        {
            return new PixelColor(r, g, b, 255);
        }

        public static PixelColor FromArgb(byte a, byte r, byte g, byte b)
        {
            return new PixelColor(r, g, b, a);
        }

        public static readonly PixelColor Transparent = new PixelColor(0, 0, 0, 0);
        public static readonly PixelColor Black = new PixelColor(0, 0, 0, 255);
        public static readonly PixelColor White = new PixelColor(255, 255, 255, 255);
        public static readonly PixelColor Red = new PixelColor(255, 0, 0, 255);
        public static readonly PixelColor Green = new PixelColor(0, 255, 0, 255);
        public static readonly PixelColor Blue = new PixelColor(0, 0, 255, 255);
        public static readonly PixelColor Yellow = new PixelColor(255, 255, 0, 255);
        public static readonly PixelColor Cyan = new PixelColor(0, 255, 255, 255);
        public static readonly PixelColor Magenta = new PixelColor(255, 0, 255, 255);

        public static PixelColor Blend(PixelColor bg, PixelColor fg, float opacity = 1.0f)
        {
            if (opacity <= 0.0f || fg.A == 0) return bg;

            float clampedOpacity = Math.Max(0.0f, Math.Min(1.0f, opacity));
            float fgA = (fg.A / 255.0f) * clampedOpacity;
            if (fgA <= 0.0f) return bg;

            float bgA = bg.A / 255.0f;
            float outA = fgA + bgA * (1.0f - fgA);

            if (outA <= 0.0001f)
            {
                return Transparent;
            }

            float outR = (fg.R * fgA + bg.R * bgA * (1.0f - fgA)) / outA;
            float outG = (fg.G * fgA + bg.G * bgA * (1.0f - fgA)) / outA;
            float outB = (fg.B * fgA + bg.B * bgA * (1.0f - fgA)) / outA;

            return new PixelColor(
                (byte)Math.Max(0, Math.Min(255, (int)Math.Round(outR))),
                (byte)Math.Max(0, Math.Min(255, (int)Math.Round(outG))),
                (byte)Math.Max(0, Math.Min(255, (int)Math.Round(outB))),
                (byte)Math.Max(0, Math.Min(255, (int)Math.Round(outA * 255.0f)))
            );
        }

        public static PixelColor ParseHex(string hex, PixelColor fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            string s = hex.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);

            if (s.Equals("black", StringComparison.OrdinalIgnoreCase)) return Black;
            if (s.Equals("white", StringComparison.OrdinalIgnoreCase)) return White;
            if (s.Equals("red", StringComparison.OrdinalIgnoreCase)) return Red;
            if (s.Equals("green", StringComparison.OrdinalIgnoreCase)) return Green;
            if (s.Equals("blue", StringComparison.OrdinalIgnoreCase)) return Blue;
            if (s.Equals("yellow", StringComparison.OrdinalIgnoreCase)) return Yellow;
            if (s.Equals("cyan", StringComparison.OrdinalIgnoreCase)) return Cyan;
            if (s.Equals("magenta", StringComparison.OrdinalIgnoreCase)) return Magenta;
            if (s.Equals("transparent", StringComparison.OrdinalIgnoreCase)) return Transparent;

            if (s.Length == 6)
            {
                byte r, g, b;
                if (byte.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                    byte.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                    byte.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
                {
                    return new PixelColor(r, g, b, 255);
                }
            }
            else if (s.Length == 8)
            {
                byte r, g, b, a;
                if (byte.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                    byte.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                    byte.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b) &&
                    byte.TryParse(s.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                {
                    return new PixelColor(r, g, b, a);
                }
            }

            return fallback;
        }

        public bool Equals(PixelColor other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PixelColor)) return false;
            return Equals((PixelColor)obj);
        }

        public override int GetHashCode()
        {
            return (R << 24) | (G << 16) | (B << 8) | A;
        }

        public static bool operator ==(PixelColor left, PixelColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PixelColor left, PixelColor right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("RGBA({0},{1},{2},{3})", R, G, B, A);
        }
    }
}
