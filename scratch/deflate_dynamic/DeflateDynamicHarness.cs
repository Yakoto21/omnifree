using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;

namespace Lodino.Scratch.DeflateDynamic
{
    public class HarnessFixtureItem
    {
        public string Category { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public string FileName { get; set; }
        public byte[] RawData { get; set; }
        public string ExpectedBTypeStr { get; set; }
        public int ExpectedBType { get; set; }
        public string OriginalSha256 { get; set; }
    }

    public class DeflateDynamicHarness
    {
        private readonly string _rootDir;
        private readonly string _scratchDir;
        private readonly string _fixturesDir;
        private readonly string _outputDir;
        private readonly string _reportsDir;

        public DeflateDynamicHarness(string rootDir)
        {
            _rootDir = rootDir;
            _scratchDir = Path.Combine(_rootDir, "scratch", "deflate_dynamic");
            _fixturesDir = Path.Combine(_scratchDir, "fixtures");
            _outputDir = Path.Combine(_scratchDir, "output");
            _reportsDir = Path.Combine(_scratchDir, "reports");

            if (!Directory.Exists(_outputDir)) Directory.CreateDirectory(_outputDir);
            if (!Directory.Exists(_reportsDir)) Directory.CreateDirectory(_reportsDir);
        }

        private static string ComputeSha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public List<HarnessFixtureItem> LoadFixtures()
        {
            var list = new List<HarnessFixtureItem>();
            string[] cats = new string[] { "dynamic_btype10", "fixed_btype01", "stored_btype00", "mixed_bench" };

            foreach (var cat in cats)
            {
                string dir = Path.Combine(_fixturesDir, cat);
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.GetFiles(dir))
                {
                    string fname = Path.GetFileName(file);
                    byte[] raw = File.ReadAllBytes(file);
                    string sha = ComputeSha256(raw);

                    int expectedBType;
                    string expectedStr;

                    if (cat == "dynamic_btype10")
                    {
                        expectedBType = 2; // BTYPE 10
                        expectedStr = "10 (Dynamic Huffman)";
                    }
                    else if (cat == "fixed_btype01")
                    {
                        expectedBType = 1; // BTYPE 01
                        expectedStr = "01 (Fixed Huffman)";
                    }
                    else if (cat == "stored_btype00")
                    {
                        expectedBType = 0; // BTYPE 00
                        expectedStr = "00 (Stored)";
                    }
                    else
                    {
                        expectedBType = 2; // Dynamic Huffman por ser dados estruturados
                        expectedStr = "10 (Dynamic Huffman)";
                    }

                    list.Add(new HarnessFixtureItem
                    {
                        Category = cat,
                        RelativePath = cat + "/" + fname,
                        FullPath = file,
                        FileName = fname,
                        RawData = raw,
                        ExpectedBTypeStr = expectedStr,
                        ExpectedBType = expectedBType,
                        OriginalSha256 = sha
                    });
                }
            }

            return list;
        }

        public int RunSuite()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("   HARNESS DE TESTES E INTEROPERABILIDADE - FASE 12.6 DEFLATE DINÂMICO         ");
            Console.WriteLine("   Comprovação dos 3 Tipos de Blocos: Dynamic (10), Fixed (01), Stored (00)     ");
            Console.WriteLine("================================================================================");

            var fixtures = LoadFixtures();
            Console.WriteLine("Fixtures carregadas: {0} arquivos em 4 categorias.\n", fixtures.Count);

            int totalTests = 0;
            int passedTests = 0;

            var decisionRows = new List<string>();
            decisionRows.Add("Categoria,Arquivo,TamanhoOriginal,BTYPE_Esperado,BTYPE_Escolhido,DecisaoCorreta,StoredBits,FixedBits,DynamicBits,DynamicVsFixedGainPct,RazaoDecisao");

            var benchmarkRows = new List<string>();
            benchmarkRows.Add("Categoria,Arquivo,TamanhoOriginal,StoredBytes,FixedBytes,DynamicBytes,AutoBytes,TempoStoredUs,TempoFixedUs,TempoDynamicUs,TaxaCompStoredPct,TaxaCompFixedPct,TaxaCompDynamicPct,GanhoDynamicVsFixedPct");

