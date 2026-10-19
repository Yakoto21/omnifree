using System;
using System.IO;
using System.Text;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Data;
using Lodino.Core.Formats.Image;
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
            RunTest("BMP: Round-trip de fidelidade 32-bit BGRA com canal alfa", Test_Bmp_32bit_Alpha_RoundTrip);
            RunTest("BMP: Normalização de 32-bit BGRX (todos alfa zerados -> 255)", Test_Bmp_32bit_Bgrx_AlphaRecovery);

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

            // --- BMP: Integração do Motor BmpEngine e Registry ---
            RunTest("BMP (Engine): Inspeção de metadados via BmpEngine.Inspect", Test_Bmp_Engine_Inspect);
            RunTest("BMP (Engine): Pipeline completo de conversão e transformações via BmpEngine", Test_Bmp_Engine_ConvertPipeline);
            RunTest("REGISTRY: Resolução dinâmica do motor image-bmp para bmp -> bmp", Test_Bmp_Registry_Resolution);

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

        private static void Test_Bmp_32bit_Alpha_RoundTrip()
        {
            var pb = new PixelBuffer(4, 4);
            pb.SetPixel(0, 0, new PixelColor(255, 0, 0, 128));
            pb.SetPixel(1, 1, new PixelColor(0, 255, 0, 64));
            pb.SetPixel(2, 2, new PixelColor(0, 0, 255, 200));
            pb.SetPixel(3, 3, new PixelColor(255, 255, 255, 0));
            byte[] encoded = BmpEncoder.Encode(pb, 32);
            AssertEqual(118, encoded.Length, "Tamanho total 32-bit BMP");
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(new PixelColor(255, 0, 0, 128), decoded.GetPixel(0, 0), "Pixel com alfa 128");
            AssertEqual(new PixelColor(0, 255, 0, 64), decoded.GetPixel(1, 1), "Pixel com alfa 64");
            AssertEqual(new PixelColor(0, 0, 255, 200), decoded.GetPixel(2, 2), "Pixel com alfa 200");
            AssertEqual(new PixelColor(255, 255, 255, 0), decoded.GetPixel(3, 3), "Pixel com alfa 0");
        }

        private static void Test_Bmp_32bit_Bgrx_AlphaRecovery()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, new PixelColor(100, 150, 200, 0));
            pb.SetPixel(1, 0, new PixelColor(110, 160, 210, 0));
            pb.SetPixel(0, 1, new PixelColor(120, 170, 220, 0));
            pb.SetPixel(1, 1, new PixelColor(130, 180, 230, 0));
            byte[] encoded = BmpEncoder.Encode(pb, 32);
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual((byte)255, decoded.GetPixel(0, 0).A, "Alfa normalizado para 255 em BGRX");
            AssertEqual((byte)255, decoded.GetPixel(1, 1).A, "Alfa normalizado para 255 em BGRX");
            AssertEqual((byte)100, decoded.GetPixel(0, 0).R, "Canal R preservado");
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

        #endregion
    }
}
