using System;
using System.IO;

namespace Lodino.Core.IO
{
    /// <summary>
    /// Fachada de compatibilidade para CRC-32 (IEEE 802.3 / ISO 3309).
    /// Redireciona chamadas para o módulo neutro Lodino.Core.Checksums.Crc32.
    /// </summary>
    public static class Crc32
    {
        public static uint InitialCrc
        {
            get { return Lodino.Core.Checksums.Crc32.InitialCrc; }
        }

        public static uint Calculate(byte[] buffer)
        {
            return Lodino.Core.Checksums.Crc32.Calculate(buffer);
        }

        public static uint Calculate(byte[] buffer, int offset, int count)
        {
            return Lodino.Core.Checksums.Crc32.Calculate(buffer, offset, count);
        }

        public static uint Calculate(Stream stream)
        {
            return Lodino.Core.Checksums.Crc32.Calculate(stream);
        }

        public static uint Update(uint currentCrc, byte[] buffer, int offset, int count)
        {
            return Lodino.Core.Checksums.Crc32.Update(currentCrc, buffer, offset, count);
        }

        public static uint Update(uint currentCrc, byte b)
        {
            return Lodino.Core.Checksums.Crc32.Update(currentCrc, b);
        }

        public static uint Finalize(uint currentCrc)
        {
            return Lodino.Core.Checksums.Crc32.Finalize(currentCrc);
        }
    }
}
