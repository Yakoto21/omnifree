using System;
using System.IO;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Leitor de fluxo de bits para descompressão DEFLATE (RFC 1951).
    /// Os bits são consumidos na ordem LSB-first (do bit menos significativo para o mais significativo).
    /// </summary>
    public class DeflateBitReader
    {
        private readonly Stream _stream;
        private ulong _bitBuffer;
        private int _bitsCount;

        public DeflateBitReader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            _stream = stream;
            _bitBuffer = 0;
            _bitsCount = 0;
        }

        public int ReadBits(int count)
        {
            if (count < 0 || count > 32)
            {
                throw new ArgumentOutOfRangeException("count", "Contagem de bits deve estar entre 0 e 32.");
            }
            if (count == 0) return 0;

            while (_bitsCount < count)
            {
                int b = _stream.ReadByte();
                if (b < 0)
                {
                    throw new CorruptDataException("Fim inesperado do fluxo de dados DEFLATE ao ler bits.");
                }
                _bitBuffer |= ((ulong)(byte)b) << _bitsCount;
                _bitsCount += 8;
            }

            int result = (int)(_bitBuffer & ((1UL << count) - 1));
            _bitBuffer >>= count;
            _bitsCount -= count;
            return result;
        }

        public int ReadBit()
        {
            return ReadBits(1);
        }

        public void AlignToByte()
        {
            int discard = _bitsCount % 8;
            if (discard > 0)
            {
                _bitBuffer >>= discard;
                _bitsCount -= discard;
            }
        }

        public byte ReadByte()
        {
            if (_bitsCount >= 8)
            {
                byte b = (byte)(_bitBuffer & 0xFF);
                _bitBuffer >>= 8;
                _bitsCount -= 8;
                return b;
            }

            int next = _stream.ReadByte();
            if (next < 0)
            {
                throw new CorruptDataException("Fim inesperado do fluxo DEFLATE ao ler byte alinhado.");
            }
            return (byte)next;
        }

        public ushort ReadUInt16LittleEndian()
        {
            byte b0 = ReadByte();
            byte b1 = ReadByte();
            return (ushort)(b0 | (b1 << 8));
        }

        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = ReadByte();
            }
        }
    }
}
