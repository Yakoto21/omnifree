using System;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Gerenciamento de tabelas de Huffman JPEG (ITU-T T.81 / ISO/IEC 10918-1 Anexo C e Anexo F).
    /// Fornece as 4 tabelas padrão do Anexo K (Luminância DC/AC e Crominância DC/AC),
    /// decodificação rápida bit a bit (procedimento DECODE do Anexo F) e mapeamento direto para codificação.
    /// </summary>
    public class JpegHuffmanTable
    {
        #region Tabelas Padrão Anexo K

        // Tabela K.3: Luminância DC (Bits: 16 contagens, Valores: 12 símbolos)
        public static readonly byte[] StandardLuminanceDcBits = new byte[16]
        {
            0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0
        };
        public static readonly byte[] StandardLuminanceDcValues = new byte[12]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
        };

        // Tabela K.4: Crominância DC (Bits: 16 contagens, Valores: 12 símbolos)
        public static readonly byte[] StandardChrominanceDcBits = new byte[16]
        {
            0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0
        };
        public static readonly byte[] StandardChrominanceDcValues = new byte[12]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
        };

        // Tabela K.5: Luminância AC (Bits: 16 contagens, Valores: 162 símbolos)
        public static readonly byte[] StandardLuminanceAcBits = new byte[16]
        {
            0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 125
        };
        public static readonly byte[] StandardLuminanceAcValues = new byte[162]
        {
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
            0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
            0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
            0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
            0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
            0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
            0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
            0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
            0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
            0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
            0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
            0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
            0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
            0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
            0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA
        };

        // Tabela K.6: Crominância AC (Bits: 16 contagens, Valores: 162 símbolos)
        public static readonly byte[] StandardChrominanceAcBits = new byte[16]
        {
            0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 119
        };
        public static readonly byte[] StandardChrominanceAcValues = new byte[162]
        {
            0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
            0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
            0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
            0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
            0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
            0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
            0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
            0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
            0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
            0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
            0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
            0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
            0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
            0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
            0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
            0xF9, 0xFA
        };

        #endregion

        private readonly byte[] _bits = new byte[16];
        private readonly byte[] _values;

        // Estruturas de decodificação ITU-T T.81 Anexo F (índices 1..16)
        private readonly int[] _mincode = new int[17];
        private readonly int[] _maxcode = new int[17];
        private readonly int[] _valptr = new int[17];

        // Mapeamento direto de codificação (índice: símbolo 0..255)
        private readonly int[] _codes = new int[256];
        private readonly int[] _codeLengths = new int[256];

        public byte[] Bits { get { return _bits; } }
        public byte[] Values { get { return _values; } }
        public int[] Codes { get { return _codes; } }
        public int[] CodeLengths { get { return _codeLengths; } }

        public JpegHuffmanTable(byte[] bits16, byte[] values)
        {
            if (bits16 == null || bits16.Length != 16)
            {
                throw new ArgumentException("O array de contagens 'bits' deve ter exatamente 16 bytes.");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            Array.Copy(bits16, _bits, 16);
            _values = new byte[values.Length];
            Array.Copy(values, _values, values.Length);

            GenerateTables();
        }

        private void GenerateTables()
        {
            // Inicializa tabelas
            for (int i = 0; i < 256; i++)
            {
                _codeLengths[i] = 0;
            }

            int huffCode = 0;
            int vIndex = 0;

            for (int len = 1; len <= 16; len++)
            {
                int count = _bits[len - 1];
                if (count == 0)
                {
                    _maxcode[len] = -1;
                }
                else
                {
                    _valptr[len] = vIndex;
                    _mincode[len] = huffCode;

                    for (int c = 0; c < count; c++)
                    {
                        if (vIndex >= _values.Length)
                        {
                            throw new CorruptDataException("Tabela de Huffman declara mais códigos do que os símbolos fornecidos.");
                        }

                        byte sym = _values[vIndex];
                        _codes[sym] = huffCode + c;
                        _codeLengths[sym] = len;
                        vIndex++;
                    }

                    huffCode += count - 1;
                    _maxcode[len] = huffCode;
                    huffCode++;
                }

                huffCode <<= 1;
            }
        }

        /// <summary>
        /// Decodifica um símbolo utilizando o algoritmo canônico do Anexo F (Figura F.16 DECODE).
        /// </summary>
        public int Decode(JpegBitReader reader)
        {
            int code = 0;
            for (int len = 1; len <= 16; len++)
            {
                int bit = reader.ReadBit();
                if (bit < 0)
                {
                    throw new CorruptDataException("Fim prematuro do fluxo durante a decodificação de Huffman.");
                }

                code = (code << 1) | bit;

                if (code <= _maxcode[len])
                {
                    int index = _valptr[len] + (code - _mincode[len]);
                    if (index < 0 || index >= _values.Length)
                    {
                        throw new CorruptDataException("Índice de símbolo de Huffman calculado fora do intervalo válido.");
                    }
                    return _values[index];
                }
            }

            throw new CorruptDataException("Código de Huffman inválido ou não pertencente à tabela atual.");
        }
    }
}
