using System;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Gerenciamento de tabelas de quantização JPEG (ITU-T T.81 / ISO/IEC 10918-1 Anexo K).
    /// Suporta ordenação zigzag, desquantização, quantização e escalonamento por fator de qualidade (1..100).
    /// </summary>
    public class JpegQuantizationTable
    {
        /// <summary>
        /// Mapeamento da ordem zigzag (0..63) para a ordem natural (row * 8 + col).
        /// </summary>
        public static readonly int[] ZigzagToNatural = new int[64]
        {
             0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };

        /// <summary>
        /// Mapeamento da ordem natural (row * 8 + col) para a ordem zigzag (0..63).
        /// </summary>
        public static readonly int[] NaturalToZigzag = new int[64];

        /// <summary>
        /// Tabela padrão de luminância de 8-bit (ITU-T T.81 Tabela K.1) em ordem natural.
        /// </summary>
        public static readonly int[] StandardLuminance = new int[64]
        {
            16,  11,  10,  16,  24,  40,  51,  61,
            12,  12,  14,  19,  26,  58,  60,  55,
            14,  13,  16,  24,  40,  57,  69,  56,
            14,  17,  22,  29,  51,  87,  80,  62,
            18,  22,  37,  56,  68, 109, 103,  77,
            24,  35,  55,  64,  81, 104, 113,  92,
            49,  64,  78,  87, 103, 121, 120, 101,
            72,  92,  95,  98, 112, 100, 103,  99
        };

        /// <summary>
        /// Tabela padrão de crominância de 8-bit (ITU-T T.81 Tabela K.2) em ordem natural.
        /// </summary>
        public static readonly int[] StandardChrominance = new int[64]
        {
            17,  18,  24,  47,  99,  99,  99,  99,
            18,  21,  26,  66,  99,  99,  99,  99,
            24,  26,  56,  99,  99,  99,  99,  99,
            47,  66,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99
        };

        static JpegQuantizationTable()
        {
            for (int i = 0; i < 64; i++)
            {
                NaturalToZigzag[ZigzagToNatural[i]] = i;
            }
        }

        private readonly int[] _elements = new int[64]; // Sempre armazenado em ordem natural (row-major)

        public int[] Elements { get { return _elements; } }

        public JpegQuantizationTable()
        {
        }

        public JpegQuantizationTable(int[] elementsNatural)
        {
            if (elementsNatural == null || elementsNatural.Length != 64)
            {
                throw new ArgumentException("A tabela de quantização deve conter exatamente 64 elementos.");
            }
            Array.Copy(elementsNatural, _elements, 64);
        }

        public static JpegQuantizationTable FromZigzag(byte[] zigzagBytes)
        {
            if (zigzagBytes == null || zigzagBytes.Length < 64)
            {
                throw new ArgumentException("Buffer de quantização zigzag insuficiente.");
            }
            var table = new JpegQuantizationTable();
            for (int z = 0; z < 64; z++)
            {
                int naturalIndex = ZigzagToNatural[z];
                int val = zigzagBytes[z];
                if (val < 1) val = 1; // Evita divisão por zero
                table._elements[naturalIndex] = val;
            }
            return table;
        }

        public byte[] ToZigzagBytes()
        {
            byte[] result = new byte[64];
            for (int z = 0; z < 64; z++)
            {
                int naturalIndex = ZigzagToNatural[z];
                int val = _elements[naturalIndex];
                if (val < 1) val = 1;
                if (val > 255) val = 255;
                result[z] = (byte)val;
            }
            return result;
        }

        /// <summary>
        /// Escala uma tabela base por um fator de qualidade JPEG (1 a 100), seguindo a fórmula padrão IJG.
        /// </summary>
        public static JpegQuantizationTable Scale(int[] baseTableNatural, int quality)
        {
            if (quality < 1) quality = 1;
            if (quality > 100) quality = 100;

            int scale = (quality < 50) ? (5000 / quality) : (200 - quality * 2);

            var scaled = new JpegQuantizationTable();
            for (int i = 0; i < 64; i++)
            {
                int val = (baseTableNatural[i] * scale + 50) / 100;
                if (val < 1) val = 1;
                if (val > 255) val = 255;
                scaled._elements[i] = val;
            }
            return scaled;
        }
    }
}
