using System;
using Lodino.Core.Checksums;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Implementação de compatibilidade do CRC-32 para PNG, delegando para o utilitário compartilhado Lodino.Core.IO.Crc32.
    /// </summary>
    public static class PngCrc32
    {
        public static uint Calculate(byte[] buffer, int offset, int count)
        {
            return Crc32.Calculate(buffer, offset, count);
        }

        public static uint Calculate(string fourCc, byte[] data, int dataOffset, int dataCount)
        {
            uint crc = Crc32.InitialCrc;
            byte[] tagBytes = System.Text.Encoding.ASCII.GetBytes(fourCc);
            crc = Crc32.Update(crc, tagBytes, 0, tagBytes.Length);
            if (data != null && dataCount > 0)
            {
                crc = Crc32.Update(crc, data, dataOffset, dataCount);
            }
            return Crc32.Finalize(crc);
        }

        public static uint Update(uint currentCrc, byte[] buffer, int offset, int count)
        {
            return Crc32.Update(currentCrc, buffer, offset, count);
        }
    }
}
