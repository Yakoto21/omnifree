using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Representa o cabeçalho GZIP com campos fixos e opcionais (RFC 1952 § 2.3.1).
    /// </summary>
    public class GzipHeader
    {
        public byte CompressionMethod { get; set; }
        public byte Flags { get; set; }
        public uint MTimeRaw { get; set; }
        public DateTime? ModificationTime { get; set; }
        public byte ExtraFlags { get; set; }
        public GzipOperatingSystem OperatingSystem { get; set; }

        public byte[] ExtraData { get; set; }
        public string FileName { get; set; }
        public string Comment { get; set; }
        public ushort? HeaderCrc { get; set; }

        public bool HasText
        {
            get { return (Flags & GzipConstants.FlagText) != 0; }
            set { SetFlag(GzipConstants.FlagText, value); }
        }

        public bool HasHeaderCrc
        {
            get { return (Flags & GzipConstants.FlagHeaderCrc) != 0; }
            set { SetFlag(GzipConstants.FlagHeaderCrc, value); }
        }

        public bool HasExtra
        {
            get { return (Flags & GzipConstants.FlagExtra) != 0; }
            set { SetFlag(GzipConstants.FlagExtra, value); }
        }

        public bool HasFileName
        {
            get { return (Flags & GzipConstants.FlagFileName) != 0; }
            set { SetFlag(GzipConstants.FlagFileName, value); }
        }

        public bool HasComment
        {
            get { return (Flags & GzipConstants.FlagComment) != 0; }
            set { SetFlag(GzipConstants.FlagComment, value); }
        }

        private void SetFlag(byte mask, bool val)
        {
            if (val) Flags |= mask;
            else Flags = (byte)(Flags & ~mask);
        }

        public GzipHeader()
        {
            CompressionMethod = GzipConstants.CompressionMethodDeflate;
            Flags = 0;
            MTimeRaw = 0;
            ModificationTime = null;
            ExtraFlags = 0;
            OperatingSystem = GzipOperatingSystem.Fat;
        }

        public static GzipHeader Read(Stream stream, byte? firstByte = null)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            return ReadInternal(b =>
            {
                int val = stream.ReadByte();
                if (val < 0) throw new CorruptDataException("Fim prematuro do fluxo GZIP ao ler o cabeçalho.");
                return (byte)val;
            }, (buf, off, count) =>
            {
                int read = 0;
                while (read < count)
                {
                    int r = stream.Read(buf, off + read, count - read);
                    if (r <= 0) throw new CorruptDataException("Fim prematuro do fluxo GZIP ao ler campos opcionais.");
                    read += r;
                }
            }, firstByte);
        }

        public static GzipHeader Read(DeflateBitReader reader, byte? firstByte = null)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return ReadInternal(b => reader.ReadByte(), (buf, off, count) => reader.ReadBytes(buf, off, count), firstByte);
        }

        private static GzipHeader ReadInternal(Func<object, byte> readByteFunc, Action<byte[], int, int> readBytesAction, byte? firstByte)
        {
            var headerBytesAccumulator = new List<byte>(32);

            Func<byte> readByteTracked = () =>
            {
                byte b = readByteFunc(null);
                headerBytesAccumulator.Add(b);
                SecurityLimits.AssertGzipHeaderSize(headerBytesAccumulator.Count);
                return b;
            };

            // 1. Cabeçalho fixo de 10 bytes (RFC 1952 § 2.3.1)
            byte id1;
            if (firstByte.HasValue)
            {
                id1 = firstByte.Value;
                headerBytesAccumulator.Add(id1);
            }
            else
            {
                id1 = readByteTracked();
            }
            byte id2 = readByteTracked();

            if (id1 != GzipConstants.Id1 || id2 != GzipConstants.Id2)
            {
                throw new CorruptDataException(string.Format(
                    "Assinatura mágica GZIP inválida: 0x{0:X2} 0x{1:X2} (esperado: 0x1F 0x8B).",
                    id1, id2));
            }

            byte cm = readByteTracked();
            if (cm != GzipConstants.CompressionMethodDeflate)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Método de compressão GZIP não suportado: {0} (suportado exclusivamente: 8 = DEFLATE).",
                    cm));
            }

            byte flg = readByteTracked();
            if ((flg & GzipConstants.FlagReservedMask) != 0)
            {
                throw new CorruptDataException(string.Format(
                    "Flags reservadas do GZIP estão ativas (FLG = 0x{0:X2}, máscara 0xE0), violando a RFC 1952.",
                    flg));
            }

            byte m0 = readByteTracked();
            byte m1 = readByteTracked();
            byte m2 = readByteTracked();
            byte m3 = readByteTracked();
            uint mtime = (uint)(m0 | (m1 << 8) | (m2 << 16) | (m3 << 24));

            byte xfl = readByteTracked();
            byte os = readByteTracked();

            var header = new GzipHeader
            {
                CompressionMethod = cm,
                Flags = flg,
                MTimeRaw = mtime,
                ExtraFlags = xfl,
                OperatingSystem = (GzipOperatingSystem)os
            };

            if (mtime > 0)
            {
                try
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    header.ModificationTime = epoch.AddSeconds(mtime);
                }
                catch
                {
                    header.ModificationTime = null;
                }
            }

            // 2. Campo opcional FEXTRA (se bit 2 ativo)
            if (header.HasExtra)
            {
                byte x0 = readByteTracked();
                byte x1 = readByteTracked();
                ushort xlen = (ushort)(x0 | (x1 << 8));

                byte[] extra = new byte[xlen];
                for (int i = 0; i < xlen; i++)
                {
                    extra[i] = readByteTracked();
                }
                header.ExtraData = extra;
            }

            // 3. Campo opcional FNAME (se bit 3 ativo)
            if (header.HasFileName)
            {
                var nameBytes = new List<byte>(64);
                while (true)
                {
                    byte b = readByteTracked();
                    if (b == 0) break;
                    nameBytes.Add(b);
                    SecurityLimits.AssertGzipFilenameLength(nameBytes.Count);
                }
                header.FileName = Encoding.GetEncoding("ISO-8859-1").GetString(nameBytes.ToArray());
            }

            // 4. Campo opcional FCOMMENT (se bit 4 ativo)
            if (header.HasComment)
            {
                var commentBytes = new List<byte>(128);
                while (true)
                {
                    byte b = readByteTracked();
                    if (b == 0) break;
                    commentBytes.Add(b);
                    SecurityLimits.AssertGzipCommentLength(commentBytes.Count);
                }
                header.Comment = Encoding.GetEncoding("ISO-8859-1").GetString(commentBytes.ToArray());
            }

            // 5. Campo opcional FHCRC (se bit 1 ativo)
            if (header.HasHeaderCrc)
            {
                // Guarda a contagem de bytes do cabeçalho ANTES de ler o FHCRC
                byte[] bytesForCrc = headerBytesAccumulator.ToArray();

                byte c0 = readByteFunc(null);
                byte c1 = readByteFunc(null);
                ushort expectedCrc16 = (ushort)(c0 | (c1 << 8));
                header.HeaderCrc = expectedCrc16;

                // Validação: os 16 bits menos significativos do CRC-32 de todos os bytes anteriores
                uint fullCrc32 = Crc32.Calculate(bytesForCrc);
                ushort calculatedCrc16 = (ushort)(fullCrc32 & 0xFFFF);

                if (expectedCrc16 != calculatedCrc16)
                {
                    throw new CorruptDataException(string.Format(
                        "Falha de integridade no cabeçalho GZIP: checksum FHCRC (0x{0:X4}) difere do calculado (0x{1:X4}).",
                        expectedCrc16, calculatedCrc16));
                }
            }

            return header;
        }

        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var ms = new MemoryStream();

            // 1. Cabeçalho fixo (10 bytes)
            ms.WriteByte(GzipConstants.Id1);
            ms.WriteByte(GzipConstants.Id2);
            ms.WriteByte(CompressionMethod);
            ms.WriteByte(Flags);

            uint mtime = MTimeRaw;
            if (mtime == 0 && ModificationTime.HasValue)
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var span = ModificationTime.Value.ToUniversalTime() - epoch;
                if (span.TotalSeconds > 0)
                {
                    mtime = (uint)Math.Min((double)uint.MaxValue, span.TotalSeconds);
                }
            }

            ms.WriteByte((byte)(mtime & 0xFF));
            ms.WriteByte((byte)((mtime >> 8) & 0xFF));
            ms.WriteByte((byte)((mtime >> 16) & 0xFF));
            ms.WriteByte((byte)((mtime >> 24) & 0xFF));

            ms.WriteByte(ExtraFlags);
            ms.WriteByte((byte)OperatingSystem);

            // 2. FEXTRA
            if (HasExtra && ExtraData != null)
            {
                ushort xlen = (ushort)Math.Min(ushort.MaxValue, ExtraData.Length);
                ms.WriteByte((byte)(xlen & 0xFF));
                ms.WriteByte((byte)((xlen >> 8) & 0xFF));
                ms.Write(ExtraData, 0, xlen);
            }

            // 3. FNAME
            if (HasFileName && !string.IsNullOrEmpty(FileName))
            {
                byte[] nameBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(FileName);
                ms.Write(nameBytes, 0, nameBytes.Length);
                ms.WriteByte(0); // Terminador nulo
            }

            // 4. FCOMMENT
            if (HasComment && !string.IsNullOrEmpty(Comment))
            {
                byte[] commentBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(Comment);
                ms.Write(commentBytes, 0, commentBytes.Length);
                ms.WriteByte(0); // Terminador nulo
            }

            // 5. FHCRC
            if (HasHeaderCrc)
            {
                byte[] headerSoFar = ms.ToArray();
                uint fullCrc32 = Crc32.Calculate(headerSoFar);
                ushort crc16 = (ushort)(fullCrc32 & 0xFFFF);
                ms.WriteByte((byte)(crc16 & 0xFF));
                ms.WriteByte((byte)((crc16 >> 8) & 0xFF));
            }

            byte[] finalHeader = ms.ToArray();
            stream.Write(finalHeader, 0, finalHeader.Length);
        }
    }
}
