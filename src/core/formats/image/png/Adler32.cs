using System;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Implementação original do algoritmo de checksum Adler-32 (RFC 1950).
    /// Utilizado pela camada de encapsulamento ZLIB para verificação de integridade.
    /// </summary>
    public static class Adler32
    {
        private const uint ModAdler = 65521;
        private const int MaxAccumulatedBytes = 5552;

        public static uint Calculate(byte[] buffer, int offset, int count)
        {
            return Update(1u, buffer, offset, count);
        }

        public static uint Update(uint currentAdler, byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("Intervalo de buffer inválido.");
            }

            uint s1 = currentAdler & 0xFFFF;
            uint s2 = (currentAdler >> 16) & 0xFFFF;

            int remaining = count;
            int currentOffset = offset;

            while (remaining > 0)
            {
                int chunk = remaining > MaxAccumulatedBytes ? MaxAccumulatedBytes : remaining;
                remaining -= chunk;

                for (int i = 0; i < chunk; i++)
                {
                    s1 += buffer[currentOffset++];
                    s2 += s1;
                }

                s1 %= ModAdler;
                s2 %= ModAdler;
            }

            return (s2 << 16) | s1;
        }
    }
}
