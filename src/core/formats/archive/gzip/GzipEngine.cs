using System;
using System.Diagnostics;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Archive.Zip;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Motor de integração do formato GZIP (RFC 1952) com o núcleo nativo Lodino.
    /// Suporta inspeção de metadados, compressão de arquivo único e descompressão.
    /// </summary>
    public class GzipEngine : ICoreEngine
    {
        public string EngineId
        {
            get { return "archive-gzip"; }
        }

        public string EngineLabel
        {
            get { return "Motor Nativo de Compressão GZIP (RFC 1952)"; }
        }

        public string[] SupportedInputs
        {
            get { return new[] { "gz", "gzip" }; }
        }

        public string[] SupportedOutputs
        {
            get { return new[] { "gz", "gzip", "extract", "decompress", "ungz" }; }
        }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;
            string src = sourceFormat.TrimStart('.').ToLowerInvariant();
            string tgt = targetFormat.TrimStart('.').ToLowerInvariant();

            // Descompressão: gz/gzip -> extract/decompress/ungz
            if ((src == "gz" || src == "gzip") && (tgt == "extract" || tgt == "decompress" || tgt == "ungz"))
            {
                return true;
            }

            // Compactação: qualquer extensão -> gz/gzip
            if (tgt == "gz" || tgt == "gzip")
            {
                return true;
            }

            // Descompressão direta indicando o formato original: gz -> txt, gz -> csv, etc.
            if ((src == "gz" || src == "gzip") && tgt != "dir" && tgt != "folder")
            {
                return true;
            }

            return false;
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            var info = new FormatInspection
            {
                FormatId = "gzip",
                MimeType = "application/gzip",
                IsSupported = true,
                FileSizeBytes = inputStream.CanSeek ? inputStream.Length : 0
            };
            info.Metadata["FormatLabel"] = "GZIP Compressed Stream (RFC 1952)";

            try
            {
                var members = GzipReader.InspectMembers(inputStream);
                if (members.Count > 0)
                {
                    var first = members[0];
                    info.Metadata["CompressionMethod"] = "DEFLATE (RFC 1951)";
                    info.Metadata["OperatingSystem"] = first.Header.OperatingSystem.ToString();

                    if (first.Header.ModificationTime.HasValue)
                    {
                        info.Metadata["ModificationTimeUtc"] = first.Header.ModificationTime.Value.ToString("u");
                    }

                    if (!string.IsNullOrEmpty(first.Header.FileName))
                    {
                        info.Metadata["OriginalFileName"] = first.Header.FileName;
                    }

                    if (!string.IsNullOrEmpty(first.Header.Comment))
                    {
                        info.Metadata["Comment"] = first.Header.Comment;
                    }

                    if (first.Header.HeaderCrc.HasValue)
                    {
                        info.Metadata["HeaderCrc16"] = string.Format("0x{0:X4}", first.Header.HeaderCrc.Value);
                    }

                    info.Metadata["TrailerCRC32"] = string.Format("0x{0:X8}", first.Trailer.Crc32);
                    info.Metadata["UncompressedISize"] = string.Format("{0:N0} bytes (mod 2^32)", first.Trailer.ISize);

                    if (inputStream.CanSeek && first.Trailer.ISize > 0)
                    {
                        double ratio = (inputStream.Length * 100.0) / first.Trailer.ISize;
                        info.Metadata["CompressionRatio"] = string.Format("{0:F1}%", ratio);
                    }
                }
            }
            catch (Exception ex)
            {
                info.IsSupported = false;
                info.Metadata["InspectError"] = ex.Message;
            }

            return info;
        }

        public ConvertResult Convert(ConvertRequest request, Action<int> onProgress)
        {
            if (request == null) throw new ArgumentNullException("request");
            var sw = Stopwatch.StartNew();

            if (Directory.Exists(request.InputPath))
            {
                throw new InvalidOperationException(
                    "O motor GZIP comprime arquivos individuais. Para empacotar e compactar diretórios inteiros, utilize o motor ZIP.");
            }

            if (!File.Exists(request.InputPath))
            {
                throw new FileNotFoundException("Arquivo de entrada não encontrado.", request.InputPath);
            }

            string srcExt = Path.GetExtension(request.InputPath).TrimStart('.').ToLowerInvariant();
            string tgt = request.TargetFormat.TrimStart('.').ToLowerInvariant();

            bool isGzipInput = (srcExt == "gz" || srcExt == "gzip");

            // 1. Operação de DESCOMPACTAÇÃO: .gz -> arquivo restaurado
            if (isGzipInput && (tgt != "gz" && tgt != "gzip"))
            {
                string destinationFile = DetermineDecompressedOutputPath(request);
                GzipReader.DecompressFile(request.InputPath, destinationFile, overwrite: true, onProgress: onProgress);

                sw.Stop();
                long inputBytes = new FileInfo(request.InputPath).Length;
                return ConvertResult.Success(request.JobId, destinationFile, inputBytes, sw.ElapsedMilliseconds);
            }

            // 2. Operação de COMPACTAÇÃO: arquivo -> .gz
            if (tgt == "gz" || tgt == "gzip")
            {
                string targetDir = request.OutputDirectory;
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = Path.GetDirectoryName(request.InputPath) ?? Environment.CurrentDirectory;
                }

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string destinationGz = Path.Combine(targetDir, Path.GetFileName(request.InputPath) + ".gz");

                var options = new GzipWriteOptions
                {
                    FileName = Path.GetFileName(request.InputPath),
                    ModificationTime = File.GetLastWriteTimeUtc(request.InputPath)
                };

                string compSetting;
                if (request.Settings != null && request.Settings.TryGetValue("compression", out compSetting))
                {
                    if (compSetting.Equals("fast", StringComparison.OrdinalIgnoreCase) || compSetting == "1")
                    {
                        options.CompressionLevel = ZipCompressionLevel.Fast;
                    }
                    else if (compSetting.Equals("store", StringComparison.OrdinalIgnoreCase) || compSetting == "0")
                    {
                        options.CompressionLevel = ZipCompressionLevel.Store;
                    }
                    else
                    {
                        options.CompressionLevel = ZipCompressionLevel.Optimal;
                    }
                }

                GzipWriter.CompressFile(request.InputPath, destinationGz, options, overwrite: true, onProgress: onProgress);

                sw.Stop();
                long inputBytes = new FileInfo(request.InputPath).Length;
                return ConvertResult.Success(request.JobId, destinationGz, inputBytes, sw.ElapsedMilliseconds);
            }

            throw new UnsupportedFormatException(string.Format(
                "Operação não suportada pelo motor GZIP: '{0}' -> '{1}'.",
                srcExt, tgt));
        }

        private string DetermineDecompressedOutputPath(ConvertRequest request)
        {
            string targetDir = request.OutputDirectory;
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = Path.GetDirectoryName(request.InputPath) ?? Environment.CurrentDirectory;
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string baseName = Path.GetFileName(request.InputPath);
            string outName;

            if (baseName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                outName = baseName.Substring(0, baseName.Length - 3);
            }
            else if (baseName.EndsWith(".gzip", StringComparison.OrdinalIgnoreCase))
            {
                outName = baseName.Substring(0, baseName.Length - 5);
            }
            else
            {
                outName = baseName + ".out";
            }

            return Path.Combine(targetDir, outName);
        }
    }
}
