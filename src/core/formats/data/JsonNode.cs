using System;
using System.Collections.Generic;

namespace Lodino.Core.Formats.Data
{
    public enum JsonType
    {
        Null,
        Boolean,
        Number,
        String,
        Array,
        Object
    }

    public abstract class JsonNode
    {
        public abstract JsonType Type { get; }

        public virtual bool AsBoolean()
        {
            throw new InvalidOperationException("Este nó JSON não é do tipo Boolean.");
        }

        public virtual double AsDouble()
        {
            throw new InvalidOperationException("Este nó JSON não é do tipo Number.");
        }

        public virtual long AsLong()
        {
            throw new InvalidOperationException("Este nó JSON não é do tipo Number.");
        }

        public virtual string AsString()
        {
            return ToString();
        }

        public virtual JsonNode this[string key]
        {
            get
            {
                var obj = this as JsonObject;
                if (obj != null) return obj[key];
                return JsonNull.Value;
            }
            set
            {
                var obj = this as JsonObject;
                if (obj != null) obj[key] = value;
                else throw new InvalidOperationException("Indexação por chave é suportada apenas em JsonObject.");
            }
        }

        public virtual JsonNode this[int index]
        {
            get
            {
                var arr = this as JsonArray;
                if (arr != null && index >= 0 && index < arr.Count) return arr[index];
                return JsonNull.Value;
            }
            set
            {
                var arr = this as JsonArray;
                if (arr != null && index >= 0 && index < arr.Count) arr[index] = value;
                else throw new InvalidOperationException("Indexação numérica é suportada apenas em JsonArray.");
            }
        }
    }

    public class JsonNull : JsonNode
    {
        public static readonly JsonNull Value = new JsonNull();
        private JsonNull() { }

        public override JsonType Type { get { return JsonType.Null; } }
        public override string ToString() { return "null"; }
    }

    public class JsonBoolean : JsonNode
    {
        public bool Value { get; private set; }

        public JsonBoolean(bool value)
        {
            Value = value;
        }

        public override JsonType Type { get { return JsonType.Boolean; } }
        public override bool AsBoolean() { return Value; }
        public override string ToString() { return Value ? "true" : "false"; }
    }

    public class JsonNumber : JsonNode
    {
        public double DoubleValue { get; private set; }
        public long LongValue { get; private set; }
        public bool IsInteger { get; private set; }
        public string RawText { get; private set; }

        public JsonNumber(double value, string rawText = null)
        {
            DoubleValue = value;
            LongValue = (long)value;
            IsInteger = Math.Abs(value - LongValue) < double.Epsilon;
            RawText = rawText ?? value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public JsonNumber(long value)
        {
            LongValue = value;
            DoubleValue = value;
            IsInteger = true;
            RawText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public override JsonType Type { get { return JsonType.Number; } }
        public override double AsDouble() { return DoubleValue; }
        public override long AsLong() { return LongValue; }
        public override string AsString() { return RawText; }
        public override string ToString() { return RawText; }
    }

    public class JsonString : JsonNode
    {
        public string Value { get; private set; }

        public JsonString(string value)
        {
            Value = value ?? string.Empty;
        }

        public override JsonType Type { get { return JsonType.String; } }
        public override string AsString() { return Value; }
        public override string ToString() { return Value; }
    }

    public class JsonArray : JsonNode
    {
        public List<JsonNode> Items { get; private set; }

        public JsonArray()
        {
            Items = new List<JsonNode>();
        }

        public JsonArray(IEnumerable<JsonNode> items)
        {
            Items = new List<JsonNode>(items);
        }

        public override JsonType Type { get { return JsonType.Array; } }

        public int Count { get { return Items.Count; } }

        public void Add(JsonNode item)
        {
            Items.Add(item ?? JsonNull.Value);
        }

        public override JsonNode this[int index]
        {
            get { return Items[index]; }
            set { Items[index] = value ?? JsonNull.Value; }
        }
    }

    public class JsonObject : JsonNode
    {
        public Dictionary<string, JsonNode> Properties { get; private set; }

        public JsonObject()
        {
            Properties = new Dictionary<string, JsonNode>(StringComparer.Ordinal);
        }

        public override JsonType Type { get { return JsonType.Object; } }

        public int Count { get { return Properties.Count; } }

        public bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        public void Add(string key, JsonNode value)
        {
            Properties[key] = value ?? JsonNull.Value;
        }

        public override JsonNode this[string key]
        {
            get
            {
                JsonNode node;
                if (Properties.TryGetValue(key, out node)) return node;
                return JsonNull.Value;
            }
            set
            {
                Properties[key] = value ?? JsonNull.Value;
            }
        }
    }
}
