using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Archive.Zip;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Configurações e metadados para escrita de arquivos GZIP (RFC 1952).
    /// </summary>
    public class GzipWriteOptions
    {
        public string FileName { get; set; }
        public string Comment { get; set; }
        public DateTime? ModificationTime { get; set; }
        public ZipCompressionLevel CompressionLevel { get; set; }
        public GzipOperatingSystem OperatingSystem { get; set; }
        public bool IncludeHeaderCrc { get; set; }
        public bool IsText { get; set; }
        public byte[] ExtraData { get; set; }

        public GzipWriteOptions()
        {
            CompressionLevel = ZipCompressionLevel.Optimal;
            OperatingSystem = GzipOperatingSystem.Fat;
            IncludeHeaderCrc = false;
            IsText = false;
        }
    }

    /// <summary>
    /// Compressor e gerador de arquivos GZIP compatível com a RFC 1952.
    /// Reutiliza os motores proprietários DeflateCompressor e Crc32 em C# puro.
    /// </summary>
    public static class GzipWriter
    {
        public static byte[] Compress(byte[] rawData, GzipWriteOptions options = null)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            using (var msIn = new MemoryStream(rawData, writable: false))
            using (var msOut = new MemoryStream())
            {
                Compress(msIn, msOut, options);
                return msOut.ToArray();
            }
        }

        public static void Compress(
            Stream inputStream,
            Stream outputStream,
            GzipWriteOptions options = null,
            Action<int> onProgress = null)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            if (options == null)
            {
                options = new GzipWriteOptions();
            }

            if (onProgress != null) onProgress(10);

            // 1. Lê todos os dados da stream de entrada e calcula CRC-32 e ISIZE
            byte[] rawData;
            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertGzipInputFileSize(inputStream.Length);
                rawData = new byte[inputStream.Length];
                int offset = 0;
                int read;
                while (offset < rawData.Length && (read = inputStream.Read(rawData, offset, rawData.Length - offset)) > 0)
                {
                    offset += read;
                }
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    byte[] buf = new byte[8192];
                    int r;
                    while ((r = inputStream.Read(buf, 0, buf.Length)) > 0)
                    {
                        ms.Write(buf, 0, r);
                        SecurityLimits.AssertGzipInputFileSize(ms.Length);
                    }
                    rawData = ms.ToArray();
                }
            }

            if (onProgress != null) onProgress(30);

            uint crc32 = Crc32.Calculate(rawData);
            uint isize = (uint)(rawData.Length & 0xFFFFFFFFL);

            // 2. Constrói e escreve o cabeçalho RFC 1952
            var header = new GzipHeader
            {
                CompressionMethod = GzipConstants.CompressionMethodDeflate,
                OperatingSystem = options.OperatingSystem,
                ModificationTime = options.ModificationTime ?? DateTime.UtcNow,
                HasText = options.IsText,
                HasHeaderCrc = options.IncludeHeaderCrc
            };

            if (options.CompressionLevel == ZipCompressionLevel.Fast)
            {
                header.ExtraFlags = GzipConstants.XflFastestCompression;
            }
            else if (options.CompressionLevel == ZipCompressionLevel.Optimal)
            {
                header.ExtraFlags = GzipConstants.XflMaximumCompression;
            }

            if (options.ExtraData != null && options.ExtraData.Length > 0)
            {
                header.HasExtra = true;
                header.ExtraData = options.ExtraData;
            }

            if (!string.IsNullOrEmpty(options.FileName))
            {
                header.HasFileName = true;
                // Salva apenas o nome do arquivo, sem caminho de diretório
                header.FileName = Path.GetFileName(options.FileName);
            }

            if (!string.IsNullOrEmpty(options.Comment))
            {
                header.HasComment = true;
                header.Comment = options.Comment;
            }

            header.Write(outputStream);

            if (onProgress != null) onProgress(45);

            // 3. Comprime o payload usando DeflateCompressor proprietário
            DeflateCompressor.Compress(rawData, outputStream, options.CompressionLevel);

            if (onProgress != null) onProgress(85);

            // 4. Escreve o trailer de 8 bytes
            var trailer = new GzipTrailer(crc32, isize);
            trailer.Write(outputStream);

            if (onProgress != null) onProgress(100);
        }

        public static void CompressFile(
            string sourceFilePath,
            string targetGzPath,
            GzipWriteOptions options = null,
            bool overwrite = false,
            Action<int> onProgress = null)
        {
            if (string.IsNullOrEmpty(sourceFilePath)) throw new ArgumentNullException("sourceFilePath");
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Arquivo de origem não encontrado.", sourceFilePath);
            }

            if (string.IsNullOrEmpty(targetGzPath)) throw new ArgumentNullException("targetGzPath");

            var fileInfo = new FileInfo(sourceFilePath);
            SecurityLimits.AssertGzipInputFileSize(fileInfo.Length);

            if (File.Exists(targetGzPath) && !overwrite)
            {
                throw new IOException(string.Format(
                    "O arquivo de destino '{0}' já existe e a opção de sobrescrita (overwrite) não está ativa.",
                    targetGzPath));
            }

            if (options == null)
            {
                options = new GzipWriteOptions
                {
                    FileName = Path.GetFileName(sourceFilePath),
                    ModificationTime = fileInfo.LastWriteTimeUtc
                };
            }
            else if (string.IsNullOrEmpty(options.FileName))
            {
                options.FileName = Path.GetFileName(sourceFilePath);
                if (!options.ModificationTime.HasValue)
                {
                    options.ModificationTime = fileInfo.LastWriteTimeUtc;
                }
            }

            string targetDir = Path.GetDirectoryName(Path.GetFullPath(targetGzPath));
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string tempFile = Path.Combine(targetDir, string.Format(
                ".lodinotmp_{0}_{1}.tmp",
                Path.GetFileName(targetGzPath),
                Guid.NewGuid().ToString("N").Substring(0, 8)));

            try
            {
                using (var inStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var outStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Compress(inStream, outStream, options, onProgress);
                }

                if (File.Exists(targetGzPath))
                {
                    string backupFile = targetGzPath + ".lodinobak_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    try
                    {
                        File.Replace(tempFile, targetGzPath, backupFile, ignoreMetadataErrors: true);
                        if (File.Exists(backupFile))
                        {
                            try { File.Delete(backupFile); } catch { }
                        }
                    }
                    catch
                    {
                        File.Delete(targetGzPath);
                        File.Move(tempFile, targetGzPath);
                    }
                }
                else
                {
                    File.Move(tempFile, targetGzPath);
                }
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
                throw;
            }
        }
    }
}
