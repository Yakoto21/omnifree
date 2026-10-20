using System;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Estrutura e manipulador de chunks binários PNG (ISO/IEC 15948).
    /// Cada chunk é composto por: Comprimento (4 bytes), Tipo FourCC (4 bytes), Dados (N bytes) e CRC-32 (4 bytes).
    /// </summary>
    public class PngChunk
    {
        public const int MaxChunkSize = 64 * 1024 * 1024; // Limite de 64 MB por chunk contra ataques DoS

        public string Type { get; set; }
        public byte[] Data { get; set; }
        public uint Crc { get; set; }

        public PngChunk(string type, byte[] data)
        {
            if (string.IsNullOrEmpty(type) || type.Length != 4)
            {
                throw new ArgumentException("O tipo do chunk deve possuir exatamente 4 caracteres ASCII.", "type");
            }
            Type = type;
            Data = data ?? new byte[0];
            Crc = PngCrc32.Calculate(Type, Data, 0, Data.Length);
        }

        public static PngChunk Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            byte[] header = new byte[8]; // 4 bytes comprimento + 4 bytes tipo
            int read = StreamUtils.ReadExact(stream, header, 0, 8);
            if (read == 0) return null; // Fim legítimo do arquivo
            if (read < 8)
            {
                throw new CorruptDataException("Cabeçalho de chunk PNG incompleto ou truncado.");
            }

            uint length = ((uint)header[0] << 24) |
                          ((uint)header[1] << 16) |
                          ((uint)header[2] << 8)  |
                           (uint)header[3];

            if (length > MaxChunkSize)
            {
                throw new ResourceLimitException(string.Format(
                    "Chunk PNG com tamanho ({0:N0} bytes) excede o limite máximo permitido de {1:N0} bytes.",
                    length, MaxChunkSize));
            }

            string type = Encoding.ASCII.GetString(header, 4, 4);

            byte[] data = new byte[length];
            StreamUtils.ReadExactOrThrow(stream, data, 0, (int)length,
                string.Format("Fim inesperado ao ler dados do chunk '{0}'.", type));

            byte[] crcBytes = new byte[4];
            StreamUtils.ReadExactOrThrow(stream, crcBytes, 0, 4,
                string.Format("CRC-32 do chunk '{0}' incompleto ou truncado.", type));

            uint fileCrc = ((uint)crcBytes[0] << 24) |
                           ((uint)crcBytes[1] << 16) |
                           ((uint)crcBytes[2] << 8)  |
                            (uint)crcBytes[3];

            uint actualCrc = PngCrc32.Calculate(type, data, 0, data.Length);
            if (actualCrc != fileCrc)
            {
                throw new CorruptDataException(string.Format(
                    "Falha de integridade no chunk '{0}': CRC-32 calculado (0x{1:X8}) difere do arquivo (0x{2:X8}).",
                    type, actualCrc, fileCrc));
            }

            return new PngChunk(type, data) { Crc = fileCrc };
        }

        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            int length = Data != null ? Data.Length : 0;

            // 1. Comprimento (4 bytes Big-Endian)
            stream.WriteByte((byte)((length >> 24) & 0xFF));
            stream.WriteByte((byte)((length >> 16) & 0xFF));
            stream.WriteByte((byte)((length >> 8) & 0xFF));
            stream.WriteByte((byte)(length & 0xFF));

            // 2. Tipo (4 bytes ASCII)
            byte[] typeBytes = Encoding.ASCII.GetBytes(Type);
            stream.Write(typeBytes, 0, 4);

            // 3. Dados (N bytes)
            if (length > 0)
            {
                stream.Write(Data, 0, length);
            }

            // 4. CRC-32 (4 bytes Big-Endian)
            uint crc = PngCrc32.Calculate(Type, Data, 0, length);
            stream.WriteByte((byte)((crc >> 24) & 0xFF));
            stream.WriteByte((byte)((crc >> 16) & 0xFF));
            stream.WriteByte((byte)((crc >> 8) & 0xFF));
            stream.WriteByte((byte)(crc & 0xFF));
        }
    }
}
