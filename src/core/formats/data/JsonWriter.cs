using System;
using System.IO;
using System.Text;

namespace Lodino.Core.Formats.Data
{
    public class JsonWriter
    {
        public bool Indented { get; set; }
        public string IndentString { get; set; }

        public JsonWriter(bool indented = true)
        {
            Indented = indented;
            IndentString = "  ";
        }

        public string WriteToString(JsonNode node)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Write(node, writer);
            }
            return sb.ToString();
        }

        public void Write(JsonNode node, TextWriter writer)
        {
            WriteValue(node, writer, 0);
        }

        private void WriteValue(JsonNode node, TextWriter writer, int depth)
        {
            if (node == null || node.Type == JsonType.Null)
            {
                writer.Write("null");
                return;
            }

            switch (node.Type)
            {
                case JsonType.Boolean:
                    writer.Write(node.AsBoolean() ? "true" : "false");
                    break;

                case JsonType.Number:
                    writer.Write(node.AsString());
                    break;

                case JsonType.String:
                    WriteString(node.AsString(), writer);
                    break;

                case JsonType.Array:
                    WriteArray((JsonArray)node, writer, depth);
                    break;

                case JsonType.Object:
                    WriteObject((JsonObject)node, writer, depth);
                    break;
            }
        }

        private void WriteString(string value, TextWriter writer)
        {
            writer.Write('"');
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                switch (ch)
                {
                    case '"': writer.Write("\\\""); break;
                    case '\\': writer.Write("\\\\"); break;
                    case '\b': writer.Write("\\b"); break;
                    case '\f': writer.Write("\\f"); break;
                    case '\n': writer.Write("\\n"); break;
                    case '\r': writer.Write("\\r"); break;
                    case '\t': writer.Write("\\t"); break;
                    default:
                        if (ch < 0x20)
                        {
                            writer.Write(string.Format("\\u{0:x4}", (int)ch));
                        }
                        else
                        {
                            writer.Write(ch);
                        }
                        break;
                }
            }
            writer.Write('"');
        }

        private void WriteArray(JsonArray array, TextWriter writer, int depth)
        {
            if (array.Count == 0)
            {
                writer.Write("[]");
                return;
            }

            writer.Write('[');
            if (Indented) writer.WriteLine();

            for (int i = 0; i < array.Count; i++)
            {
                if (Indented) WriteIndent(writer, depth + 1);
                WriteValue(array[i], writer, depth + 1);
                if (i < array.Count - 1) writer.Write(',');
                if (Indented) writer.WriteLine();
            }

            if (Indented) WriteIndent(writer, depth);
            writer.Write(']');
        }

        private void WriteObject(JsonObject obj, TextWriter writer, int depth)
        {
            if (obj.Count == 0)
            {
                writer.Write("{}");
                return;
            }

            writer.Write('{');
            if (Indented) writer.WriteLine();

            int count = 0;
            int total = obj.Count;

            foreach (var kvp in obj.Properties)
            {
                count++;
                if (Indented) WriteIndent(writer, depth + 1);
                WriteString(kvp.Key, writer);
                writer.Write(Indented ? ": " : ":");
                WriteValue(kvp.Value, writer, depth + 1);
                if (count < total) writer.Write(',');
                if (Indented) writer.WriteLine();
            }

            if (Indented) WriteIndent(writer, depth);
            writer.Write('}');
        }

        private void WriteIndent(TextWriter writer, int depth)
        {
            for (int i = 0; i < depth; i++)
            {
                writer.Write(IndentString);
            }
        }
    }
}
