using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lodino.Core.Formats.Data
{
    public class CsvWriter
    {
        public char Delimiter { get; set; }
        public string NewLine { get; set; }

        public CsvWriter(char delimiter = ',', string newLine = "\r\n")
        {
            Delimiter = delimiter;
            NewLine = newLine;
        }

        public string EscapeValue(string value)
        {
            if (value == null) return string.Empty;

            bool mustQuote = value.IndexOf(Delimiter) >= 0 ||
                             value.IndexOf('"') >= 0 ||
                             value.IndexOf('\r') >= 0 ||
                             value.IndexOf('\n') >= 0;

            if (mustQuote)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        public string WriteToString(CsvDocument doc)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Write(doc, writer);
            }
            return sb.ToString();
        }

        public void Write(CsvDocument doc, TextWriter writer)
        {
            if (doc == null) return;

            // Write headers
            if (doc.Headers.Count > 0)
            {
                for (int i = 0; i < doc.Headers.Count; i++)
                {
                    if (i > 0) writer.Write(Delimiter);
                    writer.Write(EscapeValue(doc.Headers[i]));
                }
                writer.Write(NewLine);
            }

            // Write rows
            for (int r = 0; r < doc.Rows.Count; r++)
            {
                var row = doc.Rows[r];
                for (int c = 0; c < doc.Headers.Count; c++)
                {
                    if (c > 0) writer.Write(Delimiter);
                    string val = c < row.Count ? row[c] : string.Empty;
                    writer.Write(EscapeValue(val));
                }
                writer.Write(NewLine);
            }
        }
    }
}
