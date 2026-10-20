using System;
using System.Collections.Generic;
using System.IO;
using Lodino.Core.Formats.Archive.Zip;
using Lodino.Core.Safety;

namespace Lodino.Core.Compression.Deflate
{
    /// <summary>
    /// Compressor proprietário DEFLATE (RFC 1951) em C# puro.
    /// Suporta blocos Stored (BTYPE 00), Fixed Huffman (BTYPE 01) e Dynamic Huffman (BTYPE 10)
    /// com casamento LZ77 (janela deslizante de 32 KB) e seleção adaptativa por modelo de custo em bits.
    /// Emite fluxo bruto sem cabeçalhos ZLIB nem checksums Adler-32, 100% compatível com PKWARE ZIP.
    /// </summary>
    public static class DeflateCompressor
    {
        private const int MaxBlockSize = 65535;

        private static readonly int[] CodeLengthOrder = new int[19]
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        };

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
            // 1. Literais e comprimentos fixos (RFC 1951 § 3.2.6)
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

            // 2. Distâncias fixas (0..31, 5 bits canônicos 0..31)
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

        #region Buffer de Tokens LZ77

        internal sealed class Lz77TokenBuffer
        {
            private ushort[] _litOrLen;
            private ushort[] _dist;
            private int _count;

            public Lz77TokenBuffer(int capacity = 65536)
            {
                _litOrLen = new ushort[capacity];
                _dist = new ushort[capacity];
                _count = 0;
            }

            public int Count { get { return _count; } }
            public ushort GetLitOrLen(int index) { return _litOrLen[index]; }
            public ushort GetDist(int index) { return _dist[index]; }
            public bool IsMatch(int index) { return _dist[index] > 0; }

            public void Clear()
            {
                _count = 0;
            }

            public void AddLiteral(byte b)
            {
                EnsureCapacity();
                _litOrLen[_count] = b;
                _dist[_count] = 0;
                _count++;
            }

            public void AddMatch(int length, int distance)
            {
                EnsureCapacity();
                _litOrLen[_count] = (ushort)length;
                _dist[_count] = (ushort)distance;
                _count++;
            }

            private void EnsureCapacity()
            {
                if (_count >= _litOrLen.Length)
                {
                    int newCap = _litOrLen.Length * 2;
                    Array.Resize(ref _litOrLen, newCap);
                    Array.Resize(ref _dist, newCap);
                }
            }
        }

        #endregion

        #region Gerador de Árvore Canônica de Huffman com Limite de Bits (Kraft-McMillan)

        internal static class HuffmanTreeBuilder
        {
            /// <summary>
            /// Constrói os comprimentos de código canônicos a partir de frequências, limitados a maxBits
            /// (15 para literais/distâncias e 7 para comprimentos de código), garantindo a desigualdade de Kraft.
            /// </summary>
            public static int[] BuildLengths(long[] frequencies, int maxBits)
            {
                int n = frequencies.Length;
                int[] lengths = new int[n];

                var active = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    if (frequencies[i] > 0)
                    {
                        active.Add(i);
                    }
                }

                if (active.Count == 0)
                {
                    return lengths;
                }

                if (active.Count == 1)
                {
                    // Caso monossímbolo: RFC 1951 e Kraft
                    int sym = active[0];
                    lengths[sym] = 1;
                    int dummy = (sym == 0) ? 1 : 0;
                    lengths[dummy] = 1;
                    return lengths;
                }

                int numLeaves = active.Count;
                int maxNodes = numLeaves * 2;
                long[] weight = new long[maxNodes];
                int[] parent = new int[maxNodes];
                int[] leafSymbol = new int[maxNodes];

                int[] heap = new int[maxNodes + 1];
                int heapSize = 0;

                for (int i = 0; i < numLeaves; i++)
                {
                    int sym = active[i];
                    int node = i;
                    weight[node] = frequencies[sym];
                    parent[node] = -1;
                    leafSymbol[node] = sym;
                    HeapPush(heap, ref heapSize, node, weight);
                }

                int nextInternal = numLeaves;
                while (heapSize > 1)
                {
                    int node1 = HeapPop(heap, ref heapSize, weight);
                    int node2 = HeapPop(heap, ref heapSize, weight);

                    int parentNode = nextInternal++;
                    weight[parentNode] = weight[node1] + weight[node2];
                    parent[node1] = parentNode;
                    parent[node2] = parentNode;

                    HeapPush(heap, ref heapSize, parentNode, weight);
                }

