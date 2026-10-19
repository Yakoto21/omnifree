using System;
using Lodino.Core.Errors;

namespace Lodino.Core.Safety
{
    public static class SecurityLimits
    {
        public const long DefaultMaxDataFileSize = 100 * 1024 * 1024; // 100 MB
        public const int MaxJsonNestingDepth = 128;
        public const int MaxCsvColumns = 10000;
        public const int MaxCsvRows = 1000000;
        public const int MaxIniSections = 5000;
        public const int MaxIniKeysPerSection = 10000;

        public const int MaxImageDimension = 16384;
        public const long MaxImagePixelCount = 64 * 1024 * 1024; // 64 Megapixels (~256 MB em RGBA)
        public const long DefaultMaxImageFileSize = 250 * 1024 * 1024; // 250 MB

        public static void AssertFileSize(long length, long maxBytes = DefaultMaxDataFileSize)
        {
            if (length > maxBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "O arquivo ({0:N0} bytes) excede o limite máximo de segurança permitido de {1:N0} bytes.",
                    length, maxBytes));
            }
        }

        public static void AssertImageDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new CorruptDataException(string.Format(
                    "Dimensões de imagem inválidas: {0}x{1} (devem ser maiores que zero).",
                    width, height));
            }
            if (width > MaxImageDimension || height > MaxImageDimension)
            {
                throw new ResourceLimitException(string.Format(
                    "Dimensões da imagem ({0}x{1}) excedem o limite de segurança de {2}x{2} pixels.",
                    width, height, MaxImageDimension));
            }
            long totalPixels = (long)width * (long)height;
            if (totalPixels > MaxImagePixelCount)
            {
                throw new ResourceLimitException(string.Format(
                    "Contagem total de pixels ({0:N0}) excede o limite máximo permitido de {1:N0} pixels.",
                    totalPixels, MaxImagePixelCount));
            }
        }

        public static void AssertJsonDepth(int currentDepth)
        {
            if (currentDepth > MaxJsonNestingDepth)
            {
                throw new ResourceLimitException(string.Format(
                    "Profundidade da estrutura JSON ({0}) ultrapassa o teto máximo permitido de {1} níveis.",
                    currentDepth, MaxJsonNestingDepth));
            }
        }
    }
}
