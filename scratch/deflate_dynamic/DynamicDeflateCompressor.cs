using System;
using System.Collections.Generic;
using System.IO;

namespace Lodino.Scratch.DeflateDynamic
{
    public enum DeflateMode
    {
        Auto = 0,
        Stored = 1,      // BTYPE 00
        Fixed = 2,       // BTYPE 01
        Dynamic = 3      // BTYPE 10
    }

    public class DeflateBlockDecision
    {
        public int ChosenBType { get; set; }
        public string ChosenBTypeName { get; set; }
        public long StoredCostBits { get; set; }
        public long FixedCostBits { get; set; }
        public long DynamicCostBits { get; set; }
        public int DynamicHeaderBits { get; set; }
        public double FixedVsStoredGain { get; set; }
        public double DynamicVsFixedGain { get; set; }
        public string DecisionRationale { get; set; }
    }

    /// <summary>
    /// BitWriter LSB-first compatível com RFC 1951.
    /// </summary>
    public class DynamicBitWriter
    {
        private readonly Stream _stream;
        private uint _buffer = 0;
        private int _bitCount = 0;
        private long _totalBitsWritten = 0;

        public long TotalBitsWritten { get { return _totalBitsWritten; } }

        public DynamicBitWriter(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            _stream = stream;
        }

        public void WriteBits(int value, int count)
        {
            if (count <= 0) return;
            _buffer |= ((uint)(value & ((1 << count) - 1))) << _bitCount;
            _bitCount += count;
            _totalBitsWritten += count;
            while (_bitCount >= 8)
            {
                _stream.WriteByte((byte)(_buffer & 0xFF));
                _buffer >>= 8;
                _bitCount -= 8;
            }
        }

        public void WriteByte(byte b)
        {
            WriteBits(b, 8);
        }

        public void FlushToByte()
        {
            if (_bitCount > 0)
            {
                _stream.WriteByte((byte)(_buffer & 0xFF));
                _totalBitsWritten += (8 - _bitCount);
                _buffer = 0;
                _bitCount = 0;
            }
        }
    }

    /// <summary>
    /// Token de LZ77: ou literal (IsMatch=false) ou casamento comprimento/distância (IsMatch=true).
    /// </summary>
    internal struct LzToken
    {
        public bool IsMatch;
        public int Literal; // 0..255
        public int Length;  // 3..258
        public int Distance;// 1..32768
    }

    /// <summary>
    /// Compressor DEFLATE puro em C# com suporte a Huffman Dinâmico (BTYPE 10),
    /// Fixo (BTYPE 01) e Stored (BTYPE 00), com cálculo analítico de custo e decisão ótima.
    /// </summary>
    public static class DynamicDeflateCompressor
    {
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

        static DynamicDeflateCompressor()
        {
            // 1. Árvore fixa de literais (RFC 1951 § 3.2.6)
            for (int i = 0; i <= 143; i++) { FixedLitLens[i] = 8; FixedLitCodes[i] = 0x30 + i; }
            for (int i = 144; i <= 255; i++) { FixedLitLens[i] = 9; FixedLitCodes[i] = 0x190 + (i - 144); }
            for (int i = 256; i <= 279; i++) { FixedLitLens[i] = 7; FixedLitCodes[i] = i - 256; }
            for (int i = 280; i <= 287; i++) { FixedLitLens[i] = 8; FixedLitCodes[i] = 0xC0 + (i - 280); }

            for (int i = 0; i < 288; i++)
            {
                int len = FixedLitLens[i];
                int code = FixedLitCodes[i];
                int rev = 0;
                for (int b = 0; b < len; b++) rev = (rev << 1) | ((code >> b) & 1);
                FixedLitReversed[i] = rev;
            }

            // 2. Árvore fixa de distâncias (5 bits canônicos 0..31)
            for (int i = 0; i < 32; i++)
            {
                FixedDistLens[i] = 5;
                FixedDistCodes[i] = i;
                int rev = 0;
                for (int b = 0; b < 5; b++) rev = (rev << 1) | ((i >> b) & 1);
                FixedDistReversed[i] = rev;
            }

            // 3. Mapeamento de comprimentos (3..258)
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
                int bLen = lengthBase[symIdx];
                int extraBits = lengthExtra[symIdx];
                int count = 1 << extraBits;
                for (int offset = 0; offset < count; offset++)
                {
                    int len = bLen + offset;
                    if (len <= 258)
                    {
                        LengthCodeMap[len] = sym;
                        LengthExtraBitsMap[len] = extraBits;
                        LengthExtraValues[len] = offset;
                    }
                }
            }
        }

