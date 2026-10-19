using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Descompressor proprietário de blocos DEFLATE (RFC 1951).
    /// Suporta blocos não comprimidos (BTYPE 00), blocos Huffman fixos (BTYPE 01)
    /// e blocos Huffman dinâmicos (BTYPE 10).
    /// </summary>
    public static class DeflateDecompressor
    {
        private static readonly int[] CodeLengthOrder = new int[19]
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        };

        private static readonly int[] LengthBase = new int[29]
        {
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
            35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
        };

        private static readonly int[] LengthExtraBits = new int[29]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
            3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
        };

        private static readonly int[] DistanceBase = new int[30]
        {
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
            257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577
        };

        private static readonly int[] DistanceExtraBits = new int[30]
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
            7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
        };

        public static byte[] Decompress(Stream inputStream, long maxOutputBytes = SecurityLimits.DefaultMaxImageFileSize)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            using (var output = new MemoryStream())
            {
                Decompress(inputStream, output, maxOutputBytes);
                return output.ToArray();
            }
        }

        public static void Decompress(Stream inputStream, Stream outputStream, long maxOutputBytes = SecurityLimits.DefaultMaxImageFileSize)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            var reader = new DeflateBitReader(inputStream);
            byte[] history = new byte[32768];
            int historyPos = 0;
            long totalOutputBytes = 0;

            int bfinal;
            do
            {
                bfinal = reader.ReadBit();
                int btype = reader.ReadBits(2);

                switch (btype)
                {
                    case 0: // Não comprimido / Stored (BTYPE 00)
                        DecodeStoredBlock(reader, outputStream, history, ref historyPos, ref totalOutputBytes, maxOutputBytes);
                        break;

                    case 1: // Huffman Fixo (BTYPE 01)
                        DecodeHuffmanBlock(
                            reader,
                            outputStream,
                            HuffmanTree.GetFixedLiteralTree(),
                            HuffmanTree.GetFixedDistanceTree(),
                            history,
                            ref historyPos,
                            ref totalOutputBytes,
                            maxOutputBytes);
                        break;

                    case 2: // Huffman Dinâmico (BTYPE 10)
                        DecodeDynamicBlock(reader, outputStream, history, ref historyPos, ref totalOutputBytes, maxOutputBytes);
                        break;

                    default: // BTYPE 11 reservado
                        throw new CorruptDataException("Tipo de bloco DEFLATE inválido (BTYPE = 11 reservado).");
                }

            } while (bfinal == 0);
        }

        private static void DecodeStoredBlock(
            DeflateBitReader reader,
            Stream outputStream,
            byte[] history,
            ref int historyPos,
            ref long totalOutputBytes,
            long maxOutputBytes)
        {
            reader.AlignToByte();
            ushort len = reader.ReadUInt16LittleEndian();
            ushort nlen = reader.ReadUInt16LittleEndian();

            if ((ushort)(~len) != nlen)
            {
                throw new CorruptDataException(string.Format(
                    "Bloco DEFLATE não comprimido corrompido: NLEN (0x{0:X4}) != ~LEN (~0x{1:X4}).", nlen, len));
            }

            for (int i = 0; i < len; i++)
            {
                byte b = reader.ReadByte();
                outputStream.WriteByte(b);
                history[historyPos] = b;
                historyPos = (historyPos + 1) & 32767;

                totalOutputBytes++;
                if (totalOutputBytes > maxOutputBytes)
                {
                    throw new ResourceLimitException(string.Format(
                        "Descompressão DEFLATE excedeu o limite máximo seguro de {0:N0} bytes.", maxOutputBytes));
                }
            }
        }

        private static void DecodeDynamicBlock(
            DeflateBitReader reader,
            Stream outputStream,
            byte[] history,
            ref int historyPos,
            ref long totalOutputBytes,
            long maxOutputBytes)
        {
            int hlit = reader.ReadBits(5) + 257;
            int hdist = reader.ReadBits(5) + 1;
            int hclen = reader.ReadBits(4) + 4;

            int[] clLengths = new int[19];
            for (int i = 0; i < hclen; i++)
            {
                clLengths[CodeLengthOrder[i]] = reader.ReadBits(3);
            }

            HuffmanTree clTree = HuffmanTree.Build(clLengths);

            int totalCodes = hlit + hdist;
            int[] allLengths = new int[totalCodes];
            int index = 0;

            while (index < totalCodes)
            {
                int sym = clTree.DecodeSymbol(reader);
                if (sym < 16)
                {
                    allLengths[index++] = sym;
                }
                else if (sym == 16)
                {
                    if (index == 0)
                    {
                        throw new CorruptDataException("Código de repetição 16 no início do fluxo de comprimentos de código.");
                    }
                    int prev = allLengths[index - 1];
                    int repeat = 3 + reader.ReadBits(2);
                    if (index + repeat > totalCodes)
                    {
                        throw new CorruptDataException("Código de repetição 16 ultrapassa a quantidade total de comprimentos de código.");
                    }
                    while (repeat-- > 0)
                    {
                        allLengths[index++] = prev;
                    }
                }
                else if (sym == 17)
                {
                    int repeat = 3 + reader.ReadBits(3);
                    if (index + repeat > totalCodes)
                    {
                        throw new CorruptDataException("Código de repetição 17 ultrapassa a quantidade total de comprimentos de código.");
                    }
                    while (repeat-- > 0)
                    {
                        allLengths[index++] = 0;
                    }
                }
                else if (sym == 18)
                {
                    int repeat = 11 + reader.ReadBits(7);
                    if (index + repeat > totalCodes)
                    {
                        throw new CorruptDataException("Código de repetição 18 ultrapassa a quantidade total de comprimentos de código.");
                    }
                    while (repeat-- > 0)
                    {
                        allLengths[index++] = 0;
                    }
                }
                else
                {
                    throw new CorruptDataException("Símbolo de comprimento de código desconhecido: " + sym);
                }
            }

            int[] litLengths = new int[hlit];
            Array.Copy(allLengths, 0, litLengths, 0, hlit);

            int[] distLengths = new int[hdist];
            Array.Copy(allLengths, hlit, distLengths, 0, hdist);

            HuffmanTree litTree = HuffmanTree.Build(litLengths);
            HuffmanTree distTree = HuffmanTree.Build(distLengths);

            DecodeHuffmanBlock(reader, outputStream, litTree, distTree, history, ref historyPos, ref totalOutputBytes, maxOutputBytes);
        }

        private static void DecodeHuffmanBlock(
            DeflateBitReader reader,
            Stream outputStream,
            HuffmanTree litTree,
            HuffmanTree distTree,
            byte[] history,
            ref int historyPos,
            ref long totalOutputBytes,
            long maxOutputBytes)
        {
            while (true)
            {
                int sym = litTree.DecodeSymbol(reader);

                if (sym < 256)
                {
                    // Byte literal
                    outputStream.WriteByte((byte)sym);
                    history[historyPos] = (byte)sym;
                    historyPos = (historyPos + 1) & 32767;

                    totalOutputBytes++;
                    if (totalOutputBytes > maxOutputBytes)
                    {
                        throw new ResourceLimitException(string.Format(
                            "Descompressão DEFLATE excedeu o limite máximo seguro de {0:N0} bytes.", maxOutputBytes));
                    }
                }
                else if (sym == 256)
                {
                    // Fim do bloco (End of Block)
                    break;
                }
                else if (sym >= 257 && sym <= 285)
                {
                    int lenIdx = sym - 257;
                    int length = LengthBase[lenIdx];
                    int extraBits = LengthExtraBits[lenIdx];
                    if (extraBits > 0)
                    {
                        length += reader.ReadBits(extraBits);
                    }

                    int distSym = distTree.DecodeSymbol(reader);
                    if (distSym < 0 || distSym >= DistanceBase.Length)
                    {
                        throw new CorruptDataException("Símbolo de distância fora dos limites permitidos: " + distSym);
                    }

                    int distance = DistanceBase[distSym];
                    int distExtraBits = DistanceExtraBits[distSym];
                    if (distExtraBits > 0)
                    {
                        distance += reader.ReadBits(distExtraBits);
                    }

                    if (distance > totalOutputBytes)
                    {
                        throw new CorruptDataException(string.Format(
                            "Distância LZ77 ({0}) aponta para antes do início do fluxo ({1} bytes emitidos).",
                            distance, totalOutputBytes));
                    }

                    // Cópia deslizante do histórico LZ77
                    for (int k = 0; k < length; k++)
                    {
                        int srcPos = (historyPos - distance) & 32767;
                        byte b = history[srcPos];
                        outputStream.WriteByte(b);
                        history[historyPos] = b;
                        historyPos = (historyPos + 1) & 32767;

                        totalOutputBytes++;
                        if (totalOutputBytes > maxOutputBytes)
                        {
                            throw new ResourceLimitException(string.Format(
                                "Descompressão DEFLATE excedeu o limite máximo seguro de {0:N0} bytes.", maxOutputBytes));
                        }
                    }
                }
                else
                {
                    throw new CorruptDataException("Símbolo de código DEFLATE literal/comprimento desconhecido: " + sym);
                }
            }
        }
    }
}
