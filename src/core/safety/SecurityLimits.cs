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
        public const long MaxSingleChunkSize = 64 * 1024 * 1024; // 64 MB por chunk auxiliar RIFF (não aplicável ao chunk 'data')
        public const long MaxAuxiliaryHeaderBytes = 16 * 1024 * 1024; // 16 MB acumulados de chunks auxiliares antes do data
        public const int MaxAudioChunkCount = 4096; // Inclui fmt, data e chunks auxiliares processados
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

            long memoryBytes = checked((long)channels * frameCount * sizeof(int));
            if (memoryBytes > MaxAudioMemoryBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "Memória estimada para o buffer de áudio ({0:N0} bytes) excede o teto de segurança permitido de {1:N0} bytes.",
                    memoryBytes, MaxAudioMemoryBytes));
            }
        }

        public const long DefaultMaxZipFileSize = 500 * 1024 * 1024; // 500 MB
        public const int MaxZipEntries = 10000; // 10.000 entradas por arquivo ZIP
        public const long MaxZipEntrySize = 500 * 1024 * 1024; // 500 MB por entrada descompactada
        public const long MaxZipTotalExtractedBytes = 1024 * 1024 * 1024; // 1 GB total extraído (proteção contra Zip Bomb)
        public const int MaxZipCommentLength = 65535; // 64 KB limite de comentário ZIP

        public static void AssertZipEntryCount(int entryCount)
        {
            if (entryCount < 0)
            {
                throw new CorruptDataException("Contagem de entradas do arquivo ZIP não pode ser negativa.");
            }
            if (entryCount > MaxZipEntries)
            {
                throw new ResourceLimitException(string.Format(
                    "Contagem de entradas do arquivo ZIP ({0:N0}) excede o limite máximo permitido de {1:N0} entradas.",
                    entryCount, MaxZipEntries));
            }
        }

        public static void AssertZipEntrySize(long uncompressedSize)
        {
            if (uncompressedSize < 0)
            {
                throw new CorruptDataException("Tamanho descompactado de entrada ZIP não pode ser negativo.");
            }
            if (uncompressedSize > MaxZipEntrySize)
            {
                throw new ResourceLimitException(string.Format(
                    "Tamanho descompactado da entrada ZIP ({0:N0} bytes) excede o limite máximo seguro de {1:N0} bytes.",
                    uncompressedSize, MaxZipEntrySize));
            }
        }

        public static void AssertZipTotalExtracted(long accumulatedBytes)
        {
            if (accumulatedBytes > MaxZipTotalExtractedBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "Total acumulado de bytes extraídos do arquivo ZIP ({0:N0} bytes) excede o teto de segurança contra Zip Bomb de {1:N0} bytes.",
                    accumulatedBytes, MaxZipTotalExtractedBytes));
            }
        }

        // ==========================================
        // LIMITES DE SEGURANÇA PARA GZIP (RFC 1952)
        // ==========================================
        public const long DefaultMaxGzipInputFileSize = 500 * 1024 * 1024; // 500 MB
        public const long DefaultMaxGzipExtractedBytes = 500 * 1024 * 1024; // 500 MB (consistente com ZIP)
        public const int MaxGzipHeaderSize = 64 * 1024; // 64 KB (limite do cabeçalho incluindo FEXTRA, FNAME, FCOMMENT)
        public const int MaxGzipFilenameLength = 4096; // 4 KB
        public const int MaxGzipCommentLength = 4096; // 4 KB
        public const int MaxGzipConcatenatedMembers = 1024; // 1024 membros por fluxo GZIP

        public static void AssertGzipInputFileSize(long length, long maxBytes = DefaultMaxGzipInputFileSize)
        {
            if (length > maxBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "O arquivo GZIP ({0:N0} bytes) excede o limite máximo de segurança permitido de {1:N0} bytes.",
                    length, maxBytes));
            }
        }

        public static void AssertGzipExtractedBytes(long accumulatedBytes, long maxBytes = DefaultMaxGzipExtractedBytes)
        {
            if (accumulatedBytes > maxBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "Total acumulado de bytes descompactados do GZIP ({0:N0} bytes) excede o teto de segurança contra Decompression Bomb de {1:N0} bytes.",
                    accumulatedBytes, maxBytes));
            }
        }

        public static void AssertGzipHeaderSize(int currentHeaderBytes)
        {
            if (currentHeaderBytes > MaxGzipHeaderSize)
            {
                throw new ResourceLimitException(string.Format(
                    "Tamanho do cabeçalho GZIP ({0:N0} bytes) excede o teto de segurança permitido de {1:N0} bytes.",
                    currentHeaderBytes, MaxGzipHeaderSize));
            }
        }

        public static void AssertGzipFilenameLength(int length)
        {
            if (length > MaxGzipFilenameLength)
            {
                throw new ResourceLimitException(string.Format(
                    "Comprimento do nome de arquivo GZIP (FNAME: {0:N0} bytes) excede o limite de segurança de {1:N0} bytes.",
                    length, MaxGzipFilenameLength));
            }
        }

        public static void AssertGzipCommentLength(int length)
        {
            if (length > MaxGzipCommentLength)
            {
                throw new ResourceLimitException(string.Format(
                    "Comprimento do comentário GZIP (FCOMMENT: {0:N0} bytes) excede o limite de segurança de {1:N0} bytes.",
                    length, MaxGzipCommentLength));
            }
        }

        public static void AssertGzipMemberCount(int count)
        {
            if (count > MaxGzipConcatenatedMembers)
            {
                throw new ResourceLimitException(string.Format(
                    "Número de membros GZIP concatenados ({0:N0}) excede o limite máximo de segurança de {1:N0} membros.",
                    count, MaxGzipConcatenatedMembers));
            }
        }
    }
}
