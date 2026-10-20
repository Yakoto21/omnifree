using System;
using System.IO;
using Lodino.Core.Checksums;
using Lodino.Core.Compression.Deflate;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Implementação original da camada de encapsulamento ZLIB (RFC 1950).
    /// Valida cabeçalho (CMF/FLG), processa o fluxo DEFLATE subjacente e verifica o checksum Adler-32.
    /// </summary>
    public static class ZlibStream
    {
        public static byte[] Decompress(byte[] compressedData, long maxOutputBytes = SecurityLimits.DefaultMaxImageFileSize)
        {
            if (compressedData == null) throw new ArgumentNullException("compressedData");
            using (var ms = new MemoryStream(compressedData, false))
            {
                return Decompress(ms, maxOutputBytes);
            }
        }

        public static byte[] Decompress(Stream stream, long maxOutputBytes = SecurityLimits.DefaultMaxImageFileSize, bool allowTrailingBytes = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            // 1. Cabeçalho ZLIB (2 bytes)
            int cmf = stream.ReadByte();
            int flg = stream.ReadByte();
            if (cmf < 0 || flg < 0)
            {
                throw new CorruptDataException("Fluxo ZLIB incompleto: cabeçalho ausente ou truncado.");
            }

            int check = (cmf << 8) | flg;
            if (check % 31 != 0)
            {
                throw new CorruptDataException(string.Format(
                    "Cabeçalho ZLIB inválido: verificação FCHECK falhou (0x{0:X4} não é múltiplo de 31).", check));
            }

            int cm = cmf & 0x0F;
            if (cm != 8)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Método de compressão ZLIB {0} não é suportado (apenas DEFLATE = 8 é suportado).", cm));
            }

            int cinfo = (cmf >> 4) & 0x0F;
            if (cinfo > 7)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Tamanho de janela ZLIB (CINFO = {0}) excede o máximo de 32 KB permitido.", cinfo));
            }

            int fdict = (flg >> 5) & 0x01;
            if (fdict != 0)
            {
                throw new UnsupportedFormatException("Fluxos ZLIB com dicionário predefinido (FDICT) não são permitidos em PNG.");
            }

            // 2. Descompressão do corpo DEFLATE
            byte[] decompressed = DeflateDecompressor.Decompress(stream, maxOutputBytes);

            // 3. Verificação do Adler-32 final (4 bytes, Big-Endian)
            byte[] adlerBytes = new byte[4];
            Lodino.Core.IO.StreamUtils.ReadExactOrThrow(stream, adlerBytes, 0, 4,
                "Checksum Adler-32 ausente ou truncado no final do fluxo ZLIB.");

            uint expectedAdler = ((uint)adlerBytes[0] << 24) |
                                 ((uint)adlerBytes[1] << 16) |
                                 ((uint)adlerBytes[2] << 8)  |
                                  (uint)adlerBytes[3];

            uint actualAdler = Adler32.Calculate(decompressed, 0, decompressed.Length);

            if (actualAdler != expectedAdler)
            {
                throw new CorruptDataException(string.Format(
                    "Falha de integridade ZLIB: Adler-32 calculado (0x{0:X8}) não coincide com o esperado (0x{1:X8}).",
                    actualAdler, expectedAdler));
            }

            // ISO/IEC 15948 §10.2 estabelece que um decodificador PNG deve ignorar bytes residuais não consumidos
            // após o término do fluxo ZLIB dentro da sequência de chunks IDAT.
            // Para fluxos ZLIB puros isolados (RFC 1950), a presença de bytes extras continua sendo estritamente rejeitada.
            if (!allowTrailingBytes && stream.ReadByte() != -1)
            {
                throw new CorruptDataException("Dados espúrios detectados após o término do fluxo ZLIB.");
            }

            return decompressed;
        }

        public static byte[] Compress(byte[] rawData)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            using (var ms = new MemoryStream())
            {
                Compress(rawData, ms);
                return ms.ToArray();
            }
        }

        public static void Compress(byte[] rawData, Stream outputStream)
        {
            if (rawData == null) throw new ArgumentNullException("rawData");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            // 1. Grava cabeçalho ZLIB (CMF = 0x78, FLG = 0x01 -> check = 0x7801 = 30721 = 31 * 991)
            outputStream.WriteByte(0x78);
            outputStream.WriteByte(0x01);

            // 2. Grava blocos DEFLATE não comprimidos (BTYPE = 00)
            // Divide em blocos de até 65535 bytes
            const int maxBlockSize = 65535;
            int offset = 0;
            int remaining = rawData.Length;

            if (remaining == 0)
            {
                // Bloco vazio final
                outputStream.WriteByte(0x01); // BFINAL = 1, BTYPE = 00
                outputStream.WriteByte(0x00); outputStream.WriteByte(0x00); // LEN = 0
                outputStream.WriteByte(0xFF); outputStream.WriteByte(0xFF); // NLEN = ~0
            }
            else
            {
                while (remaining > 0)
                {
                    int blockSize = remaining > maxBlockSize ? maxBlockSize : remaining;
                    remaining -= blockSize;
                    bool isFinal = remaining == 0;

                    byte headerByte = (byte)(isFinal ? 0x01 : 0x00);
                    outputStream.WriteByte(headerByte);

                    ushort len = (ushort)blockSize;
                    ushort nlen = (ushort)(~len);

                    outputStream.WriteByte((byte)(len & 0xFF));
                    outputStream.WriteByte((byte)((len >> 8) & 0xFF));
                    outputStream.WriteByte((byte)(nlen & 0xFF));
                    outputStream.WriteByte((byte)((nlen >> 8) & 0xFF));

                    outputStream.Write(rawData, offset, blockSize);
                    offset += blockSize;
                }
            }

            // 3. Grava checksum Adler-32 (4 bytes Big-Endian)
            uint adler = Adler32.Calculate(rawData, 0, rawData.Length);
            outputStream.WriteByte((byte)((adler >> 24) & 0xFF));
            outputStream.WriteByte((byte)((adler >> 16) & 0xFF));
            outputStream.WriteByte((byte)((adler >> 8) & 0xFF));
            outputStream.WriteByte((byte)(adler & 0xFF));
        }
    }
}
