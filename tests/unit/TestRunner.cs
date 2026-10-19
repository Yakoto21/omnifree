using System;
using System.IO;
using System.Text;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Data;
using Lodino.Core.Formats.Image;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;
using Lodino.Core.Registry;
using Lodino.Core.Safety;

namespace Lodino.Tests
{
    public class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — SUÍTE DE TESTES EXPANDIDA ");
            Console.WriteLine("  Módulo: Motor Piloto 1 (Dados Estruturados)    ");
            Console.WriteLine("=================================================\n");

            // --- CSV (RFC 4180 & Edge Cases) ---
            RunTest("CSV: Parser básico de colunas e linhas", Test_Csv_BasicParsing);
            RunTest("CSV: Conformidade RFC 4180 com vírgulas em campos citados", Test_Csv_Rfc4180_QuotesAndCommas);
            RunTest("CSV: Conformidade RFC 4180 com valores multilinha", Test_Csv_Rfc4180_Multiline);
            RunTest("CSV: Conformidade RFC 4180 com aspas duplas escapadas (\"\")", Test_Csv_Rfc4180_EscapedQuotes);
            RunTest("CSV: Round-trip de serialização e leitura", Test_Csv_WriterRoundTrip);
            RunTest("CSV: Suporte a quebras de linha mistas (CRLF, LF e CR)", Test_Csv_LineEndings_CRLF_LF_CR);
            RunTest("CSV: Campos vazios consecutivos e vírgulas finais", Test_Csv_EmptyFieldsAndTrailingCommas);
            RunTest("CSV: Remoção segura de UTF-8 BOM sem corromper primeiro caractere", Test_Csv_Utf8BomHandling);
            RunTest("CSV: Normalização de registros com contagem inconsistente de colunas", Test_Csv_InconsistentRecordLengths);
            RunTest("CSV: Resiliência contra aspas não fechadas no fim do arquivo", Test_Csv_Malformed_UnclosedQuotes);
            RunTest("CSV (Estrito RFC 4180): Rejeição de contagem inconsistente de colunas", Test_Csv_StrictRfc4180_RejectionOfInconsistentColumns);
            RunTest("CSV (Estrito RFC 4180): Rejeição de aspas não fechadas no EOF", Test_Csv_StrictRfc4180_RejectionOfUnclosedQuotes);
            RunTest("CSV (Estrito RFC 4180): Rejeição de caractere inesperado após aspas", Test_Csv_StrictRfc4180_RejectionOfCharAfterQuotes);

            // --- JSON (ECMA-404 & Edge Cases) ---
            RunTest("JSON: Parser de tipos primitivos, objetos e arrays", Test_Json_PrimitivesAndObjects);
            RunTest("JSON: Suporte a sequências de escape Unicode (\\uXXXX)", Test_Json_UnicodeEscapes);
            RunTest("JSON: Decodificação de pares substitutos Unicode (Surrogate Pairs / Emojis)", Test_Json_Unicode_SurrogatePairs);
            RunTest("JSON: Números grandes e notação científica com expoente", Test_Json_LargeNumbersAndScientificNotation);
            RunTest("JSON: Preservação de texto bruto para evitar perda numérica de precisão", Test_Json_NumericPrecisionLimitation);
            RunTest("JSON: Rejeição estrita de caracteres de controle (< 0x20) sem escape", Test_Json_ControlCharacterRejection);
            RunTest("JSON: Política de propriedades duplicadas (última ocorrência prevalece)", Test_Json_DuplicatePropertiesPolicy);
            RunTest("JSON: Rejeição de zeros à esquerda em inteiros (ex: 0123)", Test_Json_LeadingZerosRejection);
            RunTest("JSON: Rejeição estrita de sintaxe malformada e vírgula pendente", Test_Json_MalformedRejection);
            RunTest("JSON: Proteção contra ataque DoS de profundidade excessiva", Test_Json_DepthLimitSecurity);

            // --- INI (Dialeto Formal & Edge Cases) ---
            RunTest("INI: Parser de seções, propriedades e comentários", Test_Ini_SectionsAndComments);
            RunTest("INI: Propriedades globais sem seção", Test_Ini_GlobalProperties);
            RunTest("INI: Valores vazios e aparo de espaços em branco (trimming)", Test_Ini_EmptyValuesAndWhitespace);
            RunTest("INI: Suporte a comentários iniciados por '#' e ';'", Test_Ini_Comments_HashAndSemicolon);
            RunTest("INI: Política de chaves duplicadas na mesma seção", Test_Ini_DuplicateKeysPolicy);
            RunTest("INI: Valores entre aspas preservando caracteres de comentário", Test_Ini_QuotedValuesWithSpecialChars);

            // --- MATRIZ DE CONVERSÃO (TODAS AS 9 ROTAS COMBINATÓRIAS) ---
            RunTest("ROTA 1/9: CSV -> CSV (Normalização e Re-quoting)", Test_Route_CsvToCsv);
            RunTest("ROTA 2/9: CSV -> JSON (Tabela para Array de Objetos)", Test_Route_CsvToJson);
            RunTest("ROTA 3/9: CSV -> INI (Tabela para Seções e Propriedades)", Test_Route_CsvToIni);
            RunTest("ROTA 4/9: JSON -> JSON (Validação, Canonicalização e Pretty-Print)", Test_Route_JsonToJson);
            RunTest("ROTA 5/9: JSON -> CSV (Array de Objetos para Tabela Aplanada)", Test_Route_JsonToCsv);
            RunTest("ROTA 6/9: JSON -> INI (Objetos Hierárquicos para Seções INI)", Test_Route_JsonToIni);
            RunTest("ROTA 7/9: INI -> INI (Normalização Estrutural)", Test_Route_IniToIni);
            RunTest("ROTA 8/9: INI -> JSON (Seções para Objetos Aninhados)", Test_Route_IniToJson);
            RunTest("ROTA 9/9: INI -> CSV (Seções e Chaves para Tabela com 3 Colunas)", Test_Route_IniToCsv);

            // --- SEGURANÇA E REGISTRY ---
            RunTest("SEGURANÇA: Geração de caminhos únicos e proteção anti-sobrescrita", Test_SafeFileWriter_UniquePaths);
            RunTest("REGISTRY: Resolução dinâmica do motor pelo catálogo de formatos", Test_EngineRegistry_Resolution);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR PILOTO 2            ");
            Console.WriteLine("  Módulo: Motor Nativo de Imagens BMP Próprio    ");
            Console.WriteLine("=================================================\n");

            // --- BMP: Dimensões Pequenas & Padding de Scanline ---
            RunTest("BMP: 1x1 24-bit com padding de 1 byte por scanline", Test_Bmp_1x1_Padding1);
            RunTest("BMP: 2x2 24-bit com padding de 2 bytes por scanline", Test_Bmp_2x2_Padding2);
            RunTest("BMP: 3x3 24-bit com padding de 3 bytes por scanline", Test_Bmp_3x3_Padding3);
            RunTest("BMP: 4x4 24-bit alinhado sem padding (múltiplo de 4)", Test_Bmp_4x4_NoPadding);
            RunTest("BMP: 7x5 24-bit com dimensões ímpares e padding", Test_Bmp_7x5_OddDimensions);

            // --- BMP: Variantes 24-bit e 32-bit Round-trip ---
            RunTest("BMP: Round-trip de fidelidade 24-bit BGR", Test_Bmp_24bit_RoundTrip);
            RunTest("BMP: 32-bit BI_RGB semântica BGRX opaca com valores variados no byte alto", Test_Bmp_32bit_Bgrx_ArbitraryHighBytes_NormalizesOpaque);
            RunTest("BMP: Round-trip 32-bit BGRX (preservação estrita dos canais RGB com A=255)", Test_Bmp_32bit_Bgrx_RoundTrip);

            // --- BMP: Orientação de Linhas ---
            RunTest("BMP: Decodificação correta de Bottom-Up (padrão Windows)", Test_Bmp_BottomUp_Orientation);
            RunTest("BMP: Decodificação correta de Top-Down (altura negativa)", Test_Bmp_TopDown_Orientation);

            // --- BMP: Transformações de Imagem ---
            RunTest("BMP: Redimensionamento por Vizinho Mais Próximo (Nearest-Neighbor)", Test_Bmp_Resize_NearestNeighbor);
            RunTest("BMP: Redimensionamento Bilinear contínuo com interpolação ponderada", Test_Bmp_Resize_Bilinear);
            RunTest("BMP: Recorte de retângulo arbitrário com validação de limites", Test_Bmp_Crop_Arbitrary);
            RunTest("BMP: Recorte nos 9 quadrantes (top-left, center, bottom-right)", Test_Bmp_Crop_Quadrants);
            RunTest("BMP: Marca d'água textual com renderizador BitmapFont 8x8 proprietário", Test_Bmp_Watermark_Text);
            RunTest("BMP: Marca d'água por imagem com composição alfa (Porter-Duff Source-Over)", Test_Bmp_Watermark_Image_AlphaComposite);

            // --- BMP: Segurança e Rejeição Estrita de Variantes Não Suportadas ---
            RunTest("BMP (Segurança): Rejeição de assinatura mágica inválida (não 'BM')", Test_Bmp_Security_InvalidMagic);
            RunTest("BMP (Segurança): Rejeição de cabeçalho DIB não suportado (biSize != 40)", Test_Bmp_Security_UnsupportedHeaderSize);
            RunTest("BMP (Segurança): Rejeição de variantes indexadas com paleta (1, 4 e 8 bpp)", Test_Bmp_Security_RejectPalettes);
            RunTest("BMP (Segurança): Rejeição de variantes comprimidas (BI_RLE8, BI_RLE4, etc.)", Test_Bmp_Security_RejectCompression);
            RunTest("BMP (Segurança): Rejeição de dimensões zero ou negativas", Test_Bmp_Security_InvalidDimensions);
            RunTest("BMP (Segurança): Proteção contra dimensões excessivas (> 16.384 pixels)", Test_Bmp_Security_ExceedMaxDimension);
            RunTest("BMP (Segurança): Rejeição de arquivo truncado (dados de scanline insuficientes)", Test_Bmp_Security_TruncatedFile);
            RunTest("BMP (Segurança): Rejeição de bfOffBits apontando para fora do arquivo", Test_Bmp_Security_InvalidOffBits);

            // --- BMP: Fixtures Independentes e Validação Rigorosa (Fase B) ---
            RunTest("BMP (Fixture Independente): 24-bit com variantes de padding (0, 1, 2 e 3 bytes)", Test_Bmp_Independent_24bit_PaddingVariants_0_1_2_3);
            RunTest("BMP (Fixture Independente): Deslocamento bfOffBits personalizado com metadados", Test_Bmp_Independent_CustomOffBits_WithMetadataHeader);
            RunTest("BMP (Fixture Independente): Equivalência exata entre Bottom-Up e Top-Down", Test_Bmp_Independent_TopDown_vs_BottomUp_ExactDuality);
            RunTest("BMP (Fixture Independente): Truncamento de cabeçalho e corrupção de offsets", Test_Bmp_Independent_HeaderTruncation_And_SizeMismatch);
            RunTest("BMP (Fixture Independente): Detecção e bloqueio de overflow em dimensões", Test_Bmp_Independent_DimensionOverflow);

            // --- BMP: Integração do Motor BmpEngine e Registry ---
            RunTest("BMP (Engine): Inspeção de metadados via BmpEngine.Inspect", Test_Bmp_Engine_Inspect);
            RunTest("BMP (Engine): Pipeline completo de conversão e transformações via BmpEngine", Test_Bmp_Engine_ConvertPipeline);
            RunTest("REGISTRY: Resolução dinâmica do motor image-bmp para bmp -> bmp", Test_Bmp_Registry_Resolution);

            // =================================================
            //   LODINO NATIVE CORE — MOTOR PILOTO 3            
            //   Módulo: Motor Nativo de Imagens PNG Próprio    
            // =================================================

            RunTest("PNG (Checksum): Validação de vetores CRC-32 (ISO 3309)", Test_Png_Checksum_Crc32_Vectors);
            RunTest("PNG (Checksum): Validação de vetores Adler-32 (RFC 1950)", Test_Png_Checksum_Adler32_Vectors);
            RunTest("PNG (DEFLATE): Blocos não comprimidos (Stored BTYPE 00) e integridade NLEN", Test_Png_Deflate_StoredBlocks_SingleAndMulti);
            RunTest("PNG (DEFLATE): Descompressão com árvores de Huffman fixas (BTYPE 01)", Test_Png_Deflate_FixedHuffman_Decompression);
            RunTest("PNG (DEFLATE): Descompressão com árvores de Huffman dinâmicas (BTYPE 10)", Test_Png_Deflate_DynamicHuffman_Decompression);
            RunTest("PNG (Filtros): Reversibilidade e fidelidade dos 5 filtros (None, Sub, Up, Average, Paeth)", Test_Png_Filters_All5_RoundTrip);
            RunTest("PNG (Fixture Independente): 24-bit Truecolor RGB (ColorType 2) montado byte a byte", Test_Png_Independent_Raw_Rgb24_Fixture);
            RunTest("PNG (Fixture Independente): 32-bit Truecolor RGBA (ColorType 6) com preservação de alfa", Test_Png_Independent_Raw_Rgba32_Fixture);
            RunTest("PNG (IDAT Múltiplo): Descompressão de fluxo ZLIB fragmentado em múltiplos chunks", Test_Png_Multiple_Idat_Chunks);
            RunTest("PNG (Round-Trip): Fidelidade exata de codificação e decodificação RGB e RGBA", Test_Png_RoundTrip_Rgb_And_Rgba);
            RunTest("PNG (Encoder): Round-trip com seleção explícita dos 5 filtros de linha", Test_Png_Encoder_All5_Filters);
            RunTest("PNG (Segurança): Rejeição estrita de variantes não suportadas e dados corrompidos", Test_Png_Security_StrictRejections);
            RunTest("PNG (Engine): Inspeção completa de cabeçalho e metadados via PngEngine.Inspect", Test_Png_Engine_Inspect);
            RunTest("PNG (Engine): Pipeline completo de transformações (crop, resize, watermark)", Test_Png_Engine_Convert_Transforms);
            RunTest("PNG (Rotas Bidirecionais): Conversão PNG <-> BMP e resolução no EngineRegistry", Test_Png_Engine_Bidirectional_Bmp_Png);