                int root = heap[1];
                parent[root] = -1;

                int[] rawDepths = new int[numLeaves];
                int maxDepth = 0;
                for (int i = 0; i < numLeaves; i++)
                {
                    int d = 0;
                    int curr = i;
                    while (parent[curr] != -1)
                    {
                        d++;
                        curr = parent[curr];
                    }
                    rawDepths[i] = d;
                    if (d > maxDepth) maxDepth = d;
                }

                if (maxDepth <= maxBits)
                {
                    for (int i = 0; i < numLeaves; i++)
                    {
                        lengths[leafSymbol[i]] = rawDepths[i];
                    }
                    return lengths;
                }

                // Nivelamento Kraft-McMillan com limite de profundidade
                int[] blCount = new int[maxDepth + 1];
                for (int i = 0; i < numLeaves; i++)
                {
                    blCount[rawDepths[i]]++;
                }

                for (int d = maxBits + 1; d <= maxDepth; d++)
                {
                    blCount[maxBits] += blCount[d];
                    blCount[d] = 0;
                }

                long kraftSum = 0;
                for (int d = 1; d <= maxBits; d++)
                {
                    kraftSum += ((long)blCount[d]) << (maxBits - d);
                }

                long targetKraft = 1L << maxBits;
                while (kraftSum > targetKraft)
                {
                    int bits = maxBits - 1;
                    while (bits > 0 && blCount[bits] == 0) bits--;
                    if (bits == 0) break;

                    blCount[bits]--;
                    blCount[bits + 1] += 2;
                    blCount[maxBits]--;
                    kraftSum -= 1;
                }

                int[] sortedIndices = new int[numLeaves];
                for (int i = 0; i < numLeaves; i++) sortedIndices[i] = i;
                Array.Sort(sortedIndices, (a, b) =>
                {
                    int cmp = frequencies[leafSymbol[b]].CompareTo(frequencies[leafSymbol[a]]);
                    if (cmp != 0) return cmp;
                    return leafSymbol[a].CompareTo(leafSymbol[b]);
                });

                int currentDepth = 1;
                for (int i = 0; i < numLeaves; i++)
                {
                    while (currentDepth <= maxBits && blCount[currentDepth] == 0)
                    {
                        currentDepth++;
                    }
                    int leafIdx = sortedIndices[i];
                    lengths[leafSymbol[leafIdx]] = currentDepth;
                    blCount[currentDepth]--;
                }

                return lengths;
            }

            public static void BuildCanonicalCodes(int[] lengths, out int[] codes, out int[] reversedCodes)
            {
                int n = lengths.Length;
                codes = new int[n];
                reversedCodes = new int[n];

                int maxBits = 0;
                for (int i = 0; i < n; i++)
                {
                    if (lengths[i] > maxBits) maxBits = lengths[i];
                }

                if (maxBits == 0) return;

                int[] blCount = new int[maxBits + 1];
                for (int i = 0; i < n; i++)
                {
                    if (lengths[i] > 0) blCount[lengths[i]]++;
                }

                int[] nextCode = new int[maxBits + 1];
                int code = 0;
                blCount[0] = 0;
                for (int bits = 1; bits <= maxBits; bits++)
                {
                    code = (code + blCount[bits - 1]) << 1;
                    nextCode[bits] = code;
                }

                for (int i = 0; i < n; i++)
                {
                    int len = lengths[i];
                    if (len > 0)
                    {
                        int c = nextCode[len]++;
                        codes[i] = c;

                        // Inversão bit-a-bit para emissão LSB-first no DeflateBitWriter
                        int rev = 0;
                        for (int b = 0; b < len; b++)
                        {
                            rev = (rev << 1) | ((c >> b) & 1);
                        }
                        reversedCodes[i] = rev;
                    }
                }
            }

            private static void HeapPush(int[] heap, ref int size, int node, long[] weight)
            {
                heap[++size] = node;
                int i = size;
                while (i > 1 && weight[heap[i]] < weight[heap[i / 2]])
                {
                    int tmp = heap[i];
                    heap[i] = heap[i / 2];
                    heap[i / 2] = tmp;
                    i /= 2;
                }
            }

