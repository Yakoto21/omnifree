using System;
using System.Diagnostics;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Motor de integração do formato ZIP com o núcleo nativo Lodino.
    /// Implementa inspeção, criação, listagem e extração de contêineres ZIP.
    /// </summary>
    public class ZipEngine : ICoreEngine
    {
        public string EngineId
        {
            get { return "archive-zip"; }
        }

        public string EngineLabel
        {
            get { return "Motor Nativo de Arquivos ZIP"; }
        }

        public string[] SupportedInputs
        {
            get { return new[] { "zip", "dir", "folder" }; }
        }

        public string[] SupportedOutputs
        {
            get { return new[] { "zip", "dir", "extract" }; }
        }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;
            string src = sourceFormat.TrimStart('.').ToLowerInvariant();
            string tgt = targetFormat.TrimStart('.').ToLowerInvariant();

            // Extração: zip -> dir ou zip -> extract
            if (src == "zip" && (tgt == "dir" || tgt == "extract" || tgt == "folder"))
            {
                return true;
            }

            // Compactação: qualquer formato/diretório -> zip
            if (tgt == "zip")
            {
                return true;
            }

            // Recompactação / verificação: zip -> zip
            if (src == "zip" && tgt == "zip")
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
                FormatId = "zip",
                MimeType = "application/zip",
                IsSupported = true,
                FileSizeBytes = inputStream.CanSeek ? inputStream.Length : 0
            };
            info.Metadata["FormatLabel"] = "PKWARE ZIP Archive";

            try
            {
                using (var reader = new ZipReader(inputStream, leaveOpen: true))
                {
                    long totalUncompressed = 0;
                    long totalCompressed = 0;
                    int dirCount = 0;
                    int fileCount = 0;

                    foreach (var entry in reader.Entries)
                    {
                        if (entry.IsDirectory)
                        {
                            dirCount++;
                        }
                        else
                        {
                            fileCount++;
                            totalUncompressed += entry.UncompressedSize;
                            totalCompressed += entry.CompressedSize;
                        }
                    }

                    info.Metadata["TotalEntries"] = reader.Entries.Count.ToString();
                    info.Metadata["FileCount"] = fileCount.ToString();
                    info.Metadata["DirectoryCount"] = dirCount.ToString();
                    info.Metadata["TotalUncompressedBytes"] = totalUncompressed.ToString("N0") + " bytes";
                    info.Metadata["TotalCompressedBytes"] = totalCompressed.ToString("N0") + " bytes";

                    double ratio = totalUncompressed > 0
                        ? (totalCompressed * 100.0 / totalUncompressed)
                        : 0.0;
                    info.Metadata["CompressionRatio"] = string.Format("{0:F1}%", ratio);

                    if (!string.IsNullOrEmpty(reader.Comment))
                    {
                        info.Metadata["ArchiveComment"] = reader.Comment;
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

            string srcExt = Path.GetExtension(request.InputPath).TrimStart('.').ToLowerInvariant();
            string tgt = request.TargetFormat.TrimStart('.').ToLowerInvariant();

            bool isInputDirectory = Directory.Exists(request.InputPath);
            bool isInputZip = srcExt == "zip" && File.Exists(request.InputPath);

            // Operação de EXTRAÇÃO: zip -> dir / extract
            if (isInputZip && (tgt == "dir" || tgt == "extract" || tgt == "folder"))
            {
                string targetDir = request.OutputDirectory;
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = Path.Combine(
                        Path.GetDirectoryName(request.InputPath),
                        Path.GetFileNameWithoutExtension(request.InputPath));
                }

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                using (var reader = new ZipReader(request.InputPath))
                {
                    reader.ExtractAll(targetDir, onProgress);
                }

                sw.Stop();
                long inputBytes = new FileInfo(request.InputPath).Length;
                return ConvertResult.Success(request.JobId, targetDir, inputBytes, sw.ElapsedMilliseconds);
            }

            // Operação de COMPACTAÇÃO: arquivo/diretório -> zip
            if (tgt == "zip")
            {
                string baseName = isInputDirectory
                    ? new DirectoryInfo(request.InputPath).Name
                    : Path.GetFileNameWithoutExtension(request.InputPath);

                string targetDir = request.OutputDirectory;
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = isInputDirectory
                        ? (Path.GetDirectoryName(Path.GetFullPath(request.InputPath)) ?? Environment.CurrentDirectory)
                        : (Path.GetDirectoryName(request.InputPath) ?? Environment.CurrentDirectory);
                }

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string targetZipFile = SafeFileWriter.GetUniqueOutputPath(targetDir, baseName, "zip");

                string fullTarget = Path.GetFullPath(targetZipFile);
                string fullInput = Path.GetFullPath(request.InputPath);
                if (string.Equals(fullTarget, fullInput, StringComparison.OrdinalIgnoreCase))
                {
                    throw new PathSecurityException(string.Format(
                        "O arquivo ZIP de destino não pode ser idêntico ao arquivo de entrada: '{0}'.",
                        request.InputPath));
                }

                ZipCompressionLevel level = ZipCompressionLevel.Optimal;
                string levelSetting;
                if (request.Settings != null && request.Settings.TryGetValue("compression", out levelSetting))
                {
                    switch (levelSetting.ToLowerInvariant())
                    {
                        case "store":
                        case "none":
                        case "0":
                            level = ZipCompressionLevel.Store;
                            break;
                        case "fast":
                        case "1":
                            level = ZipCompressionLevel.Fast;
                            break;
                        case "optimal":
                        case "best":
                        case "2":
                        default:
                            level = ZipCompressionLevel.Optimal;
                            break;
                    }
                }

                long totalBytes = 0;
                try
                {
                    using (var writer = new ZipWriter(targetZipFile))
                    {
                        if (isInputDirectory)
                        {
                            writer.AddDirectoryRecursive(request.InputPath, "", level);
                        }
                        else
                        {
                            writer.AddFile(request.InputPath, Path.GetFileName(request.InputPath), level);
                        }
                        writer.Finish();
                        totalBytes = new FileInfo(targetZipFile).Length;
                    }
                }
                catch
                {
                    try
                    {
                        if (File.Exists(targetZipFile))
                        {
                            File.Delete(targetZipFile);
                        }
                    }
                    catch
                    {
                        // Supressão defensiva de falha secundária de limpeza
                    }
                    throw;
                }

                if (onProgress != null) onProgress(100);
                sw.Stop();
                return ConvertResult.Success(request.JobId, targetZipFile, totalBytes, sw.ElapsedMilliseconds);
            }

            throw new UnsupportedFormatException(string.Format(
                "Operação ZIP não suportada para entrada '{0}' e destino '{1}'.", request.InputPath, request.TargetFormat));
        }
    }
}
