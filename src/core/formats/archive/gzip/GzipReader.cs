using System;
using System.Collections.Generic;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Informações sobre um membro individual dentro de um arquivo GZIP.
    /// </summary>
    public class GzipMemberInfo
    {
        public int Index { get; set; }
        public GzipHeader Header { get; set; }
        public GzipTrailer Trailer { get; set; }
        public long UncompressedBytes { get; set; }
        public long CompressedBytes { get; set; }
    }

    /// <summary>
    /// Descompressor e leitor do formato GZIP (RFC 1952).
    /// Suporta membros simples e múltiplos membros concatenados sequencialmente (§ 2.2).
    /// </summary>
    public class GzipReader
    {
        private class CrcTrackingStream : Stream
        {
            private readonly Stream _target;
            private uint _crc;
            private long _bytesWritten;
            private readonly long _maxBytes;

            public CrcTrackingStream(Stream target, long maxBytes)
            {
                _target = target;
                _crc = Crc32.InitialCrc;
                _bytesWritten = 0;
                _maxBytes = maxBytes;
            }

            public uint CurrentCrc
            {
                get { return Crc32.Finalize(_crc); }
            }

            public long BytesWritten
            {
                get { return _bytesWritten; }
            }

            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override long Length { get { return _bytesWritten; } }
            public override long Position
            {
                get { return _bytesWritten; }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
                _target.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count <= 0) return;

                checked
                {
                    if (_bytesWritten + count > _maxBytes)
                    {
                        throw new ResourceLimitException(string.Format(
                            "Total de dados descomprimidos ({0:N0} bytes) excede o teto de segurança contra Decompression Bomb de {1:N0} bytes.",
                            _bytesWritten + count, _maxBytes));
                    }
                }

                _target.Write(buffer, offset, count);
                _crc = Crc32.Update(_crc, buffer, offset, count);
                _bytesWritten += count;
            }

            public override void WriteByte(byte value)
            {
                checked
                {
                    if (_bytesWritten + 1 > _maxBytes)
                    {
                        throw new ResourceLimitException(string.Format(
                            "Total de dados descomprimidos ({0:N0} bytes) excede o teto de segurança contra Decompression Bomb de {1:N0} bytes.",
                            _bytesWritten + 1, _maxBytes));
                    }
                }

                _target.WriteByte(value);
                _crc = Crc32.Update(_crc, value);
                _bytesWritten++;
            }
        }

        public static byte[] Decompress(byte[] gzipData, long maxOutputBytes = SecurityLimits.DefaultMaxGzipExtractedBytes)
        {
            if (gzipData == null) throw new ArgumentNullException("gzipData");
            using (var msIn = new MemoryStream(gzipData, writable: false))
            using (var msOut = new MemoryStream())
            {
                Decompress(msIn, msOut, maxOutputBytes);
                return msOut.ToArray();
            }
        }

        public static byte[] Decompress(Stream inputStream, long maxOutputBytes = SecurityLimits.DefaultMaxGzipExtractedBytes)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            using (var msOut = new MemoryStream())
            {
                Decompress(inputStream, msOut, maxOutputBytes);
                return msOut.ToArray();
            }
        }

        public static List<GzipMemberInfo> Decompress(
            Stream inputStream,
            Stream outputStream,
            long maxOutputBytes = SecurityLimits.DefaultMaxGzipExtractedBytes,
            Action<int> onProgress = null)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertGzipInputFileSize(inputStream.Length);
            }

            var reader = new DeflateBitReader(inputStream);
            var members = new List<GzipMemberInfo>();
            long totalExtractedBytes = 0;
            int memberIndex = 0;

            long totalInputBytes = inputStream.CanSeek ? inputStream.Length : 0;

            while (true)
            {
                GzipHeader header;
                long startByte = reader.BytesConsumed;

                if (memberIndex == 0)
                {
                    header = GzipHeader.Read(reader);
                }
                else
                {
                    reader.AlignToByte();
                    int next = -1;
                    while (true)
                    {
                        next = reader.TryReadByte();
                        if (next < 0) break; // EOF atingido
                        if (next == 0) continue; // Pula padding nulo após trailer
                        break;
                    }

                    if (next < 0)
                    {
                        // Fim do arquivo após o último membro
                        break;
                    }

                    if (next != GzipConstants.Id1)
                    {
                        throw new CorruptDataException(string.Format(
                            "Dados inesperados após o término do membro GZIP {0}: byte 0x{1:X2} (esperado 0x1F ou EOF).",
                            memberIndex, next));
                    }

                    header = GzipHeader.Read(reader, (byte)next);
                }

                long remainingBudget = maxOutputBytes - totalExtractedBytes;
                if (remainingBudget <= 0)
                {
                    throw new ResourceLimitException(string.Format(
                        "Total de bytes extraídos ({0:N0}) atingiu o limite de segurança de {1:N0} bytes.",
                        totalExtractedBytes, maxOutputBytes));
                }

                var trackingStream = new CrcTrackingStream(outputStream, remainingBudget);

                // Descomprime o payload DEFLATE do membro atual
                DeflateDecompressor.Decompress(reader, trackingStream, remainingBudget);

                // O DeflateDecompressor já executou reader.AlignToByte(). Lê o trailer de 8 bytes
                var trailer = GzipTrailer.Read(reader);
                trailer.Validate(trackingStream.CurrentCrc, trackingStream.BytesWritten);

                long endByte = reader.BytesConsumed;
                long memberCompressed = endByte - startByte;

                var memberInfo = new GzipMemberInfo
                {
                    Index = memberIndex,
                    Header = header,
                    Trailer = trailer,
                    UncompressedBytes = trackingStream.BytesWritten,
                    CompressedBytes = memberCompressed
                };
                members.Add(memberInfo);

                totalExtractedBytes += trackingStream.BytesWritten;
                SecurityLimits.AssertGzipExtractedBytes(totalExtractedBytes, maxOutputBytes);

                memberIndex++;
                SecurityLimits.AssertGzipMemberCount(memberIndex);

                if (onProgress != null && totalInputBytes > 0)
                {
                    int pct = (int)Math.Min(100, Math.Max(0, (reader.BytesConsumed * 100) / totalInputBytes));
                    onProgress(pct);
                }
            }

            if (onProgress != null)
            {
                onProgress(100);
            }

            return members;
        }

        public static void DecompressFile(
            string inputGzPath,
            string outputFilePath,
            bool overwrite = false,
            long maxOutputBytes = SecurityLimits.DefaultMaxGzipExtractedBytes,
            Action<int> onProgress = null)
        {
            if (string.IsNullOrEmpty(inputGzPath)) throw new ArgumentNullException("inputGzPath");
            if (!File.Exists(inputGzPath))
            {
                throw new FileNotFoundException("Arquivo GZIP de entrada não encontrado.", inputGzPath);
            }

            if (string.IsNullOrEmpty(outputFilePath)) throw new ArgumentNullException("outputFilePath");

            var fileInfo = new FileInfo(inputGzPath);
            SecurityLimits.AssertGzipInputFileSize(fileInfo.Length);

            if (File.Exists(outputFilePath) && !overwrite)
            {
                throw new IOException(string.Format(
                    "O arquivo de destino '{0}' já existe e a opção de sobrescrita (overwrite) não está ativa.",
                    outputFilePath));
            }

            string targetDir = Path.GetDirectoryName(Path.GetFullPath(outputFilePath));
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Escrita atômica em arquivo temporário
            string tempFile = Path.Combine(targetDir, string.Format(
                ".lodinotmp_{0}_{1}.tmp",
                Path.GetFileName(outputFilePath),
                Guid.NewGuid().ToString("N").Substring(0, 8)));

            try
            {
                using (var inStream = new FileStream(inputGzPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var outStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Decompress(inStream, outStream, maxOutputBytes, onProgress);
                }

                // Substituição segura
                if (File.Exists(outputFilePath))
                {
                    string backupFile = outputFilePath + ".lodinobak_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    try
                    {
                        File.Replace(tempFile, outputFilePath, backupFile, ignoreMetadataErrors: true);
                        if (File.Exists(backupFile))
                        {
                            try { File.Delete(backupFile); } catch { }
                        }
                    }
                    catch
                    {
                        File.Delete(outputFilePath);
                        File.Move(tempFile, outputFilePath);
                    }
                }
                else
                {
                    File.Move(tempFile, outputFilePath);
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

        public static List<GzipMemberInfo> InspectMembers(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            var members = new List<GzipMemberInfo>();
            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertGzipInputFileSize(inputStream.Length);
            }

            // Lê cabeçalho do primeiro membro
            long pos = inputStream.CanSeek ? inputStream.Position : 0;
            var header = GzipHeader.Read(inputStream);

            var info = new GzipMemberInfo
            {
                Index = 0,
                Header = header
            };

            // Se for seekable e tiver pelo menos 18 bytes (10 cabeçalho + 8 trailer), podemos inspecionar o trailer final
            if (inputStream.CanSeek && inputStream.Length >= 18)
            {
                long currentPos = inputStream.Position;
                inputStream.Seek(-8, SeekOrigin.End);
                info.Trailer = GzipTrailer.Read(inputStream);
                info.UncompressedBytes = info.Trailer.ISize;
                info.CompressedBytes = inputStream.Length;
                inputStream.Seek(currentPos, SeekOrigin.Begin);
            }

            members.Add(info);
            return members;
        }
    }
}
