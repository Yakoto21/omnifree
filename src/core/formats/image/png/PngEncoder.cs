using System;
using System.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Codificador proprietário de imagens PNG (ISO/IEC 15948 / W3C PNG 1.2).
    /// Gera arquivos PNG em conformidade estrita com a norma, suportando 8-bit RGB (ColorType 2)
    /// e 8-bit RGBA (ColorType 6), filtros adaptativos e compressão ZLIB/DEFLATE proprietária.
    /// </summary>
    public static class PngEncoder
    {
        public static byte[] Encode(PixelBuffer buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            bool hasAlpha = HasTransparency(buffer);
            return Encode(buffer, hasAlpha, PngFilter.FilterNone);
        }

        public static byte[] Encode(PixelBuffer buffer, bool hasAlpha, byte filterType = PngFilter.FilterNone)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            using (var ms = new MemoryStream())
            {
                Encode(buffer, ms, hasAlpha, filterType);
                return ms.ToArray();
            }
        }

        public static void Encode(PixelBuffer buffer, Stream outputStream, bool hasAlpha = true, byte filterType = PngFilter.FilterNone)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (outputStream == null) throw new ArgumentNullException("outputStream");
            if (!outputStream.CanWrite) throw new ArgumentException("Stream deve suportar escrita.", "outputStream");

            int width = buffer.Width;
            int height = buffer.Height;
            SecurityLimits.AssertImageDimensions(width, height);

            int bytesPerPixel = hasAlpha ? 4 : 3;
            int scanlineRawLength = width * bytesPerPixel;
            int filteredScanlineLength = 1 + scanlineRawLength;
            int totalUncompressedBytes = height * filteredScanlineLength;

            byte[] filteredData = new byte[totalUncompressedBytes];
            byte[] currentRaw = new byte[scanlineRawLength];
            byte[] priorRaw = null;
            byte[] scanlineFiltered = new byte[filteredScanlineLength];
            byte[] srcPixels = buffer.Pixels;

            for (int y = 0; y < height; y++)
            {
                int srcRowOffset = y * width * 4;

                if (hasAlpha)
                {
                    Buffer.BlockCopy(srcPixels, srcRowOffset, currentRaw, 0, scanlineRawLength);
                }
                else
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = srcRowOffset + x * 4;
                        int dstIdx = x * 3;

                        currentRaw[dstIdx] = srcPixels[srcIdx];         // R
                        currentRaw[dstIdx + 1] = srcPixels[srcIdx + 1]; // G
                        currentRaw[dstIdx + 2] = srcPixels[srcIdx + 2]; // B
                    }
                }

                PngFilter.FilterScanline(
                    filterType,
                    currentRaw,
                    priorRaw,
                    scanlineFiltered,
                    scanlineRawLength,
                    bytesPerPixel);

                Buffer.BlockCopy(scanlineFiltered, 0, filteredData, y * filteredScanlineLength, filteredScanlineLength);

                if (priorRaw == null)
                {
                    priorRaw = new byte[scanlineRawLength];
                }
                Buffer.BlockCopy(currentRaw, 0, priorRaw, 0, scanlineRawLength);
            }

            // 1. Grava assinatura de 8 bytes
            PngHeader.WriteSignature(outputStream);

            // 2. Grava chunk IHDR
            PngChunk ihdrChunk = PngHeader.CreateIhdrChunk(width, height, hasAlpha);
            ihdrChunk.Write(outputStream);

            // 3. Comprime dados com ZLIB / DEFLATE proprietário e grava chunk IDAT
            byte[] compressedIdat = ZlibStream.Compress(filteredData);
            PngChunk idatChunk = new PngChunk("IDAT", compressedIdat);
            idatChunk.Write(outputStream);

            // 4. Grava chunk IEND
            PngChunk iendChunk = new PngChunk("IEND", new byte[0]);
            iendChunk.Write(outputStream);

            outputStream.Flush();
        }

        public static bool HasTransparency(PixelBuffer buffer)
        {
            if (buffer == null) return false;
            byte[] px = buffer.Pixels;
            for (int i = 3; i < px.Length; i += 4)
            {
                if (px[i] < 255) return true;
            }
            return false;
        }
    }
}
