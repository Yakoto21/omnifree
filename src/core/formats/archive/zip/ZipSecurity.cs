using System;
using System.IO;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Utilitários de segurança para validação de caminhos, prevenção de Zip Slip e proteção contra DoS.
    /// </summary>
    public static class ZipSecurity
    {
        private static readonly char[] InvalidPathChars = new[] { '<', '>', '"', '|', '?', '*' };

        private static readonly string[] ReservedDeviceNames = new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string ValidateSafeRelativePath(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
            {
                throw new CorruptDataException("Nome de entrada ZIP inválido: nome vazio ou nulo.");
            }

            // Rejeita caminhos com prefixos de dispositivo Win32 (\\.\ ou \\?\) ou UNC
            if (entryName.StartsWith("\\\\") || entryName.StartsWith("//"))
            {
                throw new PathSecurityException(string.Format(
                    "Nome de entrada ZIP inválido: caminho UNC ou de dispositivo proibido em '{0}'.", entryName));
            }

            // Normaliza separadores de diretório
            string normalized = entryName.Replace('\\', '/');

            // Rejeita caminhos com caracteres ilegais ou fluxos NTFS alternativos (:)
            if (normalized.IndexOf(':') >= 0)
            {
                throw new PathSecurityException(string.Format(
                    "Nome de entrada ZIP inválido: caractere ':' proibido detectado (possível Zip Slip ou NTFS stream) em '{0}'.", entryName));
            }

            for (int i = 0; i < InvalidPathChars.Length; i++)
            {
                if (normalized.IndexOf(InvalidPathChars[i]) >= 0)
                {
                    throw new CorruptDataException(string.Format(
                        "Nome de entrada ZIP inválido: caractere proibido '{0}' detectado em '{1}'.", InvalidPathChars[i], entryName));
                }
            }

            // Remove barras iniciais (evita caminhos absolutos /etc/passwd)
            while (normalized.StartsWith("/"))
            {
                normalized = normalized.Substring(1);
            }

            if (string.IsNullOrEmpty(normalized))
            {
                throw new CorruptDataException("Nome de entrada ZIP inválido: refere-se à raiz do sistema de arquivos.");
            }

            // Divide em segmentos para verificar travessia (..), nomes reservados e sufixos perigosos
            string[] segments = normalized.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];
                if (string.IsNullOrEmpty(seg)) continue;

                if (seg.Trim() == "..")
                {
                    throw new PathSecurityException(string.Format(
                        "Tentativa de travessia de diretório detectada (Zip Slip) na entrada: '{0}'.", entryName));
                }

                // Rejeita segmentos terminando em ponto ou espaço (Win32 remove silenciosamente causando colisão/evasão)
                if (seg.EndsWith(".") || seg.EndsWith(" "))
                {
                    throw new PathSecurityException(string.Format(
                        "Nome de entrada ZIP inválido: segmento '{0}' termina com espaço ou ponto na entrada '{1}'.",
                        seg, entryName));
                }

                // Rejeita nomes de dispositivos reservados do Windows (ex: CON, PRN, AUX, NUL, COM1..9, LPT1..9, e CON.txt)
                string baseName = seg;
                int dotIndex = seg.IndexOf('.');
                if (dotIndex >= 0)
                {
                    baseName = seg.Substring(0, dotIndex);
                }
                for (int r = 0; r < ReservedDeviceNames.Length; r++)
                {
                    if (string.Equals(baseName, ReservedDeviceNames[r], StringComparison.OrdinalIgnoreCase))
                    {
                        throw new PathSecurityException(string.Format(
                            "Nome de dispositivo reservado do Windows '{0}' detectado no segmento '{1}' da entrada '{2}'.",
                            ReservedDeviceNames[r], seg, entryName));
                    }
                }
            }

            return normalized;
        }

        public static string GetSafeExtractionPath(string destinationDirectory, string entryName)
        {
            if (string.IsNullOrEmpty(destinationDirectory))
            {
                throw new ArgumentNullException("destinationDirectory");
            }

            string safeRelative = ValidateSafeRelativePath(entryName);

            string destFull = Path.GetFullPath(destinationDirectory);
            string destWithSlash = destFull.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? destFull
                : destFull + Path.DirectorySeparatorChar;

            string targetRelativeOnDisk = safeRelative.Replace('/', Path.DirectorySeparatorChar);
            string combined = Path.Combine(destFull, targetRelativeOnDisk);
            string targetFull = Path.GetFullPath(combined);

            if (!targetFull.StartsWith(destWithSlash, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(targetFull, destFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new PathSecurityException(string.Format(
                    "Tentativa de travessia de diretório detectada (Zip Slip): a entrada '{0}' escapa do diretório de destino '{1}'.",
                    entryName, destFull));
            }

            return targetFull;
        }
    }
}
