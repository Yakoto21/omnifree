using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lodino.Scratch.DeflateDynamic
{
    /// <summary>
    /// Informações analíticas de um bloco DEFLATE (RFC 1951).
    /// </summary>
    public class DeflateBlockInfo
    {
        public int BlockIndex { get; set; }
        public long BitOffset { get; set; }
        public bool BFinal { get; set; }
        public int BType { get; set; }
        public string BTypeName { get; set; }
        public int HeaderBits { get; set; }
        public long CompressedBits { get; set; }
        public string Details { get; set; }

        public override string ToString()
        {
            return string.Format("Bloco #{0}: BFINAL={1}, BTYPE={2} ({3}), OffsetBit={4}, Detalhes: {5}",
                BlockIndex, BFinal ? 1 : 0, BType, BTypeName, BitOffset, Details);
        }
    }

    /// <summary>
    /// Relatório de inspeção analítica de um fluxo DEFLATE.
    /// </summary>
    public class DeflateInspectionReport
    {
        public string SourceName { get; set; }
        public long TotalStreamBytes { get; set; }
        public List<DeflateBlockInfo> Blocks { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public int PrimaryBType
        {
            get
            {
                if (Blocks == null || Blocks.Count == 0) return -1;
                return Blocks[0].BType;
            }
        }

        public string PrimaryBTypeName
        {
            get
            {
                if (Blocks == null || Blocks.Count == 0) return "UNKNOWN";
                return Blocks[0].BTypeName;
            }
        }

        public DeflateInspectionReport(string sourceName, long totalBytes)
        {
            SourceName = sourceName;
            TotalStreamBytes = totalBytes;
            Blocks = new List<DeflateBlockInfo>();
            IsValid = true;
        }
    }

    /// <summary>
    /// Inspecionador analítico de blocos DEFLATE (RFC 1951) e contêineres ZIP.
    /// Inspeciona o bitstream diretamente no nível de bits para comprovar programaticamente
    /// a emissão de BTYPE 00 (Stored), BTYPE 01 (Fixed Huffman) e BTYPE 10 (Dynamic Huffman).
    /// </summary>
    public class DeflateBlockInspector
    {
        private static readonly int[] CodeLengthOrder = new int[19]
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        };

        private readonly byte[] _data;
        private readonly int _length;
        private int _bytePos;
        private int _bitBuffer;
        private int _bitCount;
        private long _totalBitsRead;

        public DeflateBlockInspector(byte[] data)
        {
            _data = data ?? new byte[0];
            _length = _data.Length;
            _bytePos = 0;
            _bitBuffer = 0;
            _bitCount = 0;
            _totalBitsRead = 0;
        }

        public static DeflateInspectionReport InspectRaw(byte[] rawDeflate, string name = "raw_deflate")
        {
            var inspector = new DeflateBlockInspector(rawDeflate);
            return inspector.Inspect(name);
        }

        public static DeflateInspectionReport InspectZipEntry(string zipPath, string targetEntryName = null)
        {
            if (!File.Exists(zipPath))
            {
                throw new FileNotFoundException("Arquivo ZIP não encontrado.", zipPath);
            }

            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs))
            {
                while (fs.Position + 30 <= fs.Length)
                {
                    uint sig = reader.ReadUInt32();
                    if (sig == 0x02014B50 || sig == 0x06054B50)
                    {
                        // Central Directory ou EOCD alcançado
                        break;
                    }
                    if (sig != 0x04034B50)
                    {
                        // Pula byte a byte até encontrar próxima assinatura
                        fs.Seek(-3, SeekOrigin.Current);
                        continue;
                    }

                    ushort version = reader.ReadUInt16();
                    ushort flags = reader.ReadUInt16();
                    ushort method = reader.ReadUInt16();
                    uint modTime = reader.ReadUInt32();
                    uint crc32 = reader.ReadUInt32();
                    uint compSize = reader.ReadUInt32();
                    uint uncompSize = reader.ReadUInt32();
                    ushort nameLen = reader.ReadUInt16();
                    ushort extraLen = reader.ReadUInt16();

                    byte[] nameBytes = reader.ReadBytes(nameLen);
                    string entryName = Encoding.UTF8.GetString(nameBytes);
                    fs.Seek(extraLen, SeekOrigin.Current);

                    if (targetEntryName == null || string.Equals(entryName, targetEntryName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (method == 0) // Store
                        {
                            var r = new DeflateInspectionReport(entryName + " (ZIP Method 0 - Store)", compSize);
                            r.Blocks.Add(new DeflateBlockInfo
                            {
                                BlockIndex = 0,
                                BitOffset = 0,
                                BFinal = true,
                                BType = 0,
                                BTypeName = "STORED (ZIP Method 0)",
                                Details = string.Format("Armazenado sem compressão ZIP (Tamanho: {0} bytes)", compSize)
                            });
                            return r;
                        }
                        else if (method == 8) // DEFLATE
                        {
                            byte[] compData = reader.ReadBytes((int)compSize);
                            var r = InspectRaw(compData, entryName + " [DEFLATE Payload]");
                            return r;
                        }
                        else
                        {
                            var r = new DeflateInspectionReport(entryName, compSize);
                            r.IsValid = false;
                            r.ErrorMessage = "Método de compressão desconhecido no ZIP: " + method;
                            return r;
                        }
                    }
                    else
                    {
                        fs.Seek(compSize, SeekOrigin.Current);
                    }
                }
            }

            var notFound = new DeflateInspectionReport(targetEntryName ?? "First Entry", 0);
            notFound.IsValid = false;
            notFound.ErrorMessage = "Nenhuma entrada correspondente encontrada no arquivo ZIP.";
            return notFound;
        }

        public DeflateInspectionReport Inspect(string name)
        {
            var report = new DeflateInspectionReport(name, _length);
            if (_length == 0)
            {
                report.IsValid = false;
                report.ErrorMessage = "Fluxo vazio (0 bytes).";
                return report;
            }

            int blockIdx = 0;
            bool bfinal = false;

            try
            {
                while (!bfinal && HasMoreBits())
                {
                    long startBit = _totalBitsRead;
                    int bfinalBit = ReadBits(1);
                    bfinal = (bfinalBit == 1);

                    int btype = ReadBits(2);
                    string btypeName;
                    string details = "";

                    switch (btype)
                    {
                        case 0:
                            btypeName = "Stored (BTYPE 00)";
                            // Inspeciona cabeçalho Stored
                            AlignToByte();
                            if (_bytePos + 4 <= _length)
                            {
                                int len = _data[_bytePos] | (_data[_bytePos + 1] << 8);
                                int nlen = _data[_bytePos + 2] | (_data[_bytePos + 3] << 8);
                                bool nlenValid = ((ushort)~len == (ushort)nlen);
                                details = string.Format("LEN={0}, NLEN=0x{1:X4} (Válido: {2})", len, nlen, nlenValid);
                                _bytePos += 4;
                                _totalBitsRead += 32;
                                // Pula dados não comprimidos
                                _bytePos += len;
                                _totalBitsRead += (long)len * 8;
                            }
                            else
                            {
                                details = "Truncado no cabeçalho LEN/NLEN";
                            }
                            break;

                        case 1:
                            btypeName = "Fixed Huffman (BTYPE 01)";
                            details = "Árvore canônica pré-definida RFC 1951 § 3.2.6 (0 overhead de árvore)";
                            // Para inspeção básica, não precisamos decodificar todo o fluxo
                            break;

                        case 2:
                            btypeName = "Dynamic Huffman (BTYPE 10)";
                            // Inspeciona cabeçalho da árvore dinâmica
                            if (HasBits(14))
                            {
                                int hlit = ReadBits(5);
                                int hdist = ReadBits(5);
                                int hclen = ReadBits(4);
                                int totalLitCodes = hlit + 257;
                                int totalDistCodes = hdist + 1;
                                int totalClenCodes = hclen + 4;
                                details = string.Format("HLIT={0} ({1} códigos literais), HDIST={2} ({3} códigos dist), HCLEN={4} ({5} comprimentos)",
                                    hlit, totalLitCodes, hdist, totalDistCodes, hclen, totalClenCodes);
                            }
                            else
                            {
                                details = "Truncado no cabeçalho HLIT/HDIST/HCLEN";
                            }
                            break;

                        case 3:
                            btypeName = "Reservado / Inválido (BTYPE 11)";
                            details = "VIOLAÇÃO RFC 1951: BTYPE 11 é proibido";
                            report.IsValid = false;
                            report.ErrorMessage = details;
                            break;

                        default:
                            btypeName = "Desconhecido";
                            break;
                    }

                    long endBit = _totalBitsRead;
                    report.Blocks.Add(new DeflateBlockInfo
                    {
                        BlockIndex = blockIdx++,
                        BitOffset = startBit,
                        BFinal = bfinal,
                        BType = btype,
                        BTypeName = btypeName,
                        HeaderBits = (int)(endBit - startBit),
                        CompressedBits = (endBit - startBit),
                        Details = details
                    });

                    // Se não for Stored e tivermos chegado aqui na inspeção analítica inicial,
                    // consideramos o primeiro bloco representativo se bfinal == true
                    if (bfinal || btype == 1 || btype == 2)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                report.IsValid = false;
                report.ErrorMessage = "Erro durante inspeção analítica: " + ex.Message;
            }

            return report;
        }

        private bool HasMoreBits()
        {
            return _bytePos < _length || _bitCount > 0;
        }

        private bool HasBits(int count)
        {
            int availableBits = _bitCount + (_length - _bytePos) * 8;
            return availableBits >= count;
        }

        private int ReadBits(int count)
        {
            while (_bitCount < count)
            {
                if (_bytePos >= _length)
                {
                    throw new EndOfStreamException("Fim inesperado do fluxo ao ler bits DEFLATE.");
                }
                _bitBuffer |= (_data[_bytePos++] << _bitCount);
                _bitCount += 8;
            }

            int value = _bitBuffer & ((1 << count) - 1);
            _bitBuffer >>= count;
            _bitCount -= count;
            _totalBitsRead += count;
            return value;
        }

        private void AlignToByte()
        {
            int dropped = _bitCount % 8;
            _bitBuffer >>= dropped;
            _bitCount -= dropped;
            _totalBitsRead += dropped;
            // Se restarem bytes no buffer de bits, recua o ponteiro para mantê-los alinhados
            int remainingBytes = _bitCount / 8;
            _bytePos -= remainingBytes;
            _totalBitsRead -= (remainingBytes * 8);
            _bitBuffer = 0;
            _bitCount = 0;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("    LODINO DEFLATE BLOCK INSPECTOR - ANÁLISE BITSTREAM BTYPE (RFC 1951)        ");
            Console.WriteLine("================================================================================");

            if (args.Length == 0)
            {
                Console.WriteLine("Uso: DeflateBlockInspector.exe <arquivo.zip ou arquivo.deflate>");
                return;
            }

            string path = args[0];
            if (!File.Exists(path))
            {
                Console.WriteLine("Erro: Arquivo não encontrado: " + path);
                return;
            }

            if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Modo: Inspeção de Contêiner ZIP: " + Path.GetFileName(path));
                var report = InspectZipEntry(path);
                PrintReport(report);
            }
            else
            {
                Console.WriteLine("Modo: Inspeção de Fluxo DEFLATE Bruto: " + Path.GetFileName(path));
                byte[] raw = File.ReadAllBytes(path);
                var report = InspectRaw(raw, Path.GetFileName(path));
                PrintReport(report);
            }
        }

        private static void PrintReport(DeflateInspectionReport report)
        {
            Console.WriteLine("Origem:       {0}", report.SourceName);
            Console.WriteLine("Tamanho Total: {0} bytes", report.TotalStreamBytes);
            Console.WriteLine("Válido:       {0}", report.IsValid ? "SIM (RFC 1951 Compatível)" : "NÃO");
            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                Console.WriteLine("Erro:         {0}", report.ErrorMessage);
            }
            Console.WriteLine("Total Blocos: {0}", report.Blocks.Count);
            Console.WriteLine("BTYPE Principal: {0} ({1})", report.PrimaryBType, report.PrimaryBTypeName);
            Console.WriteLine("--------------------------------------------------------------------------------");
            foreach (var b in report.Blocks)
            {
                Console.WriteLine("  * {0}", b.ToString());
            }
            Console.WriteLine("================================================================================");
        }
    }
}