            // =================================================
            //   LODINO NATIVE CORE — HARDENING BMP + PNG       
            //   Módulo: Segurança e Resiliência contra Ataques  
            // =================================================
            RunTest("HARDENING: Proteção contra Decompression Bomb (Zip Bomb em PNG)", Test_Hardening_DecompressionBomb_Protection);
            RunTest("HARDENING: Particionamento de IDAT em chunks consecutivos de 32 KB", Test_Hardening_Idat_Chunking_Generation);
            RunTest("HARDENING: Resiliência contra fluxos fragmentados (1-3 bytes por leitura)", Test_Hardening_Stream_Fragmentation_Resilience);
            RunTest("HARDENING: Prevenção contra overflow de inteiros em PixelBuffer.Crop", Test_Hardening_PixelBuffer_Crop_Overflow);
            RunTest("HARDENING: Rejeição de distância LZ77 anterior ao início do fluxo", Test_Hardening_Deflate_Lz77_Distance_Underflow);
            RunTest("HARDENING: Rejeição de códigos de repetição dinâmicos DEFLATE além dos limites", Test_Hardening_Deflate_Dynamic_Repeat_Overflow);
            RunTest("HARDENING: Rejeição de árvores de Huffman sobre-subscritas (Violação de Kraft)", Test_Hardening_Huffman_Oversubscribed_Tree);
            RunTest("HARDENING: Leitura segura de streams não-navegáveis (CanSeek = false) no BMP", Test_Hardening_Bmp_NonSeekable_Stream);
            RunTest("HARDENING: Rejeição de bytes espúrios após Adler-32 em fluxo ZLIB", Test_Hardening_Zlib_Trailing_Garbage);
            RunTest("HARDENING: Tolerância a bytes residuais (padding) no último IDAT (ISO/IEC 15948 §10.2)", Test_Hardening_Png_Idat_TrailingPadding_Allowed);
            RunTest("HARDENING: Rejeição de streams não-seekable no BMP que excedam o limite de entrada", Test_Hardening_Bmp_NonSeekable_Stream_LimitExceeded);

            Console.WriteLine("\n-------------------------------------------------");
            Console.WriteLine(string.Format("Resultado Final: {0} aprovados, {1} falhas.", _passed, _failed));
            Console.WriteLine("-------------------------------------------------");

