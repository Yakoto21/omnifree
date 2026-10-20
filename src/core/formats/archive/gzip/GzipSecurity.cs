using System;
using System.Globalization;
using System.Text;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Fornece utilitários de segurança e mitigação adversarial para o formato GZIP.
    /// Inclui sanitização de metadados externos para exibição segura em consoles e logs (CWE-150).
    /// </summary>
    public static class GzipSecurity
    {
        /// <summary>
        /// Sanitiza uma string de metadados para exibição segura em consoles e logs.
        /// Preserva caracteres imprimíveis ASCII e Unicode legítimo (acentos, CJK, etc.).
        /// Neutraliza códigos de controle C0/C1, sequências ANSI, DEL e caracteres de controle Bidi (Trojan Source).
        /// </summary>
        public static string SanitizeForConsole(string input, int maxLength = 4096)
        {
            if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

            var sb = new StringBuilder(input.Length + 16);
            int count = 0;

            for (int i = 0; i < input.Length; i++)
            {
                if (count >= maxLength)
                {
                    sb.Append("... [TRUNCATED]");
                    break;
                }

                char c = input[i];

                // 1. Caracteres ASCII imprimíveis (0x20..0x7E exceto DEL e controles)
                if (c >= 0x20 && c <= 0x7E)
                {
                    sb.Append(c);
                    count++;
                    continue;
                }

                // 2. Escapes C0 específicos comuns
                if (c == '\r')
                {
                    sb.Append("\\r");
                    count += 2;
                }
                else if (c == '\n')
                {
                    sb.Append("\\n");
                    count += 2;
                }
                else if (c == '\t')
                {
                    sb.Append("\\t");
                    count += 2;
                }
                else if (c == '\0')
                {
                    sb.Append("\\0");
                    count += 2;
                }
                else if (c == '\a') // 0x07 Bell
                {
                    sb.Append("\\a");
                    count += 2;
                }
                else if (c == '\b') // 0x08 Backspace
                {
                    sb.Append("\\b");
                    count += 2;
                }
                else if (c == '\v') // 0x0B Vertical Tab
                {
                    sb.Append("\\v");
                    count += 2;
                }
                else if (c == '\f') // 0x0C Form Feed
                {
                    sb.Append("\\f");
                    count += 2;
                }
                else if (c == '\x1B') // 0x1B Escape (ANSI ESC)
                {
                    sb.Append("\\x1B");
                    count += 4;
                }
                else if (c == 0x7F) // DEL
                {
                    sb.Append("\\x7F");
                    count += 4;
                }
                // 3. Outros caracteres C0 (< 0x20)
                else if (c < 0x20)
                {
                    sb.Append(string.Format("\\x{0:X2}", (int)c));
                    count += 4;
                }
                // 4. Controles C1 (0x80..0x9F) — ex: 0x9B é CSI em 8-bit ANSI
                else if (c >= 0x80 && c <= 0x9F)
                {
                    sb.Append(string.Format("\\u{0:X4}", (int)c));
                    count += 6;
                }
                // 5. Controles de formato bidirecional (Trojan Source: RLO, LRO, etc.)
                else if (IsBidiControl(c))
                {
                    sb.Append(string.Format("\\u{0:X4}", (int)c));
                    count += 6;
                }
                // 6. Pares substitutos Unicode (ex: Emojis U+10000..U+10FFFF)
                else if (char.IsSurrogatePair(input, i))
                {
                    sb.Append(c);
                    sb.Append(input[i + 1]);
                    i++; // Avança o low surrogate
                    count += 2;
                }
                // 7. Caracteres Unicode genéricos
                else
                {
                    var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (cat == UnicodeCategory.Control || cat == UnicodeCategory.Format ||
                        cat == UnicodeCategory.OtherNotAssigned)
                    {
                        sb.Append(string.Format("\\u{0:X4}", (int)c));
                        count += 6;
                    }
                    else
                    {
                        // Unicode imprimível legítimo (Acentuados, CJK, Grego, Cirílico, etc.)
                        sb.Append(c);
                        count++;
                    }
                }
            }

            return sb.ToString();
        }

        private static bool IsBidiControl(char c)
        {
            return c == '\u202A' || // Left-to-Right Embedding (LRE)
                   c == '\u202B' || // Right-to-Left Embedding (RLE)
                   c == '\u202C' || // Pop Directional Formatting (PDF)
                   c == '\u202D' || // Left-to-Right Override (LRO)
                   c == '\u202E' || // Right-to-Left Override (RLO)
                   c == '\u2066' || // Left-to-Right Isolate (LRI)
                   c == '\u2067' || // Right-to-Left Isolate (RLI)
                   c == '\u2068' || // First Strong Isolate (FSI)
                   c == '\u2069' || // Pop Directional Isolate (PDI)
                   c == '\u200E' || // Left-to-Right Mark (LRM)
                   c == '\u200F' || // Right-to-Left Mark (RLM)
                   c == '\u061C';   // Arabic Letter Mark (ALM)
        }
    }
}
