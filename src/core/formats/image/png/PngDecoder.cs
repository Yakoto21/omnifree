using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Decodificador proprietário de imagens PNG (ISO/IEC 15948 / W3C PNG 1.2).
    /// Perfil suportado: 8-bit Truecolor RGB (ColorType 2) e 8-bit Truecolor RGBA (ColorType 6), não entrelaçado.
    /// Suporta múltiplos chunks IDAT consecutivos, todos os 5 filtros PNG e descompressão DEFLATE/ZLIB pura.
    /// </summary>
    public static class PngDecoder
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

            // 1. Validação da assinatura padrão PNG (8 bytes: 89 50 4E 47 0D 0A 1A 0A)
            PngHeader.ValidateSignature(stream);

            // 2. O primeiro chunk DEVE ser IHDR
            PngChunk firstChunk = PngChunk.Read(stream);
            if (firstChunk == null)
            {
                throw new CorruptDataException("Arquivo PNG truncado imediatamente após a assinatura.");
            }

            PngHeader header = PngHeader.FromChunk(firstChunk);

            // 3. Leitura e acumulação dos chunks IDAT e tratamento dos demais chunks
            bool inIdatSequence = false;
            bool idatFinished = false;
            MemoryStream idatStream = new MemoryStream();
            bool reachedEnd = false;

            try
            {
                while (!reachedEnd)
                {
                    PngChunk chunk = PngChunk.Read(stream);
                    if (chunk == null)
                    {
                        throw new CorruptDataException("Arquivo PNG truncado antes de atingir o chunk 'IEND'.");
                    }

                    if (chunk.Type == "IHDR")
                    {
                        throw new CorruptDataException("Chunk 'IHDR' duplicado encontrado no fluxo PNG.");
                    }
                    else if (chunk.Type == "IDAT")
                    {
                        if (idatFinished)
                        {
                            throw new CorruptDataException("Chunk 'IDAT' não consecutivo encontrado após outros chunks intermediários.");
                        }
                        inIdatSequence = true;

                        // Limite de segurança de acumulação de dados comprimidos
                        if (idatStream.Length + chunk.Data.Length > SecurityLimits.DefaultMaxImageFileSize)
                        {
                            throw new ResourceLimitException(string.Format(
                                "Tamanho agregado dos chunks IDAT ({0:N0} bytes) excede o limite máximo permitido.",
                                idatStream.Length + chunk.Data.Length));
                        }

                        idatStream.Write(chunk.Data, 0, chunk.Data.Length);
                    }
                    else if (chunk.Type == "IEND")
                    {
                        if (inIdatSequence)
                        {
                            idatFinished = true;
                        }
                        reachedEnd = true;
                        break;
                    }
                    else if (chunk.Type == "PLTE")
                    {
                        if (inIdatSequence)
                        {
                            idatFinished = true;
                        }
                        // Para imagens Truecolor, PLTE é opcional e pode ser ignorada com segurança
                    }
                    else
                    {
                        if (inIdatSequence)
                        {
                            idatFinished = true;
                        }

                        // Se for um chunk crítico desconhecido (primeira letra maiúscula), rejeitamos por conformidade
                        if (char.IsUpper(chunk.Type[0]))
                        {
                            throw new UnsupportedFormatException(string.Format(
                                "Chunk crítico PNG desconhecido ou não suportado: '{0}'.", chunk.Type));
                        }
                        // Chunks auxiliares (primeira letra minúscula) são ignorados com segurança
                    }
                }

                if (idatStream.Length == 0)
                {
                    throw new CorruptDataException("Arquivo PNG não contém nenhum chunk 'IDAT' com dados de imagem.");
                }

                // 4. Descompressão do fluxo ZLIB acumulado
                byte[] compressedIdat = idatStream.ToArray();
                byte[] decompressed = ZlibStream.Decompress(compressedIdat, SecurityLimits.DefaultMaxImageFileSize);

                // 5. Validação do tamanho descomprimido esperado
                int width = header.Width;
                int height = header.Height;
                int bytesPerPixel = header.BytesPerPixel;
                int scanlineRawLength = width * bytesPerPixel;
                int expectedTotalBytes = height * (1 + scanlineRawLength);

                if (decompressed.Length != expectedTotalBytes)
                {
                    throw new CorruptDataException(string.Format(
                        "Tamanho dos dados descomprimidos do PNG ({0} bytes) difere do esperado ({1} bytes) para dimensões {2}x{3} com {4} bytes/pixel.",
                        decompressed.Length, expectedTotalBytes, width, height, bytesPerPixel));
                }

                // 6. Reconstrução e defiltragem das scanlines
                var buffer = new PixelBuffer(width, height);
                byte[] rawPixels = buffer.Pixels;

                byte[] priorRecon = null;
                byte[] currentRecon = new byte[scanlineRawLength];
                int inOffset = 0;

                for (int y = 0; y < height; y++)
                {
                    byte filterType = decompressed[inOffset++];

                    PngFilter.UnfilterScanline(
                        filterType,
                        decompressed,
                        inOffset,
                        priorRecon,
                        currentRecon,
                        scanlineRawLength,
                        bytesPerPixel);

                    inOffset += scanlineRawLength;

                    int dstRowOffset = y * width * 4;

                    if (bytesPerPixel == 3) // RGB (ColorType 2)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = x * 3;
                            int dstIdx = dstRowOffset + x * 4;

                            rawPixels[dstIdx] = currentRecon[srcIdx];         // R
                            rawPixels[dstIdx + 1] = currentRecon[srcIdx + 1]; // G
                            rawPixels[dstIdx + 2] = currentRecon[srcIdx + 2]; // B
                            rawPixels[dstIdx + 3] = 255;                     // A (RGB é 100% opaco)
                        }
                    }
                    else if (bytesPerPixel == 4) // RGBA (ColorType 6)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int srcIdx = x * 4;
                            int dstIdx = dstRowOffset + x * 4;

                            rawPixels[dstIdx] = currentRecon[srcIdx];         // R
                            rawPixels[dstIdx + 1] = currentRecon[srcIdx + 1]; // G
                            rawPixels[dstIdx + 2] = currentRecon[srcIdx + 2]; // B
                            rawPixels[dstIdx + 3] = currentRecon[srcIdx + 3]; // A
                        }
                    }

                    if (priorRecon == null)
                    {
                        priorRecon = new byte[scanlineRawLength];
                    }
                    Buffer.BlockCopy(currentRecon, 0, priorRecon, 0, scanlineRawLength);
                }

                return buffer;
            }
            finally
            {
                idatStream.Dispose();
            }
        }
    }
}