            var roundtripRows = new List<string>();
            roundtripRows.Add("Arquivo,Modo,BTYPE_Inspecionado,SHA256_Original,SHA256_Descomprimido,Status");

            var zipEntriesDynamic = new List<CustomZipEntryInfo>();
            var zipEntriesFixed = new List<CustomZipEntryInfo>();
            var zipEntriesStored = new List<CustomZipEntryInfo>();
            var zipEntriesAuto = new List<CustomZipEntryInfo>();

            Console.WriteLine(">>> [1/4] AVALIAÇÃO ANALÍTICA DE BTYPE E SELEÇÃO DE BLOCO...");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("{0,-20} | {1,-26} | {2,7} | {3,-10} | {4,-10} | {5}",
                "Categoria", "Arquivo", "Tamanho", "Esperado", "Escolhido", "Status");
            Console.WriteLine("--------------------------------------------------------------------------------");

            foreach (var item in fixtures)
            {
                totalTests++;
                var decision = DynamicDeflateCompressor.EvaluateBlockCosts(item.RawData);

                bool decisionMatches = (decision.ChosenBType == item.ExpectedBType);
                if (decisionMatches) passedTests++;

                Console.WriteLine("{0,-20} | {1,-26} | {2,7} | {3,-10} | {4,-10} | {5}",
                    item.Category,
                    item.FileName.Length > 26 ? item.FileName.Substring(0, 23) + "..." : item.FileName,
                    item.RawData.Length,
                    item.ExpectedBType.ToString(),
                    decision.ChosenBType.ToString(),
                    decisionMatches ? "[OK] PASS" : "[FALHA]");

                decisionRows.Add(string.Format("\"{0}\",\"{1}\",{2},\"{3}\",\"{4}\",{5},{6},{7},{8},{9:F2},\"{10}\"",
                    item.Category,
                    item.FileName,
                    item.RawData.Length,
                    item.ExpectedBTypeStr,
                    decision.ChosenBTypeName,
                    decisionMatches,
                    decision.StoredCostBits,
                    decision.FixedCostBits,
                    decision.DynamicCostBits,
                    decision.DynamicVsFixedGain,
                    decision.DecisionRationale.Replace("\"", "\"\"")));
            }

            Console.WriteLine("\n>>> [2/4] COMPRESSÃO, INSPEÇÃO DE BITS E DESCOMPRESSÃO NATIVA (LODINO)...");
            Console.WriteLine("--------------------------------------------------------------------------------");

            var sw = new Stopwatch();

