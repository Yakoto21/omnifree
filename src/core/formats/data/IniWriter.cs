using System;
using System.IO;
using System.Text;

namespace Lodino.Core.Formats.Data
{
    public class IniWriter
    {
        public string NewLine { get; set; }

        public IniWriter(string newLine = "\r\n")
        {
            NewLine = newLine;
        }

        public string WriteToString(IniDocument doc)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Write(doc, writer);
            }
            return sb.ToString();
        }

        public void Write(IniDocument doc, TextWriter writer)
        {
            if (doc == null) return;

            // Global section first (unnamed)
            if (doc.Sections.ContainsKey(IniDocument.GlobalSectionName))
            {
                var globalSec = doc.Sections[IniDocument.GlobalSectionName];
                foreach (var kvp in globalSec)
                {
                    writer.Write(string.Format("{0}={1}{2}", kvp.Key, kvp.Value, NewLine));
                }
                if (globalSec.Count > 0 && doc.Sections.Count > 1)
                {
                    writer.Write(NewLine);
                }
            }

            // Named sections
            bool first = true;
            foreach (var secPair in doc.Sections)
            {
                if (secPair.Key == IniDocument.GlobalSectionName) continue;

                if (!first) writer.Write(NewLine);
                first = false;

                writer.Write(string.Format("[{0}]{1}", secPair.Key, NewLine));
                foreach (var prop in secPair.Value)
                {
                    writer.Write(string.Format("{0}={1}{2}", prop.Key, prop.Value, NewLine));
                }
            }
        }
    }
}
