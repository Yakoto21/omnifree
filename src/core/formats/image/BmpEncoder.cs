using System;
using System.IO;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image
{
    /// <summary>
    /// Codificador proprietário de imagens BMP não comprimidas (24-bit e 32-bit).
    /// Gera arquivos compatíveis com o padrão Windows DIB (bottom-up, BI_RGB).
    /// </summary>
    public static class BmpEncoder
    {
        public static byte[] Encode(PixelBuffer buffer, ushort bitCount = 24)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            using (var ms = new MemoryStream())
            {
                Encode(buffer, ms, bitCount);
                return ms.ToArray();
            }
        }

        public static void Encode(PixelBuffer buffer, Stream stream, ushort bitCount = 24)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (stream == null) throw new ArgumentNullException("stream");
            if (bitCount != 24 && bitCount != 32)
            {
                throw new ArgumentException("Apenas 24 ou 32 bits são suportados para codificação BMP.", "bitCount");
            }

            int width = buffer.Width;
            int height = buffer.Height;
            int bytesPerPixel = bitCount / 8;
            int rowDataBytes = width * bytesPerPixel;
            int stride = BmpHeader.CalculateStride(width, bitCount);
            int paddingBytes = stride - rowDataBytes;

            byte[] rawPixels = buffer.Pixels;
            byte[] rowBuffer = new byte[stride];

            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                var header = new BmpHeader();
                header.Write(writer, width, height, bitCount);

                // Grava de baixo para cima (bottom-up), da linha (height - 1) até 0
                for (int y = height - 1; y >= 0; y--)
                {
                    int srcRowOffset = y * width * 4;

                    if (bitCount == 24)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = srcRowOffset + x * 4;
                            int dstIdx = x * 3;

                            byte r = rawPixels[srcIdx];
                            byte g = rawPixels[srcIdx + 1];
                            byte b = rawPixels[srcIdx + 2];

                            rowBuffer[dstIdx] = b;
                            rowBuffer[dstIdx + 1] = g;
                            rowBuffer[dstIdx + 2] = r;
                        }

                        // Preenchimento de alinhamento com zeros
                        for (int p = 0; p < paddingBytes; p++)
                        {
                            rowBuffer[rowDataBytes + p] = 0;
                        }
                    }
                    else if (bitCount == 32)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = srcRowOffset + x * 4;
                            int dstIdx = x * 4;

                            byte r = rawPixels[srcIdx];
                            byte g = rawPixels[srcIdx + 1];
                            byte b = rawPixels[srcIdx + 2];

                            rowBuffer[dstIdx] = b;
                            rowBuffer[dstIdx + 1] = g;
                            rowBuffer[dstIdx + 2] = r;
                            rowBuffer[dstIdx + 3] = 0; // Byte reservado X (BGRX opaco em BI_RGB)
                        }
                    }

                    stream.Write(rowBuffer, 0, stride);
                }
            }
        }
    }
}
