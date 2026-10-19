using System;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Implementação original de CRC-32 (ISO 3309 / ITU-T V.42 / PNG standard).
    /// Polinômio reverso: 0xEDB88320.
    /// </summary>
    public static class PngCrc32
    {
        private static readonly uint[] CrcTable;

        static PngCrc32()
        {
            CrcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) != 0)
                    {
                        c = 0xEDB88320u ^ (c >> 1);
                    }
                    else
                    {
                        c = c >> 1;
                    }
                }
                CrcTable[i] = c;
            }
        }

        public static uint Calculate(byte[] buffer, int offset, int count)
        {
            return Update(0xFFFFFFFFu, buffer, offset, count) ^ 0xFFFFFFFFu;
        }

        public static uint Calculate(string fourCc, byte[] data, int dataOffset, int dataCount)
        {
            uint crc = 0xFFFFFFFFu;
            byte[] tagBytes = System.Text.Encoding.ASCII.GetBytes(fourCc);
            crc = Update(crc, tagBytes, 0, tagBytes.Length);
            if (data != null && dataCount > 0)
            {
                crc = Update(crc, data, dataOffset, dataCount);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        public static uint Update(uint currentCrc, byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("Intervalo de buffer inválido.");
            }

            uint c = currentCrc;
            for (int i = 0; i < count; i++)
            {
                byte index = (byte)((c ^ buffer[offset + i]) & 0xFF);
                c = CrcTable[index] ^ (c >> 8);
            }
            return c;
        }
    }
}