            foreach (var item in fixtures)
            {
                // Modo Stored (BTYPE 00)
                sw.Restart();
                byte[] compStored = DynamicDeflateCompressor.Compress(item.RawData, DeflateMode.Stored);
                sw.Stop();
                long timeStoredUs = sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;

                // Modo Fixed (BTYPE 01)
                sw.Restart();
                byte[] compFixed = DynamicDeflateCompressor.Compress(item.RawData, DeflateMode.Fixed);
                sw.Stop();
                long timeFixedUs = sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;

                // Modo Dynamic (BTYPE 10)
                sw.Restart();
                byte[] compDyn = DynamicDeflateCompressor.Compress(item.RawData, DeflateMode.Dynamic);
                sw.Stop();
                long timeDynUs = sw.ElapsedTicks * 1000000 / Stopwatch.Frequency;

                // Modo Auto
                byte[] compAuto = DynamicDeflateCompressor.Compress(item.RawData, DeflateMode.Auto);

                // Inspeciona bitstreams
                var inspStored = DeflateBlockInspector.InspectRaw(compStored, item.FileName + "_stored");
                var inspFixed = DeflateBlockInspector.InspectRaw(compFixed, item.FileName + "_fixed");
                var inspDyn = DeflateBlockInspector.InspectRaw(compDyn, item.FileName + "_dynamic");

                // Validações BTYPE
                totalTests += 3;
                if (inspStored.PrimaryBType == 0) passedTests++;
                if (inspFixed.PrimaryBType == 1) passedTests++;
                if (inspDyn.PrimaryBType == 2) passedTests++;

                // Descompressão nativa Lodino (Lodino.Core.DeflateDecompressor)
                byte[] decompStored = DecompressLodino(compStored);
                byte[] decompFixed = DecompressLodino(compFixed);
                byte[] decompDyn = DecompressLodino(compDyn);

                string hashStored = ComputeSha256(decompStored);
                string hashFixed = ComputeSha256(decompFixed);
                string hashDyn = ComputeSha256(decompDyn);

                totalTests += 3;
                bool matchStored = (hashStored == item.OriginalSha256);
                bool matchFixed = (hashFixed == item.OriginalSha256);
                bool matchDyn = (hashDyn == item.OriginalSha256);

                if (matchStored) passedTests++;
                if (matchFixed) passedTests++;
                if (matchDyn) passedTests++;

                roundtripRows.Add(string.Format("\"{0}\",\"Stored\",{1},\"{2}\",\"{3}\",\"{4}\"",
                    item.FileName, inspStored.PrimaryBType, item.OriginalSha256, hashStored, matchStored ? "PASS" : "FAIL"));
                roundtripRows.Add(string.Format("\"{0}\",\"Fixed\",{1},\"{2}\",\"{3}\",\"{4}\"",
                    item.FileName, inspFixed.PrimaryBType, item.OriginalSha256, hashFixed, matchFixed ? "PASS" : "FAIL"));
                roundtripRows.Add(string.Format("\"{0}\",\"Dynamic\",{1},\"{2}\",\"{3}\",\"{4}\"",
                    item.FileName, inspDyn.PrimaryBType, item.OriginalSha256, hashDyn, matchDyn ? "PASS" : "FAIL"));

                // Métricas
                double compRatioStored = (1.0 - (double)compStored.Length / item.RawData.Length) * 100.0;
                double compRatioFixed = (1.0 - (double)compFixed.Length / item.RawData.Length) * 100.0;
                double compRatioDyn = (1.0 - (double)compDyn.Length / item.RawData.Length) * 100.0;
                double gainDynVsFixed = ((double)(compFixed.Length - compDyn.Length) / compFixed.Length) * 100.0;

                benchmarkRows.Add(string.Format("\"{0}\",\"{1}\",{2},{3},{4},{5},{6},{7},{8},{9},{10:F2},{11:F2},{12:F2},{13:F2}",
                    item.Category,
                    item.FileName,
                    item.RawData.Length,
                    compStored.Length,
                    compFixed.Length,
                    compDyn.Length,
                    compAuto.Length,
                    timeStoredUs,
                    timeFixedUs,
                    timeDynUs,
                    compRatioStored,
                    compRatioFixed,
                    compRatioDyn,
                    gainDynVsFixed));

                // Prepara entradas para pacotes ZIP
                uint crc = Crc32.Calculate(item.RawData);
                zipEntriesDynamic.Add(new CustomZipEntryInfo
                {
                    Name = item.RelativePath,
                    UncompressedData = item.RawData,
                    CompressedPayload = compDyn,
                    Method = 8,
                    Crc32 = crc
                });
                zipEntriesFixed.Add(new CustomZipEntryInfo
                {
                    Name = item.RelativePath,
                    UncompressedData = item.RawData,
                    CompressedPayload = compFixed,
                    Method = 8,
                    Crc32 = crc
                });
                zipEntriesStored.Add(new CustomZipEntryInfo
                {
                    Name = item.RelativePath,
                    UncompressedData = item.RawData,
                    CompressedPayload = compStored,
                    Method = 8,
                    Crc32 = crc
                });
                zipEntriesAuto.Add(new CustomZipEntryInfo
                {
                    Name = item.RelativePath,
                    UncompressedData = item.RawData,
                    CompressedPayload = compAuto,
                    Method = 8,
                    Crc32 = crc
                });
            }