            return _failed == 0 ? 0 : 1;
        }

        private static void RunTest(string testName, Action testAction)
        {
            try
            {
                testAction();
                _passed++;
                Console.WriteLine(string.Format("  [PASS] {0}", testName));
            }
            catch (Exception ex)
            {
                _failed++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("  [FAIL] {0}", testName));
                Console.WriteLine(string.Format("         Detalhe: {0}", ex.Message));
                Console.ResetColor();
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Falha na asserção: " + message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string field)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(string.Format("Esperado '{0}' para {1}, mas obteve '{2}'.", expected, field, actual));
            }
        }

        #region Casos de Teste CSV

        private static void Test_Csv_BasicParsing()
        {
            string csv = "id,name,role\r\n1,Alice,Engineer\r\n2,Bob,Architect";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(3, doc.ColumnCount, "ColumnCount");
            AssertEqual(2, doc.RowCount, "RowCount");
            AssertEqual("Alice", doc.Rows[0][1], "Row[0][1]");
            AssertEqual("Bob", doc.Rows[1][1], "Row[1][1]");
        }

        private static void Test_Csv_Rfc4180_QuotesAndCommas()
        {
            string csv = "col1,col2\r\nval1,\"Special, with comma\"\r\n";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(1, doc.RowCount, "RowCount");
            AssertEqual("Special, with comma", doc.Rows[0][1], "Campo com vírgula");
        }

        private static void Test_Csv_Rfc4180_Multiline()
        {
            string csv = "id,text\r\n1,\"Linha 1\r\nLinha 2\r\nLinha 3\"\r\n2,Normal";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(2, doc.RowCount, "RowCount");
            Assert(doc.Rows[0][1].Contains("Linha 2"), "Conteúdo multilinha preservado");
            AssertEqual("Normal", doc.Rows[1][1], "Próximo registro intacto");
        }

        private static void Test_Csv_Rfc4180_EscapedQuotes()
        {
            string csv = "id,text\r\n1,\"Ele disse: \"\"Olá mundo!\"\" ao entrar\"\r\n";
            var doc = new CsvParser().Parse(csv);

            AssertEqual("Ele disse: \"Olá mundo!\" ao entrar", doc.Rows[0][1], "Aspas escapadas tratadas");
        }

        private static void Test_Csv_WriterRoundTrip()
        {
            var original = new CsvDocument(new[] { "A", "B, with comma", "C" });
            original.AddRow(new[] { "1", "Valor \"com aspas\"", "Normal" });

            string serialized = new CsvWriter().WriteToString(original);
            var reloaded = new CsvParser().Parse(serialized);

            AssertEqual(original.Headers.Count, reloaded.Headers.Count, "Header count");
            AssertEqual("B, with comma", reloaded.Headers[1], "Header[1]");
            AssertEqual("Valor \"com aspas\"", reloaded.Rows[0][1], "Row[0][1]");
        }

        private static void Test_Csv_LineEndings_CRLF_LF_CR()
        {
            // Mixed CRLF, LF, and single CR line endings
            string mixed = "H1,H2\r\nR1C1,R1C2\nR2C1,R2C2\rR3C1,R3C2";
            var doc = new CsvParser().Parse(mixed);

            AssertEqual(3, doc.RowCount, "Leu corretamente 3 registros com quebras mistas");
            AssertEqual("R1C1", doc.Rows[0][0], "R1C1");
            AssertEqual("R2C1", doc.Rows[1][0], "R2C1");
            AssertEqual("R3C1", doc.Rows[2][0], "R3C1");
        }

        private static void Test_Csv_EmptyFieldsAndTrailingCommas()
        {
            string csv = "A,B,C,D\r\n,val,,end\r\n1,,,";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(2, doc.RowCount, "RowCount");
            AssertEqual("", doc.Rows[0][0], "Campo inicial vazio");
            AssertEqual("val", doc.Rows[0][1], "Campo preenchido");
            AssertEqual("", doc.Rows[0][2], "Campo intermediário vazio");
            AssertEqual("end", doc.Rows[0][3], "Campo final");
            AssertEqual("", doc.Rows[1][3], "Campo final vazio com vírgula terminal");
        }

        private static void Test_Csv_Utf8BomHandling()
        {
            string csvWithBom = "\uFEFFname,code\r\nItem,100";
            var doc = new CsvParser().Parse(csvWithBom);

            AssertEqual("name", doc.Headers[0], "BOM removido sem truncar caractere 'n'");
            AssertEqual("Item", doc.Rows[0][0], "Linha lida normalmente");
        }

        private static void Test_Csv_InconsistentRecordLengths()
        {
            // Row 1 has 2 cols, Row 2 has 4 cols (with 3-col header)
            string csv = "C1,C2,C3\r\nR1A,R1B\r\nR2A,R2B,R2C,R2D";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(4, doc.Headers.Count, "Cabeçalho expandido para acomodar coluna extra");
            AssertEqual("ExtraColumn_4", doc.Headers[3], "Nome gerado para coluna excedente");
            AssertEqual("", doc.Rows[0][2], "Linha curta preenchida com string vazia");
            AssertEqual("R2D", doc.Rows[1][3], "Dado da coluna excedente preservado");
        }

        private static void Test_Csv_Malformed_UnclosedQuotes()
        {
            string malformed = "id,text\r\n1,\"Unclosed quote at EOF";
            var doc = new CsvParser().Parse(malformed);

            AssertEqual(1, doc.RowCount, "Tratamento tolerante de aspas inacabadas em EOF");
            Assert(doc.Rows[0][1].Contains("Unclosed quote"), "Valor retido sem travar processo");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfInconsistentColumns()
        {
            string csv = "C1,C2,C3\r\nR1A,R1B\r\n";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(csv);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita linha com contagem divergente de colunas em modo estrito RFC 4180");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfUnclosedQuotes()
        {
            string malformed = "id,text\r\n1,\"Unclosed quote at EOF";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(malformed);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita aspas não fechadas no final do arquivo em modo estrito RFC 4180");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfCharAfterQuotes()
        {
            string invalid = "id,text\r\n1,\"Quoted\"ExtraChar,next\r\n";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita caractere espúrio após aspas de fechamento em modo estrito RFC 4180");
        }

        #endregion

        #region Casos de Teste JSON

        private static void Test_Json_PrimitivesAndObjects()
        {
            string json = "{\"name\":\"Lodino\",\"version\":2,\"active\":true,\"pi\":3.14,\"extra\":null,\"items\":[1,2,3]}";
            var node = new JsonParser().Parse(json);

            AssertEqual(JsonType.Object, node.Type, "Root Type");
            var obj = (JsonObject)node;
            AssertEqual("Lodino", obj["name"].AsString(), "name");
            AssertEqual(2L, obj["version"].AsLong(), "version");
            AssertEqual(true, obj["active"].AsBoolean(), "active");
            AssertEqual(3.14, obj["pi"].AsDouble(), "pi");
            AssertEqual(JsonType.Null, obj["extra"].Type, "extra is null");

            var items = (JsonArray)obj["items"];
            AssertEqual(3, items.Count, "items.Count");
            AssertEqual(2L, items[1].AsLong(), "items[1]");
        }

        private static void Test_Json_UnicodeEscapes()
        {
            string json = "{\"copyright\":\"\\u00A9 2026 Lodino\"}";
            var node = new JsonParser().Parse(json);
            var obj = (JsonObject)node;

            Assert(obj["copyright"].AsString().StartsWith("©"), "Unicode escape decodificado corretamente");
        }

        private static void Test_Json_Unicode_SurrogatePairs()
        {
            // \uD83D\uDE00 is UTF-16 surrogate pair for U+1F600 (Grinning Face emoji)
            string json = "{\"emoji\":\"\\uD83D\\uDE00\"}";
            var node = new JsonParser().Parse(json);
            string val = node["emoji"].AsString();

            AssertEqual(2, val.Length, "Surrogate pair retém os 2 chars UTF-16");
            AssertEqual(0x1F600, char.ConvertToUtf32(val, 0), "Código escalar Unicode 32-bit verificado");
        }

        private static void Test_Json_LargeNumbersAndScientificNotation()
        {
            string json = "{\"big\":1.23456789e+10,\"small\":-2.5E-4,\"maxSafe\":9007199254740991}";
            var node = new JsonParser().Parse(json);

            AssertEqual(12345678900.0, node["big"].AsDouble(), "Notação científica e+10");
            AssertEqual(-0.00025, node["small"].AsDouble(), "Notação científica E-4 negativa");
            AssertEqual(9007199254740991L, node["maxSafe"].AsLong(), "Inteiro máximo seguro (53 bits)");
        }

        private static void Test_Json_NumericPrecisionLimitation()
        {
            // Number exceeding 64-bit float precision (70 digits)
            string hugeNumber = "1234567890123456789012345678901234567890123456789012345678901234567890";
            string json = "{\"huge\":" + hugeNumber + "}";
            var node = new JsonParser().Parse(json);

            // JsonNumber retains RawText, preventing silent serialization precision loss
            string reserialized = new JsonWriter(false).WriteToString(node);
            Assert(reserialized.Contains(hugeNumber), "RawText preservado sem corrupção na serialização");
        }

        private static void Test_Json_ControlCharacterRejection()
        {
            // Literal unescaped newline (0x0A) inside string is forbidden by ECMA-404
            string invalid = "{\"key\": \"linha1\nlinha2\"}";
            bool threw = false;
            try
            {
                new JsonParser().Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita caractere de controle não escapado em string literal");
        }

        private static void Test_Json_DuplicatePropertiesPolicy()
        {
            string json = "{\"dup\":\"initial\",\"dup\":\"overridden\"}";
            var node = new JsonParser().Parse(json);

            // Documented policy: last occurrence wins
            AssertEqual("overridden", node["dup"].AsString(), "Última propriedade prevalece em chaves repetidas");
        }

        private static void Test_Json_LeadingZerosRejection()
        {
            string invalid = "{\"number\": 0123}";
            bool threw = false;
            try
            {
                new JsonParser().Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita zeros à esquerda em números inteiros (0123)");
        }

        private static void Test_Json_MalformedRejection()
        {
            string malformed = "{\"unclosed\": \"string without end, 123";
            bool threw = false;
            try
            {
                new JsonParser().Parse(malformed);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }

            Assert(threw, "Lança CorruptDataException em JSON malformado");
        }

        private static void Test_Json_DepthLimitSecurity()
        {
            var sb = new StringBuilder();
            int limit = SecurityLimits.MaxJsonNestingDepth + 5;
            for (int i = 0; i < limit; i++) sb.Append("[");
            sb.Append("1");
            for (int i = 0; i < limit; i++) sb.Append("]");

            bool caughtResourceLimit = false;
            try
            {
                new JsonParser().Parse(sb.ToString());
            }
            catch (ResourceLimitException)
            {
                caughtResourceLimit = true;
            }

            Assert(caughtResourceLimit, "Bloqueia profundidade de aninhamento excessiva para evitar DoS");
        }

        #endregion

        #region Casos de Teste INI

        private static void Test_Ini_SectionsAndComments()
        {
            string ini = "; Comentário geral\r\n[Server]\r\nhost = localhost ; inline comment\r\nport = 8080\r\n# Outro comentário\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("localhost", doc.Get("Server", "host"), "Server.host");
            AssertEqual("8080", doc.Get("Server", "port"), "Server.port");
        }

        private static void Test_Ini_GlobalProperties()
        {
            string ini = "app_name = Lodino\r\nversion = 1.0\r\n[Section1]\r\nk = v\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("Lodino", doc.Get(IniDocument.GlobalSectionName, "app_name"), "app_name global");
            AssertEqual("v", doc.Get("Section1", "k"), "Section1.k");
        }

        private static void Test_Ini_EmptyValuesAndWhitespace()
        {
            string ini = "[Config]\r\n  spaced_key   =   padded value   \r\nempty_key =\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("padded value", doc.Get("Config", "spaced_key"), "Trimming de espaços");
            AssertEqual("", doc.Get("Config", "empty_key"), "Valor vazio tratado como string vazia");
        }

        private static void Test_Ini_Comments_HashAndSemicolon()
        {
            string ini = "# Comentário cerquilha\r\n; Comentário ponto-e-vírgula\r\n[Sec]\r\nk1 = v1\r\n# linha comentada\r\nk2 = v2 ; inline semicolon\r\nk3 = v3 # inline hash";
            var doc = new IniParser().Parse(ini);

            AssertEqual("v1", doc.Get("Sec", "k1"), "k1");
            AssertEqual("v2", doc.Get("Sec", "k2"), "k2 sem comentário inline");
            AssertEqual("v3", doc.Get("Sec", "k3"), "k3 sem comentário inline");
        }

        private static void Test_Ini_DuplicateKeysPolicy()
        {
            string ini = "[Sec]\r\nrepeat = first\r\nrepeat = final\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("final", doc.Get("Sec", "repeat"), "Última atribuição prevalece");
        }

        private static void Test_Ini_QuotedValuesWithSpecialChars()
        {
            string ini = "[Sec]\r\ncommented = \"value with # and ; inside\"\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("value with # and ; inside", doc.Get("Sec", "commented"), "Caracteres especiais preservados dentro de aspas");
        }

        #endregion

        #region Casos de Teste das 9 Rotas de Conversão

        private static void Test_Route_CsvToCsv()
        {
            var engine = new StructuredDataEngine();
            string csv = "id,name\r\n1,\"Alice, Senior\"";
            string result = engine.ConvertString(csv, "csv", "csv");

            var reloaded = new CsvParser().Parse(result);
            AssertEqual(1, reloaded.RowCount, "Linha preservada");
            AssertEqual("Alice, Senior", reloaded.Rows[0][1], "Campo citado recodificado com segurança");
        }

        private static void Test_Route_CsvToJson()
        {
            var engine = new StructuredDataEngine();
            string csv = "id,name\r\n1,Alpha\r\n2,Beta";
            string json = engine.ConvertString(csv, "csv", "json");

            var parsedJson = new JsonParser().Parse(json);
            AssertEqual(JsonType.Array, parsedJson.Type, "Result is JsonArray");
            var arr = (JsonArray)parsedJson;
            AssertEqual(2, arr.Count, "Array has 2 items");
            AssertEqual("Alpha", arr[0]["name"].AsString(), "arr[0].name");
        }

        private static void Test_Route_CsvToIni()
        {
            var engine = new StructuredDataEngine();
            string csv = "section,ip,status\r\nServerA,10.0.0.1,up\r\nServerB,10.0.0.2,down";
            string ini = engine.ConvertString(csv, "csv", "ini");

            var parsed = new IniParser().Parse(ini);
            AssertEqual("10.0.0.1", parsed.Get("ServerA", "ip"), "ServerA.ip");
            AssertEqual("down", parsed.Get("ServerB", "status"), "ServerB.status");
        }

        private static void Test_Route_JsonToJson()
        {
            var engine = new StructuredDataEngine();
            string json = "{\"compact\":1,\"valid\":true}";
            string pretty = engine.ConvertString(json, "json", "json");

            var parsed = new JsonParser().Parse(pretty);
            AssertEqual(1L, parsed["compact"].AsLong(), "Valor preservado");
            AssertEqual(true, parsed["valid"].AsBoolean(), "Booleano preservado");
            Assert(pretty.Contains("\n"), "Reformatado com indentação");
        }

        private static void Test_Route_JsonToCsv()
        {
            var engine = new StructuredDataEngine();
            string json = "[{\"id\":\"10\",\"name\":\"Widget\"},{\"id\":\"20\",\"name\":\"Gadget\"}]";
            string csv = engine.ConvertString(json, "json", "csv");

            var parsedCsv = new CsvParser().Parse(csv);
            AssertEqual(2, parsedCsv.RowCount, "CSV has 2 rows");
            AssertEqual("Widget", parsedCsv.Rows[0][1], "Row[0][1]");
        }

        private static void Test_Route_JsonToIni()
        {
            var engine = new StructuredDataEngine();
            string json = "{\"Database\":{\"Port\":\"5432\",\"Host\":\"localhost\"}}";
            string ini = engine.ConvertString(json, "json", "ini");

            var parsed = new IniParser().Parse(ini);
            AssertEqual("5432", parsed.Get("Database", "Port"), "Port");
            AssertEqual("localhost", parsed.Get("Database", "Host"), "Host");
        }

        private static void Test_Route_IniToIni()
        {
            var engine = new StructuredDataEngine();
            string ini = "[Config]\r\nk = v\r\n";
            string res = engine.ConvertString(ini, "ini", "ini");

            var parsed = new IniParser().Parse(res);
            AssertEqual("v", parsed.Get("Config", "k"), "Config.k");
        }

        private static void Test_Route_IniToJson()
        {
            var engine = new StructuredDataEngine();
            string ini = "[App]\r\nName = Lodino\r\nMode = Fast\r\n";
            string json = engine.ConvertString(ini, "ini", "json");

            var parsed = new JsonParser().Parse(json);
            AssertEqual("Lodino", parsed["App"]["Name"].AsString(), "parsed[App][Name]");
        }

        private static void Test_Route_IniToCsv()
        {
            var engine = new StructuredDataEngine();
            string ini = "[Database]\r\nport = 5432\r\n";
            string csv = engine.ConvertString(ini, "ini", "csv");

            var parsed = new CsvParser().Parse(csv);
            AssertEqual("Database", parsed.Rows[0][0], "Coluna Section");
            AssertEqual("port", parsed.Rows[0][1], "Coluna Key");
            AssertEqual("5432", parsed.Rows[0][2], "Coluna Value");
        }

        #endregion

        #region Testes de Infraestrutura e Segurança

        private static void Test_SafeFileWriter_UniquePaths()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string path1 = SafeFileWriter.GetUniqueOutputPath(tempDir, "relatorio", "csv");
                File.WriteAllText(path1, "conteudo 1");

                string path2 = SafeFileWriter.GetUniqueOutputPath(tempDir, "relatorio", "csv");
                Assert(path2.EndsWith("relatorio (1).csv"), "Gera sufixo incremental anti-sobrescrita");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_EngineRegistry_Resolution()
        {
            var registry = EngineRegistry.Default;
            var engine = registry.Resolve("csv", "json");
            Assert(engine != null, "Motor resolvido");
            AssertEqual("lodino.engine.data", engine.EngineId, "EngineId correto");
        }

        #endregion

        #region Testes do Motor BMP Próprio

        private static void Test_Bmp_1x1_Padding1()
        {
            var pb = new PixelBuffer(1, 1);
            pb.SetPixel(0, 0, new PixelColor(255, 0, 0, 255));
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(58, bytes.Length, "Tamanho total BMP 1x1 (54 cabeçalho + 4 bytes linha)");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(1, decoded.Width, "Width");
            AssertEqual(1, decoded.Height, "Height");
            AssertEqual(new PixelColor(255, 0, 0, 255), decoded.GetPixel(0, 0), "Pixel (0,0) Vermelho");
        }

        private static void Test_Bmp_2x2_Padding2()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Green);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.White);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(70, bytes.Length, "Tamanho total BMP 2x2 com 2 bytes padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Pixel (0,0)");
            AssertEqual(PixelColor.Green, decoded.GetPixel(1, 0), "Pixel (1,0)");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Pixel (0,1)");
            AssertEqual(PixelColor.White, decoded.GetPixel(1, 1), "Pixel (1,1)");
        }

        private static void Test_Bmp_3x3_Padding3()
        {
            var pb = new PixelBuffer(3, 3);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 80), (byte)(y * 80), 150, 255));
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(90, bytes.Length, "Tamanho total BMP 3x3 com 3 bytes padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(3, decoded.Width, "Width");
            AssertEqual(3, decoded.Height, "Height");
            AssertEqual(new PixelColor(80, 80, 150, 255), decoded.GetPixel(1, 1), "Pixel central (1,1)");
        }

        private static void Test_Bmp_4x4_NoPadding()
        {
            var pb = new PixelBuffer(4, 4);
            pb.Fill(PixelColor.Yellow);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(102, bytes.Length, "Tamanho total BMP 4x4 sem padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(PixelColor.Yellow, decoded.GetPixel(2, 2), "Pixel (2,2) amarelo");
        }

        private static void Test_Bmp_7x5_OddDimensions()
        {
            var pb = new PixelBuffer(7, 5);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(6, 4, PixelColor.Cyan);
            pb.SetPixel(3, 2, PixelColor.Magenta);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(174, bytes.Length, "Tamanho total BMP 7x5 com padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(7, decoded.Width, "Width");
            AssertEqual(5, decoded.Height, "Height");
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Canto superior esquerdo");
            AssertEqual(PixelColor.Cyan, decoded.GetPixel(6, 4), "Canto inferior direito");
            AssertEqual(PixelColor.Magenta, decoded.GetPixel(3, 2), "Pixel central");
        }

        private static void Test_Bmp_24bit_RoundTrip()
        {
            var pb = new PixelBuffer(16, 16);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 16), (byte)(y * 16), (byte)((x + y) * 8), 255));
                }
            }
            byte[] encoded = BmpEncoder.Encode(pb, 24);
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(16, decoded.Width, "Width");
            AssertEqual(16, decoded.Height, "Height");
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    AssertEqual(pb.GetPixel(x, y), decoded.GetPixel(x, y), string.Format("Pixel ({0},{1})", x, y));
                }
            }
        }

        private static void Test_Bmp_32bit_Bgrx_ArbitraryHighBytes_NormalizesOpaque()
        {
            // Constrói manualmente um BMP de 2x2 32-bit BI_RGB onde o 4º byte varia: 0x00, 0x40, 0x80, 0xFF
            // Todos os 4 pixels devem decodificar com A=255 (opacos) e manter os canais RGB exatos
            byte[] bytes = new byte[54 + 8 * 2]; // 2x2 32-bit: stride = 2 * 4 = 8 bytes. Total = 70 bytes.
            using (var ms = new MemoryStream(bytes))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0x4D42);
                bw.Write((uint)bytes.Length);
                bw.Write((uint)0);
                bw.Write((uint)54);
                bw.Write((uint)40);
                bw.Write((int)2);
                bw.Write((int)2); // bottom-up
                bw.Write((ushort)1);
                bw.Write((ushort)32); // 32 bpp
                bw.Write((uint)0); // BI_RGB
                bw.Write((uint)(8 * 2));
                bw.Write((int)2835);
                bw.Write((int)2835);
                bw.Write((uint)0);
                bw.Write((uint)0);
                // Scanline 0 do arquivo (fundo, y=1):
                // (0, 1): B=10, G=20, R=30, X=0x00
                bw.Write((byte)10); bw.Write((byte)20); bw.Write((byte)30); bw.Write((byte)0x00);
                // (1, 1): B=40, G=50, R=60, X=0x40
                bw.Write((byte)40); bw.Write((byte)50); bw.Write((byte)60); bw.Write((byte)0x40);
                // Scanline 1 do arquivo (topo, y=0):
                // (0, 0): B=70, G=80, R=90, X=0x80
                bw.Write((byte)70); bw.Write((byte)80); bw.Write((byte)90); bw.Write((byte)0x80);
                // (1, 0): B=100, G=110, R=120, X=0xFF
                bw.Write((byte)100); bw.Write((byte)110); bw.Write((byte)120); bw.Write((byte)0xFF);
            }

            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(2, decoded.Width, "Width");
            AssertEqual(2, decoded.Height, "Height");

            // Verifica normalização do alfa para 255 e preservação de RGB
            AssertEqual(new PixelColor(90, 80, 70, 255), decoded.GetPixel(0, 0), "Pixel (0,0) com X=0x80 normalizado para A=255");
            AssertEqual(new PixelColor(120, 110, 100, 255), decoded.GetPixel(1, 0), "Pixel (1,0) com X=0xFF normalizado para A=255");
            AssertEqual(new PixelColor(30, 20, 10, 255), decoded.GetPixel(0, 1), "Pixel (0,1) com X=0x00 normalizado para A=255");
            AssertEqual(new PixelColor(60, 50, 40, 255), decoded.GetPixel(1, 1), "Pixel (1,1) com X=0x40 normalizado para A=255");
        }

        private static void Test_Bmp_32bit_Bgrx_RoundTrip()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 50), (byte)(y * 50), 200, 255));
                }
            }
            byte[] encoded = BmpEncoder.Encode(pb, 32);
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(4, decoded.Width, "Width 4");
            AssertEqual(4, decoded.Height, "Height 4");
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    AssertEqual(pb.GetPixel(x, y), decoded.GetPixel(x, y), string.Format("Pixel BGRX ({0},{1})", x, y));
                }
            }
        }

        private static void Test_Bmp_BottomUp_Orientation()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Red);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.Blue);
            byte[] encoded = BmpEncoder.Encode(pb, 24);

            AssertEqual((byte)255, encoded[54], "Primeiro byte dos dados é B=255 (Azul)");
            AssertEqual((byte)0, encoded[54 + 1], "G=0");
            AssertEqual((byte)0, encoded[54 + 2], "R=0");

            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Linha superior é Vermelha");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Linha inferior é Azul");
        }

        private static void Test_Bmp_TopDown_Orientation()
        {
            byte[] topDownBytes = new byte[54 + 8 * 2];
            using (var ms = new MemoryStream(topDownBytes))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0x4D42);
                bw.Write((uint)topDownBytes.Length);
                bw.Write((uint)0);
                bw.Write((uint)54);
                bw.Write((uint)40);
                bw.Write((int)2);
                bw.Write((int)-2);
                bw.Write((ushort)1);
                bw.Write((ushort)24);
                bw.Write((uint)0);
                bw.Write((uint)(8 * 2));
                bw.Write((int)2835);
                bw.Write((int)2835);
                bw.Write((uint)0);
                bw.Write((uint)0);
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)255);
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)255);
                bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)255); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)255); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)0); bw.Write((byte)0);
            }
            var decoded = BmpDecoder.Decode(topDownBytes);
            AssertEqual(2, decoded.Width, "Width");
            AssertEqual(2, decoded.Height, "Height");
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Topo decodificado é Vermelho");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Fundo decodificado é Azul");
        }

        private static void Test_Bmp_Resize_NearestNeighbor()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Green);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.White);
            var resized = pb.ResizeNearestNeighbor(4, 4);
            AssertEqual(4, resized.Width, "Width 4");
            AssertEqual(4, resized.Height, "Height 4");
            AssertEqual(PixelColor.Red, resized.GetPixel(0, 0), "Bloco SE 0,0");
            AssertEqual(PixelColor.Red, resized.GetPixel(1, 1), "Bloco SE 1,1");
            AssertEqual(PixelColor.Green, resized.GetPixel(2, 0), "Bloco SD 2,0");
            AssertEqual(PixelColor.Green, resized.GetPixel(3, 1), "Bloco SD 3,1");
            AssertEqual(PixelColor.Blue, resized.GetPixel(0, 2), "Bloco IE 0,2");
            AssertEqual(PixelColor.Blue, resized.GetPixel(1, 3), "Bloco IE 1,3");
            AssertEqual(PixelColor.White, resized.GetPixel(2, 2), "Bloco ID 2,2");
            AssertEqual(PixelColor.White, resized.GetPixel(3, 3), "Bloco ID 3,3");
        }

        private static void Test_Bmp_Resize_Bilinear()
        {
            var pb = new PixelBuffer(2, 1);
            pb.SetPixel(0, 0, new PixelColor(0, 0, 0, 255));
            pb.SetPixel(1, 0, new PixelColor(254, 254, 254, 255));
            var resized = pb.ResizeBilinear(3, 1);
            AssertEqual(3, resized.Width, "Width 3");
            AssertEqual(1, resized.Height, "Height 1");
            PixelColor center = resized.GetPixel(1, 0);
            Assert(center.R >= 120 && center.R <= 134, "Canal R interpolado ~ 127");
            Assert(center.G >= 120 && center.G <= 134, "Canal G interpolado ~ 127");
            Assert(center.B >= 120 && center.B <= 134, "Canal B interpolado ~ 127");
        }

        private static void Test_Bmp_Crop_Arbitrary()
        {
            var pb = new PixelBuffer(10, 10);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 50, 255));
            var cropped = pb.Crop(2, 3, 4, 5);
            AssertEqual(4, cropped.Width, "Cropped Width");
            AssertEqual(5, cropped.Height, "Cropped Height");
            AssertEqual(new PixelColor(2, 3, 50, 255), cropped.GetPixel(0, 0), "Pixel recortado (0,0)");
            AssertEqual(new PixelColor(5, 7, 50, 255), cropped.GetPixel(3, 4), "Pixel recortado (3,4)");
        }

        private static void Test_Bmp_Crop_Quadrants()
        {
            var pb = new PixelBuffer(9, 9);
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 0, 255));

            var tl = pb.CropQuadrant(3, 3, "top-left");
            AssertEqual(new PixelColor(0, 0, 0, 255), tl.GetPixel(0, 0), "Top-Left");

            var c = pb.CropQuadrant(3, 3, "center");
            AssertEqual(new PixelColor(3, 3, 0, 255), c.GetPixel(0, 0), "Center");

            var br = pb.CropQuadrant(3, 3, "bottom-right");
            AssertEqual(new PixelColor(6, 6, 0, 255), br.GetPixel(0, 0), "Bottom-Right");
        }

        private static void Test_Bmp_Watermark_Text()
        {
            var pb = new PixelBuffer(60, 20);
            pb.Fill(PixelColor.Black);
            pb.ApplyTextWatermark("LODINO", 5, 5, PixelColor.White, 1);
            bool hasWhite = false;
            for (int y = 0; y < pb.Height; y++)
            {
                for (int x = 0; x < pb.Width; x++)
                {
                    if (pb.GetPixel(x, y) == PixelColor.White)
                    {
                        hasWhite = true;
                        break;
                    }
                }
            }
            Assert(hasWhite, "Texto 'LODINO' rasterizado com glifos proprietários");
            AssertEqual(PixelColor.Black, pb.GetPixel(0, 0), "Pixel fora da marca permanece preto");
        }

        private static void Test_Bmp_Watermark_Image_AlphaComposite()
        {
            var bg = new PixelBuffer(10, 10);
            bg.Fill(PixelColor.Blue);
            var fg = new PixelBuffer(4, 4);
            fg.Fill(PixelColor.Red);
            bg.ApplyImageWatermark(fg, 2, 2, 0.5f);
            PixelColor blended = bg.GetPixel(3, 3);
            Assert(blended.R >= 125 && blended.R <= 130, "R composto ~ 128");
            AssertEqual((byte)0, blended.G, "G composto = 0");
            Assert(blended.B >= 125 && blended.B <= 130, "B composto ~ 128");
            AssertEqual((byte)255, blended.A, "A composto = 255");
            AssertEqual(PixelColor.Blue, bg.GetPixel(0, 0), "Pixel externo permanece azul");
        }

        private static void Test_Bmp_Security_InvalidMagic()
        {
            byte[] badMagic = new byte[54];
            badMagic[0] = (byte)'Z';
            badMagic[1] = (byte)'Z';
            bool threw = false;
            try { BmpDecoder.Decode(badMagic); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita assinatura mágica diferente de 'BM'");
        }

        private static void Test_Bmp_Security_UnsupportedHeaderSize()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[14] = 12; b[15] = 0; b[16] = 0; b[17] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita cabeçalho DIB não suportado (biSize = 12)");
        }

        private static void Test_Bmp_Security_RejectPalettes()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[28] = 8; b[29] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita BMP com paleta indexada (8 bpp)");
        }

        private static void Test_Bmp_Security_RejectCompression()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[30] = 1; b[31] = 0; b[32] = 0; b[33] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita compressão BI_RLE8");
        }

        private static void Test_Bmp_Security_InvalidDimensions()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[18] = 0; b[19] = 0; b[20] = 0; b[21] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita largura BMP igual a zero");
        }

        private static void Test_Bmp_Security_ExceedMaxDimension()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[18] = 0x20; b[19] = 0x4E; b[20] = 0; b[21] = 0; // 20.000
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (ResourceLimitException) { threw = true; }
            Assert(threw, "Rejeita dimensões que excedem limite de segurança (16.384)");
        }

        private static void Test_Bmp_Security_TruncatedFile()
        {
            var pb = new PixelBuffer(20, 20);
            byte[] full = BmpEncoder.Encode(pb, 24);
            byte[] truncated = new byte[70];
            Array.Copy(full, truncated, 70);
            bool threw = false;
            try { BmpDecoder.Decode(truncated); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita arquivo BMP truncado");
        }

        private static void Test_Bmp_Security_InvalidOffBits()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[10] = 0x50; b[11] = 0xC3; b[12] = 0; b[13] = 0; // 50.000
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita bfOffBits fora dos limites do arquivo");
        }

        private static void Test_Bmp_Engine_Inspect()
        {
            var pb = new PixelBuffer(16, 8);
            pb.Fill(PixelColor.Green);
            byte[] bmpData = BmpEncoder.Encode(pb, 24);
            var engine = new BmpEngine();
            using (var ms = new MemoryStream(bmpData))
            {
                var inspection = engine.Inspect(ms, "foto.bmp");
                Assert(inspection.IsSupported, "IsSupported == true");
                AssertEqual("bmp", inspection.FormatId, "FormatId");
                AssertEqual(16, inspection.Width, "Width");
                AssertEqual(8, inspection.Height, "Height");
                AssertEqual("24", inspection.Metadata["BitDepth"], "BitDepth");
            }
        }

        private static void Test_Bmp_Engine_ConvertPipeline()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_bmp_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string inPath = Path.Combine(tempDir, "input.bmp");
                var pb = new PixelBuffer(20, 20);
                pb.Fill(PixelColor.Blue);
                File.WriteAllBytes(inPath, BmpEncoder.Encode(pb, 24));

                var req = new ConvertRequest
                {
                    InputPath = inPath,
                    TargetFormat = "bmp",
                    OutputDirectory = tempDir,
                    OutputFileName = "processed"
                };
                req.Settings["width"] = "40";
                req.Settings["height"] = "40";
                req.Settings["watermarkText"] = "LODINO";

                var engine = new BmpEngine();
                var result = engine.Convert(req, null);
                AssertEqual("completed", result.Status, "Status");
                Assert(File.Exists(result.OutputPath), "Arquivo resultante existe");

                var outputPb = BmpDecoder.Decode(File.ReadAllBytes(result.OutputPath));
                AssertEqual(40, outputPb.Width, "Largura redimensionada para 40");
                AssertEqual(40, outputPb.Height, "Altura redimensionada para 40");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Bmp_Registry_Resolution()
        {
            var registry = EngineRegistry.Default;
            var engine = registry.Resolve("bmp", "bmp");
            Assert(engine != null, "Motor BMP resolvido");
            AssertEqual("image-bmp", engine.EngineId, "EngineId image-bmp");
        }

        #region Testes de Fixtures Independentes (Fase B)

        private static byte[] CreateRawBmp(int width, int rawHeight, ushort bpp, byte[] pixelBytes, int offBits = 54, byte[] extraHeaderBytes = null)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int extraLen = extraHeaderBytes != null ? extraHeaderBytes.Length : 0;
                int totalOffBits = Math.Max(offBits, 54 + extraLen);
                int totalSize = totalOffBits + pixelBytes.Length;

                // BITMAPFILEHEADER (14 bytes)
                bw.Write((ushort)0x4D42);
                bw.Write((uint)totalSize);
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write((uint)totalOffBits);

                // BITMAPINFOHEADER (40 bytes)
                bw.Write((uint)40);
                bw.Write(width);
                bw.Write(rawHeight);
                bw.Write((ushort)1);
                bw.Write(bpp);
                bw.Write((uint)0); // BI_RGB
                bw.Write((uint)pixelBytes.Length);
                bw.Write(2835);
                bw.Write(2835);
                bw.Write((uint)0);
                bw.Write((uint)0);

                // Metadados adicionais pré-pixel (se offBits > 54)
                if (extraLen > 0)
                {
                    bw.Write(extraHeaderBytes, 0, extraLen);
                }

                // Preenchimento até offBits se houver lacuna
                while (ms.Position < totalOffBits)
                {
                    bw.Write((byte)0);
                }

                bw.Write(pixelBytes, 0, pixelBytes.Length);
                return ms.ToArray();
            }
        }

        private static void Test_Bmp_Independent_24bit_PaddingVariants_0_1_2_3()
        {
            // 1. Padding 0: 4x1 (stride = 12, rowData = 12, pad = 0)
            // Pixels BGR: P0=(0,0,255), P1=(0,255,0), P2=(255,0,0), P3=(255,255,255)
            byte[] p0_data = new byte[] {
                0, 0, 255,     // P0: Red
                0, 255, 0,     // P1: Green
                255, 0, 0,     // P2: Blue
                255, 255, 255  // P3: White
            };
            var dec0 = BmpDecoder.Decode(CreateRawBmp(4, 1, 24, p0_data));
            AssertEqual(PixelColor.Red, dec0.GetPixel(0, 0), "Pad0 P0");
            AssertEqual(PixelColor.Green, dec0.GetPixel(1, 0), "Pad0 P1");
            AssertEqual(PixelColor.Blue, dec0.GetPixel(2, 0), "Pad0 P2");
            AssertEqual(PixelColor.White, dec0.GetPixel(3, 0), "Pad0 P3");

            // 2. Padding 1: 1x2 (stride = 4, rowData = 3, pad = 1 com byte de lixo 0xAA)
            // Bottom-up: scanline 0 é y=1, scanline 1 é y=0
            byte[] p1_data = new byte[] {
                0, 255, 255, 0xAA, // y=1: Amarelo (B=0, G=255, R=255) + pad 0xAA
                255, 255, 0, 0xBB  // y=0: Ciano (B=255, G=255, R=0) + pad 0xBB
            };
            var dec1 = BmpDecoder.Decode(CreateRawBmp(1, 2, 24, p1_data));
            AssertEqual(PixelColor.Cyan, dec1.GetPixel(0, 0), "Pad1 y=0 Ciano");
            AssertEqual(PixelColor.Yellow, dec1.GetPixel(0, 1), "Pad1 y=1 Amarelo");

            // 3. Padding 2: 2x1 (stride = 8, rowData = 6, pad = 2 com lixo 0xDE, 0xAD)
            byte[] p2_data = new byte[] {
                255, 0, 255,   // P0: Magenta
                0, 0, 0,       // P1: Preto
                0xDE, 0xAD     // pad de 2 bytes
            };
            var dec2 = BmpDecoder.Decode(CreateRawBmp(2, 1, 24, p2_data));
            AssertEqual(PixelColor.Magenta, dec2.GetPixel(0, 0), "Pad2 P0 Magenta");
            AssertEqual(PixelColor.Black, dec2.GetPixel(1, 0), "Pad2 P1 Preto");

            // 4. Padding 3: 3x1 (stride = 12, rowData = 9, pad = 3 com lixo 0x11, 0x22, 0x33)
            byte[] p3_data = new byte[] {
                50, 60, 70,     // P0
                80, 90, 100,    // P1
                110, 120, 130,  // P2
                0x11, 0x22, 0x33 // pad de 3 bytes
            };
            var dec3 = BmpDecoder.Decode(CreateRawBmp(3, 1, 24, p3_data));
            AssertEqual(new PixelColor(70, 60, 50, 255), dec3.GetPixel(0, 0), "Pad3 P0");
            AssertEqual(new PixelColor(100, 90, 80, 255), dec3.GetPixel(1, 0), "Pad3 P1");
            AssertEqual(new PixelColor(130, 120, 110, 255), dec3.GetPixel(2, 0), "Pad3 P2");
        }

        private static void Test_Bmp_Independent_CustomOffBits_WithMetadataHeader()
        {
            // offBits = 80 (54 bytes do cabeçalho + 20 bytes de dados personalizados + 6 bytes pad)
            byte[] extra = Encoding.ASCII.GetBytes("LODINO-METADATA-PAD!"); // 20 bytes
            byte[] pixelData = new byte[] { 100, 150, 200, 0 }; // 1x1 24-bit stride 4
            byte[] fileBytes = CreateRawBmp(1, 1, 24, pixelData, 80, extra);

            var decoded = BmpDecoder.Decode(fileBytes);
            AssertEqual(1, decoded.Width, "Width");
            AssertEqual(1, decoded.Height, "Height");
            AssertEqual(new PixelColor(200, 150, 100, 255), decoded.GetPixel(0, 0), "Pixel decodificado após offset arbitrário");
        }

        private static void Test_Bmp_Independent_TopDown_vs_BottomUp_ExactDuality()
        {
            // Imagem 2x2:
            // y=0: P(0,0)=Vermelho, P(1,0)=Verde
            // y=1: P(0,1)=Azul, P(1,1)=Branco
            // Stride = 8 bytes (6 dados + 2 pad)

            // Bottom-Up: Linha 0 do arquivo é y=1 (Azul, Branco), Linha 1 do arquivo é y=0 (Vermelho, Verde)
            byte[] bu_pixels = new byte[] {
                255, 0, 0,    255, 255, 255, 0, 0, // y=1: Azul, Branco, pad 2
                0, 0, 255,    0, 255, 0,     0, 0  // y=0: Vermelho, Verde, pad 2
            };
            byte[] bu_file = CreateRawBmp(2, 2, 24, bu_pixels);

            // Top-Down: Linha 0 do arquivo é y=0 (Vermelho, Verde), Linha 1 do arquivo é y=1 (Azul, Branco)
            byte[] td_pixels = new byte[] {
                0, 0, 255,    0, 255, 0,     0, 0, // y=0: Vermelho, Verde, pad 2
                255, 0, 0,    255, 255, 255, 0, 0  // y=1: Azul, Branco, pad 2
            };
            byte[] td_file = CreateRawBmp(2, -2, 24, td_pixels);

            var decBU = BmpDecoder.Decode(bu_file);
            var decTD = BmpDecoder.Decode(td_file);

            AssertEqual(decBU.GetPixel(0, 0), decTD.GetPixel(0, 0), "Duality (0,0) Vermelho");
            AssertEqual(decBU.GetPixel(1, 0), decTD.GetPixel(1, 0), "Duality (1,0) Verde");
            AssertEqual(decBU.GetPixel(0, 1), decTD.GetPixel(0, 1), "Duality (0,1) Azul");
            AssertEqual(decBU.GetPixel(1, 1), decTD.GetPixel(1, 1), "Duality (1,1) Branco");
        }

        private static void Test_Bmp_Independent_HeaderTruncation_And_SizeMismatch()
        {
            // Arquivo com menos de 54 bytes
            byte[] shortFile = new byte[35];
            shortFile[0] = 0x42; shortFile[1] = 0x4D;
            bool threwShort = false;
            try { BmpDecoder.Decode(shortFile); } catch (CorruptDataException) { threwShort = true; }
            Assert(threwShort, "Rejeita arquivo com tamanho menor que cabeçalho padrão");

            // bfOffBits menor que 54
            byte[] badOff = CreateRawBmp(1, 1, 24, new byte[] { 0, 0, 0, 0 });
            badOff[10] = 30; badOff[11] = 0; badOff[12] = 0; badOff[13] = 0;
            bool threwBadOff = false;
            try { BmpDecoder.Decode(badOff); } catch (CorruptDataException) { threwBadOff = true; }
            Assert(threwBadOff, "Rejeita bfOffBits < 54");

            // bfOffBits apontando para além do comprimento do arquivo
            badOff[10] = 0x00; badOff[11] = 0x10; // 4096
            bool threwBeyond = false;
            try { BmpDecoder.Decode(badOff); } catch (CorruptDataException) { threwBeyond = true; }
            Assert(threwBeyond, "Rejeita bfOffBits apontando além do EOF");
        }

        private static void Test_Bmp_Independent_DimensionOverflow()
        {
            byte[] bmp = CreateRawBmp(2, 2, 24, new byte[16]);

            // biWidth = 0x40000000 (causaria overflow em (width * 24 + 31)/32 se não protegido)
            bmp[18] = 0x00; bmp[19] = 0x00; bmp[20] = 0x00; bmp[21] = 0x40;
            bool threwWidth = false;
            try { BmpDecoder.Decode(bmp); }
            catch (Exception ex) { if (ex is ResourceLimitException || ex is CorruptDataException) threwWidth = true; }
            Assert(threwWidth, "Bloqueia overflow de largura (0x40000000)");

            // biHeight = 0
            bmp[18] = 2; bmp[19] = 0; bmp[20] = 0; bmp[21] = 0; // restaura width
            bmp[22] = 0; bmp[23] = 0; bmp[24] = 0; bmp[25] = 0; // height = 0
            bool threwZeroH = false;
            try { BmpDecoder.Decode(bmp); } catch (CorruptDataException) { threwZeroH = true; }
            Assert(threwZeroH, "Rejeita biHeight == 0");

            // biWidth = -5
            bmp[18] = 0xFB; bmp[19] = 0xFF; bmp[20] = 0xFF; bmp[21] = 0xFF; // -5
            bmp[22] = 2; bmp[23] = 0; bmp[24] = 0; bmp[25] = 0;
            bool threwNegW = false;
            try { BmpDecoder.Decode(bmp); } catch (CorruptDataException) { threwNegW = true; }
            Assert(threwNegW, "Rejeita biWidth negativo");
        }

        #endregion

        #region Motor PNG (Testes e Fixtures Independentes)

        #region Helpers de Fixtures PNG Independentes

        private class SimpleBitWriter
        {
            private readonly MemoryStream _ms = new MemoryStream();
            private int _bitBuffer = 0;
            private int _bitCount = 0;

            public void WriteBits(int value, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int bit = (value >> i) & 1;
                    _bitBuffer |= (bit << _bitCount);
                    _bitCount++;
                    if (_bitCount == 8)
                    {
                        _ms.WriteByte((byte)_bitBuffer);
                        _bitBuffer = 0;
                        _bitCount = 0;
                    }
                }
            }

            public void WriteHuffman(int code, int bitLength)
            {
                for (int i = bitLength - 1; i >= 0; i--)
                {
                    int bit = (code >> i) & 1;
                    WriteBits(bit, 1);
                }
            }

            public byte[] ToArray()
            {
                if (_bitCount > 0)
                {
                    _ms.WriteByte((byte)_bitBuffer);
                }
                return _ms.ToArray();
            }
        }

        private static byte[] CreateRawPng(int width, int height, bool hasAlpha, byte[] rawFilteredScanlines)
        {
            using (var ms = new MemoryStream())
            {
                // 1. Assinatura PNG de 8 bytes
                ms.Write(PngHeader.PngSignature, 0, 8);

                // 2. Chunk IHDR (13 bytes)
                byte[] ihdrData = new byte[13];
                ihdrData[0] = (byte)((width >> 24) & 0xFF);
                ihdrData[1] = (byte)((width >> 16) & 0xFF);
                ihdrData[2] = (byte)((width >> 8) & 0xFF);
                ihdrData[3] = (byte)(width & 0xFF);

                ihdrData[4] = (byte)((height >> 24) & 0xFF);
                ihdrData[5] = (byte)((height >> 16) & 0xFF);
                ihdrData[6] = (byte)((height >> 8) & 0xFF);
                ihdrData[7] = (byte)(height & 0xFF);

                ihdrData[8] = 8; // Bit depth: 8
                ihdrData[9] = hasAlpha ? (byte)6 : (byte)2; // ColorType: 6 (RGBA) ou 2 (RGB)
                ihdrData[10] = 0; // Compressão: DEFLATE
                ihdrData[11] = 0; // Filtro: adaptativo
                ihdrData[12] = 0; // Interlace: nenhum
                WriteRawChunk(ms, "IHDR", ihdrData);

                // 3. Chunk IDAT (Fluxo ZLIB com bloco DEFLATE armazenado e checksum Adler-32)
                using (var zlibMs = new MemoryStream())
                {
                    zlibMs.WriteByte(0x78); // CMF
                    zlibMs.WriteByte(0x01); // FLG (check = 0x7801 = múltiplo de 31)

                    // Bloco DEFLATE armazenado (BFINAL = 1, BTYPE = 00)
                    zlibMs.WriteByte(0x01);
                    ushort len = (ushort)rawFilteredScanlines.Length;
                    ushort nlen = (ushort)(~len);
                    zlibMs.WriteByte((byte)(len & 0xFF));
                    zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                    zlibMs.WriteByte((byte)(nlen & 0xFF));
                    zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                    zlibMs.Write(rawFilteredScanlines, 0, rawFilteredScanlines.Length);

                    // Checksum Adler-32 no final
                    uint adler = Adler32.Calculate(rawFilteredScanlines, 0, rawFilteredScanlines.Length);
                    zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                    zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                    zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                    zlibMs.WriteByte((byte)(adler & 0xFF));

                    byte[] zlibBytes = zlibMs.ToArray();
                    WriteRawChunk(ms, "IDAT", zlibBytes);
                }

                // 4. Chunk IEND
                WriteRawChunk(ms, "IEND", new byte[0]);

                return ms.ToArray();
            }
        }

        private static void WriteRawChunk(Stream stream, string type, byte[] data)
        {
            int len = data != null ? data.Length : 0;
            stream.WriteByte((byte)((len >> 24) & 0xFF));
            stream.WriteByte((byte)((len >> 16) & 0xFF));
            stream.WriteByte((byte)((len >> 8) & 0xFF));
            stream.WriteByte((byte)(len & 0xFF));

            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes, 0, 4);

            if (len > 0)
            {
                stream.Write(data, 0, len);
            }

            uint crc = PngCrc32.Calculate(type, data, 0, len);
            stream.WriteByte((byte)((crc >> 24) & 0xFF));
            stream.WriteByte((byte)((crc >> 16) & 0xFF));
            stream.WriteByte((byte)((crc >> 8) & 0xFF));
            stream.WriteByte((byte)(crc & 0xFF));
        }

        #endregion

        private static void Test_Png_Checksum_Crc32_Vectors()
        {
            byte[] empty = new byte[0];
            Assert(PngCrc32.Calculate(empty, 0, 0) == 0x00000000, "CRC32 de vazio deve ser 0");

            byte[] v1 = Encoding.ASCII.GetBytes("123456789");
            Assert(PngCrc32.Calculate(v1, 0, v1.Length) == 0xCBF43926, "CRC32 de '123456789' deve ser 0xCBF43926");

            // Chunk IEND com dados vazios
            Assert(PngCrc32.Calculate("IEND", new byte[0], 0, 0) == 0xAE426082, "CRC32 do chunk 'IEND' deve ser 0xAE426082");
        }

        private static void Test_Png_Checksum_Adler32_Vectors()
        {
            byte[] empty = new byte[0];
            Assert(Adler32.Calculate(empty, 0, 0) == 1, "Adler32 de vazio deve ser 1");

            byte[] v1 = Encoding.ASCII.GetBytes("123456789");
            Assert(Adler32.Calculate(v1, 0, v1.Length) == 0x091E01DE, "Adler32 de '123456789' deve ser 0x091E01DE");

            byte[] v2 = Encoding.ASCII.GetBytes("Wikipedia");
            Assert(Adler32.Calculate(v2, 0, v2.Length) == 0x11E60398, "Adler32 de 'Wikipedia' deve ser 0x11E60398");

            // Detecção de corrupção do Adler-32 em ZlibStream
            byte[] compressed = ZlibStream.Compress(v1);
            compressed[compressed.Length - 1] ^= 0xFF; // corrompe último byte do checksum
            bool threwAdler = false;
            try { ZlibStream.Decompress(compressed); }
            catch (CorruptDataException) { threwAdler = true; }
            Assert(threwAdler, "ZlibStream deve rejeitar fluxo com Adler-32 corrompido");
        }

        private static void Test_Png_Deflate_StoredBlocks_SingleAndMulti()
        {
            byte[] data1 = Encoding.ASCII.GetBytes("Hello, Lodino Proprietary Core! ");
            byte[] data2 = Encoding.ASCII.GetBytes("Second Stored Block DEFLATE.");

            using (var ms = new MemoryStream())
            {
                // Bloco 1 (BFINAL = 0, BTYPE = 00)
                ms.WriteByte(0x00);
                ushort len1 = (ushort)data1.Length;
                ushort nlen1 = (ushort)(~len1);
                ms.WriteByte((byte)(len1 & 0xFF)); ms.WriteByte((byte)((len1 >> 8) & 0xFF));
                ms.WriteByte((byte)(nlen1 & 0xFF)); ms.WriteByte((byte)((nlen1 >> 8) & 0xFF));
                ms.Write(data1, 0, data1.Length);

                // Bloco 2 (BFINAL = 1, BTYPE = 00)
                ms.WriteByte(0x01);
                ushort len2 = (ushort)data2.Length;
                ushort nlen2 = (ushort)(~len2);
                ms.WriteByte((byte)(len2 & 0xFF)); ms.WriteByte((byte)((len2 >> 8) & 0xFF));
                ms.WriteByte((byte)(nlen2 & 0xFF)); ms.WriteByte((byte)((nlen2 >> 8) & 0xFF));
                ms.Write(data2, 0, data2.Length);

                byte[] streamBytes = ms.ToArray();
                byte[] decompressed = DeflateDecompressor.Decompress(new MemoryStream(streamBytes));
                string text = Encoding.ASCII.GetString(decompressed);
                Assert(text == "Hello, Lodino Proprietary Core! Second Stored Block DEFLATE.", "Descompressão multi-bloco stored deve ser exata");

                // Corrompe NLEN
                streamBytes[3] ^= 0xFF;
                bool threwNlen = false;
                try { DeflateDecompressor.Decompress(new MemoryStream(streamBytes)); }
                catch (CorruptDataException) { threwNlen = true; }
                Assert(threwNlen, "Deve rejeitar bloco com NLEN corrompido");
            }
        }

        private static void Test_Png_Deflate_FixedHuffman_Decompression()
        {
            // Bloco de teste DEFLATE com Huffman Fixo (BFINAL = 1, BTYPE = 01)
            byte[] fixedDeflate = new byte[]
            {
                0xF3, 0xF1, 0x77, 0xF1, 0xF4, 0xF3, 0x8F, 0x77, 0x71, 0x75, 0xF3, 0x71, 0x0C, 0x71, 0x8D, 0x77,
                0x89, 0xF4, 0x73, 0xF4, 0xF5, 0x74, 0x8E, 0x0F, 0x71, 0x0D, 0x0E, 0x89, 0x0F, 0x0E, 0x09, 0xF2,
                0xF4, 0x73, 0x8F, 0x37, 0x34, 0x32, 0x36, 0x31, 0x05, 0x00
            };

            byte[] decomp = DeflateDecompressor.Decompress(new MemoryStream(fixedDeflate));
            string str = Encoding.ASCII.GetString(decomp);
            Assert(str == "LODINO_DEFLATE_DYNAMIC_TEST_STRING_12345", "Fixed Huffman deve descomprimir string com exatidão");
        }

        private static void Test_Png_Deflate_DynamicHuffman_Decompression()
        {
            // Construção independente de bloco Huffman Dinâmico (BTYPE 10, BFINAL 1)
            var writer = new SimpleBitWriter();

            // 1. Cabeçalho de bloco: BFINAL=1, BTYPE=10
            writer.WriteBits(1, 1);
            writer.WriteBits(2, 2);

            // 2. Dimensões das tabelas dinâmicas
            int hlitVal = 0;   // hlit = 257 (0 + 257)
            int hdistVal = 0;  // hdist = 1 (0 + 1)
            int hclenVal = 14; // hclen = 18 (14 + 4)
            writer.WriteBits(hlitVal, 5);
            writer.WriteBits(hdistVal, 5);
            writer.WriteBits(hclenVal, 4);

            // 3. Tabela de comprimentos de código (18 entradas):
            // CodeLengthOrder: [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1]
            // Índice 2 é símbolo 18: comprimento 2
            // Índice 3 é símbolo 0: comprimento 1
            // Índice 17 é símbolo 1: comprimento 2
            for (int i = 0; i < 18; i++)
            {
                if (i == 3) writer.WriteBits(1, 3);      // símbolo 0: len 1 (código 1)
                else if (i == 2) writer.WriteBits(2, 3); // símbolo 18: len 2 (código 00)
                else if (i == 17) writer.WriteBits(2, 3);// símbolo 1: len 2 (código 01)
                else writer.WriteBits(0, 3);
            }

            // 4. Emissão dos 258 comprimentos de código (257 literais + 1 distância)
            // Símbolo 0 tem len 1 -> código 0 (1 bit: 0)
            // Símbolo 1 tem len 2 -> código 2 (2 bits: 10)
            // Símbolo 18 tem len 2 -> código 3 (2 bits: 11)
            // Símbolos 0..64 (65 símbolos) com comprimento 0: símbolo 18 com repeat 65 (54 nos 7 bits extras)
            writer.WriteHuffman(3, 2); // símbolo 18 (código 11)
            writer.WriteBits(54, 7);   // 11 + 54 = 65 repetições

            // Símbolo 65 ('A') com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Símbolos 66..203 (138 símbolos) com comprimento 0: símbolo 18 com repeat 138 (127 nos 7 bits extras)
            writer.WriteHuffman(3, 2);
            writer.WriteBits(127, 7);  // 11 + 127 = 138 repetições

            // Símbolos 204..255 (52 símbolos) com comprimento 0: símbolo 18 com repeat 52 (41 nos 7 bits extras)
            writer.WriteHuffman(3, 2);
            writer.WriteBits(41, 7);   // 11 + 41 = 52 repetições

            // Símbolo 256 (EOB) com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Símbolo de distância 0 com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Agora a árvore literal tem: 'A' (65) = código 0 (1 bit), EOB (256) = código 1 (1 bit)
            // Emitimos "AAAA" seguido de EOB
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(1, 1); // EOB (256)

            byte[] dynamicBlock = writer.ToArray();
            byte[] decompressed = DeflateDecompressor.Decompress(new MemoryStream(dynamicBlock));
            string resultStr = Encoding.ASCII.GetString(decompressed);
            Assert(resultStr == "AAAA", "Dynamic Huffman deve descomprimir string 'AAAA' com fidelidade");
        }

        private static void Test_Png_Filters_All5_RoundTrip()
        {
            byte[] currentRaw = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 };
            byte[] priorRaw = new byte[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95, 105, 115, 125, 135, 145, 155 };

            for (byte f = 0; f <= 4; f++)
            {
                byte[] filtered = new byte[1 + currentRaw.Length];
                PngFilter.FilterScanline(f, currentRaw, priorRaw, filtered, currentRaw.Length, 4);
                Assert(filtered[0] == f, "Byte 0 deve registrar o tipo de filtro");

                byte[] reconstructed = new byte[currentRaw.Length];
                PngFilter.UnfilterScanline(f, filtered, 1, priorRaw, reconstructed, currentRaw.Length, 4);

                for (int i = 0; i < currentRaw.Length; i++)
                {
                    Assert(reconstructed[i] == currentRaw[i],
                        string.Format("Filtro {0}: byte {1} reconstruído (0x{2:X2}) != original (0x{3:X2})",
                        f, i, reconstructed[i], currentRaw[i]));
                }
            }
        }

        private static void Test_Png_Independent_Raw_Rgb24_Fixture()
        {
            // Imagem 2x2 24-bit Truecolor RGB (ColorType 2)
            // Linha 0 (Filtro 0 = None): Red (255, 0, 0), Green (0, 255, 0)
            // Linha 1 (Filtro 1 = Sub): Blue (0, 0, 255), White (255, 255, 255)
            // Filtro Sub na linha 1:
            //   Pixel 0 (Blue: 0, 0, 255): diff de 0 é (0, 0, 255)
            //   Pixel 1 (White: 255, 255, 255): diff de Pixel 0 é (255-0=255, 255-0=255, 255-255=0)
            byte[] scanlines = new byte[]
            {
                // Linha 0 (Filtro None = 0)
                0x00, 255, 0, 0,  0, 255, 0,
                // Linha 1 (Filtro Sub = 1)
                0x01, 0, 0, 255,  255, 255, 0
            };

            byte[] pngBytes = CreateRawPng(2, 2, false, scanlines);
            PixelBuffer buf = PngDecoder.Decode(pngBytes);

            Assert(buf.Width == 2 && buf.Height == 2, "Dimensões devem ser 2x2");
            Assert(buf.GetPixel(0, 0).R == 255 && buf.GetPixel(0, 0).G == 0 && buf.GetPixel(0, 0).B == 0 && buf.GetPixel(0, 0).A == 255, "(0,0) deve ser Vermelho opaco");
            Assert(buf.GetPixel(1, 0).R == 0 && buf.GetPixel(1, 0).G == 255 && buf.GetPixel(1, 0).B == 0 && buf.GetPixel(1, 0).A == 255, "(1,0) deve ser Verde opaco");
            Assert(buf.GetPixel(0, 1).R == 0 && buf.GetPixel(0, 1).G == 0 && buf.GetPixel(0, 1).B == 255 && buf.GetPixel(0, 1).A == 255, "(0,1) deve ser Azul opaco");
            Assert(buf.GetPixel(1, 1).R == 255 && buf.GetPixel(1, 1).G == 255 && buf.GetPixel(1, 1).B == 255 && buf.GetPixel(1, 1).A == 255, "(1,1) deve ser Branco opaco");
        }

        private static void Test_Png_Independent_Raw_Rgba32_Fixture()
        {
            // Imagem 2x2 32-bit Truecolor RGBA (ColorType 6)
            // Linha 0 (Filtro 0 = None): (255, 0, 0, 0), (0, 255, 0, 64)
            // Linha 1 (Filtro 2 = Up):   (0, 0, 255, 128), (255, 255, 0, 255)
            // Filtro Up na linha 1: diff de Linha 0:
            //   Pixel 0: (0-255)=1, (0-0)=0, (255-0)=255, (128-0)=128
            //   Pixel 1: (255-0)=255, (255-255)=0, (0-0)=0, (255-64)=191
            byte[] scanlines = new byte[]
            {
                // Linha 0 (Filtro None = 0)
                0x00, 255, 0, 0, 0,  0, 255, 0, 64,
                // Linha 1 (Filtro Up = 2)
                0x02, 1, 0, 255, 128,  255, 0, 0, 191
            };

            byte[] pngBytes = CreateRawPng(2, 2, true, scanlines);
            PixelBuffer buf = PngDecoder.Decode(pngBytes);

            Assert(buf.Width == 2 && buf.Height == 2, "Dimensões devem ser 2x2");
            Assert(buf.GetPixel(0, 0).R == 255 && buf.GetPixel(0, 0).A == 0, "(0,0) deve preservar canal Alfa = 0");
            Assert(buf.GetPixel(1, 0).G == 255 && buf.GetPixel(1, 0).A == 64, "(1,0) deve preservar canal Alfa = 64");
            Assert(buf.GetPixel(0, 1).B == 255 && buf.GetPixel(0, 1).A == 128, "(0,1) deve preservar canal Alfa = 128");
            Assert(buf.GetPixel(1, 1).R == 255 && buf.GetPixel(1, 1).G == 255 && buf.GetPixel(1, 1).A == 255, "(1,1) deve preservar canal Alfa = 255");
        }

        private static void Test_Png_Multiple_Idat_Chunks()
        {
            // Gera um PNG onde o fluxo ZLIB é fragmentado em múltiplos chunks IDAT
            byte[] scanlines = new byte[]
            {
                0x00, 10, 20, 30,  40, 50, 60,
                0x00, 70, 80, 90, 100, 110, 120
            };

            using (var zlibMs = new MemoryStream())
            {
                zlibMs.WriteByte(0x78); zlibMs.WriteByte(0x01);
                zlibMs.WriteByte(0x01);
                ushort len = (ushort)scanlines.Length;
                ushort nlen = (ushort)(~len);
                zlibMs.WriteByte((byte)(len & 0xFF)); zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(nlen & 0xFF)); zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                zlibMs.Write(scanlines, 0, scanlines.Length);

                uint adler = Adler32.Calculate(scanlines, 0, scanlines.Length);
                zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(adler & 0xFF));

                byte[] zlibBytes = zlibMs.ToArray();

                using (var ms = new MemoryStream())
                {
                    ms.Write(PngHeader.PngSignature, 0, 8);
                    WriteRawChunk(ms, "IHDR", new byte[] { 0, 0, 0, 2, 0, 0, 0, 2, 8, 2, 0, 0, 0 });

                    // Divide em 3 chunks IDAT consecutivos
                    int part1 = 7;
                    int part2 = 8;
                    int part3 = zlibBytes.Length - part1 - part2;

                    byte[] c1 = new byte[part1];
                    Buffer.BlockCopy(zlibBytes, 0, c1, 0, part1);
                    WriteRawChunk(ms, "IDAT", c1);

                    byte[] c2 = new byte[part2];
                    Buffer.BlockCopy(zlibBytes, part1, c2, 0, part2);
                    WriteRawChunk(ms, "IDAT", c2);

                    byte[] c3 = new byte[part3];
                    Buffer.BlockCopy(zlibBytes, part1 + part2, c3, 0, part3);
                    WriteRawChunk(ms, "IDAT", c3);

                    WriteRawChunk(ms, "IEND", new byte[0]);

                    byte[] multiIdatPng = ms.ToArray();
                    PixelBuffer buf = PngDecoder.Decode(multiIdatPng);
                    Assert(buf.Width == 2 && buf.Height == 2, "Decodificação com múltiplos IDATs deve ser íntegra");
                    Assert(buf.GetPixel(0, 0).R == 10 && buf.GetPixel(1, 1).B == 120, "Pixels reconstruídos com perfeição");
                }
            }
        }

        private static void Test_Png_RoundTrip_Rgb_And_Rgba()
        {
            var orig = new PixelBuffer(12, 10);
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    byte r = (byte)(x * 20);
                    byte g = (byte)(y * 25);
                    byte b = (byte)((x + y) * 10);
                    byte a = (byte)(50 + (x * y) % 205);
                    orig.SetPixel(x, y, new PixelColor(r, g, b, a));
                }
            }

            // 1. Round-trip RGBA (32-bit)
            byte[] rgbaPng = PngEncoder.Encode(orig, true, PngFilter.FilterNone);
            PixelBuffer decRgba = PngDecoder.Decode(rgbaPng);
            Assert(decRgba.Width == 12 && decRgba.Height == 10, "Largura e altura RGBA preservadas");
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    PixelColor o = orig.GetPixel(x, y);
                    PixelColor d = decRgba.GetPixel(x, y);
                    Assert(o.R == d.R && o.G == d.G && o.B == d.B && o.A == d.A,
                        string.Format("Pixel RGBA ({0},{1}) divergente", x, y));
                }
            }

            // 2. Round-trip RGB (24-bit)
            byte[] rgbPng = PngEncoder.Encode(orig, false, PngFilter.FilterNone);
            PixelBuffer decRgb = PngDecoder.Decode(rgbPng);
            Assert(decRgb.Width == 12 && decRgb.Height == 10, "Largura e altura RGB preservadas");
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    PixelColor o = orig.GetPixel(x, y);
                    PixelColor d = decRgb.GetPixel(x, y);
                    Assert(o.R == d.R && o.G == d.G && o.B == d.B && d.A == 255,
                        string.Format("Pixel RGB ({0},{1}) divergente", x, y));
                }
            }
        }

        private static void Test_Png_Encoder_All5_Filters()
        {
            var buf = new PixelBuffer(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    buf.SetPixel(x, y, new PixelColor((byte)(x * 30), (byte)(y * 30), (byte)((x + y) * 15), 255));
                }
            }

            for (byte f = 0; f <= 4; f++)
            {
                byte[] png = PngEncoder.Encode(buf, true, f);
                PixelBuffer dec = PngDecoder.Decode(png);
                Assert(dec.Width == 8 && dec.Height == 8, "Dimensões preservadas no filtro " + f);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        PixelColor o = buf.GetPixel(x, y);
                        PixelColor d = dec.GetPixel(x, y);
                        Assert(o.R == d.R && o.G == d.G && o.B == d.B && o.A == d.A,
                            string.Format("Filtro {0}: Pixel ({1},{2}) divergente", f, x, y));
                    }
                }
            }
        }

        private static void Test_Png_Security_StrictRejections()
        {
            byte[] validPng = CreateRawPng(1, 1, false, new byte[] { 0, 255, 255, 255 });

            // 1. Assinatura mágica inválida
            byte[] badSig = (byte[])validPng.Clone();
            badSig[0] = 0x00;
            bool threwSig = false;
            try { PngDecoder.Decode(badSig); } catch (CorruptDataException) { threwSig = true; }
            Assert(threwSig, "Rejeita assinatura mágica inválida");

            // 2. Arquivo truncado
            byte[] truncated = new byte[7];
            Array.Copy(validPng, 0, truncated, 0, 7);
            bool threwTrunc = false;
            try { PngDecoder.Decode(truncated); } catch (CorruptDataException) { threwTrunc = true; }
            Assert(threwTrunc, "Rejeita arquivo truncado antes da assinatura completa");

            // 3. CRC-32 do chunk IHDR adulterado
            byte[] badCrc = (byte[])validPng.Clone();
            badCrc[29] ^= 0xFF; // corrompe CRC do IHDR
            bool threwCrc = false;
            try { PngDecoder.Decode(badCrc); } catch (CorruptDataException) { threwCrc = true; }
            Assert(threwCrc, "Rejeita chunk com CRC-32 adulterado");

            // 4. ColorType 3 (Indexado com paleta)
            byte[] indexedIhdr = (byte[])validPng.Clone();
            indexedIhdr[25] = 3; // ColorType = 3 (offset 16 + 9)
            uint crcIndexed = PngCrc32.Calculate("IHDR", indexedIhdr, 16, 13);
            indexedIhdr[29] = (byte)((crcIndexed >> 24) & 0xFF);
            indexedIhdr[30] = (byte)((crcIndexed >> 16) & 0xFF);
            indexedIhdr[31] = (byte)((crcIndexed >> 8) & 0xFF);
            indexedIhdr[32] = (byte)(crcIndexed & 0xFF);
            bool threwIndexed = false;
            try { PngDecoder.Decode(indexedIhdr); } catch (UnsupportedFormatException) { threwIndexed = true; }
            Assert(threwIndexed, "Rejeita estritamente PNG indexado (ColorType 3)");

            // 5. ColorType 0 (Escala de cinza)
            byte[] grayIhdr = (byte[])validPng.Clone();
            grayIhdr[25] = 0; // ColorType = 0
            uint crcGray = PngCrc32.Calculate("IHDR", grayIhdr, 16, 13);
            grayIhdr[29] = (byte)((crcGray >> 24) & 0xFF);
            grayIhdr[30] = (byte)((crcGray >> 16) & 0xFF);
            grayIhdr[31] = (byte)((crcGray >> 8) & 0xFF);
            grayIhdr[32] = (byte)(crcGray & 0xFF);
            bool threwGray = false;
            try { PngDecoder.Decode(grayIhdr); } catch (UnsupportedFormatException) { threwGray = true; }
            Assert(threwGray, "Rejeita estritamente PNG em escala de cinza (ColorType 0)");

            // 6. InterlaceMethod = 1 (Adam7)
            byte[] adam7Ihdr = (byte[])validPng.Clone();
            adam7Ihdr[28] = 1; // Interlace = 1 (offset 16 + 12)
            uint crcAdam7 = PngCrc32.Calculate("IHDR", adam7Ihdr, 16, 13);
            adam7Ihdr[29] = (byte)((crcAdam7 >> 24) & 0xFF);
            adam7Ihdr[30] = (byte)((crcAdam7 >> 16) & 0xFF);
            adam7Ihdr[31] = (byte)((crcAdam7 >> 8) & 0xFF);
            adam7Ihdr[32] = (byte)(crcAdam7 & 0xFF);
            bool threwAdam7 = false;
            try { PngDecoder.Decode(adam7Ihdr); } catch (UnsupportedFormatException) { threwAdam7 = true; }
            Assert(threwAdam7, "Rejeita estritamente entrelaçamento Adam7");

            // 7. Dimensão zero (Width = 0)
            byte[] zeroWIhdr = (byte[])validPng.Clone();
            zeroWIhdr[16] = 0; zeroWIhdr[17] = 0; zeroWIhdr[18] = 0; zeroWIhdr[19] = 0;
            uint crcZeroW = PngCrc32.Calculate("IHDR", zeroWIhdr, 16, 13);
            zeroWIhdr[29] = (byte)((crcZeroW >> 24) & 0xFF);
            zeroWIhdr[30] = (byte)((crcZeroW >> 16) & 0xFF);
            zeroWIhdr[31] = (byte)((crcZeroW >> 8) & 0xFF);
            zeroWIhdr[32] = (byte)(crcZeroW & 0xFF);
            bool threwZeroW = false;
            try { PngDecoder.Decode(zeroWIhdr); } catch (CorruptDataException) { threwZeroW = true; }
            Assert(threwZeroW, "Rejeita largura zero (0x0)");

            // 8. Dimensão excessiva (> 16384)
            byte[] hugeWIhdr = (byte[])validPng.Clone();
            hugeWIhdr[16] = 0; hugeWIhdr[17] = 0; hugeWIhdr[18] = 0x50; hugeWIhdr[19] = 0x00; // 20480
            uint crcHugeW = PngCrc32.Calculate("IHDR", hugeWIhdr, 16, 13);
            hugeWIhdr[29] = (byte)((crcHugeW >> 24) & 0xFF);
            hugeWIhdr[30] = (byte)((crcHugeW >> 16) & 0xFF);
            hugeWIhdr[31] = (byte)((crcHugeW >> 8) & 0xFF);
            hugeWIhdr[32] = (byte)(crcHugeW & 0xFF);
            bool threwHugeW = false;
            try { PngDecoder.Decode(hugeWIhdr); } catch (ResourceLimitException) { threwHugeW = true; }
            Assert(threwHugeW, "Protege contra dimensões excessivas (> 16384)");
        }

        private static void Test_Png_Engine_Inspect()
        {
            var engine = new PngEngine();
            var buf = new PixelBuffer(25, 15);
            buf.SetPixel(0, 0, new PixelColor(255, 0, 0, 100)); // transparente -> força RGBA
            byte[] png = PngEncoder.Encode(buf, true, PngFilter.FilterNone);

            using (var ms = new MemoryStream(png))
            {
                FormatInspection insp = engine.Inspect(ms, "test.png");
                Assert(insp.IsSupported, "PNG deve ser inspecionado como suportado");
                Assert(insp.FormatId == "png", "FormatId deve ser 'png'");
                Assert(insp.Width == 25, "Largura deve ser 25");
                Assert(insp.Height == 15, "Altura deve ser 15");
                Assert(insp.Metadata["BitDepth"] == "8", "BitDepth deve ser 8");
                Assert(insp.Metadata["ColorType"] == "Truecolor RGBA (6)", "ColorType deve ser Truecolor RGBA (6)");
                Assert(insp.Metadata["HasAlpha"] == "true", "HasAlpha deve ser true");
            }
        }

        private static void Test_Png_Engine_Convert_Transforms()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_png_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string inputPng = Path.Combine(tempDir, "source.png");
                var buf = new PixelBuffer(50, 50);
                for (int y = 0; y < 50; y++)
                {
                    for (int x = 0; x < 50; x++)
                    {
                        buf.SetPixel(x, y, new PixelColor((byte)(x * 5), (byte)(y * 5), 100, 255));
                    }
                }
                File.WriteAllBytes(inputPng, PngEncoder.Encode(buf));

                var engine = new PngEngine();
                var req = new ConvertRequest
                {
                    InputPath = inputPng,
                    TargetFormat = "png",
                    OutputDirectory = tempDir,
                    OutputFileName = "transformed"
                };
                req.Settings["cropWidth"] = "30";
                req.Settings["cropHeight"] = "30";
                req.Settings["cropPosition"] = "center";
                req.Settings["resizeWidth"] = "15";
                req.Settings["resizeHeight"] = "15";
                req.Settings["filter"] = "bilinear";
                req.Settings["watermarkText"] = "LODINO";

                ConvertResult res = engine.Convert(req, null);
                Assert(res.Status == "completed", "Conversão com transformações deve ter sucesso");
                Assert(File.Exists(res.OutputPath), "Arquivo de saída deve existir");

                byte[] outBytes = File.ReadAllBytes(res.OutputPath);
                PixelBuffer outBuf = PngDecoder.Decode(outBytes);
                Assert(outBuf.Width == 15 && outBuf.Height == 15, "Dimensões pós-crop e resize devem ser 15x15");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static void Test_Png_Engine_Bidirectional_Bmp_Png()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_bidir_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Cria imagem original BMP
                string origBmp = Path.Combine(tempDir, "orig.bmp");
                var origBuf = new PixelBuffer(8, 8);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        origBuf.SetPixel(x, y, new PixelColor((byte)(x * 32), (byte)(y * 32), 128, 255));
                    }
                }
                File.WriteAllBytes(origBmp, BmpEncoder.Encode(origBuf, 24));

                // 2. Resolve motor para BMP -> PNG
                ICoreEngine bmpToPngEngine = EngineRegistry.Default.Resolve("bmp", "png");
                Assert(bmpToPngEngine.EngineId == "image-png", "EngineRegistry deve resolver image-png para rota bmp -> png");

                var reqBmpToPng = new ConvertRequest
                {
                    InputPath = origBmp,
                    TargetFormat = "png",
                    OutputDirectory = tempDir,
                    OutputFileName = "converted_from_bmp"
                };
                ConvertResult resPng = bmpToPngEngine.Convert(reqBmpToPng, null);
                Assert(resPng.Status == "completed", "Conversão BMP -> PNG deve ser bem-sucedida");

                // Inspeciona PNG gerado
                using (var pngStream = File.OpenRead(resPng.OutputPath))
                {
                    FormatInspection insp = bmpToPngEngine.Inspect(pngStream, resPng.OutputPath);
                    Assert(insp.IsSupported && insp.Width == 8 && insp.Height == 8, "PNG convertido deve ter dimensões 8x8");
                }

                // 3. Resolve motor para PNG -> BMP
                ICoreEngine pngToBmpEngine = EngineRegistry.Default.Resolve("png", "bmp");
                Assert(pngToBmpEngine.EngineId == "image-png", "EngineRegistry deve resolver image-png para rota png -> bmp");

                var reqPngToBmp = new ConvertRequest
                {
                    InputPath = resPng.OutputPath,
                    TargetFormat = "bmp",
                    OutputDirectory = tempDir,
                    OutputFileName = "converted_back_to_bmp"
                };
                ConvertResult resBmp = pngToBmpEngine.Convert(reqPngToBmp, null);
                Assert(resBmp.Status == "completed", "Conversão reversa PNG -> BMP deve ser bem-sucedida");

                // Verifica fidelidade dos pixels do BMP re-convertido
                byte[] finalBmpBytes = File.ReadAllBytes(resBmp.OutputPath);
                PixelBuffer finalBuf = BmpDecoder.Decode(finalBmpBytes);
                Assert(finalBuf.Width == 8 && finalBuf.Height == 8, "BMP re-convertido deve manter 8x8");

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        PixelColor o = origBuf.GetPixel(x, y);
                        PixelColor f = finalBuf.GetPixel(x, y);
                        Assert(o.R == f.R && o.G == f.G && o.B == f.B,
                            string.Format("Pixel ({0},{1}) divergente na rota bidirecional", x, y));
                    }
                }

                // 4. Valida resolução das três rotas no EngineRegistry
                Assert(EngineRegistry.Default.Resolve("png", "png").EngineId == "image-png", "Resolução png -> png");
                Assert(EngineRegistry.Default.Resolve("png", "bmp").EngineId == "image-png", "Resolução png -> bmp");
                Assert(EngineRegistry.Default.Resolve("bmp", "png").EngineId == "image-png", "Resolução bmp -> png");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #region Hardening BMP + PNG (Testes de Segurança e Resiliência)

        private class FragmentedStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _maxPerRead;

            public FragmentedStream(Stream inner, int maxPerRead = 3)
            {
                _inner = inner;
                _maxPerRead = maxPerRead;
            }

            public override bool CanRead { get { return _inner.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int toRead = Math.Min(count, _maxPerRead);
                return _inner.Read(buffer, offset, toRead);
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private class NonSeekableStream : Stream
        {
            private readonly Stream _inner;

            public NonSeekableStream(Stream inner)
            {
                _inner = inner;
            }

            public override bool CanRead { get { return _inner.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private static void Test_Hardening_DecompressionBomb_Protection()
        {
            // Dimensões declaradas pequenas: 2x2 RGBA -> espera exatamente 2 * (1 + 8) = 18 bytes
            // Criamos um payload comprimido com DEFLATE Stored de 2000 bytes (> 18 bytes)
            byte[] bombPayload = new byte[2000];
            for (int i = 0; i < bombPayload.Length; i++) bombPayload[i] = (byte)(i & 0xFF);

            using (var zlibMs = new MemoryStream())
            {
                zlibMs.WriteByte(0x78);
                zlibMs.WriteByte(0x01);

                // Bloco DEFLATE Stored
                zlibMs.WriteByte(0x01); // BFINAL=1, BTYPE=00
                ushort len = (ushort)bombPayload.Length;
                ushort nlen = (ushort)(~len);
                zlibMs.WriteByte((byte)(len & 0xFF));
                zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(nlen & 0xFF));
                zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                zlibMs.Write(bombPayload, 0, bombPayload.Length);

                uint adler = Adler32.Calculate(bombPayload, 0, bombPayload.Length);
                zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(adler & 0xFF));

                byte[] zlibBytes = zlibMs.ToArray();

                using (var pngMs = new MemoryStream())
                {
                    pngMs.Write(PngHeader.PngSignature, 0, 8);

                    // IHDR 2x2 RGBA
                    byte[] ihdr = new byte[13];
                    ihdr[3] = 2; // Width = 2
                    ihdr[7] = 2; // Height = 2
                    ihdr[8] = 8; // 8-bit
                    ihdr[9] = 6; // RGBA
                    WriteRawChunk(pngMs, "IHDR", ihdr);

                    WriteRawChunk(pngMs, "IDAT", zlibBytes);
                    WriteRawChunk(pngMs, "IEND", new byte[0]);

                    byte[] bombPng = pngMs.ToArray();

                    bool stoppedBomb = false;
                    try
                    {
                        PngDecoder.Decode(bombPng);
                    }
                    catch (ResourceLimitException)
                    {
                        stoppedBomb = true;
                    }
                    Assert(stoppedBomb, "Decompression Bomb deve ser abortada imediatamente com ResourceLimitException ao exceder bytes esperados");
                }
            }
        }

        private static void Test_Hardening_Idat_Chunking_Generation()
        {
            // Imagem 100x100 RGBA gera ~40 KB de dados descomprimidos
            // Ao codificar, deve particionar em chunks IDAT de até 32 KB cada
            var pb = new PixelBuffer(100, 100);
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 100, 255));
                }
            }

            byte[] pngBytes = PngEncoder.Encode(pb, true);

            // Inspeciona os chunks do arquivo gerado
            int idatCount = 0;
            using (var ms = new MemoryStream(pngBytes))
            {
                byte[] sig = new byte[8];
                ms.Read(sig, 0, 8);

                PngChunk chunk;
                while ((chunk = PngChunk.Read(ms)) != null)
                {
                    if (chunk.Type == "IDAT")
                    {
                        idatCount++;
                        Assert(chunk.Data.Length <= 32 * 1024, "Cada chunk IDAT não deve ultrapassar 32 KB");
                    }
                }
            }

            Assert(idatCount >= 2, string.Format("Encoder deve gerar múltiplos chunks IDAT para imagem de ~40 KB (gerados: {0})", idatCount));

            // Decodifica e verifica fidelidade
            PixelBuffer decoded = PngDecoder.Decode(pngBytes);
            AssertEqual(100, decoded.Width, "Width pós-chunking");
            AssertEqual(100, decoded.Height, "Height pós-chunking");
            AssertEqual(pb.GetPixel(50, 50), decoded.GetPixel(50, 50), "Pixel fidelity pós multi-chunk IDAT");
        }

        private static void Test_Hardening_Stream_Fragmentation_Resilience()
        {
            // Testa BMP com stream fragmentado (2 bytes por Read)
            var pbBmp = new PixelBuffer(5, 5);
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    pbBmp.SetPixel(x, y, new PixelColor((byte)(x * 50), (byte)(y * 50), 200, 255));

            byte[] bmpBytes = BmpEncoder.Encode(pbBmp, 24);
            using (var innerBmp = new MemoryStream(bmpBytes))
            using (var fragBmp = new FragmentedStream(innerBmp, 2))
            {
                PixelBuffer decodedBmp = BmpDecoder.Decode(fragBmp);
                AssertEqual(5, decodedBmp.Width, "BMP Width via FragmentedStream");
                AssertEqual(5, decodedBmp.Height, "BMP Height via FragmentedStream");
                AssertEqual(pbBmp.GetPixel(2, 2), decodedBmp.GetPixel(2, 2), "BMP Pixel (2,2) via FragmentedStream");
            }

            // Testa PNG com stream fragmentado (1 byte por Read!)
            var pbPng = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pbPng.SetPixel(x, y, new PixelColor((byte)(x * 60), (byte)(y * 60), 150, 200));

            byte[] pngBytes = PngEncoder.Encode(pbPng, true);
            using (var innerPng = new MemoryStream(pngBytes))
            using (var fragPng = new FragmentedStream(innerPng, 1))
            {
                PixelBuffer decodedPng = PngDecoder.Decode(fragPng);
                AssertEqual(4, decodedPng.Width, "PNG Width via FragmentedStream (1 byte/read)");
                AssertEqual(4, decodedPng.Height, "PNG Height via FragmentedStream (1 byte/read)");
                AssertEqual(pbPng.GetPixel(3, 3), decodedPng.GetPixel(3, 3), "PNG Pixel (3,3) via FragmentedStream");
            }
        }

        private static void Test_Hardening_PixelBuffer_Crop_Overflow()
        {
            var pb = new PixelBuffer(10, 10);

            bool threwXOverflow = false;
            try
            {
                pb.Crop(int.MaxValue - 5, 0, 10, 10);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwXOverflow = true;
            }
            Assert(threwXOverflow, "Crop com x próximo a int.MaxValue deve lançar ArgumentOutOfRangeException");

            bool threwYOverflow = false;
            try
            {
                pb.Crop(0, int.MaxValue - 5, 10, 10);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwYOverflow = true;
            }
            Assert(threwYOverflow, "Crop com y próximo a int.MaxValue deve lançar ArgumentOutOfRangeException");
        }

        private static void Test_Hardening_Deflate_Lz77_Distance_Underflow()
        {
            // Cria um fluxo DEFLATE onde o primeiro símbolo é uma referência retroativa (Length 3, Distance 1)
            // antes de emitir qualquer byte literal
            var bitWriter = new SimpleBitWriter();
            bitWriter.WriteBits(1, 1); // BFINAL = 1
            bitWriter.WriteBits(1, 2); // BTYPE = 01 (Huffman Fixo)

            // Símbolo 257 (comprimento 3) em Huffman fixo: código canônico 1, 7 bits '0000001'
            bitWriter.WriteHuffman(1, 7);

            // Símbolo de distância 0 (distância 1): 5 bits '00000'
            bitWriter.WriteBits(0, 5);

            byte[] deflateStream = bitWriter.ToArray();

            bool threwDistanceUnderflow = false;
            try
            {
                using (var ms = new MemoryStream(deflateStream))
                {
                    DeflateDecompressor.Decompress(ms, 1024);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("Distância LZ77") || ex.Message.Contains("antes do início do fluxo"))
                {
                    threwDistanceUnderflow = true;
                }
            }
            Assert(threwDistanceUnderflow, "Distância LZ77 anterior ao início do fluxo deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Deflate_Dynamic_Repeat_Overflow()
        {
            // Constrói cabeçalho dinâmico DEFLATE com código de repetição 18 estourando totalCodes
            var bitWriter = new SimpleBitWriter();
            bitWriter.WriteBits(1, 1); // BFINAL = 1
            bitWriter.WriteBits(2, 2); // BTYPE = 10 (Huffman Dinâmico)

            // HLIT = 0 (257 literais), HDIST = 0 (1 distância) -> totalCodes = 258
            bitWriter.WriteBits(0, 5);
            bitWriter.WriteBits(0, 5);
            // HCLEN = 1 (5 comprimentos para clLengths: 16, 17, 18, 0, 8)
            bitWriter.WriteBits(1, 4);

            // 5 comprimentos de 3 bits cada:
            // 16: len 0 (3 bits: 0)
            bitWriter.WriteBits(0, 3);
            // 17: len 0 (3 bits: 0)
            bitWriter.WriteBits(0, 3);
            // 18: len 1 (código '1' porque símbolo 0 vem antes no vetor e recebe '0')
            bitWriter.WriteBits(1, 3);
            // 0: len 1 (código '0')
            bitWriter.WriteBits(1, 3);
            // 8: len 0
            bitWriter.WriteBits(0, 3);

            // Agora escrevemos os códigos de comprimento para literais/distâncias:
            // Código 18 (bit '1') com 7 bits de repetição (valor 127 -> repete 11 + 127 = 138 zeros)
            bitWriter.WriteBits(1, 1);
            bitWriter.WriteBits(127, 7);

            // Código 18 novamente: repete mais 138 zeros -> total 276 > 258!
            bitWriter.WriteBits(1, 1);
            bitWriter.WriteBits(127, 7);

            byte[] deflateStream = bitWriter.ToArray();

            bool threwRepeatOverflow = false;
            try
            {
                using (var ms = new MemoryStream(deflateStream))
                {
                    DeflateDecompressor.Decompress(ms, 1024);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("ultrapassa a quantidade total de comprimentos"))
                {
                    threwRepeatOverflow = true;
                }
            }
            Assert(threwRepeatOverflow, "Código de repetição DEFLATE estourando totalCodes deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Huffman_Oversubscribed_Tree()
        {
            // Três símbolos de comprimento 1 violam Kraft: 1/2 + 1/2 + 1/2 = 1.5 > 1
            int[] codeLengths = new int[] { 1, 1, 1 };

            bool threwOversubscribed = false;
            try
            {
                HuffmanTree.Build(codeLengths);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("sobre-subscrita") || ex.Message.Contains("inválida"))
                {
                    threwOversubscribed = true;
                }
            }
            Assert(threwOversubscribed, "Árvore de Huffman sobre-subscrita deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Bmp_NonSeekable_Stream()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 50), 120, (byte)(y * 50), 255));

            byte[] bmpBytes = BmpEncoder.Encode(pb, 24);

            using (var inner = new MemoryStream(bmpBytes))
            using (var nonSeekable = new NonSeekableStream(inner))
            {
                PixelBuffer decoded = BmpDecoder.Decode(nonSeekable);
                AssertEqual(4, decoded.Width, "Decodificação BMP em stream não-seekable");
                AssertEqual(4, decoded.Height, "Decodificação BMP em stream não-seekable");
                AssertEqual(pb.GetPixel(1, 1), decoded.GetPixel(1, 1), "Pixel fidelity em BMP não-seekable");
            }
        }

        private static void Test_Hardening_Zlib_Trailing_Garbage()
        {
            byte[] raw = new byte[] { 10, 20, 30, 40, 50 };
            byte[] compressed = ZlibStream.Compress(raw);

            // Anexa lixo espúrio no final
            byte[] corrupt = new byte[compressed.Length + 1];
            Buffer.BlockCopy(compressed, 0, corrupt, 0, compressed.Length);
            corrupt[corrupt.Length - 1] = 0xAA;

            bool threwTrailing = false;
            try
            {
                ZlibStream.Decompress(corrupt);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("espúrios") || ex.Message.Contains("término"))
                {
                    threwTrailing = true;
                }
            }
            Assert(threwTrailing, "ZLIB com bytes espúrios após Adler-32 deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Png_Idat_TrailingPadding_Allowed()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 60), (byte)(y * 60), 100, 255));

            byte[] validPng = PngEncoder.Encode(pb, true);

            // Reconstrói o PNG adicionando 4 bytes de padding no último IDAT com CRC recalculado
            using (var msIn = new MemoryStream(validPng))
            using (var msOut = new MemoryStream())
            {
                byte[] sig = new byte[8];
                msIn.Read(sig, 0, 8);
                msOut.Write(sig, 0, 8);

                PngChunk chunk;
                while ((chunk = PngChunk.Read(msIn)) != null)
                {
                    if (chunk.Type == "IDAT")
                    {
                        // Adiciona 4 bytes de preenchimento (padding) após o término do fluxo ZLIB
                        byte[] paddedData = new byte[chunk.Data.Length + 4];
                        Buffer.BlockCopy(chunk.Data, 0, paddedData, 0, chunk.Data.Length);
                        // Cria chunk com dados aumentados (PngChunk calcula novo CRC-32 válido)
                        var paddedChunk = new PngChunk("IDAT", paddedData);
                        paddedChunk.Write(msOut);
                    }
                    else
                    {
                        chunk.Write(msOut);
                    }
                }

                byte[] paddedPngBytes = msOut.ToArray();

                // Decodifica via Lodino PngDecoder: deve ignorar bytes residuais conforme ISO/IEC 15948 §10.2
                PixelBuffer decoded = PngDecoder.Decode(paddedPngBytes);
                AssertEqual(4, decoded.Width, "Width com IDAT padded");
                AssertEqual(4, decoded.Height, "Height com IDAT padded");
                AssertEqual(pb.GetPixel(2, 2), decoded.GetPixel(2, 2), "Pixel fidelity com IDAT padded");
            }
        }

        private class SyntheticOverLimitStream : Stream
        {
            private readonly long _totalBytes;
            private long _bytesRead = 0;

            public SyntheticOverLimitStream(long totalBytes)
            {
                _totalBytes = totalBytes;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_bytesRead >= _totalBytes) return 0;
                int toRead = (int)Math.Min(count, _totalBytes - _bytesRead);
                Array.Clear(buffer, offset, toRead);
                _bytesRead += toRead;
                return toRead;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private static void Test_Hardening_Bmp_NonSeekable_Stream_LimitExceeded()
        {
            // Stream sintético não-seekable com 50.000 bytes e limite injetado de 10.000 bytes
            using (var stream = new SyntheticOverLimitStream(50000))
            {
                bool threwLimit = false;
                try
                {
                    BmpDecoder.Decode(stream, 10000);
                }
                catch (ResourceLimitException ex)
                {
                    if (ex.Message.Contains("excede o tamanho máximo permitido"))
                    {
                        threwLimit = true;
                    }
                }
                Assert(threwLimit, "Stream não-seekable excedendo maxStreamBytes deve lançar ResourceLimitException");
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
