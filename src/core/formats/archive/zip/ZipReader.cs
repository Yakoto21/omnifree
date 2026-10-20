using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Checksums;
using Lodino.Core.Compression.Deflate;
using Lodino.Core.IO;
using Lodino.Core.Safety;
using Crc32 = Lodino.Core.Checksums.Crc32;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Leitor e extrator proprietário de arquivos ZIP (PKWARE APPNOTE.TXT).
    /// Realiza leitura canônica do Diretório Central, validação de integridade e extração segura anti-Zip Slip.
    /// </summary>
    public class ZipReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly List<ZipEntry> _entries = new List<ZipEntry>();
        private string _comment = string.Empty;
        private bool _disposed = false;

        public IReadOnlyList<ZipEntry> Entries
        {
            get { return _entries.AsReadOnly(); }
        }

        public string Comment
        {
            get { return _comment; }
        }

        public ZipReader(Stream stream, bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException("O stream deve permitir leitura.", "stream");
            _stream = stream;
            _leaveOpen = leaveOpen;

            if (_stream.CanSeek)
            {
                SecurityLimits.AssertFileSize(_stream.Length, SecurityLimits.DefaultMaxZipFileSize);
                ReadCentralDirectory();
            }
            else
            {
                throw new UnsupportedFormatException("O leitor ZIP requer um stream navegável (CanSeek = true) para localizar o Diretório Central.");
            }
        }

        public ZipReader(string filePath)
            : this(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
        }

        private void ReadCentralDirectory()
        {
            long streamLength = _stream.Length;
            if (streamLength < 22)
            {
                throw new CorruptDataException("Arquivo ZIP inválido ou truncado: tamanho menor que o cabeçalho mínimo EOCD de 22 bytes.");
            }

            // 1. Localiza o registro End of Central Directory (EOCD) buscando de trás para frente (com backtracking e verificação de invariante)
            long maxSearchBytes = Math.Min(streamLength, (long)SecurityLimits.MaxZipCommentLength + 22);
            long searchStart = Math.Max(0, streamLength - maxSearchBytes);
            _stream.Seek(searchStart, SeekOrigin.Begin);

            byte[] searchBuffer = new byte[streamLength - searchStart];
            StreamUtils.ReadExactOrThrow(_stream, searchBuffer, 0, searchBuffer.Length, "Falha ao ler fim do arquivo ZIP para busca do EOCD.");

            int eocdIndex = -1;
            for (int i = searchBuffer.Length - 22; i >= 0; i--)
            {
                if (searchBuffer[i] == 0x50 &&
                    searchBuffer[i + 1] == 0x4B &&
                    searchBuffer[i + 2] == 0x05 &&
                    searchBuffer[i + 3] == 0x06)
                {
                    int candidateCommentLen = searchBuffer[i + 20] | (searchBuffer[i + 21] << 8);
                    long candidateEocdOffset = searchStart + i;
                    if (candidateEocdOffset + 22 + candidateCommentLen == streamLength)
                    {
                        eocdIndex = i;
                        break;
                    }
                }
            }

            if (eocdIndex < 0)
            {
                throw new CorruptDataException("Registro End-of-Central-Directory (EOCD) não encontrado. Arquivo corrompido ou não é um ZIP válido.");
            }

            long eocdOffset = searchStart + eocdIndex;
            _stream.Seek(eocdOffset, SeekOrigin.Begin);
            var reader = new BinaryReader(_stream, Encoding.UTF8);

            uint signature = reader.ReadUInt32();
            if (signature != ZipConstants.EndOfCentralDirectorySignature)
            {
                throw new CorruptDataException("Assinatura EOCD inválida.");
            }

            ushort diskNumber = reader.ReadUInt16();
            ushort cdDiskNumber = reader.ReadUInt16();
            if (diskNumber != 0 || cdDiskNumber != 0)
            {
                throw new UnsupportedFormatException("Arquivos ZIP divididos em múltiplos volumes/discos não são suportados.");
            }

            ushort entriesOnDisk = reader.ReadUInt16();
            ushort totalEntries = reader.ReadUInt16();
            uint cdSize = reader.ReadUInt32();
            uint cdOffset = reader.ReadUInt32();
            ushort commentLen = reader.ReadUInt16();

            // Detecção de ZIP64
            if (diskNumber == 0xFFFF || cdDiskNumber == 0xFFFF ||
                entriesOnDisk == 0xFFFF || totalEntries == 0xFFFF ||
                cdSize == 0xFFFFFFFF || cdOffset == 0xFFFFFFFF)
            {
                throw new UnsupportedFormatException("Arquivos no formato ZIP64 (superiores a 4 GB ou 65.535 entradas) não são suportados pelo motor padrão.");
            }

            SecurityLimits.AssertZipEntryCount(totalEntries);

            if (commentLen > 0)
            {
                byte[] commentBytes = reader.ReadBytes(commentLen);
                _comment = Encoding.UTF8.GetString(commentBytes);
            }

            if ((long)cdOffset + cdSize > eocdOffset)
            {
                throw new CorruptDataException("Tamanho ou deslocamento do Diretório Central corrompido (ultrapassa início do EOCD).");
            }

            // 2. Lê cada entrada do Diretório Central (CDH)
            _stream.Seek(cdOffset, SeekOrigin.Begin);
            for (int i = 0; i < totalEntries; i++)
            {
                uint cdhSig = reader.ReadUInt32();
                if (cdhSig != ZipConstants.CentralDirectoryHeaderSignature)
                {
                    throw new CorruptDataException(string.Format(
                        "Assinatura do Central Directory Header corrompida na entrada {0} (esperado 0x02014B50, lido 0x{1:X8}).",
                        i, cdhSig));
                }

                ushort versionMadeBy = reader.ReadUInt16();
                ushort versionNeeded = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                ushort method = reader.ReadUInt16();
                ushort modTime = reader.ReadUInt16();
                ushort modDate = reader.ReadUInt16();
                uint crc = reader.ReadUInt32();
                uint compSize = reader.ReadUInt32();
                uint uncompSize = reader.ReadUInt32();
                ushort nameLen = reader.ReadUInt16();
                ushort extraLen = reader.ReadUInt16();
                ushort fileCommentLen = reader.ReadUInt16();
                ushort diskNumStart = reader.ReadUInt16();
                ushort internalAttr = reader.ReadUInt16();
                uint externalAttr = reader.ReadUInt32();
                uint localHeaderOffset = reader.ReadUInt32();

                if (diskNumStart != 0)
                {
                    throw new UnsupportedFormatException("Entrada ZIP referenciando disco externo não suportada.");
                }

                byte[] nameBytes = reader.ReadBytes(nameLen);
                bool isUtf8 = (flags & ZipConstants.FlagUtf8) != 0;
                string entryName = isUtf8 ? Encoding.UTF8.GetString(nameBytes) : DecodeOemOrUtf8(nameBytes);
                entryName = ZipEntry.NormalizeEntryName(entryName);

                byte[] extraBytes = extraLen > 0 ? reader.ReadBytes(extraLen) : new byte[0];
                byte[] fileCommentBytes = fileCommentLen > 0 ? reader.ReadBytes(fileCommentLen) : new byte[0];
                string fileComment = fileCommentBytes.Length > 0 ? Encoding.UTF8.GetString(fileCommentBytes) : string.Empty;

                var entry = new ZipEntry
                {
                    Name = entryName,
                    Method = (ZipCompressionMethod)method,
                    Crc32 = crc,
                    CompressedSize = compSize,
                    UncompressedSize = uncompSize,
                    LastWriteTime = ZipEntry.FromDosDateTime(modDate, modTime),
                    Comment = fileComment,
                    ExtraField = extraBytes,
                    LocalHeaderOffset = localHeaderOffset,
                    ExternalAttributes = externalAttr,
                    GeneralPurposeFlags = flags,
                    VersionNeeded = versionNeeded,
                    VersionMadeBy = versionMadeBy
                };

                _entries.Add(entry);
            }
        }

        private static string DecodeOemOrUtf8(byte[] bytes)
        {
            try
            {
                // Tenta UTF-8 sem BOM; se for UTF-8 válido, usa
                var utf8 = new UTF8Encoding(false, true);
                return utf8.GetString(bytes);
            }
            catch
            {
                return Encoding.Default.GetString(bytes);
            }
        }

        public void ExtractEntry(ZipEntry entry, Stream destinationStream)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            if (destinationStream == null) throw new ArgumentNullException("destinationStream");

            if (entry.IsDirectory)
            {
                return;
            }

            if ((entry.GeneralPurposeFlags & ZipConstants.FlagEncrypted) != 0)
            {
                throw new UnsupportedFormatException(string.Format(
                    "A entrada ZIP '{0}' está protegida por senha/criptografia não suportada.", entry.Name));
            }

            if (entry.Method != ZipCompressionMethod.Store && entry.Method != ZipCompressionMethod.Deflate)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Método de compressão {0} ({1}) não é suportado pelo motor nativo na entrada '{2}'.",
                    (ushort)entry.Method, entry.Method, entry.Name));
            }

            SecurityLimits.AssertZipEntrySize(entry.UncompressedSize);

            // Posiciona no Local File Header da entrada
            if (entry.LocalHeaderOffset + 30 > _stream.Length)
            {
                throw new CorruptDataException(string.Format(
                    "Deslocamento do cabeçalho local da entrada '{0}' ({1}) excede o tamanho do arquivo.",
                    entry.Name, entry.LocalHeaderOffset));
            }

            _stream.Seek(entry.LocalHeaderOffset, SeekOrigin.Begin);
            var reader = new BinaryReader(_stream, Encoding.UTF8);

            uint lfhSig = reader.ReadUInt32();
            if (lfhSig != ZipConstants.LocalFileHeaderSignature)
            {
                throw new CorruptDataException(string.Format(
                    "Assinatura do Local File Header corrompida para '{0}' (esperado 0x04034B50, lido 0x{1:X8}).",
                    entry.Name, lfhSig));
            }

            // Pula campos redundantes do LFH até os comprimentos de nome e extra (22 bytes)
            reader.ReadBytes(22);
            ushort localNameLen = reader.ReadUInt16();
            ushort localExtraLen = reader.ReadUInt16();

            long payloadStart = entry.LocalHeaderOffset + 30 + localNameLen + localExtraLen;
            if (payloadStart + entry.CompressedSize > _stream.Length)
            {
                throw new CorruptDataException(string.Format(
                    "Dados comprimidos da entrada '{0}' ({1} bytes) ultrapassam o final do arquivo.",
                    entry.Name, entry.CompressedSize));
            }

            _stream.Seek(payloadStart, SeekOrigin.Begin);

            uint computedCrc = Crc32.InitialCrc;
            long bytesWritten = 0;

            if (entry.Method == ZipCompressionMethod.Store)
            {
                if (entry.CompressedSize != entry.UncompressedSize)
                {
                    throw new CorruptDataException(string.Format(
                        "Inconsistência na entrada Store '{0}': tamanho comprimido ({1}) != descomprimido ({2}).",
                        entry.Name, entry.CompressedSize, entry.UncompressedSize));
                }

                byte[] buffer = new byte[8192];
                long remaining = entry.CompressedSize;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(buffer.Length, remaining);
                    int read = _stream.Read(buffer, 0, toRead);
                    if (read <= 0)
                    {
                        throw new CorruptDataException(string.Format(
                            "Fim inesperado ao ler dados Store da entrada '{0}'.", entry.Name));
                    }
                    computedCrc = Crc32.Update(computedCrc, buffer, 0, read);
                    destinationStream.Write(buffer, 0, read);
                    bytesWritten += read;
                    remaining -= read;
                }
            }
            else // ZipCompressionMethod.Deflate
            {
                if (entry.UncompressedSize == 0 && entry.CompressedSize == 0)
                {
                    computedCrc = Crc32.InitialCrc;
                    bytesWritten = 0;
                }
                else
                {
                    using (var boundedStream = new BoundedStream(_stream, entry.CompressedSize))
                    using (var crcStream = new CrcWritingStream(destinationStream, computedCrc))
                    {
                        // Defesa contra mentira de tamanho no cabeçalho / Zip Bomb: limita descompressão ao menor entre declarado e limite global
                        long maxDecompressBytes = Math.Min(entry.UncompressedSize, SecurityLimits.MaxZipEntrySize);
                        try
                        {
                            DeflateDecompressor.Decompress(boundedStream, crcStream, maxDecompressBytes);
                        }
                        catch (ResourceLimitException ex)
                        {
                            if (entry.UncompressedSize < SecurityLimits.MaxZipEntrySize)
                            {
                                throw new CorruptDataException(string.Format(
                                    "Tamanho descompactado incorreto na entrada '{0}': excede o tamanho declarado no cabeçalho ({1:N0} bytes).",
                                    entry.Name, entry.UncompressedSize), ex);
                            }
                            throw;
                        }
                        computedCrc = crcStream.CurrentCrc;
                        bytesWritten = crcStream.BytesWritten;
                    }
                }
            }

            uint finalCrc = Crc32.Finalize(computedCrc);

            if (bytesWritten != entry.UncompressedSize)
            {
                throw new CorruptDataException(string.Format(
                    "Tamanho descompactado incorreto na entrada '{0}': esperado {1:N0} bytes, gravado {2:N0} bytes.",
                    entry.Name, entry.UncompressedSize, bytesWritten));
            }

            if (finalCrc != entry.Crc32)
            {
                throw new CorruptDataException(string.Format(
                    "Falha de integridade CRC-32 na entrada '{0}': calculado 0x{1:X8}, esperado 0x{2:X8}.",
                    entry.Name, finalCrc, entry.Crc32));
            }
        }

        public void ExtractAll(string destinationDirectory, Action<int> onProgress, Func<bool> isCancelled = null)
        {
            ExtractAll(destinationDirectory, overwrite: true, onProgress: onProgress, isCancelled: isCancelled);
        }

        public void ExtractAll(string destinationDirectory, bool overwrite = true, Action<int> onProgress = null, Func<bool> isCancelled = null)
        {
            if (string.IsNullOrEmpty(destinationDirectory)) throw new ArgumentNullException("destinationDirectory");
            string destFull = Path.GetFullPath(destinationDirectory);
            if (!Directory.Exists(destFull))
            {
                Directory.CreateDirectory(destFull);
            }

            // Pré-validação do tamanho total descompactado declarado de todas as entradas
            long totalDeclaredBytes = 0;
            for (int k = 0; k < _entries.Count; k++)
            {
                if (!_entries[k].IsDirectory)
                {
                    totalDeclaredBytes += _entries[k].UncompressedSize;
                }
            }
            SecurityLimits.AssertZipTotalExtracted(totalDeclaredBytes);

            var dirTimestamps = new List<KeyValuePair<string, DateTime>>();
            long totalAccumulatedBytes = 0;
            int total = _entries.Count;

            for (int i = 0; i < total; i++)
            {
                if (isCancelled != null && isCancelled())
                {
                    break;
                }

                var entry = _entries[i];
                string targetPath = ZipSecurity.GetSafeExtractionPath(destFull, entry.Name);

                if (entry.IsDirectory)
                {
                    if (!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    dirTimestamps.Add(new KeyValuePair<string, DateTime>(targetPath, entry.LastWriteTime));
                }
                else
                {
                    if (File.Exists(targetPath) && !overwrite)
                    {
                        throw new IOException(string.Format(
                            "O arquivo de destino já existe e a substituição não foi permitida: '{0}'.", targetPath));
                    }

                    string parentDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    // Gravação atômica segura: extrai para arquivo temporário e substitui com proteção de backup contra perda
                    string tempFile = targetPath + ".lodinotmp_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    try
                    {
                        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            ExtractEntry(entry, fs);
                        }

                        totalAccumulatedBytes += entry.UncompressedSize;
                        SecurityLimits.AssertZipTotalExtracted(totalAccumulatedBytes);

                        if (File.Exists(targetPath))
                        {
                            string backupFile = targetPath + ".lodinobak_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                            File.Move(targetPath, backupFile);
                            try
                            {
                                File.Move(tempFile, targetPath);
                                try { File.Delete(backupFile); } catch { }
                            }
                            catch
                            {
                                if (File.Exists(backupFile))
                                {
                                    try { File.Move(backupFile, targetPath); } catch { }
                                }
                                throw;
                            }
                        }
                        else
                        {
                            File.Move(tempFile, targetPath);
                        }

                        try
                        {
                            File.SetLastWriteTime(targetPath, entry.LastWriteTime);
                        }
                        catch { }
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

                if (onProgress != null && total > 0)
                {
                    int pct = (int)(((long)(i + 1) * 100) / total);
                    onProgress(pct);
                }
            }

            // Restaura carimbos de data/hora dos diretórios após extração completa dos arquivos
            for (int d = 0; d < dirTimestamps.Count; d++)
            {
                try
                {
                    if (Directory.Exists(dirTimestamps[d].Key))
                    {
                        Directory.SetLastWriteTime(dirTimestamps[d].Key, dirTimestamps[d].Value);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Descompacta e retorna os bytes de uma entrada específica em memória.
        /// </summary>
        public byte[] ReadEntryBytes(ZipEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            int capacity = (int)Math.Min(entry.UncompressedSize, 1024 * 1024);
            using (var ms = new MemoryStream(capacity))
            {
                ExtractEntry(entry, ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Extrai todos os arquivos de um contêiner ZIP para o diretório de destino.
        /// </summary>
        public static void ExtractToDirectory(string zipFilePath, string destinationDirectory, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(zipFilePath)) throw new ArgumentNullException("zipFilePath");
            using (var reader = new ZipReader(zipFilePath))
            {
                reader.ExtractAll(destinationDirectory, overwrite: overwrite);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (!_leaveOpen && _stream != null)
                {
                    _stream.Dispose();
                }
            }
        }

        private class BoundedStream : Stream
        {
            private readonly Stream _inner;
            private readonly long _maxBytes;
            private long _bytesRead;

            public BoundedStream(Stream inner, long maxBytes)
            {
                _inner = inner;
                _maxBytes = maxBytes;
                _bytesRead = 0;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return _maxBytes; } }
            public override long Position
            {
                get { return _bytesRead; }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_bytesRead >= _maxBytes) return 0;
                int toRead = (int)Math.Min((long)count, _maxBytes - _bytesRead);
                int read = _inner.Read(buffer, offset, toRead);
                if (read > 0) _bytesRead += read;
                return read;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private class CrcWritingStream : Stream
        {
            private readonly Stream _dest;
            private uint _crc;
            private long _written;

            public uint CurrentCrc { get { return _crc; } }
            public long BytesWritten { get { return _written; } }

            public CrcWritingStream(Stream dest, uint initialCrc)
            {
                _dest = dest;
                _crc = initialCrc;
                _written = 0;
            }

            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override long Length { get { return _written; } }
            public override long Position
            {
                get { return _written; }
                set { throw new NotSupportedException(); }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count <= 0) return;
                _crc = Crc32.Update(_crc, buffer, offset, count);
                _dest.Write(buffer, offset, count);
                _written += count;
            }

            public override void Flush() { _dest.Flush(); }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
        }
    }
}