            private static int HeapPop(int[] heap, ref int size, long[] weight)
            {
                int top = heap[1];
                heap[1] = heap[size--];
                int i = 1;
                while (true)
                {
                    int left = i * 2;
                    int right = left + 1;
                    int smallest = i;

                    if (left <= size && weight[heap[left]] < weight[heap[smallest]])
                        smallest = left;
                    if (right <= size && weight[heap[right]] < weight[heap[smallest]])
                        smallest = right;

                    if (smallest != i)
                    {
                        int tmp = heap[i];
                        heap[i] = heap[smallest];
                        heap[smallest] = tmp;
                        i = smallest;
                    }
                    else break;
                }
                return top;
            }
        }

        #endregion

        #region API Pública de Compressão

        public static byte[] Compress(byte[] rawData, DeflateCompressionLevel level = DeflateCompressionLevel.Optimal)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            using (var msIn = new MemoryStream(rawData, writable: false))
            using (var msOut = new MemoryStream())
            {
                CompressStream(msIn, msOut, level);
                return msOut.ToArray();
            }
        }

        public static byte[] Compress(byte[] rawData, ZipCompressionLevel level)
        {
            return Compress(rawData, (DeflateCompressionLevel)level);
        }

        public static void Compress(byte[] rawData, Stream outputStream, DeflateCompressionLevel level = DeflateCompressionLevel.Optimal)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            using (var msIn = new MemoryStream(rawData, writable: false))
            {
                CompressStream(msIn, outputStream, level);
            }
        }

        public static void Compress(byte[] rawData, Stream outputStream, ZipCompressionLevel level)
        {
            Compress(rawData, outputStream, (DeflateCompressionLevel)level);
        }

        public static void Compress(Stream inputStream, Stream outputStream, DeflateCompressionLevel level = DeflateCompressionLevel.Optimal)
        {
            CompressStream(inputStream, outputStream, level);
        }

        public static void Compress(Stream inputStream, Stream outputStream, ZipCompressionLevel level)
        {
            CompressStream(inputStream, outputStream, (DeflateCompressionLevel)level);
        }

        /// <summary>
        /// Comprime um fluxo de dados em formato DEFLATE bruto (RFC 1951)
        /// com uso estritamente O(1) de memória através de duplo-buffer de lookahead.
        /// </summary>
        public static void CompressStream(
            Stream inputStream,
            Stream outputStream,
            DeflateCompressionLevel level = DeflateCompressionLevel.Optimal)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            byte[] currentBuf = new byte[MaxBlockSize];
            byte[] nextBuf = new byte[MaxBlockSize];
            int currentLen = ReadBlock(inputStream, currentBuf, MaxBlockSize);

            if (currentLen == 0)
            {
                // Fluxo vazio (0 bytes)
                if (level == DeflateCompressionLevel.Store)
                {
                    outputStream.WriteByte(0x01); // BFINAL = 1, BTYPE = 00
                    outputStream.WriteByte(0x00); outputStream.WriteByte(0x00); // LEN = 0
                    outputStream.WriteByte(0xFF); outputStream.WriteByte(0xFF); // NLEN = 0xFFFF
                }
                else
                {
                    var writer = new DeflateBitWriter(outputStream);
                    writer.WriteBits(3, 3); // BFINAL = 1, BTYPE = 01
                    writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
                    writer.Flush();
                }
                return;
            }

            var bitWriter = new DeflateBitWriter(outputStream);
            var tokens = (level != DeflateCompressionLevel.Store) ? new Lz77TokenBuffer(MaxBlockSize) : null;
            int[] head = (level != DeflateCompressionLevel.Store) ? new int[65536] : null;
            int[] prev = (level != DeflateCompressionLevel.Store) ? new int[32768] : null;

            while (true)
            {
                int nextLen = ReadBlock(inputStream, nextBuf, MaxBlockSize);
                bool isFinal = (nextLen == 0);

                if (level == DeflateCompressionLevel.Store)
                {
                    CompressBlockStored(currentBuf, 0, currentLen, bitWriter, isFinal);
                }
                else if (level == DeflateCompressionLevel.Fast)
                {
                    CompressBlockFixed(currentBuf, 0, currentLen, tokens, head, prev, bitWriter, isFinal);
                }
                else
                {
                    CompressBlockOptimal(currentBuf, 0, currentLen, tokens, head, prev, bitWriter, isFinal);
                }

                if (isFinal)
                {
                    bitWriter.Flush();
                    break;
                }

                byte[] tmp = currentBuf;
                currentBuf = nextBuf;
                nextBuf = tmp;
                currentLen = nextLen;
            }
        }

        public static void CompressStream(Stream inputStream, Stream outputStream, ZipCompressionLevel level)
        {
            CompressStream(inputStream, outputStream, (DeflateCompressionLevel)level);
        }

        private static int ReadBlock(Stream stream, byte[] buffer, int maxCount)
        {
            int totalRead = 0;
            while (totalRead < maxCount)
            {
                int read = stream.Read(buffer, totalRead, maxCount - totalRead);
                if (read <= 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        private static void CompressBlockStored(byte[] buffer, int offset, int length, DeflateBitWriter writer, bool isFinal)
        {
            EmitStoredBlock(buffer, offset, length, writer, isFinal);
        }

        private static void CompressBlockFixed(byte[] buffer, int offset, int length, Lz77TokenBuffer tokens, int[] head, int[] prev, DeflateBitWriter writer, bool isFinal)
        {
            tokens.Clear();
            for (int i = 0; i < 65536; i++) head[i] = -1;
            RunLz77(buffer, offset, length, tokens, head, prev);
            EmitFixedBlock(tokens, writer, isFinal);
        }

        private static void CompressBlockOptimal(
            byte[] buffer, int offset, int blockSize,
            Lz77TokenBuffer tokens, int[] head, int[] prev,
            DeflateBitWriter writer, bool isFinal)
        {
            tokens.Clear();
            for (int i = 0; i < 65536; i++) head[i] = -1;
            RunLz77(buffer, offset, blockSize, tokens, head, prev);

            int[] litLens, distLens, clLens;
            List<int> rleSymbols, rleExtra, rleExtraBits;
            int hlit, hdist, hclen;

            PrepareDynamicTrees(tokens, out litLens, out distLens, out clLens,
                out rleSymbols, out rleExtra, out rleExtraBits,
                out hlit, out hdist, out hclen);

            long costStored = CalculateStoredCost(blockSize, writer.BitCount);
            long costFixed = CalculateFixedCost(tokens);
            long costDynamic = CalculateDynamicCost(tokens, litLens, distLens, rleSymbols, rleExtraBits, clLens, hlit, hdist, hclen);

            int chosenBType = 2; // Dynamic
            long minCost = costDynamic;

            if (costFixed < minCost)
            {
                chosenBType = 1;
                minCost = costFixed;
            }

            if (costStored <= minCost && blockSize <= MaxBlockSize)
            {
                chosenBType = 0;
                minCost = costStored;
            }

            if (chosenBType == 0)
            {
                EmitStoredBlock(buffer, offset, blockSize, writer, isFinal);
            }
            else if (chosenBType == 1)
            {
                EmitFixedBlock(tokens, writer, isFinal);
            }
            else
            {
                EmitDynamicBlock(tokens, litLens, distLens, clLens,
                    rleSymbols, rleExtra, rleExtraBits,
                    hlit, hdist, hclen, writer, isFinal);
            }
        }

        #endregion

        #region Modos de Compressão Específicos (Stored, Fixed, Dynamic, Optimal)

        public static void CompressStored(byte[] rawData, Stream outputStream)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

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
                int blockSize = remaining > MaxBlockSize ? MaxBlockSize : remaining;
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

        public static void CompressFixed(byte[] rawData, Stream outputStream)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            var writer = new DeflateBitWriter(outputStream);
            if (rawData.Length == 0)
            {
                // Bloco vazio: BFINAL=1, BTYPE=01, EOB 256
                writer.WriteBits(3, 3);
                writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
                writer.Flush();
                return;
            }

            var tokens = new Lz77TokenBuffer(Math.Min(rawData.Length, MaxBlockSize));
            int[] head = new int[65536];
            for (int i = 0; i < 65536; i++) head[i] = -1;
            int[] prev = new int[32768];

            int pos = 0;
            while (pos < rawData.Length)
            {
                int blockSize = Math.Min(MaxBlockSize, rawData.Length - pos);
                bool isFinal = (pos + blockSize >= rawData.Length);

                tokens.Clear();
                RunLz77(rawData, pos, blockSize, tokens, head, prev);
                EmitFixedBlock(tokens, writer, isFinal);

                pos += blockSize;
            }

            writer.Flush();
        }

        public static void CompressDynamic(byte[] rawData, Stream outputStream)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            var writer = new DeflateBitWriter(outputStream);
            if (rawData.Length == 0)
            {
                // Para 0 bytes, emitimos bloco Fixed que tem apenas 10 bits
                writer.WriteBits(3, 3);
                writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
                writer.Flush();
                return;
            }

            var tokens = new Lz77TokenBuffer(Math.Min(rawData.Length, MaxBlockSize));
            int[] head = new int[65536];
            for (int i = 0; i < 65536; i++) head[i] = -1;
            int[] prev = new int[32768];

            int pos = 0;
            while (pos < rawData.Length)
            {
                int blockSize = Math.Min(MaxBlockSize, rawData.Length - pos);
                bool isFinal = (pos + blockSize >= rawData.Length);

                tokens.Clear();
                RunLz77(rawData, pos, blockSize, tokens, head, prev);

                int[] litLens, distLens, clLens;
                List<int> rleSymbols, rleExtra, rleExtraBits;
                int hlit, hdist, hclen;

                PrepareDynamicTrees(tokens, out litLens, out distLens, out clLens,
                    out rleSymbols, out rleExtra, out rleExtraBits,
                    out hlit, out hdist, out hclen);

                EmitDynamicBlock(tokens, litLens, distLens, clLens,
                    rleSymbols, rleExtra, rleExtraBits,
                    hlit, hdist, hclen, writer, isFinal);

                pos += blockSize;
            }

            writer.Flush();
        }

        public static void CompressOptimal(byte[] rawData, Stream outputStream)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            var writer = new DeflateBitWriter(outputStream);
            if (rawData.Length == 0)
            {
                // Bloco vazio: BFINAL=1, BTYPE=01, EOB 256 (2 bytes totais)
                writer.WriteBits(3, 3);
                writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
                writer.Flush();
                return;
            }

            var tokens = new Lz77TokenBuffer(Math.Min(rawData.Length, MaxBlockSize));
            int[] head = new int[65536];
            for (int i = 0; i < 65536; i++) head[i] = -1;
            int[] prev = new int[32768];

            int pos = 0;
            while (pos < rawData.Length)
            {
                int blockSize = Math.Min(MaxBlockSize, rawData.Length - pos);
                bool isFinal = (pos + blockSize >= rawData.Length);

                tokens.Clear();
                RunLz77(rawData, pos, blockSize, tokens, head, prev);

                // Prepara árvores dinâmicas para avaliação
                int[] litLens, distLens, clLens;
                List<int> rleSymbols, rleExtra, rleExtraBits;
                int hlit, hdist, hclen;

                PrepareDynamicTrees(tokens, out litLens, out distLens, out clLens,
                    out rleSymbols, out rleExtra, out rleExtraBits,
                    out hlit, out hdist, out hclen);

                // Modelo de custo em bits
                long costStored = CalculateStoredCost(blockSize, writer.BitCount);
                long costFixed = CalculateFixedCost(tokens);
                long costDynamic = CalculateDynamicCost(tokens, litLens, distLens, rleSymbols, rleExtraBits, clLens, hlit, hdist, hclen);

                // Regra de seleção:
                // 1. Inicia com Dynamic
                // 2. Se Fixed < Dynamic, escolhe Fixed (desempate favorece Fixed por menor custo de decodificação)
                // 3. Se Stored <= menor(Fixed, Dynamic) e bloco <= 65535, escolhe Stored (protege dados incompressíveis)
                int chosenBType = 2; // Dynamic
                long minCost = costDynamic;

                if (costFixed < minCost)
                {
                    minCost = costFixed;
                    chosenBType = 1; // Fixed
                }

                if (costStored <= minCost && blockSize <= MaxBlockSize)
                {
                    minCost = costStored;
                    chosenBType = 0; // Stored
                }

                if (chosenBType == 0)
                {
                    EmitStoredBlock(rawData, pos, blockSize, writer, isFinal);
                }
                else if (chosenBType == 1)
                {
                    EmitFixedBlock(tokens, writer, isFinal);
                }
                else
                {
                    EmitDynamicBlock(tokens, litLens, distLens, clLens,
                        rleSymbols, rleExtra, rleExtraBits,
                        hlit, hdist, hclen, writer, isFinal);
                }

                pos += blockSize;
            }

            writer.Flush();
        }

        #endregion

        #region Métodos Auxiliares de Preparação de Árvores e Avaliação de Custo

        private static void PrepareDynamicTrees(
            Lz77TokenBuffer tokens,
            out int[] litLens,
            out int[] distLens,
            out int[] clLens,
            out List<int> rleSymbols,
            out List<int> rleExtra,
            out List<int> rleExtraBits,
            out int hlit,
            out int hdist,
            out int hclen)
        {
            long[] litFreqs = new long[286];
            long[] distFreqs = new long[30];

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens.IsMatch(i))
                {
                    int len = tokens.GetLitOrLen(i);
                    int dist = tokens.GetDist(i);
                    litFreqs[LengthCodeMap[len]]++;

                    int distSym = GetDistSymbol(dist);
                    distFreqs[distSym]++;
                }
                else
                {
                    litFreqs[tokens.GetLitOrLen(i)]++;
                }
            }

            // REQ-01: Símbolo 256 (EOB) com frequência >= 1 garantida
            litFreqs[256]++;

            litLens = HuffmanTreeBuilder.BuildLengths(litFreqs, 15);
            distLens = HuffmanTreeBuilder.BuildLengths(distFreqs, 15);

            // REQ-02: Se nenhuma correspondência LZ77 ocorreu (árvore de distâncias vazia),
            // a RFC 1951 exige emitir HDIST >= 1 com pelo menos 1 código de comprimento 1
            // para compatibilidade estrita com todos os decodificadores.
            hdist = 0;
            for (int i = 29; i >= 0; i--)
            {
                if (distLens[i] > 0)
                {
                    hdist = i + 1;
                    break;
                }
            }
            if (hdist == 0)
            {
                hdist = 1;
                distLens[0] = 1;
            }

            // REQ-06: Truncamento ótimo de HLIT até o último símbolo literal ativo
            hlit = 0;
            for (int i = 285; i >= 257; i--)
            {
                if (litLens[i] > 0)
                {
                    hlit = i + 1;
                    break;
                }
            }
            if (hlit < 257) hlit = 257;

            // Codificação RLE dos comprimentos literais e de distâncias
            rleSymbols = new List<int>();
            rleExtra = new List<int>();
            rleExtraBits = new List<int>();
            EncodeTreeLengthsRle(litLens, hlit, distLens, hdist, rleSymbols, rleExtra, rleExtraBits);

            long[] clFreqs = new long[19];
            for (int i = 0; i < rleSymbols.Count; i++)
            {
                clFreqs[rleSymbols[i]]++;
            }
            clLens = HuffmanTreeBuilder.BuildLengths(clFreqs, 7);

            // REQ-05: Truncamento ótimo de HCLEN até o último símbolo utilizado
            hclen = 0;
            for (int i = 18; i >= 0; i--)
            {
                if (clLens[CodeLengthOrder[i]] > 0)
                {
                    hclen = i + 1;
                    break;
                }
            }
            if (hclen < 4) hclen = 4;
        }

        private static long CalculateStoredCost(int dataLength, int currentBitPos)
        {
            if (dataLength > MaxBlockSize) return long.MaxValue;
            int pad = (8 - ((currentBitPos + 3) % 8)) % 8;
            return 3 + pad + 32 + ((long)dataLength * 8);
        }

        private static long CalculateFixedCost(Lz77TokenBuffer tokens)
        {
            long bits = 3; // BFINAL + BTYPE 01
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens.IsMatch(i))
                {
                    int len = tokens.GetLitOrLen(i);
                    int dist = tokens.GetDist(i);
                    int lenSym = LengthCodeMap[len];
                    bits += FixedLitLens[lenSym] + LengthExtraBitsMap[len];

                    int distSym = GetDistSymbol(dist);
                    bits += 5 + DistExtra[distSym];
                }
                else
                {
                    bits += FixedLitLens[tokens.GetLitOrLen(i)];
                }
            }
            bits += FixedLitLens[256]; // EOB
            return bits;
        }

        private static long CalculateDynamicCost(
            Lz77TokenBuffer tokens,
            int[] litLens,
            int[] distLens,
            List<int> rleSymbols,
            List<int> rleExtraBits,
            int[] clLens,
            int hlit,
            int hdist,
            int hclen)
        {
            long bits = 3; // BFINAL + BTYPE 10
            bits += 5 + 5 + 4; // HLIT (5) + HDIST (5) + HCLEN (4)
            bits += hclen * 3; // Comprimentos da árvore de comprimentos

            for (int i = 0; i < rleSymbols.Count; i++)
            {
                bits += clLens[rleSymbols[i]] + rleExtraBits[i];
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens.IsMatch(i))
                {
                    int len = tokens.GetLitOrLen(i);
                    int dist = tokens.GetDist(i);
                    int lenSym = LengthCodeMap[len];
                    bits += litLens[lenSym] + LengthExtraBitsMap[len];

                    int distSym = GetDistSymbol(dist);
                    bits += distLens[distSym] + DistExtra[distSym];
                }
                else
                {
                    bits += litLens[tokens.GetLitOrLen(i)];
                }
            }
            bits += litLens[256]; // EOB
            return bits;
        }

        private static void EncodeTreeLengthsRle(
            int[] litLens, int hlit,
            int[] distLens, int hdist,
            List<int> rleSymbols,
            List<int> rleExtra,
            List<int> rleExtraBits)
        {
            int total = hlit + hdist;
            int[] allLens = new int[total];
            Array.Copy(litLens, 0, allLens, 0, hlit);
            Array.Copy(distLens, 0, allLens, hlit, hdist);

            int idx = 0;
            while (idx < total)
            {
                int val = allLens[idx];
                if (val == 0)
                {
                    int run = 0;
                    while (idx + run < total && allLens[idx + run] == 0 && run < 138)
                    {
                        run++;
                    }

                    if (run >= 11)
                    {
                        rleSymbols.Add(18);
                        rleExtra.Add(run - 11);
                        rleExtraBits.Add(7);
                        idx += run;
                    }
                    else if (run >= 3)
                    {
                        rleSymbols.Add(17);
                        rleExtra.Add(run - 3);
                        rleExtraBits.Add(3);
                        idx += run;
                    }
                    else
                    {
                        rleSymbols.Add(0);
                        rleExtra.Add(0);
                        rleExtraBits.Add(0);
                        idx++;
                    }
                }
                else
                {
                    // REQ-04: Emite o literal antes de qualquer código de repetição 16.
                    // O símbolo 16 NUNCA é emitido no início da sequência.
                    int run = 1;
                    while (idx + run < total && allLens[idx + run] == val && run < 6)
                    {
                        run++;
                    }

                    rleSymbols.Add(val);
                    rleExtra.Add(0);
                    rleExtraBits.Add(0);
                    idx++;

                    if (run >= 4)
                    {
                        int repeat = run - 1; // 3..6 repetições
                        rleSymbols.Add(16);
                        rleExtra.Add(repeat - 3);
                        rleExtraBits.Add(2);
                        idx += repeat;
                    }
                }
            }
        }

        #endregion

        #region Emissão de Blocos Binários

        private static void EmitStoredBlock(byte[] rawData, int offset, int length, DeflateBitWriter writer, bool isFinal)
        {
            writer.WriteBits(isFinal ? 1 : 0, 1);
            writer.WriteBits(0, 2); // BTYPE 00 (Stored)
            writer.AlignToByte();

            ushort len = (ushort)length;
            ushort nlen = (ushort)(~len);

            writer.WriteByte((byte)(len & 0xFF));
            writer.WriteByte((byte)((len >> 8) & 0xFF));
            writer.WriteByte((byte)(nlen & 0xFF));
            writer.WriteByte((byte)((nlen >> 8) & 0xFF));

            writer.WriteBytes(rawData, offset, length);
        }

        private static void EmitFixedBlock(Lz77TokenBuffer tokens, DeflateBitWriter writer, bool isFinal)
        {
            writer.WriteBits(isFinal ? 1 : 0, 1);
            writer.WriteBits(1, 2); // BTYPE 01 (Fixed Huffman)

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens.IsMatch(i))
                {
                    int len = tokens.GetLitOrLen(i);
                    int dist = tokens.GetDist(i);
                    int lenSym = LengthCodeMap[len];
                    writer.WriteBits(FixedLitReversed[lenSym], FixedLitLens[lenSym]);
                    int extraLen = LengthExtraBitsMap[len];
                    if (extraLen > 0)
                    {
                        writer.WriteBits(LengthExtraValues[len], extraLen);
                    }

                    int distSym = GetDistSymbol(dist);
                    writer.WriteBits(FixedDistReversed[distSym], 5);
                    int extraDist = DistExtra[distSym];
                    if (extraDist > 0)
                    {
                        writer.WriteBits(dist - DistBase[distSym], extraDist);
                    }
                }
                else
                {
                    byte b = (byte)tokens.GetLitOrLen(i);
                    writer.WriteBits(FixedLitReversed[b], FixedLitLens[b]);
                }
            }

            writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]); // EOB
        }

        private static void EmitDynamicBlock(
            Lz77TokenBuffer tokens,
            int[] litLens,
            int[] distLens,
            int[] clLens,
            List<int> rleSymbols,
            List<int> rleExtra,
            List<int> rleExtraBits,
            int hlit,
            int hdist,
            int hclen,
            DeflateBitWriter writer,
            bool isFinal)
        {
            int[] litCodes, litReversed;
            HuffmanTreeBuilder.BuildCanonicalCodes(litLens, out litCodes, out litReversed);

            int[] distCodes, distReversed;
            HuffmanTreeBuilder.BuildCanonicalCodes(distLens, out distCodes, out distReversed);

            int[] clCodes, clReversed;
            HuffmanTreeBuilder.BuildCanonicalCodes(clLens, out clCodes, out clReversed);

            writer.WriteBits(isFinal ? 1 : 0, 1);
            writer.WriteBits(2, 2); // BTYPE 10 (Dynamic Huffman)

            writer.WriteBits(hlit - 257, 5);
            writer.WriteBits(hdist - 1, 5);
            writer.WriteBits(hclen - 4, 4);

            for (int i = 0; i < hclen; i++)
            {
                writer.WriteBits(clLens[CodeLengthOrder[i]], 3);
            }

            for (int i = 0; i < rleSymbols.Count; i++)
            {
                int sym = rleSymbols[i];
                writer.WriteBits(clReversed[sym], clLens[sym]);
                if (rleExtraBits[i] > 0)
                {
                    writer.WriteBits(rleExtra[i], rleExtraBits[i]);
                }
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens.IsMatch(i))
                {
                    int len = tokens.GetLitOrLen(i);
                    int dist = tokens.GetDist(i);
                    int lenSym = LengthCodeMap[len];
                    writer.WriteBits(litReversed[lenSym], litLens[lenSym]);
                    int extraLen = LengthExtraBitsMap[len];
                    if (extraLen > 0)
                    {
                        writer.WriteBits(LengthExtraValues[len], extraLen);
                    }

                    int distSym = GetDistSymbol(dist);
                    writer.WriteBits(distReversed[distSym], distLens[distSym]);
                    int extraDist = DistExtra[distSym];
                    if (extraDist > 0)
                    {
                        writer.WriteBits(dist - DistBase[distSym], extraDist);
                    }
                }
                else
                {
                    int b = tokens.GetLitOrLen(i);
                    writer.WriteBits(litReversed[b], litLens[b]);
                }
            }

            writer.WriteBits(litReversed[256], litLens[256]); // EOB
        }

        #endregion

        #region Casamento LZ77

        private static int GetDistSymbol(int dist)
        {
            for (int i = 29; i >= 0; i--)
            {
                if (dist >= DistBase[i]) return i;
            }
            return 0;
        }

        private static void RunLz77(byte[] data, int startPos, int blockLength, Lz77TokenBuffer tokens, int[] head, int[] prev)
        {
            int total = data.Length;
            int endPos = startPos + blockLength;
            int pos = startPos;

            while (pos < endPos)
            {
                int bestLen = 0;
                int bestDist = 0;

                if (pos + 2 < total)
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

                        // Clampa casamento ao limite do bloco atual
                        int limit = Math.Min(258, endPos - pos);
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
                    tokens.AddMatch(bestLen, bestDist);

                    for (int k = 1; k < bestLen; k++)
                    {
                        int p = pos + k;
                        if (p + 2 < total)
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
                    tokens.AddLiteral(data[pos]);
                    pos++;
                }
            }
        }

        #endregion
    }
}
