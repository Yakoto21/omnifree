using System;
using System.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Compressor proprietário DEFLATE (RFC 1951) em C# puro.
    /// Suporta blocos Stored (BTYPE 00) e blocos Huffman Fixos (BTYPE 01) com algoritmo LZ77.
    /// Emite fluxo bruto sem cabeçalhos ZLIB nem checksums Adler-32, compatível com PKWARE ZIP.
    /// </summary>
    public static class DeflateCompressor
    {
        private static readonly int[] FixedLitCodes = new int[288];
        private static readonly int[] FixedLitLens = new int[288];
        private static readonly int[] FixedLitReversed = new int[288];

        private static readonly int[] FixedDistCodes = new int[32];
        private static readonly int[] FixedDistLens = new int[32];
        private static readonly int[] FixedDistReversed = new int[32];

        private static readonly int[] LengthCodeMap = new int[259];
        private static readonly int[] LengthExtraBitsMap = new int[259];
        private static readonly int[] LengthExtraValues = new int[259];

        private static readonly int[] DistBase = new int[30]
        {
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
            257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577
        };

        private static readonly int[] DistExtra = new int[30]
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
        };

        static DeflateCompressor()
        {
            // 1. Literais e comprimentos (RFC 1951 § 3.2.6)
            for (int i = 0; i <= 143; i++) { FixedLitLens[i] = 8; FixedLitCodes[i] = 0x30 + i; }
            for (int i = 144; i <= 255; i++) { FixedLitLens[i] = 9; FixedLitCodes[i] = 0x190 + (i - 144); }
            for (int i = 256; i <= 279; i++) { FixedLitLens[i] = 7; FixedLitCodes[i] = i - 256; }
            for (int i = 280; i <= 287; i++) { FixedLitLens[i] = 8; FixedLitCodes[i] = 0xC0 + (i - 280); }

            for (int i = 0; i < 288; i++)
            {
                int len = FixedLitLens[i];
                int code = FixedLitCodes[i];
                int rev = 0;
                for (int b = 0; b < len; b++)
                {
                    rev = (rev << 1) | ((code >> b) & 1);
                }
                FixedLitReversed[i] = rev;
            }

            // 2. Distâncias (0..31, 5 bits canônicos 0..31)
            for (int i = 0; i < 32; i++)
            {
                FixedDistLens[i] = 5;
                FixedDistCodes[i] = i;
                int rev = 0;
                for (int b = 0; b < 5; b++)
                {
                    rev = (rev << 1) | ((i >> b) & 1);
                }
                FixedDistReversed[i] = rev;
            }

            // 3. Mapeamento de comprimentos (3..258) para símbolos 257..285
            int[] lengthBase = new int[29]
            {
                3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
                35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
            };
            int[] lengthExtra = new int[29]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
                3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
            };

            for (int symIdx = 0; symIdx < 29; symIdx++)
            {
                int sym = 257 + symIdx;
                int baseLen = lengthBase[symIdx];
                int extraBits = lengthExtra[symIdx];
                int count = 1 << extraBits;
                for (int offset = 0; offset < count; offset++)
                {
                    int len = baseLen + offset;
                    if (len <= 258)
                    {
                        LengthCodeMap[len] = sym;
                        LengthExtraBitsMap[len] = extraBits;
                        LengthExtraValues[len] = offset;
                    }
                }
            }
        }

        public static byte[] Compress(byte[] rawData, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            using (var ms = new MemoryStream())
            {
                Compress(rawData, ms, level);
                return ms.ToArray();
            }
        }

        public static void Compress(byte[] rawData, Stream outputStream, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            if (level == ZipCompressionLevel.Fast)
            {
                CompressStored(rawData, outputStream);
            }
            else
            {
                CompressFixed(rawData, outputStream);
            }
        }

        public static void Compress(Stream inputStream, Stream outputStream, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            byte[] data;
            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertZipEntrySize(inputStream.Length);
                data = new byte[inputStream.Length];
                int offset = 0;
                int read;
                while (offset < data.Length && (read = inputStream.Read(data, offset, data.Length - offset)) > 0)
                {
                    offset += read;
                }
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    byte[] buffer = new byte[8192];
                    int read;
                    long total = 0;
                    while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        SecurityLimits.AssertZipEntrySize(total);
                        ms.Write(buffer, 0, read);
                    }
                    data = ms.ToArray();
                }
            }

            Compress(data, outputStream, level);
        }

        private static void CompressStored(byte[] rawData, Stream outputStream)
        {
            const int maxBlockSize = 65535;
            int offset = 0;
            int remaining = rawData.Length;

            if (remaining == 0)
            {
                outputStream.WriteByte(0x01); // BFINAL = 1, BTYPE = 00
                outputStream.WriteByte(0x00); outputStream.WriteByte(0x00); // LEN = 0
                outputStream.WriteByte(0xFF); outputStream.WriteByte(0xFF); // NLEN = 0xFFFF
                return;
            }

            while (remaining > 0)
            {
                int blockSize = remaining > maxBlockSize ? maxBlockSize : remaining;
                remaining -= blockSize;
                bool isFinal = remaining == 0;

                byte headerByte = (byte)(isFinal ? 0x01 : 0x00);
                outputStream.WriteByte(headerByte);

                ushort len = (ushort)blockSize;
                ushort nlen = (ushort)(~len);

                outputStream.WriteByte((byte)(len & 0xFF));
                outputStream.WriteByte((byte)((len >> 8) & 0xFF));
                outputStream.WriteByte((byte)(nlen & 0xFF));
                outputStream.WriteByte((byte)((nlen >> 8) & 0xFF));

                outputStream.Write(rawData, offset, blockSize);
                offset += blockSize;
            }
        }

        private static void CompressFixed(byte[] data, Stream outputStream)
        {
            var writer = new DeflateBitWriter(outputStream);
            // BFINAL = 1, BTYPE = 01 (Fixed Huffman) -> 3 bits: 1 | (1 << 1) = 3
            writer.WriteBits(3, 3);

            int n = data.Length;
            if (n == 0)
            {
                // Bloco vazio: apenas símbolo 256 (EOB)
                writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
                writer.Flush();
                return;
            }

            int[] head = new int[65536];
            for (int i = 0; i < 65536; i++) head[i] = -1;
            int[] prev = new int[32768];

            int pos = 0;
            while (pos < n)
            {
                int bestLen = 0;
                int bestDist = 0;

                if (pos + 2 < n)
                {
                    int hash = ((data[pos] << 10) ^ (data[pos + 1] << 5) ^ data[pos + 2]) & 0xFFFF;
                    int matchPos = head[hash];
                    head[hash] = pos;
                    prev[pos & 0x7FFF] = matchPos;

                    int depth = 0;
                    while (matchPos >= 0 && depth < 32)
                    {
                        int dist = pos - matchPos;
                        if (dist <= 0 || dist > 32768) break;

                        int limit = Math.Min(258, n - pos);
                        int len = 0;
                        while (len < limit && data[pos + len] == data[matchPos + len])
                        {
                            len++;
                        }

                        if (len > bestLen)
                        {
                            bestLen = len;
                            bestDist = dist;
                            if (bestLen == 258) break;
                        }

                        matchPos = prev[matchPos & 0x7FFF];
                        depth++;
                    }
                }

                if (bestLen >= 3)
                {
                    // Emite símbolo de comprimento
                    int sym = LengthCodeMap[bestLen];
                    writer.WriteBits(FixedLitReversed[sym], FixedLitLens[sym]);
                    int extraLenBits = LengthExtraBitsMap[bestLen];
                    if (extraLenBits > 0)
                    {
                        writer.WriteBits(LengthExtraValues[bestLen], extraLenBits);
                    }

                    // Emite símbolo de distância
                    int distSym = 0;
                    int extraDistBits = 0;
                    int extraDistVal = 0;
                    for (int i = 29; i >= 0; i--)
                    {
                        if (bestDist >= DistBase[i])
                        {
                            distSym = i;
                            extraDistBits = DistExtra[i];
                            extraDistVal = bestDist - DistBase[i];
                            break;
                        }
                    }

                    writer.WriteBits(FixedDistReversed[distSym], 5);
                    if (extraDistBits > 0)
                    {
                        writer.WriteBits(extraDistVal, extraDistBits);
                    }

                    // Atualiza tabela hash para posições consumidas pelo casamento
                    for (int k = 1; k < bestLen; k++)
                    {
                        int p = pos + k;
                        if (p + 2 < n)
                        {
                            int h = ((data[p] << 10) ^ (data[p + 1] << 5) ^ data[p + 2]) & 0xFFFF;
                            prev[p & 0x7FFF] = head[h];
                            head[h] = p;
                        }
                    }

                    pos += bestLen;
                }
                else
                {
                    // Emite literal
                    byte b = data[pos];
                    writer.WriteBits(FixedLitReversed[b], FixedLitLens[b]);
                    pos++;
                }
            }

            // Símbolo 256: Fim de bloco (EOB)
            writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
            writer.Flush();
        }
    }
}
