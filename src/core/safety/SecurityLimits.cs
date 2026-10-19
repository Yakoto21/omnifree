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
        public const long MaxDecompressedRasterBytes = (long)MaxImagePixelCount * 4 + MaxImageDimension; // ~268.44 MB (64 MP RGBA + 1 byte filter per scanline)

        public static long CalculateExpectedScanlineBytes(int width, int height, int bytesPerPixel)
        {
            AssertImageDimensions(width, height);
            if (bytesPerPixel <= 0 || bytesPerPixel > 4)
            {
                throw new ArgumentOutOfRangeException("bytesPerPixel", "Bytes por pixel devem estar entre 1 e 4.");
            }

            checked
            {
                // Cada scanline contém 1 byte de tipo de filtro + (width * bytesPerPixel)
                long scanlineBytes = (long)width * bytesPerPixel + 1;
                long totalBytes = scanlineBytes * height;

                if (totalBytes > MaxDecompressedRasterBytes)
                {
                    throw new ResourceLimitException(string.Format(
                        "Tamanho de raster descomprimido ({0:N0} bytes) excede o teto de segurança permitido de {1:N0} bytes.",
                        totalBytes, MaxDecompressedRasterBytes));
                }

                return totalBytes;
            }
        }


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

        public const long DefaultMaxAudioFileSize = 500 * 1024 * 1024; // 500 MB
        public const int MaxAudioChannels = 8;
        public const int MinAudioSampleRate = 8000;
        public const int MaxAudioSampleRate = 192000;
        public const long MaxAudioFrames = 100 * 1000 * 1000; // 100 milhões de frames (~35 min a 48 kHz stereo)
        public const long MaxSingleChunkSize = 64 * 1024 * 1024; // 64 MB por chunk RIFF
        public const long MaxAuxiliaryHeaderBytes = 16 * 1024 * 1024; // 16 MB acumulados de chunks auxiliares antes do data
        public const long MaxAudioMemoryBytes = 400 * 1024 * 1024; // 400 MB de armazenamento em memória descompactada (canais * frames * sizeof(int))

        public static void AssertAudioParameters(int channels, int sampleRate, int bitsPerSample, long frameCount)
        {
            if (channels <= 0 || channels > MaxAudioChannels)
            {
                throw new CorruptDataException(string.Format(
                    "Número de canais de áudio inválido: {0} (suportado: 1 a {1}).",
                    channels, MaxAudioChannels));
            }

            if (sampleRate < MinAudioSampleRate || sampleRate > MaxAudioSampleRate)
            {
                throw new CorruptDataException(string.Format(
                    "Taxa de amostragem de áudio inválida: {0} Hz (faixa permitida: {1} Hz a {2} Hz).",
                    sampleRate, MinAudioSampleRate, MaxAudioSampleRate));
            }

            if (bitsPerSample != 8 && bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Profundidade de bits de áudio {0} não é suportada (suportado: 8, 16, 24 ou 32 bits PCM).",
                    bitsPerSample));
            }

            if (frameCount < 0)
            {
                throw new CorruptDataException("Contagem de quadros de áudio não pode ser negativa.");
            }

            if (frameCount > MaxAudioFrames)
            {
                throw new ResourceLimitException(string.Format(
                    "Contagem total de quadros de áudio ({0:N0}) excede o limite máximo permitido de {1:N0} quadros.",
                    frameCount, MaxAudioFrames));
            }

            long memoryBytes = (long)channels * frameCount * sizeof(int);
            if (memoryBytes > MaxAudioMemoryBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "Memória estimada para o buffer de áudio ({0:N0} bytes) excede o teto de segurança permitido de {1:N0} bytes.",
                    memoryBytes, MaxAudioMemoryBytes));
            }
        }
    }
}
