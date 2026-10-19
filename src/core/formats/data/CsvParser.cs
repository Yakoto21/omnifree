using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Data
{
    public class CsvParser
    {
        private enum State
        {
            StartField,
            UnquotedField,
            QuotedField,
            QuoteInQuotedField
        }

        public char Delimiter { get; set; }
        public bool HasHeader { get; set; }
        public bool StrictRfc4180 { get; set; }

        public CsvParser(char delimiter = ',', bool hasHeader = true, bool strictRfc4180 = false)
        {
            Delimiter = delimiter;
            HasHeader = hasHeader;
            StrictRfc4180 = strictRfc4180;
        }

        public CsvDocument Parse(string text)
        {
            if (text == null) text = string.Empty;
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal)) text = text.Substring(1);

            using (var reader = new StringReader(text))
            {
                return Parse(reader);
            }
        }

        public CsvDocument Parse(TextReader reader)
        {
            var rawRows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            var state = State.StartField;

            int rawChar;
            while ((rawChar = reader.Read()) != -1)
            {
                char ch = (char)rawChar;

                switch (state)
                {
                    case State.StartField:
                        if (ch == '"')
                        {
                            state = State.QuotedField;
                        }
                        else if (ch == Delimiter)
                        {
                            currentRow.Add(string.Empty);
                        }
                        else if (ch == '\r' || ch == '\n')
                        {
                            if (ch == '\r' && reader.Peek() == '\n')
                            {
                                reader.Read();
                            }
                            if (currentRow.Count > 0 || currentField.Length > 0)
                            {
                                currentRow.Add(string.Empty);
                                rawRows.Add(currentRow);
                                currentRow = new List<string>();
                            }
                        }
                        else
                        {
                            currentField.Append(ch);
                            state = State.UnquotedField;
                        }
                        break;

                    case State.UnquotedField:
                        if (ch == Delimiter)
                        {
                            currentRow.Add(currentField.ToString());
                            currentField.Length = 0;
                            state = State.StartField;
                        }
                        else if (ch == '\r' || ch == '\n')
                        {
                            if (ch == '\r' && reader.Peek() == '\n')
                            {
                                reader.Read();
                            }
                            currentRow.Add(currentField.ToString());
                            currentField.Length = 0;
                            rawRows.Add(currentRow);
                            currentRow = new List<string>();
                            state = State.StartField;
                        }
                        else
                        {
                            currentField.Append(ch);
                        }
                        break;

                    case State.QuotedField:
                        if (ch == '"')
                        {
                            state = State.QuoteInQuotedField;
                        }
                        else
                        {
                            currentField.Append(ch);
                        }
                        break;

                    case State.QuoteInQuotedField:
                        if (ch == '"')
                        {
                            // Escaped quote: "" -> "
                            currentField.Append('"');
                            state = State.QuotedField;
                        }
                        else if (ch == Delimiter)
                        {
                            currentRow.Add(currentField.ToString());
                            currentField.Length = 0;
                            state = State.StartField;
                        }
                        else if (ch == '\r' || ch == '\n')
                        {
                            if (ch == '\r' && reader.Peek() == '\n')
                            {
                                reader.Read();
                            }
                            currentRow.Add(currentField.ToString());
                            currentField.Length = 0;
                            rawRows.Add(currentRow);
                            currentRow = new List<string>();
                            state = State.StartField;
                        }
                        else
                        {
                            if (StrictRfc4180)
                            {
                                throw new CorruptDataException(string.Format("Caractere inesperado '{0}' após aspas de fechamento em modo estrito RFC 4180.", ch));
                            }
                            // Tolerant behavior: treat unexpected char after quote as unquoted trailing char
                            currentField.Append(ch);
                            state = State.UnquotedField;
                        }
                        break;
                }

                if (rawRows.Count > SecurityLimits.MaxCsvRows)
                {
                    throw new ResourceLimitException(string.Format(
                        "O arquivo CSV excede o limite máximo de {0:N0} linhas.", SecurityLimits.MaxCsvRows));
                }
            }

            // Flush final pending field/row
            if (state == State.QuotedField)
            {
                if (StrictRfc4180)
                {
                    throw new CorruptDataException("Aspas não fechadas no final do arquivo CSV em modo estrito RFC 4180.");
                }
                // Incomplete quoted field at EOF in permissive mode
                currentRow.Add(currentField.ToString());
                rawRows.Add(currentRow);
            }
            else if (state == State.QuoteInQuotedField || state == State.UnquotedField || currentField.Length > 0 || (currentRow.Count > 0 && state == State.StartField))
            {
                currentRow.Add(currentField.ToString());
                rawRows.Add(currentRow);
            }

            // Build CsvDocument
            if (rawRows.Count == 0)
            {
                return new CsvDocument();
            }

            CsvDocument doc;
            int startIndex = 0;

            if (HasHeader && rawRows.Count > 0)
            {
                doc = new CsvDocument(rawRows[0]);
                startIndex = 1;
            }
            else
            {
                int maxCols = 0;
                for (int i = 0; i < rawRows.Count; i++)
                {
                    if (rawRows[i].Count > maxCols) maxCols = rawRows[i].Count;
                }
                var defaultHeaders = new List<string>();
                for (int c = 1; c <= maxCols; c++)
                {
                    defaultHeaders.Add(string.Format("Column{0}", c));
                }
                doc = new CsvDocument(defaultHeaders);
            }

            for (int r = startIndex; r < rawRows.Count; r++)
            {
                var row = rawRows[r];
                if (StrictRfc4180 && row.Count != doc.Headers.Count)
                {
                    throw new CorruptDataException(string.Format(
                        "Linha {0} contém {1} colunas, divergindo do cabeçalho de {2} colunas em modo estrito RFC 4180.",
                        r + 1, row.Count, doc.Headers.Count));
                }
                // Expand headers if any row has more columns than headers
                while (doc.Headers.Count < row.Count)
                {
                    doc.Headers.Add(string.Format("ExtraColumn_{0}", doc.Headers.Count + 1));
                }
                // Normalize column count to header count if inconsistent
                while (row.Count < doc.Headers.Count)
                {
                    row.Add(string.Empty);
                }
                doc.AddRow(row);
            }

            return doc;
        }
    }
}
