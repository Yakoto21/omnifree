using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Compression.Deflate;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Representa e valida o rodapé (trailer) de 8 bytes do GZIP (RFC 1952 § 2.3.1).
    /// Contém o CRC-32 dos dados descomprimidos e o tamanho original descomprimido (ISIZE modulo 2^32).
    /// </summary>
    public struct GzipTrailer
    {
        public uint Crc32;
        public uint ISize;

        public GzipTrailer(uint crc32, uint isize)
        {
            Crc32 = crc32;
            ISize = isize;
        }

        public static GzipTrailer Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            byte[] buffer = new byte[8];
            int read = 0;
            while (read < 8)
            {
                int r = stream.Read(buffer, read, 8 - read);
                if (r <= 0)
                {
                    throw new CorruptDataException(string.Format(
                        "Fim prematuro do fluxo GZIP ao tentar ler o trailer de 8 bytes (lidos {0} de 8 bytes).",
                        read));
                }
                read += r;
            }

            uint crc = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            uint isize = (uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));

            return new GzipTrailer(crc, isize);
        }

        public static GzipTrailer Read(DeflateBitReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            uint crc = reader.ReadUInt32LittleEndian();
            uint isize = reader.ReadUInt32LittleEndian();
            return new GzipTrailer(crc, isize);
        }

        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            byte[] buffer = new byte[8];
            buffer[0] = (byte)(Crc32 & 0xFF);
            buffer[1] = (byte)((Crc32 >> 8) & 0xFF);
            buffer[2] = (byte)((Crc32 >> 16) & 0xFF);
            buffer[3] = (byte)((Crc32 >> 24) & 0xFF);

            buffer[4] = (byte)(ISize & 0xFF);
            buffer[5] = (byte)((ISize >> 8) & 0xFF);
            buffer[6] = (byte)((ISize >> 16) & 0xFF);
            buffer[7] = (byte)((ISize >> 24) & 0xFF);

            stream.Write(buffer, 0, 8);
        }

        public void Validate(uint calculatedCrc32, long totalUncompressedBytes)
        {
            if (calculatedCrc32 != Crc32)
            {
                throw new CorruptDataException(string.Format(
                    "Falha de integridade GZIP: CRC-32 calculado (0x{0:X8}) difere do valor no trailer (0x{1:X8}).",
                    calculatedCrc32, Crc32));
            }

            uint expectedISize = (uint)(totalUncompressedBytes & 0xFFFFFFFFL);
            if (expectedISize != ISize)
            {
                throw new CorruptDataException(string.Format(
                    "Falha de integridade GZIP: tamanho descomprimido ISIZE calculado (0x{0:X8} = {1:N0} bytes mod 2^32) difere do trailer (0x{2:X8} = {2:N0} bytes).",
                    expectedISize, totalUncompressedBytes, ISize));
            }
        }
    }
}
