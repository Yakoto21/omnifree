using System;
using System.IO;
using Lodino.Core.Checksums;
using Lodino.Core.Compression.Deflate;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Archive.Zip;
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

        public DeflateCompressionLevel DeflateLevel { get; set; }

        public ZipCompressionLevel CompressionLevel
        {
            get { return (ZipCompressionLevel)DeflateLevel; }
            set { DeflateLevel = (DeflateCompressionLevel)value; }
        }

        public GzipOperatingSystem OperatingSystem { get; set; }
        public bool IncludeHeaderCrc { get; set; }
        public bool IsText { get; set; }
        public byte[] ExtraData { get; set; }

        public GzipWriteOptions()
        {
            DeflateLevel = DeflateCompressionLevel.Optimal;
            OperatingSystem = GzipOperatingSystem.Fat;
            IncludeHeaderCrc = false;
            IsText = false;
        }
    }

    /// <summary>
    /// Compressor e gerador de arquivos GZIP compatível com a RFC 1952.
    /// Opera com consumo rigorosamente O(1) de memória através de streaming contínuo.
    /// </summary>
    public static class GzipWriter
    {
        private class Crc32MeteringInputStream : Stream
        {
            private readonly Stream _baseStream;
            private uint _crc;
            private long _bytesRead;
            private readonly long _maxBytes;

            public Crc32MeteringInputStream(Stream baseStream, long maxBytes = SecurityLimits.DefaultMaxGzipInputFileSize)
            {
                _baseStream = baseStream;
                _crc = Crc32.InitialCrc;
                _bytesRead = 0;
                _maxBytes = maxBytes;
            }

            public uint CurrentCrc
            {
                get { return Crc32.Finalize(_crc); }
            }

            public long TotalBytesRead
            {
                get { return _bytesRead; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = _baseStream.Read(buffer, offset, count);
                if (read > 0)
                {
                    checked
                    {
                        _bytesRead += read;
                        if (_bytesRead > _maxBytes)
                        {
                            throw new ResourceLimitException(string.Format(
                                "Tamanho do arquivo de entrada GZIP ({0:N0} bytes) excede o limite máximo permitido de {1:N0} bytes.",
                                _bytesRead, _maxBytes));
                        }
                    }
                    _crc = Crc32.Update(_crc, buffer, offset, read);
                }
                return read;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return _bytesRead; } }
            public override long Position
            {
                get { return _bytesRead; }
                set { throw new NotSupportedException(); }
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        public static byte[] Compress(byte[] rawData, GzipWriteOptions options = null)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            using (var msIn = new MemoryStream(rawData, writable: false))
            using (var msOut = new MemoryStream())
            {
                CompressStream(msIn, msOut, options);
                return msOut.ToArray();
            }
        }

        public static void Compress(
            Stream inputStream,
            Stream outputStream,
            GzipWriteOptions options = null,
            Action<int> onProgress = null)
        {
            CompressStream(inputStream, outputStream, options, onProgress);
        }

        public static void Compress(
            Stream inputStream,
            Stream outputStream,
            GzipHeader header,
            DeflateCompressionLevel level = DeflateCompressionLevel.Optimal)
        {
            var options = new GzipWriteOptions
            {
                FileName = header != null ? header.FileName : null,
                Comment = header != null ? header.Comment : null,
                ModificationTime = header != null ? header.ModificationTime : (DateTime?)null,
                OperatingSystem = header != null ? header.OperatingSystem : GzipOperatingSystem.Fat,
                IncludeHeaderCrc = header != null && header.HasHeaderCrc,
                IsText = header != null && header.HasText,
                ExtraData = header != null ? header.ExtraData : null,
                DeflateLevel = level
            };
            CompressStream(inputStream, outputStream, options, null);
        }

        /// <summary>
        /// Comprime um fluxo de entrada no formato GZIP (RFC 1952) com uso estritamente O(1) de memória.
        /// O cabeçalho é emitido imediatamente, os blocos DEFLATE são transmitidos em streaming
        /// e o trailer de 8 bytes é calculado e validado incrementalmente.
        /// </summary>
        public static void CompressStream(
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

            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertGzipInputFileSize(inputStream.Length);
            }

            if (onProgress != null) onProgress(10);

            // 1. Constrói e escreve o cabeçalho RFC 1952 diretamente no outputStream
            var header = new GzipHeader
            {
                CompressionMethod = GzipConstants.CompressionMethodDeflate,
                OperatingSystem = options.OperatingSystem,
                ModificationTime = options.ModificationTime ?? DateTime.UtcNow,
                HasText = options.IsText,
                HasHeaderCrc = options.IncludeHeaderCrc
            };

            if (options.DeflateLevel == DeflateCompressionLevel.Fast)
            {
                header.ExtraFlags = GzipConstants.XflFastestCompression;
            }
            else if (options.DeflateLevel == DeflateCompressionLevel.Optimal)
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
                header.FileName = Path.GetFileName(options.FileName);
            }

            if (!string.IsNullOrEmpty(options.Comment))
            {
                header.HasComment = true;
                header.Comment = options.Comment;
            }

            header.Write(outputStream);

            if (onProgress != null) onProgress(30);

            // 2. Encapsula o inputStream em um medidor incremental de CRC-32 e bytes
            var meteringStream = new Crc32MeteringInputStream(inputStream);

            // 3. Comprime o payload DEFLATE em streaming O(1)
            DeflateCompressor.CompressStream(meteringStream, outputStream, options.DeflateLevel);

            if (onProgress != null) onProgress(85);

            // 4. Escreve o trailer de 8 bytes (CRC-32 e ISIZE) após término perfeito do DEFLATE
            uint finalCrc32 = meteringStream.CurrentCrc;
            uint isize = (uint)(meteringStream.TotalBytesRead & 0xFFFFFFFFL);

            var trailer = new GzipTrailer(finalCrc32, isize);
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
                    CompressStream(inStream, outStream, options, onProgress);
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
