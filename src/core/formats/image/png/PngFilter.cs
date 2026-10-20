using System;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Implementação original dos 5 filtros de reconstrução e codificação de scanlines PNG
    /// conforme a especificação ISO/IEC 15948 / W3C PNG 1.2.
    /// Tipos: 0 (None), 1 (Sub), 2 (Up), 3 (Average), 4 (Paeth).
    /// </summary>
    public static class PngFilter
    {
        public const byte FilterNone = 0;
        public const byte FilterSub = 1;
        public const byte FilterUp = 2;
        public const byte FilterAverage = 3;
        public const byte FilterPaeth = 4;

        public static byte PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc) return (byte)a;
            if (pb <= pc) return (byte)b;
            return (byte)c;
        }

        public static void UnfilterScanline(
            byte filterType,
            byte[] rawScanline,
            int rawOffset,
            byte[] priorRecon,
            byte[] currentRecon,
            int length,
            int bytesPerPixel)
        {
            if (rawScanline == null) throw new ArgumentNullException("rawScanline");
            if (currentRecon == null) throw new ArgumentNullException("currentRecon");
            if (bytesPerPixel <= 0) throw new ArgumentException("bytesPerPixel deve ser maior que zero.");

            unchecked
            {
                switch (filterType)
                {
                    case FilterNone:
                        Buffer.BlockCopy(rawScanline, rawOffset, currentRecon, 0, length);
                        break;

                    case FilterSub:
                        for (int x = 0; x < length; x++)
                        {
                            byte a = x >= bytesPerPixel ? currentRecon[x - bytesPerPixel] : (byte)0;
                            currentRecon[x] = (byte)(rawScanline[rawOffset + x] + a);
                        }
                        break;

                    case FilterUp:
                        for (int x = 0; x < length; x++)
                        {
                            byte b = priorRecon != null ? priorRecon[x] : (byte)0;
                            currentRecon[x] = (byte)(rawScanline[rawOffset + x] + b);
                        }
                        break;

                    case FilterAverage:
                        for (int x = 0; x < length; x++)
                        {
                            int a = x >= bytesPerPixel ? currentRecon[x - bytesPerPixel] : 0;
                            int b = priorRecon != null ? priorRecon[x] : 0;
                            currentRecon[x] = (byte)(rawScanline[rawOffset + x] + ((a + b) >> 1));
                        }
                        break;

                    case FilterPaeth:
                        for (int x = 0; x < length; x++)
                        {
                            int a = x >= bytesPerPixel ? currentRecon[x - bytesPerPixel] : 0;
                            int b = priorRecon != null ? priorRecon[x] : 0;
                            int c = (priorRecon != null && x >= bytesPerPixel) ? priorRecon[x - bytesPerPixel] : 0;
                            currentRecon[x] = (byte)(rawScanline[rawOffset + x] + PaethPredictor(a, b, c));
                        }
                        break;

                    default:
                        throw new CorruptDataException(string.Format("Tipo de filtro PNG desconhecido: {0}.", filterType));
                }
            }
        }

        public static void FilterScanline(
            byte filterType,
            byte[] currentRaw,
            byte[] priorRaw,
            byte[] outputFiltered,
            int length,
            int bytesPerPixel)
        {
            if (currentRaw == null) throw new ArgumentNullException("currentRaw");
            if (outputFiltered == null) throw new ArgumentNullException("outputFiltered");

            outputFiltered[0] = filterType;

            unchecked
            {
                switch (filterType)
                {
                    case FilterNone:
                        Buffer.BlockCopy(currentRaw, 0, outputFiltered, 1, length);
                        break;

                    case FilterSub:
                        for (int x = 0; x < length; x++)
                        {
                            byte a = x >= bytesPerPixel ? currentRaw[x - bytesPerPixel] : (byte)0;
                            outputFiltered[1 + x] = (byte)(currentRaw[x] - a);
                        }
                        break;

                    case FilterUp:
                        for (int x = 0; x < length; x++)
                        {
                            byte b = priorRaw != null ? priorRaw[x] : (byte)0;
                            outputFiltered[1 + x] = (byte)(currentRaw[x] - b);
                        }
                        break;

                    case FilterAverage:
                        for (int x = 0; x < length; x++)
                        {
                            int a = x >= bytesPerPixel ? currentRaw[x - bytesPerPixel] : 0;
                            int b = priorRaw != null ? priorRaw[x] : 0;
                            outputFiltered[1 + x] = (byte)(currentRaw[x] - ((a + b) >> 1));
                        }
                        break;

                    case FilterPaeth:
                        for (int x = 0; x < length; x++)
                        {
                            int a = x >= bytesPerPixel ? currentRaw[x - bytesPerPixel] : 0;
                            int b = priorRaw != null ? priorRaw[x] : 0;
                            int c = (priorRaw != null && x >= bytesPerPixel) ? priorRaw[x - bytesPerPixel] : 0;
                            outputFiltered[1 + x] = (byte)(currentRaw[x] - PaethPredictor(a, b, c));
                        }
                        break;

                    default:
                        throw new ArgumentException("Tipo de filtro de codificação não implementado: " + filterType);
                }
            }
        }
    }
}
