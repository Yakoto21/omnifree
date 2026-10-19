using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image
{
    /// <summary>
    /// Decodificador proprietário de imagens BMP não comprimidas (24-bit BGR e 32-bit BGRA).
    /// Suporta orientação bottom-up e top-down, stride com padding e recuperação de canal alfa em BGRX.
    /// </summary>
    public static class BmpDecoder
    {
        public static PixelBuffer Decode(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            using (var ms = new MemoryStream(data, false))
            {
                return Decode(ms);
            }
        }

        public static PixelBuffer Decode(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException("Stream deve suportar leitura.", "stream");

            long streamLength;
            try
            {
                streamLength = stream.Length;
            }
            catch (NotSupportedException)
            {
                // Se o stream não suporta Length, copie para um MemoryStream seguro com limite de tamanho
                using (var ms = new MemoryStream())
                {
                    byte[] copyBuf = new byte[8192];
                    int read;
                    long totalCopied = 0;
                    while ((read = stream.Read(copyBuf, 0, copyBuf.Length)) > 0)
                    {
                        totalCopied += read;
                        if (totalCopied > SecurityLimits.DefaultMaxImageFileSize)
                        {
                            throw new ResourceLimitException(string.Format(
                                "Fluxo de imagem excede o tamanho máximo permitido de {0:N0} bytes.",
                                SecurityLimits.DefaultMaxImageFileSize));
                        }
                        ms.Write(copyBuf, 0, read);
                    }
                    ms.Position = 0;
                    return Decode(ms);
                }
            }

            using (var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true))
            {
                BmpHeader header = BmpHeader.Read(reader, streamLength);

                // Posiciona o ponteiro no início exato dos dados de pixel (bfOffBits)
                if (stream.CanSeek)
                {
                    if (stream.Position != header.OffBits)
                    {
                        stream.Seek(header.OffBits, SeekOrigin.Begin);
                    }
                }
                else
                {
                    if (header.OffBits < stream.Position)
                    {
                        throw new CorruptDataException("Deslocamento de dados do BMP precede a posição atual do fluxo não-navegável.");
                    }
                    long bytesToSkip = header.OffBits - stream.Position;
                    if (bytesToSkip > 0)
                    {
                        StreamUtils.SkipBytes(stream, bytesToSkip, SecurityLimits.DefaultMaxImageFileSize);
                    }
                }

                int width = header.Width;
                int height = header.Height;
                int bpp = header.BitCount;
                int bytesPerPixel = bpp / 8;
                int rowDataBytes = width * bytesPerPixel;
                int stride = header.Stride;
                int paddingBytes = stride - rowDataBytes;

                var buffer = new PixelBuffer(width, height);
                byte[] rawPixels = buffer.Pixels;

                byte[] rowBuffer = new byte[stride];
                bool isTopDown = header.IsTopDown;

                // Processa linha por linha respeitando a orientação do BMP
                for (int i = 0; i < height; i++)
                {
                    int targetY = isTopDown ? i : (height - 1 - i);

                    StreamUtils.ReadExactOrThrow(stream, rowBuffer, 0, stride,
                        string.Format("Fim prematuro de arquivo na scanline {0} de {1} do BMP.", i, height));

                    int dstRowOffset = targetY * width * 4;

                    if (bpp == 24)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = x * 3;
                            int dstIdx = dstRowOffset + x * 4;

                            byte b = rowBuffer[srcIdx];
                            byte g = rowBuffer[srcIdx + 1];
                            byte r = rowBuffer[srcIdx + 2];

                            rawPixels[dstIdx] = r;
                            rawPixels[dstIdx + 1] = g;
                            rawPixels[dstIdx + 2] = b;
                            rawPixels[dstIdx + 3] = 255; // 24 bits é sempre totalmente opaco
                        }
                    }
                    else if (bpp == 32)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = x * 4;
                            int dstIdx = dstRowOffset + x * 4;

                            byte b = rowBuffer[srcIdx];
                            byte g = rowBuffer[srcIdx + 1];
                            byte r = rowBuffer[srcIdx + 2];
                            // Em BITMAPINFOHEADER (40 bytes) com BI_RGB de 32 bits, o quarto byte é canal reservado (BGRX).
                            // A semântica estrita trata os pixels como opacos, normalizando o canal alfa para 255
                            // independentemente de conter 0x00, 0xFF ou qualquer valor intermediário.
                            rawPixels[dstIdx] = r;
                            rawPixels[dstIdx + 1] = g;
                            rawPixels[dstIdx + 2] = b;
                            rawPixels[dstIdx + 3] = 255;
                        }
                    }
                }

                return buffer;
            }
        }
    }
}
