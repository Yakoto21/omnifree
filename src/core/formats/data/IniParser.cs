using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Data
{
    public class IniParser
    {
        public IniDocument Parse(string text)
        {
            if (text == null) text = string.Empty;
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal)) text = text.Substring(1);

            using (var reader = new StringReader(text))
            {
                return Parse(reader);
            }
        }

        public IniDocument Parse(TextReader reader)
        {
            var doc = new IniDocument();
            string currentSection = IniDocument.GlobalSectionName;

            string line;
            int lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                string trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith(";") || trimmed.StartsWith("#")) continue;

                // Section header: [SectionName]
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    if (doc.Sections.Count > SecurityLimits.MaxIniSections)
                    {
                        throw new ResourceLimitException(string.Format(
                            "O arquivo INI excede o limite máximo de {0:N0} seções.", SecurityLimits.MaxIniSections));
                    }
                    doc.GetSection(currentSection, true);
                    continue;
                }

                // Key = Value
                int delimiterIndex = trimmed.IndexOf('=');
                if (delimiterIndex < 0)
                {
                    delimiterIndex = trimmed.IndexOf(':');
                }

                if (delimiterIndex > 0)
                {
                    string key = trimmed.Substring(0, delimiterIndex).Trim();
                    string value = trimmed.Substring(delimiterIndex + 1).Trim();

                    // Strip inline comment if not quoted
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    else
                    {
                        int commentIndex = value.IndexOf(" ;", StringComparison.Ordinal);
                        if (commentIndex < 0) commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
                        if (commentIndex >= 0)
                        {
                            value = value.Substring(0, commentIndex).Trim();
                        }
                    }

                    doc.Set(currentSection, key, value);
                }
            }

            return doc;
        }
    }
}
