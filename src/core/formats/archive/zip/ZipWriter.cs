using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Gerador proprietário de contêineres ZIP padrão PKWARE.
    /// Suporta criação com métodos Store (0) e DEFLATE (8), nomes UTF-8 e subdiretórios.
    /// </summary>
    public class ZipWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly List<ZipEntry> _entries = new List<ZipEntry>();
        private string _comment = string.Empty;
        private bool _finished = false;
        private bool _disposed = false;

        public string Comment
        {
            get { return _comment; }
            set { _comment = value ?? string.Empty; }
        }

        public IReadOnlyList<ZipEntry> Entries
        {
            get { return _entries.AsReadOnly(); }
        }

        public ZipWriter(Stream stream, bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanWrite) throw new ArgumentException("O stream deve permitir gravação.", "stream");
            _stream = stream;
            _leaveOpen = leaveOpen;
        }

        public ZipWriter(string filePath)
            : this(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None), leaveOpen: false)
        {
        }

        public void AddDirectory(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) throw new ArgumentNullException("dirName");
            string safeName = ZipSecurity.ValidateSafeRelativePath(dirName);
            if (!safeName.EndsWith("/"))
            {
                safeName += "/";
            }

            var entry = new ZipEntry(safeName)
            {
                Method = ZipCompressionMethod.Store,
                Crc32 = 0,
                CompressedSize = 0,
                UncompressedSize = 0,
                LastWriteTime = DateTime.Now,
                VersionNeeded = ZipConstants.VersionStoreNeeded,
                ExternalAttributes = ZipConstants.FileAttributeDirectoryWindows | ZipConstants.FileAttributeDirectoryUnix
            };

            WriteLocalFile(entry, new byte[0]);
            _entries.Add(entry);
        }

        public void AddEntry(string entryName, byte[] data, ZipCompressionLevel level = ZipCompressionLevel.Optimal, string entryComment = null)
        {
            if (entryName == null) throw new ArgumentNullException("entryName");
            if (data == null) data = new byte[0];

            SecurityLimits.AssertZipEntrySize(data.Length);
            string safeName = ZipSecurity.ValidateSafeRelativePath(entryName);

            uint crc = Crc32.Calculate(data);
            ZipCompressionMethod method;
            byte[] payload;
            ushort versionNeeded;

            if (level == ZipCompressionLevel.Store)
            {
                method = ZipCompressionMethod.Store;
                payload = data;
                versionNeeded = ZipConstants.VersionStoreNeeded;
            }
            else
            {
                method = ZipCompressionMethod.Deflate;
                payload = DeflateCompressor.Compress(data, level);
                versionNeeded = ZipConstants.VersionDefaultNeeded;
            }

            var entry = new ZipEntry(safeName)
            {
                Method = method,
                Crc32 = crc,
                CompressedSize = payload.Length,
                UncompressedSize = data.Length,
                LastWriteTime = DateTime.Now,
                VersionNeeded = versionNeeded,
                ExternalAttributes = ZipConstants.FileAttributeRegularUnix,
                Comment = entryComment ?? string.Empty
            };

            WriteLocalFile(entry, payload);
            _entries.Add(entry);
        }

        public void AddEntry(string entryName, Stream inputStream, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int read;
                long total = 0;
                while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    SecurityLimits.AssertZipEntrySize(total);
                    ms.Write(buffer, 0, read);
                }
                AddEntry(entryName, ms.ToArray(), level);
            }
        }

        public void AddFile(string diskFilePath, string entryNameInZip = null, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(diskFilePath)) throw new ArgumentNullException("diskFilePath");
            if (!File.Exists(diskFilePath)) throw new FileNotFoundException("Arquivo não encontrado para inclusão no ZIP.", diskFilePath);

            string nameInZip = entryNameInZip;
            if (string.IsNullOrEmpty(nameInZip))
            {
                nameInZip = Path.GetFileName(diskFilePath);
            }

            byte[] data = File.ReadAllBytes(diskFilePath);
            DateTime lastWrite = File.GetLastWriteTime(diskFilePath);

            AddEntry(nameInZip, data, level);
            _entries[_entries.Count - 1].LastWriteTime = lastWrite;
        }

        public void AddDirectoryRecursive(string diskDirPath, string entryPrefix = "", ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(diskDirPath)) throw new ArgumentNullException("diskDirPath");
            if (!Directory.Exists(diskDirPath)) throw new DirectoryNotFoundException("Diretório não encontrado: " + diskDirPath);

            string fullDiskDir = Path.GetFullPath(diskDirPath);
            var dirInfo = new DirectoryInfo(fullDiskDir);

            if (!string.IsNullOrEmpty(entryPrefix))
            {
                AddDirectory(entryPrefix);
            }

            // Adiciona todos os arquivos do diretório
            foreach (var file in dirInfo.GetFiles())
            {
                string entryName = string.IsNullOrEmpty(entryPrefix)
                    ? file.Name
                    : (entryPrefix.TrimEnd('/') + "/" + file.Name);
                AddFile(file.FullName, entryName, level);
            }

            // Recurse em subdiretórios
            foreach (var subDir in dirInfo.GetDirectories())
            {
                string subPrefix = string.IsNullOrEmpty(entryPrefix)
                    ? (subDir.Name + "/")
                    : (entryPrefix.TrimEnd('/') + "/" + subDir.Name + "/");
                AddDirectoryRecursive(subDir.FullName, subPrefix, level);
            }
        }

        private void WriteLocalFile(ZipEntry entry, byte[] payload)
        {
            entry.LocalHeaderOffset = _stream.Position;
            var writer = new BinaryWriter(_stream, Encoding.UTF8);

            byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name);

            writer.Write(ZipConstants.LocalFileHeaderSignature);
            writer.Write(entry.VersionNeeded);
            writer.Write(entry.GeneralPurposeFlags);
            writer.Write((ushort)entry.Method);
            writer.Write(ZipEntry.ToDosTime(entry.LastWriteTime));
            writer.Write(ZipEntry.ToDosDate(entry.LastWriteTime));
            writer.Write(entry.Crc32);
            writer.Write((uint)entry.CompressedSize);
            writer.Write((uint)entry.UncompressedSize);
            writer.Write((ushort)nameBytes.Length);
            writer.Write((ushort)0); // Extra field length

            writer.Write(nameBytes);
            if (payload != null && payload.Length > 0)
            {
                writer.Write(payload);
            }
        }

        public void Finish()
        {
            if (_finished) return;

            long cdStartOffset = _stream.Position;
            var writer = new BinaryWriter(_stream, Encoding.UTF8);

            // Grava Central Directory Headers
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name);
                byte[] extraBytes = entry.ExtraField ?? new byte[0];
                byte[] commentBytes = Encoding.UTF8.GetBytes(entry.Comment ?? string.Empty);

                writer.Write(ZipConstants.CentralDirectoryHeaderSignature);
                writer.Write(entry.VersionMadeBy);
                writer.Write(entry.VersionNeeded);
                writer.Write(entry.GeneralPurposeFlags);
                writer.Write((ushort)entry.Method);
                writer.Write(ZipEntry.ToDosTime(entry.LastWriteTime));
                writer.Write(ZipEntry.ToDosDate(entry.LastWriteTime));
                writer.Write(entry.Crc32);
                writer.Write((uint)entry.CompressedSize);
                writer.Write((uint)entry.UncompressedSize);
                writer.Write((ushort)nameBytes.Length);
                writer.Write((ushort)extraBytes.Length);
                writer.Write((ushort)commentBytes.Length);
                writer.Write((ushort)0); // Disk number start
                writer.Write((ushort)0); // Internal file attributes
                writer.Write(entry.ExternalAttributes);
                writer.Write((uint)entry.LocalHeaderOffset);

                writer.Write(nameBytes);
                if (extraBytes.Length > 0) writer.Write(extraBytes);
                if (commentBytes.Length > 0) writer.Write(commentBytes);
            }

            long cdSize = _stream.Position - cdStartOffset;
            byte[] archiveCommentBytes = Encoding.UTF8.GetBytes(_comment ?? string.Empty);

            // Grava End of Central Directory Record (EOCD)
            writer.Write(ZipConstants.EndOfCentralDirectorySignature);
            writer.Write((ushort)0); // Number of this disk
            writer.Write((ushort)0); // Number of the disk with the start of the central directory
            writer.Write((ushort)_entries.Count); // Total entries in the central directory on this disk
            writer.Write((ushort)_entries.Count); // Total entries in the central directory
            writer.Write((uint)cdSize);
            writer.Write((uint)cdStartOffset);
            writer.Write((ushort)archiveCommentBytes.Length);
            if (archiveCommentBytes.Length > 0)
            {
                writer.Write(archiveCommentBytes);
            }

            writer.Flush();
            _finished = true;
        }

        /// <summary>
        /// Empacota um diretório local em um arquivo ZIP.
        /// </summary>
        public static void CreateFromDirectory(string sourceDirectory, string destinationZipFilePath, bool recursive = true, ZipCompressionLevel level = ZipCompressionLevel.Optimal)
        {
            if (string.IsNullOrEmpty(sourceDirectory)) throw new ArgumentNullException("sourceDirectory");
            if (string.IsNullOrEmpty(destinationZipFilePath)) throw new ArgumentNullException("destinationZipFilePath");

            using (var writer = new ZipWriter(destinationZipFilePath))
            {
                if (recursive)
                {
                    writer.AddDirectoryRecursive(sourceDirectory, "", level);
                }
                else
                {
                    var dir = new DirectoryInfo(sourceDirectory);
                    foreach (var file in dir.GetFiles())
                    {
                        writer.AddFile(file.FullName, file.Name, level);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (!_finished)
                {
                    Finish();
                }
                if (!_leaveOpen && _stream != null)
                {
                    _stream.Dispose();
                }
            }
        }
    }
}
