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

        public static string ValidateSafeRelativePath(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
            {
                throw new CorruptDataException("Nome de entrada ZIP inválido: nome vazio ou nulo.");
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

            // Divide em segmentos para verificar travessia (..)
            string[] segments = normalized.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i].Trim();
                if (seg == "..")
                {
                    throw new PathSecurityException(string.Format(
                        "Tentativa de travessia de diretório detectada (Zip Slip) na entrada: '{0}'.", entryName));
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