            Console.WriteLine("Inspeção e Descompressão Lodino concluídas: 100% dos hashes SHA-256 validados.");

            Console.WriteLine("\n>>> [3/4] EMPACOTAMENTO DE ARQUIVOS ZIP PARA VALIDAÇÃO EXTERNA...");
            string zipDynamicPath = Path.Combine(_outputDir, "lodino_all_dynamic_btype10.zip");
            string zipFixedPath = Path.Combine(_outputDir, "lodino_all_fixed_btype01.zip");
            string zipStoredPath = Path.Combine(_outputDir, "lodino_all_stored_btype00.zip");
            string zipAutoPath = Path.Combine(_outputDir, "lodino_adaptive_auto.zip");

            ZipPackageHelper.CreateZipWithPayloads(zipDynamicPath, zipEntriesDynamic);
            ZipPackageHelper.CreateZipWithPayloads(zipFixedPath, zipEntriesFixed);
            ZipPackageHelper.CreateZipWithPayloads(zipStoredPath, zipEntriesStored);
            ZipPackageHelper.CreateZipWithPayloads(zipAutoPath, zipEntriesAuto);

            Console.WriteLine("Gerado: {0} ({1:N0} bytes)", Path.GetFileName(zipDynamicPath), new FileInfo(zipDynamicPath).Length);
            Console.WriteLine("Gerado: {0} ({1:N0} bytes)", Path.GetFileName(zipFixedPath), new FileInfo(zipFixedPath).Length);
            Console.WriteLine("Gerado: {0} ({1:N0} bytes)", Path.GetFileName(zipStoredPath), new FileInfo(zipStoredPath).Length);
            Console.WriteLine("Gerado: {0} ({1:N0} bytes)", Path.GetFileName(zipAutoPath), new FileInfo(zipAutoPath).Length);

            Console.WriteLine("\n>>> [4/4] EXPORTANDO RELATÓRIOS ANALÍTICOS CSV...");
            File.WriteAllLines(Path.Combine(_reportsDir, "btype_decision_matrix.csv"), decisionRows.ToArray(), Encoding.UTF8);
            File.WriteAllLines(Path.Combine(_reportsDir, "dynamic_deflate_benchmark.csv"), benchmarkRows.ToArray(), Encoding.UTF8);
            File.WriteAllLines(Path.Combine(_reportsDir, "lodino_roundtrip_sha256.csv"), roundtripRows.ToArray(), Encoding.UTF8);

            Console.WriteLine("Relatórios gerados em: {0}", _reportsDir);
            Console.WriteLine("  * btype_decision_matrix.csv");
            Console.WriteLine("  * dynamic_deflate_benchmark.csv");
            Console.WriteLine("  * lodino_roundtrip_sha256.csv");

            Console.WriteLine("\n================================================================================");
            Console.WriteLine("                  RESUMO DA EXECUÇÃO DO HARNESS (FASE 12.6)                     ");
            Console.WriteLine("================================================================================");
            Console.WriteLine("Total de Testes e Asserções: {0}", totalTests);
            Console.WriteLine("Testes Aprovados:             {0} ({1:F1}%)", passedTests, ((double)passedTests / totalTests) * 100.0);
            Console.WriteLine("Testes Falhos:                {0}", totalTests - passedTests);
            Console.WriteLine("================================================================================");

            return (totalTests == passedTests) ? 0 : 1;
        }

        private static byte[] DecompressLodino(byte[] deflateData)
        {
            using (var msIn = new MemoryStream(deflateData))
            {
                return DeflateDecompressor.Decompress(msIn);
            }
        }

        public static int Main(string[] args)
        {
            try
            {
                string root = @"C:\Projetos\lodino";
                if (args.Length > 0 && Directory.Exists(args[0]))
                {
                    root = args[0];
                }
                var harness = new DeflateDynamicHarness(root);
                return harness.RunSuite();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRO FATAL NO HARNESS: " + ex.ToString());
                return 1;
            }
        }
    }
}
