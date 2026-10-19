using System;
using System.IO;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Escritor de bits orientado a MSB-first com suporte a byte-stuffing automático (0xFF -> 0xFF 0x00)
    /// e empacotamento de símbolos de Huffman e valores com sinal.
    /// </summary>
    public class JpegBitWriter
    {
        private readonly Stream _stream;
        private uint _bitBuffer;
        private int _bitCount;

        public JpegBitWriter(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            _stream = stream;
            _bitBuffer = 0;
            _bitCount = 0;
        }

        /// <summary>
        /// Escreve 'count' bits (1..24) a partir do valor fornecido (bits mais significativos primeiro).
        /// </summary>
        public void WriteBits(int value, int count)
        {
            if (count <= 0) return;

            uint mask = (1u << count) - 1u;
            _bitBuffer = (_bitBuffer << count) | ((uint)value & mask);
            _bitCount += count;

            while (_bitCount >= 8)
            {
                byte b = (byte)((_bitBuffer >> (_bitCount - 8)) & 0xFF);
                _bitCount -= 8;
                _stream.WriteByte(b);

                // Regra de Byte-Stuffing: 0xFF no fluxo de entropia deve ser seguido de 0x00
                if (b == 0xFF)
                {
                    _stream.WriteByte(0x00);
                }
            }
        }

        /// <summary>
        /// Escreve o código de Huffman associado ao símbolo.
        /// </summary>
        public void WriteSymbol(int symbol, JpegHuffmanTable table)
        {
            int len = table.CodeLengths[symbol];
            if (len <= 0)
            {
                throw new InvalidOperationException(string.Format("Símbolo 0x{0:X2} não possui código na tabela de Huffman.", symbol));
            }
            WriteBits(table.Codes[symbol], len);
        }

        /// <summary>
        /// Escreve um valor com sinal (DC ou AC) codificando sua categoria na tabela de Huffman
        /// seguida pelos bits de magnitude.
        /// </summary>
        public void WriteSignedValue(int val, JpegHuffmanTable table)
        {
            int cat = GetCategory(val);
            WriteSymbol(cat, table);

            if (cat > 0)
            {
                int bits = val;
                if (val < 0)
                {
                    bits = val + (1 << cat) - 1;
                }
                WriteBits(bits, cat);
            }
        }

        /// <summary>
        /// Calcula a categoria de magnitude de um valor signed (0..11).
        /// </summary>
        public static int GetCategory(int val)
        {
            if (val == 0) return 0;
            int abs = val < 0 ? -val : val;
            int cat = 0;
            while (abs > 0)
            {
                cat++;
                abs >>= 1;
            }
            return cat;
        }

        /// <summary>
        /// Preenche o byte final com bits 1 e descarrega para o stream subjacente.
        /// Conforme ISO/IEC 10918-1 §B.1.1.5, o final do fluxo de entropia é preenchido com 1s.
        /// </summary>
        public void Flush()
        {
            if (_bitCount > 0)
            {
                int padBits = 8 - _bitCount;
                WriteBits((1 << padBits) - 1, padBits);
            }
            _stream.Flush();
        }
    }
}