        public static byte[] Compress(byte[] rawData, DeflateMode mode = DeflateMode.Auto)
        {
            DeflateBlockDecision decision;
            return Compress(rawData, mode, out decision);
        }

        public static byte[] Compress(byte[] rawData, DeflateMode mode, out DeflateBlockDecision decision)
        {
            if (rawData == null) rawData = new byte[0];

            using (var ms = new MemoryStream())
            {
                decision = CompressToStream(rawData, ms, mode);
                return ms.ToArray();
            }
        }

        public static DeflateBlockDecision EvaluateBlockCosts(byte[] data)
        {
            if (data == null) data = new byte[0];
            var tokens = TokenizeLz77(data);

            // 1. Custo Stored
            long storedBits = 3 + 5 + 32 + ((long)data.Length * 8); // BFINAL+BTYPE(3) + alinhamento(média 5 bits) + LEN/NLEN(32) + dados

            // 2. Custo Fixed
            long fixedBits = 3; // BFINAL=1, BTYPE=01
            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    fixedBits += FixedLitLens[tok.Literal];
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    fixedBits += FixedLitLens[lenSym] + LengthExtraBitsMap[tok.Length];
                    int distSym = GetDistanceSymbol(tok.Distance);
                    fixedBits += 5 + DistExtra[distSym];
                }
            }
            fixedBits += FixedLitLens[256]; // EOB

            // 3. Frequências e Custo Dynamic
            int[] litFreq = new int[286];
            int[] distFreq = new int[30];
            litFreq[256] = 1; // EOB obrigatório

            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    litFreq[tok.Literal]++;
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    litFreq[lenSym]++;
                    int distSym = GetDistanceSymbol(tok.Distance);
                    distFreq[distSym]++;
                }
            }

            int[] dynLitLens = BuildHuffmanLengths(litFreq, 15);
            int[] dynDistLens = BuildHuffmanLengths(distFreq, 15);

            // Garante pelo menos 1 código de distância (RFC 1951)
            int maxDist = 0;
            for (int i = 0; i < 30; i++) if (dynDistLens[i] > 0) maxDist = i;
            if (maxDist == 0 && dynDistLens[0] == 0) dynDistLens[0] = 1;

            int maxLit = 0;
            for (int i = 0; i < 286; i++) if (dynLitLens[i] > 0) maxLit = i;
            if (maxLit < 256) maxLit = 256;

            int hlit = Math.Max(0, maxLit - 257 + 1);
            int hdist = Math.Max(0, maxDist - 1 + 1);

            int totalCodes = (hlit + 257) + (hdist + 1);
            int[] allLengths = new int[totalCodes];
            for (int i = 0; i < hlit + 257; i++) allLengths[i] = dynLitLens[i];
            for (int i = 0; i < hdist + 1; i++) allLengths[hlit + 257 + i] = dynDistLens[i];

            List<int> rleSyms = new List<int>();
            List<int> rleExtras = new List<int>();
            List<int> rleExtraBits = new List<int>();
            EncodeCodeLengths(allLengths, rleSyms, rleExtras, rleExtraBits);

            int[] clFreq = new int[19];
            for (int i = 0; i < rleSyms.Count; i++) clFreq[rleSyms[i]]++;
            int[] clLens = BuildHuffmanLengths(clFreq, 7);

            int maxClenIdx = 3;
            for (int i = 0; i < 19; i++)
            {
                if (clLens[CodeLengthOrder[i]] > 0 && i > maxClenIdx) maxClenIdx = i;
            }
            int hclen = maxClenIdx - 3;

            long dynamicHeaderBits = 3 + 5 + 5 + 4 + ((hclen + 4) * 3);
            for (int i = 0; i < rleSyms.Count; i++)
            {
                dynamicHeaderBits += clLens[rleSyms[i]] + rleExtraBits[i];
            }

            long dynamicDataBits = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    dynamicDataBits += dynLitLens[tok.Literal];
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    dynamicDataBits += dynLitLens[lenSym] + LengthExtraBitsMap[tok.Length];
                    int distSym = GetDistanceSymbol(tok.Distance);
                    dynamicDataBits += dynDistLens[distSym] + DistExtra[distSym];
                }
            }
            dynamicDataBits += dynLitLens[256]; // EOB

            long dynamicTotalBits = dynamicHeaderBits + dynamicDataBits;

            var d = new DeflateBlockDecision
            {
                StoredCostBits = storedBits,
                FixedCostBits = fixedBits,
                DynamicCostBits = dynamicTotalBits,
                DynamicHeaderBits = (int)dynamicHeaderBits
            };

            // Decisão ótima
            if (storedBits <= fixedBits && storedBits <= dynamicTotalBits)
            {
                d.ChosenBType = 0;
                d.ChosenBTypeName = "Stored (BTYPE 00)";
                d.DecisionRationale = string.Format("Dados incompressíveis (alta entropia): Stored ({0} bits) < Fixed ({1} bits) e Dynamic ({2} bits)",
                    storedBits, fixedBits, dynamicTotalBits);
            }
            else if (dynamicTotalBits < fixedBits)
            {
                d.ChosenBType = 2;
                d.ChosenBTypeName = "Dynamic Huffman (BTYPE 10)";
                double gain = ((double)(fixedBits - dynamicTotalBits) / fixedBits) * 100.0;
                d.DynamicVsFixedGain = gain;
                d.DecisionRationale = string.Format("Árvore personalizada otimizada: Dynamic ({0} bits) economiza {1:F2}% em relação a Fixed ({2} bits)",
                    dynamicTotalBits, gain, fixedBits);
            }
            else
            {
                d.ChosenBType = 1;
                d.ChosenBTypeName = "Fixed Huffman (BTYPE 01)";
                double overheadPercent = ((double)(dynamicTotalBits - fixedBits) / fixedBits) * 100.0;
                d.DecisionRationale = string.Format("Payload pequeno/distribuição padrão: Cabeçalho dinâmico ({0} bits) não compensa; Fixed ({1} bits) é {2:F2}% mais eficiente que Dynamic ({3} bits)",
                    dynamicHeaderBits, fixedBits, overheadPercent, dynamicTotalBits);
            }

            return d;
        }

        private static DeflateBlockDecision CompressToStream(byte[] data, Stream outStream, DeflateMode mode)
        {
            var decision = EvaluateBlockCosts(data);
            int effectiveBType = decision.ChosenBType;

            if (mode == DeflateMode.Stored) effectiveBType = 0;
            else if (mode == DeflateMode.Fixed) effectiveBType = 1;
            else if (mode == DeflateMode.Dynamic) effectiveBType = 2;

            if (effectiveBType == 0)
            {
                CompressStored(data, outStream);
            }
            else if (effectiveBType == 1)
            {
                CompressFixed(data, outStream);
            }
            else
            {
                CompressDynamic(data, outStream);
            }

            return decision;
        }

        public static void CompressStored(byte[] rawData, Stream outputStream)
        {
            const int maxBlock = 65535;
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
                int blockSize = remaining > maxBlock ? maxBlock : remaining;
                remaining -= blockSize;
                bool isFinal = remaining == 0;

                byte header = (byte)(isFinal ? 0x01 : 0x00);
                outputStream.WriteByte(header);

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

        public static void CompressFixed(byte[] data, Stream outputStream)
        {
            var writer = new DynamicBitWriter(outputStream);
            // BFINAL = 1, BTYPE = 01 -> 3 bits: 1 | (1 << 1) = 3
            writer.WriteBits(3, 3);

            var tokens = TokenizeLz77(data);
            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    writer.WriteBits(FixedLitReversed[tok.Literal], FixedLitLens[tok.Literal]);
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    writer.WriteBits(FixedLitReversed[lenSym], FixedLitLens[lenSym]);
                    int extraLenBits = LengthExtraBitsMap[tok.Length];
                    if (extraLenBits > 0)
                    {
                        writer.WriteBits(LengthExtraValues[tok.Length], extraLenBits);
                    }

                    int distSym = GetDistanceSymbol(tok.Distance);
                    writer.WriteBits(FixedDistReversed[distSym], 5);
                    int extraDistBits = DistExtra[distSym];
                    if (extraDistBits > 0)
                    {
                        writer.WriteBits(tok.Distance - DistBase[distSym], extraDistBits);
                    }
                }
            }

            // EOB (símbolo 256)
            writer.WriteBits(FixedLitReversed[256], FixedLitLens[256]);
            writer.FlushToByte();
        }

        public static void CompressDynamic(byte[] data, Stream outputStream)
        {
            var writer = new DynamicBitWriter(outputStream);
            // BFINAL = 1, BTYPE = 10 -> 3 bits: 1 | (2 << 1) = 5
            writer.WriteBits(5, 3);

            var tokens = TokenizeLz77(data);

            // 1. Contagem de frequências
            int[] litFreq = new int[286];
            int[] distFreq = new int[30];
            litFreq[256] = 1; // EOB obrigatório

            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    litFreq[tok.Literal]++;
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    litFreq[lenSym]++;
                    int distSym = GetDistanceSymbol(tok.Distance);
                    distFreq[distSym]++;
                }
            }

            int[] dynLitLens = BuildHuffmanLengths(litFreq, 15);
            int[] dynDistLens = BuildHuffmanLengths(distFreq, 15);

            // RFC 1951: pelo menos 1 código de distância
            int maxDist = 0;
            for (int i = 0; i < 30; i++) if (dynDistLens[i] > 0) maxDist = i;
            if (maxDist == 0 && dynDistLens[0] == 0) dynDistLens[0] = 1;

            int maxLit = 0;
            for (int i = 0; i < 286; i++) if (dynLitLens[i] > 0) maxLit = i;
            if (maxLit < 256) maxLit = 256;

            int hlit = Math.Max(0, maxLit - 257 + 1);
            int hdist = Math.Max(0, maxDist - 1 + 1);

            int totalCodes = (hlit + 257) + (hdist + 1);
            int[] allLengths = new int[totalCodes];
            for (int i = 0; i < hlit + 257; i++) allLengths[i] = dynLitLens[i];
            for (int i = 0; i < hdist + 1; i++) allLengths[hlit + 257 + i] = dynDistLens[i];

            List<int> rleSyms = new List<int>();
            List<int> rleExtras = new List<int>();
            List<int> rleExtraBits = new List<int>();
            EncodeCodeLengths(allLengths, rleSyms, rleExtras, rleExtraBits);

            int[] clFreq = new int[19];
            for (int i = 0; i < rleSyms.Count; i++) clFreq[rleSyms[i]]++;
            int[] clLens = BuildHuffmanLengths(clFreq, 7);

            int maxClenIdx = 3;
            for (int i = 0; i < 19; i++)
            {
                if (clLens[CodeLengthOrder[i]] > 0 && i > maxClenIdx) maxClenIdx = i;
            }
            int hclen = maxClenIdx - 3;

            // Códigos canônicos invertidos
            int[] dynLitReversed = GenerateCanonicalReversedCodes(dynLitLens, 15);
            int[] dynDistReversed = GenerateCanonicalReversedCodes(dynDistLens, 15);
            int[] clReversed = GenerateCanonicalReversedCodes(clLens, 7);

            // 2. Emite cabeçalho dinâmico
            writer.WriteBits(hlit, 5);
            writer.WriteBits(hdist, 5);
            writer.WriteBits(hclen, 4);

            for (int i = 0; i < hclen + 4; i++)
            {
                writer.WriteBits(clLens[CodeLengthOrder[i]], 3);
            }

            for (int i = 0; i < rleSyms.Count; i++)
            {
                int sym = rleSyms[i];
                writer.WriteBits(clReversed[sym], clLens[sym]);
                if (rleExtraBits[i] > 0)
                {
                    writer.WriteBits(rleExtras[i], rleExtraBits[i]);
                }
            }

            // 3. Emite tokens LZ77 codificados
            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (!tok.IsMatch)
                {
                    writer.WriteBits(dynLitReversed[tok.Literal], dynLitLens[tok.Literal]);
                }
                else
                {
                    int lenSym = LengthCodeMap[tok.Length];
                    writer.WriteBits(dynLitReversed[lenSym], dynLitLens[lenSym]);
                    int extraLenBits = LengthExtraBitsMap[tok.Length];
                    if (extraLenBits > 0)
                    {
                        writer.WriteBits(LengthExtraValues[tok.Length], extraLenBits);
                    }

                    int distSym = GetDistanceSymbol(tok.Distance);
                    writer.WriteBits(dynDistReversed[distSym], dynDistLens[distSym]);
                    int extraDistBits = DistExtra[distSym];
                    if (extraDistBits > 0)
                    {
                        writer.WriteBits(tok.Distance - DistBase[distSym], extraDistBits);
                    }
                }
            }

            // 4. EOB (símbolo 256)
            writer.WriteBits(dynLitReversed[256], dynLitLens[256]);
            writer.FlushToByte();
        }

        private static List<LzToken> TokenizeLz77(byte[] data)
        {
            var tokens = new List<LzToken>();
            int n = data.Length;
            if (n == 0) return tokens;

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
                    tokens.Add(new LzToken { IsMatch = true, Length = bestLen, Distance = bestDist });

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
                    tokens.Add(new LzToken { IsMatch = false, Literal = data[pos] });
                    pos++;
                }
            }

            return tokens;
        }

        private static int GetDistanceSymbol(int dist)
        {
            for (int i = 29; i >= 0; i--)
            {
                if (dist >= DistBase[i]) return i;
            }
            return 0;
        }

        /// <summary>
        /// Codificação RLE exata para sequência de comprimentos de código conforme RFC 1951 § 3.2.7.
        /// </summary>
        private static void EncodeCodeLengths(int[] lengths, List<int> syms, List<int> extras, List<int> extraBits)
        {
            int n = lengths.Length;
            int i = 0;
            while (i < n)
            {
                int cur = lengths[i];
                if (cur == 0)
                {
                    int count = 1;
                    while (i + count < n && lengths[i + count] == 0 && count < 138)
                    {
                        count++;
                    }

                    if (count >= 11)
                    {
                        syms.Add(18);
                        extras.Add(count - 11);
                        extraBits.Add(7);
                        i += count;
                    }
                    else if (count >= 3)
                    {
                        syms.Add(17);
                        extras.Add(count - 3);
                        extraBits.Add(3);
                        i += count;
                    }
                    else
                    {
                        syms.Add(0);
                        extras.Add(0);
                        extraBits.Add(0);
                        i++;
                    }
                }
                else
                {
                    // Emite literal
                    syms.Add(cur);
                    extras.Add(0);
                    extraBits.Add(0);
                    i++;

                    // Verifica se repete
                    int repeat = 0;
                    while (i + repeat < n && lengths[i + repeat] == cur && repeat < 6)
                    {
                        repeat++;
                    }

                    if (repeat >= 3)
                    {
                        syms.Add(16);
                        extras.Add(repeat - 3);
                        extraBits.Add(2);
                        i += repeat;
                    }
                }
            }
        }

        private class Node : IComparable<Node>
        {
            public int Symbol;
            public int Freq;
            public Node Left;
            public Node Right;

            public int CompareTo(Node other)
            {
                int cmp = this.Freq.CompareTo(other.Freq);
                if (cmp != 0) return cmp;
                return this.Symbol.CompareTo(other.Symbol);
            }
        }

        public static int[] BuildHuffmanLengths(int[] freq, int maxBits)
        {
            int n = freq.Length;
            int[] lengths = new int[n];

            var list = new List<Node>();
            for (int i = 0; i < n; i++)
            {
                if (freq[i] > 0)
                {
                    list.Add(new Node { Symbol = i, Freq = freq[i] });
                }
            }

            if (list.Count == 0)
            {
                return lengths;
            }

            if (list.Count == 1)
            {
                lengths[list[0].Symbol] = 1;
                return lengths;
            }

            // Constrói árvore de Huffman padrão
            while (list.Count > 1)
            {
                list.Sort();
                Node left = list[0];
                Node right = list[1];
                list.RemoveAt(1);
                list.RemoveAt(0);

                Node parent = new Node
                {
                    Symbol = -1,
                    Freq = left.Freq + right.Freq,
                    Left = left,
                    Right = right
                };
                list.Add(parent);
            }

            Node root = list[0];
            TraverseDepth(root, 0, lengths);

            // Limita a maxBits garantindo Desigualdade de Kraft
            ClampLengths(lengths, maxBits);

            return lengths;
        }

        private static void TraverseDepth(Node node, int depth, int[] lengths)
        {
            if (node == null) return;
            if (node.Left == null && node.Right == null)
            {
                lengths[node.Symbol] = Math.Max(1, depth);
                return;
            }
            TraverseDepth(node.Left, depth + 1, lengths);
            TraverseDepth(node.Right, depth + 1, lengths);
        }

        private static void ClampLengths(int[] lengths, int maxBits)
        {
            int n = lengths.Length;
            for (int i = 0; i < n; i++)
            {
                if (lengths[i] > maxBits) lengths[i] = maxBits;
            }

            // Verifica Kraft
            long totalKraft = 0;
            long maxKraft = 1L << maxBits;
            for (int i = 0; i < n; i++)
            {
                if (lengths[i] > 0)
                {
                    totalKraft += (1L << (maxBits - lengths[i]));
                }
            }

            while (totalKraft > maxKraft)
            {
                // Encontra menor comprimento > 0 e incrementa
                int minIdx = -1;
                int minVal = int.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (lengths[i] > 0 && lengths[i] < minVal && lengths[i] < maxBits)
                    {
                        minVal = lengths[i];
                        minIdx = i;
                    }
                }
                if (minIdx == -1) break;
                totalKraft -= (1L << (maxBits - lengths[minIdx]));
                lengths[minIdx]++;
                totalKraft += (1L << (maxBits - lengths[minIdx]));
            }

            // Se totalKraft < maxKraft, a árvore de prefixos ficou incompleta (under-subscribed).
            // Decodificadores rigorosos como zlib (inftrees.c / bsdtar / libarchive) rejeitam árvores
            // incompletas com Z_DATA_ERROR ("invalid code lengths set").
            // Equalizamos para que a soma de Kraft seja exatamente 1.0 (totalKraft == maxKraft).
            int nonZeroCount = 0;
            for (int i = 0; i < n; i++) if (lengths[i] > 0) nonZeroCount++;

            if (nonZeroCount > 1)
            {
                while (totalKraft < maxKraft)
                {
                    int bestIdx = -1;
                    int maxVal = -1;
                    for (int i = 0; i < n; i++)
                    {
                        if (lengths[i] > 1)
                        {
                            long step = 1L << (maxBits - lengths[i]);
                            if (totalKraft + step <= maxKraft)
                            {
                                if (lengths[i] > maxVal)
                                {
                                    maxVal = lengths[i];
                                    bestIdx = i;
                                }
                            }
                        }
                    }
                    if (bestIdx == -1) break;
                    totalKraft += (1L << (maxBits - lengths[bestIdx]));
                    lengths[bestIdx]--;
                }
            }
        }

        public static int[] GenerateCanonicalReversedCodes(int[] lengths, int maxBits)
        {
            int n = lengths.Length;
            int[] blCount = new int[maxBits + 1];
            for (int i = 0; i < n; i++)
            {
                if (lengths[i] > 0) blCount[lengths[i]]++;
            }

            int code = 0;
            blCount[0] = 0;
            int[] nextCode = new int[maxBits + 1];
            for (int bits = 1; bits <= maxBits; bits++)
            {
                code = (code + blCount[bits - 1]) << 1;
                nextCode[bits] = code;
            }

            int[] reversedCodes = new int[n];
            for (int i = 0; i < n; i++)
            {
                int len = lengths[i];
                if (len > 0)
                {
                    int c = nextCode[len]++;
                    int rev = 0;
                    for (int b = 0; b < len; b++)
                    {
                        rev = (rev << 1) | ((c >> b) & 1);
                    }
                    reversedCodes[i] = rev;
                }
            }

            return reversedCodes;
        }
    }
}
