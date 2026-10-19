using System;
using System.IO;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Escritor de fluxo de bits para codificação DEFLATE (RFC 1951).
    /// Emite bits na ordem LSB-first e suporta alinhamento de byte.
    /// </summary>
    public class DeflateBitWriter
    {
        private readonly Stream _stream;
        private uint _buffer = 0;
        private int _bitCount = 0;

        public DeflateBitWriter(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            _stream = stream;
        }

        public void WriteBits(int value, int count)
        {
            if (count <= 0) return;
            _buffer |= ((uint)(value & ((1 << count) - 1))) << _bitCount;
            _bitCount += count;
            while (_bitCount >= 8)
            {
                _stream.WriteByte((byte)(_buffer & 0xFF));
                _buffer >>= 8;
                _bitCount -= 8;
            }
        }

        public void Flush()
        {
            if (_bitCount > 0)
            {
                _stream.WriteByte((byte)(_buffer & 0xFF));
                _buffer = 0;
                _bitCount = 0;
            }
        }

        public int BitCount
        {
            get { return _bitCount; }
        }
    }
}
