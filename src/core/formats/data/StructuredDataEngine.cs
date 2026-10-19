using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Data
{
    public class StructuredDataEngine : ICoreEngine
    {
        private static readonly string[] Inputs = new[] { "csv", "json", "ini" };
        private static readonly string[] Outputs = new[] { "csv", "json", "ini" };

        public string EngineId { get { return "lodino.engine.data"; } }
        public string EngineLabel { get { return "Motor Próprio de Dados Estruturados Lodino"; } }
        public string[] SupportedInputs { get { return Inputs; } }
        public string[] SupportedOutputs { get { return Outputs; } }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            string src = (sourceFormat ?? "").ToLowerInvariant().TrimStart('.');
            string dst = (targetFormat ?? "").ToLowerInvariant().TrimStart('.');

            return Array.IndexOf(Inputs, src) >= 0 && Array.IndexOf(Outputs, dst) >= 0;
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            var inspection = new FormatInspection
            {
                FileSizeBytes = inputStream.Length
            };

            string ext = Path.GetExtension(fileNameHint ?? "").TrimStart('.').ToLowerInvariant();
            if (ext == "json")
            {
                inspection.FormatId = "json";
                inspection.MimeType = "application/json";
                inspection.IsSupported = true;
            }
            else if (ext == "csv")
            {
                inspection.FormatId = "csv";
                inspection.MimeType = "text/csv";
                inspection.IsSupported = true;
            }
            else if (ext == "ini")
            {
                inspection.FormatId = "ini";
                inspection.MimeType = "text/plain";
                inspection.IsSupported = true;
            }

            return inspection;
        }

        public ConvertResult Convert(ConvertRequest request, Action<int> onProgress)
        {
            if (request == null) throw new ArgumentNullException("request");
            var sw = Stopwatch.StartNew();

            if (onProgress != null) onProgress(0);

            if (!File.Exists(request.InputPath))
            {
                return ConvertResult.Failure(request.JobId, "Arquivo de entrada não encontrado: " + request.InputPath, sw.ElapsedMilliseconds);
            }

            string sourceFormat = Path.GetExtension(request.InputPath).TrimStart('.').ToLowerInvariant();
            string targetFormat = (request.TargetFormat ?? "").TrimStart('.').ToLowerInvariant();

            if (!CanConvert(sourceFormat, targetFormat))
            {
                return ConvertResult.Failure(request.JobId,
                    string.Format("Combinação de formatos não suportada por este motor: .{0} -> .{1}", sourceFormat, targetFormat),
                    sw.ElapsedMilliseconds);
            }

            var fileInfo = new FileInfo(request.InputPath);
            SecurityLimits.AssertFileSize(fileInfo.Length);

            string rawText;
            using (var sr = new StreamReader(request.InputPath, Encoding.UTF8, true))
            {
                rawText = sr.ReadToEnd();
            }

            if (onProgress != null) onProgress(30);

            string convertedText;
            try
            {
                convertedText = ConvertString(rawText, sourceFormat, targetFormat);
            }
            catch (Exception ex)
            {
                return ConvertResult.Failure(request.JobId, "Erro na conversão: " + ex.Message, sw.ElapsedMilliseconds);
            }

            if (onProgress != null) onProgress(70);

            string outputDir = string.IsNullOrEmpty(request.OutputDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : request.OutputDirectory;

            string stemName = string.IsNullOrEmpty(request.OutputFileName)
                ? Path.GetFileNameWithoutExtension(request.InputPath)
                : request.OutputFileName;

            string outputPath = SafeFileWriter.GetUniqueOutputPath(outputDir, stemName, targetFormat);

            using (var swWriter = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
            {
                swWriter.Write(convertedText);
            }

            if (onProgress != null) onProgress(100);
            sw.Stop();

            return ConvertResult.Success(request.JobId, outputPath, fileInfo.Length, sw.ElapsedMilliseconds);
        }

        public string ConvertString(string input, string sourceFormat, string targetFormat)
        {
            string src = sourceFormat.ToLowerInvariant();
            string dst = targetFormat.ToLowerInvariant();

            if (src == "csv")
            {
                var csv = new CsvParser().Parse(input);
                if (dst == "csv") return new CsvWriter().WriteToString(csv);
                if (dst == "json") return CsvToJson(csv);
                if (dst == "ini") return CsvToIni(csv);
            }
            else if (src == "json")
            {
                var json = new JsonParser().Parse(input);
                if (dst == "json") return new JsonWriter().WriteToString(json);
                if (dst == "csv") return JsonToCsv(json);
                if (dst == "ini") return JsonToIni(json);
            }
            else if (src == "ini")
            {
                var ini = new IniParser().Parse(input);
                if (dst == "ini") return new IniWriter().WriteToString(ini);
                if (dst == "json") return IniToJson(ini);
                if (dst == "csv") return IniToCsv(ini);
            }

            throw new UnsupportedFormatException(string.Format("Conversão de '{0}' para '{1}' não suportada.", src, dst));
        }

        #region Mapeamentos de Conversão Específicos

        private string CsvToJson(CsvDocument csv)
        {
            var arr = new JsonArray();
            for (int r = 0; r < csv.Rows.Count; r++)
            {
                var row = csv.Rows[r];
                var obj = new JsonObject();
                for (int c = 0; c < csv.Headers.Count; c++)
                {
                    string header = csv.Headers[c];
                    string val = c < row.Count ? row[c] : string.Empty;
                    obj.Add(header, new JsonString(val));
                }
                arr.Add(obj);
            }
            return new JsonWriter().WriteToString(arr);
        }

        private string CsvToIni(CsvDocument csv)
        {
            var ini = new IniDocument();
            for (int r = 0; r < csv.Rows.Count; r++)
            {
                var row = csv.Rows[r];
                string sectionName = string.Format("Item_{0}", r + 1);
                if (row.Count > 0 && !string.IsNullOrEmpty(row[0]))
                {
                    sectionName = row[0];
                }

                for (int c = 0; c < csv.Headers.Count; c++)
                {
                    string header = csv.Headers[c];
                    string val = c < row.Count ? row[c] : string.Empty;
                    ini.Set(sectionName, header, val);
                }
            }
            return new IniWriter().WriteToString(ini);
        }

        private string JsonToCsv(JsonNode json)
        {
            var doc = new CsvDocument();

            if (json.Type == JsonType.Array)
            {
                var arr = (JsonArray)json;
                // Discover all distinct keys from objects
                var headers = new List<string>();
                var seenHeaders = new HashSet<string>(StringComparer.Ordinal);

                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i].Type == JsonType.Object)
                    {
                        var obj = (JsonObject)arr[i];
                        foreach (var key in obj.Properties.Keys)
                        {
                            if (seenHeaders.Add(key))
                            {
                                headers.Add(key);
                            }
                        }
                    }
                }

                if (headers.Count == 0)
                {
                    headers.Add("Value");
                    doc = new CsvDocument(headers);
                    for (int i = 0; i < arr.Count; i++)
                    {
                        doc.AddRow(new[] { arr[i].AsString() });
                    }
                }
                else
                {
                    doc = new CsvDocument(headers);
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var row = new List<string>();
                        if (arr[i].Type == JsonType.Object)
                        {
                            var obj = (JsonObject)arr[i];
                            for (int h = 0; h < headers.Count; h++)
                            {
                                JsonNode val;
                                if (obj.Properties.TryGetValue(headers[h], out val))
                                {
                                    row.Add(val.AsString());
                                }
                                else
                                {
                                    row.Add(string.Empty);
                                }
                            }
                        }
                        else
                        {
                            row.Add(arr[i].AsString());
                        }
                        doc.AddRow(row);
                    }
                }
            }
            else if (json.Type == JsonType.Object)
            {
                var obj = (JsonObject)json;
                doc = new CsvDocument(new[] { "Key", "Value" });
                foreach (var kvp in obj.Properties)
                {
                    doc.AddRow(new[] { kvp.Key, kvp.Value.AsString() });
                }
            }
            else
            {
                doc = new CsvDocument(new[] { "Value" });
                doc.AddRow(new[] { json.AsString() });
            }

            return new CsvWriter().WriteToString(doc);
        }

        private string JsonToIni(JsonNode json)
        {
            var ini = new IniDocument();

            if (json.Type == JsonType.Object)
            {
                var root = (JsonObject)json;
                foreach (var kvp in root.Properties)
                {
                    if (kvp.Value.Type == JsonType.Object)
                    {
                        var secObj = (JsonObject)kvp.Value;
                        foreach (var prop in secObj.Properties)
                        {
                            ini.Set(kvp.Key, prop.Key, prop.Value.AsString());
                        }
                    }
                    else
                    {
                        ini.Set(IniDocument.GlobalSectionName, kvp.Key, kvp.Value.AsString());
                    }
                }
            }
            else
            {
                ini.Set(IniDocument.GlobalSectionName, "data", json.AsString());
            }

            return new IniWriter().WriteToString(ini);
        }

        private string IniToJson(IniDocument ini)
        {
            var root = new JsonObject();

            foreach (var secPair in ini.Sections)
            {
                if (secPair.Key == IniDocument.GlobalSectionName)
                {
                    foreach (var prop in secPair.Value)
                    {
                        root.Add(prop.Key, new JsonString(prop.Value));
                    }
                }
                else
                {
                    var secObj = new JsonObject();
                    foreach (var prop in secPair.Value)
                    {
                        secObj.Add(prop.Key, new JsonString(prop.Value));
                    }
                    root.Add(secPair.Key, secObj);
                }
            }

            return new JsonWriter().WriteToString(root);
        }

        private string IniToCsv(IniDocument ini)
        {
            var doc = new CsvDocument(new[] { "Section", "Key", "Value" });

            foreach (var secPair in ini.Sections)
            {
                string secName = string.IsNullOrEmpty(secPair.Key) ? "(Global)" : secPair.Key;
                foreach (var prop in secPair.Value)
                {
                    doc.AddRow(new[] { secName, prop.Key, prop.Value });
                }
            }

            return new CsvWriter().WriteToString(doc);
        }

        #endregion
    }
}
