using System;
using System.IO;
using System.Text.RegularExpressions;
using Lodino.Core.Errors;

namespace Lodino.Core.IO
{
    public static class SafeFileWriter
    {
        private static readonly Regex InvalidCharsRegex = new Regex(@"[<>:""/\\|?*\x00-\x1F]", RegexOptions.Compiled);

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Lodino_Output";
            string cleaned = InvalidCharsRegex.Replace(name, "").Trim();
            return string.IsNullOrEmpty(cleaned) ? "Lodino_Output" : cleaned;
        }

        public static string GetUniqueOutputPath(string directory, string baseName, string extension)
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string safeExt = (extension ?? "").TrimStart('.');
            string stem = SanitizeFileName(Path.GetFileNameWithoutExtension(baseName));

            for (int attempt = 0; attempt < 1000; attempt++)
            {
                string suffix = attempt == 0 ? "" : string.Format(" ({0})", attempt);
                string candidate = Path.Combine(directory, string.Format("{0}{1}.{2}", stem, suffix, safeExt));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new LodinoException("Não foi possível gerar um nome de arquivo disponível após 1.000 tentativas.");
        }
    }
}
