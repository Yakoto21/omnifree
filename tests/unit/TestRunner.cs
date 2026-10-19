using System;
using System.IO;
using System.Text;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Data;
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
    }
}
