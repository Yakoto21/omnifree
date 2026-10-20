using System;
using System.IO;

namespace Lodino.Core.Checksums
{
    /// <summary>
    /// Implementação original de CRC-32 (IEEE 802.3 / ISO 3309 / PKWARE ZIP / PNG standard).
    /// Polinômio reverso: 0xEDB88320.
    /// Reutilizável por todos os módulos do núcleo (ZIP, PNG, etc.).
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] CrcTable;

        static Crc32()
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

        public static uint Calculate(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return Calculate(buffer, 0, buffer.Length);
        }

        public static uint Calculate(byte[] buffer, int offset, int count)
        {
            return Update(0xFFFFFFFFu, buffer, offset, count) ^ 0xFFFFFFFFu;
        }

        public static uint Calculate(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            uint crc = 0xFFFFFFFFu;
            byte[] buf = new byte[8192];
            int read;
            while ((read = stream.Read(buf, 0, buf.Length)) > 0)
            {
                crc = Update(crc, buf, 0, read);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        public static uint Update(uint currentCrc, byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("Intervalo de buffer inválido.");
            }

            unchecked
            {
                uint crc = currentCrc;
                int end = offset + count;
                for (int i = offset; i < end; i++)
                {
                    byte index = (byte)((crc ^ buffer[i]) & 0xFF);
                    crc = CrcTable[index] ^ (crc >> 8);
                }
                return crc;
            }
        }

        public static uint Update(uint currentCrc, byte b)
        {
            unchecked
            {
                byte index = (byte)((currentCrc ^ b) & 0xFF);
                return CrcTable[index] ^ (currentCrc >> 8);
            }
        }

        public static uint Finalize(uint currentCrc)
        {
            return currentCrc ^ 0xFFFFFFFFu;
        }

        public static uint InitialCrc
        {
            get { return 0xFFFFFFFFu; }
        }
    }
}
