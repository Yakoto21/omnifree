using System;
using System.Collections.Generic;

namespace Lodino.Core.Formats.Data
{
    public class CsvDocument
    {
        public List<string> Headers { get; private set; }
        public List<List<string>> Rows { get; private set; }

        public CsvDocument()
        {
            Headers = new List<string>();
            Rows = new List<List<string>>();
        }

        public CsvDocument(IEnumerable<string> headers)
        {
            Headers = new List<string>(headers);
            Rows = new List<List<string>>();
        }

        public void AddRow(IEnumerable<string> row)
        {
            Rows.Add(new List<string>(row));
        }

        public int ColumnCount
        {
            get { return Headers.Count; }
        }

        public int RowCount
        {
            get { return Rows.Count; }
        }
    }
}
