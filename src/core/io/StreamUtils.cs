using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.IO
{
    /// <summary>
    /// Utilitários de leitura e navegação em streams para o núcleo do Lodino.
    /// Provê leitura exata resiliente a streams fracionados e avanço de stream sem alocações excessivas.
    /// </summary>
    public static class StreamUtils
    {
        /// <summary>
        /// Lê exatamente 'count' bytes do stream para o buffer especificado em 'offset'.
        /// Garante leitura contínua através de laço até preencher a quantidade solicitada ou encontrar EOF.
        /// Retorna a quantidade total de bytes efetivamente lidos.
        /// </summary>
        public static int ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException();
            }

            int totalRead = 0;
            while (totalRead < count)
            {
                int n = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (n <= 0)
                {
                    break;
                }
                totalRead += n;
            }

            return totalRead;
        }

        /// <summary>
        /// Lê exatamente 'count' bytes do stream ou lança CorruptDataException caso o stream termine antes do esperado.
        /// </summary>
        public static void ReadExactOrThrow(Stream stream, byte[] buffer, int offset, int count, string truncatedErrorMessage)
        {
            int read = ReadExact(stream, buffer, offset, count);
            if (read < count)
            {
                throw new CorruptDataException(
                    truncatedErrorMessage ?? string.Format("Fim prematuro do fluxo: {0} de {1} bytes lidos.", read, count));
            }
        }

        /// <summary>
        /// Avança o stream em 'bytesToSkip' bytes sem alocar arrays de tamanho arbitrário.
        /// Em streams seekable, utiliza Seek. Em streams não-seekable, utiliza um buffer estático pequeno (4 KB).
        /// </summary>
        public static void SkipBytes(Stream stream, long bytesToSkip, long maxAllowedBytes = SecurityLimits.DefaultMaxImageFileSize)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (bytesToSkip < 0 || bytesToSkip > maxAllowedBytes)
            {
                throw new ResourceLimitException(string.Format(
                    "Deslocamento solicitado ({0:N0} bytes) é inválido ou excede o limite de segurança ({1:N0} bytes).",
                    bytesToSkip, maxAllowedBytes));
            }

            if (bytesToSkip == 0) return;

            if (stream.CanSeek)
            {
                stream.Seek(bytesToSkip, SeekOrigin.Current);
                return;
            }

            byte[] smallBuffer = new byte[4096];
            long remaining = bytesToSkip;
            while (remaining > 0)
            {
                int toRead = remaining > smallBuffer.Length ? smallBuffer.Length : (int)remaining;
                int n = stream.Read(smallBuffer, 0, toRead);
                if (n <= 0)
                {
                    throw new CorruptDataException("Fim prematuro do fluxo ao avançar dados.");
                }
                remaining -= n;
            }
        }
    }
}
