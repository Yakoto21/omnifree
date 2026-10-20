using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Data
{
    public class JsonParser
    {
        private string _json;
        private int _index;
        private int _length;
        private int _line;
        private int _column;

        public JsonNode Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new CorruptDataException("Entrada JSON vazia ou nula.");
            }

            _json = json;
            _index = 0;
            _length = json.Length;
            _line = 1;
            _column = 1;

            SkipWhitespace();
            if (_index >= _length)
            {
                throw new CorruptDataException("Entrada JSON não contém tokens válidos.");
            }

            JsonNode root = ParseValue(1);
            SkipWhitespace();

            if (_index < _length)
            {
                throw CreateException(string.Format("Caracteres inesperados após o fim do JSON: '{0}'", _json[_index]));
            }

            return root;
        }

        private JsonNode ParseValue(int depth)
        {
            SecurityLimits.AssertJsonDepth(depth);
            SkipWhitespace();

            if (_index >= _length)
            {
                throw CreateException("Fim inesperado da entrada ao ler valor JSON.");
            }

            char ch = _json[_index];

            if (ch == '{') return ParseObject(depth + 1);
            if (ch == '[') return ParseArray(depth + 1);
            if (ch == '"') return new JsonString(ParseString());
            if (ch == 't' || ch == 'f') return ParseBoolean();
            if (ch == 'n') return ParseNull();
            if (ch == '-' || (ch >= '0' && ch <= '9')) return ParseNumber();

            throw CreateException(string.Format("Caractere inválido no início do valor: '{0}'", ch));
        }

        private JsonObject ParseObject(int depth)
        {
            Expect('{');
            var obj = new JsonObject();
            SkipWhitespace();

            if (Peek('}'))
            {
                Expect('}');
                return obj;
            }

            while (true)
            {
                SkipWhitespace();
                if (!Peek('"'))
                {
                    throw CreateException("Chave do objeto JSON deve ser uma string delimitada por aspas.");
                }

                string key = ParseString();
                SkipWhitespace();
                Expect(':');
                SkipWhitespace();

                JsonNode value = ParseValue(depth);
                obj.Add(key, value);

                SkipWhitespace();
                if (Peek('}'))
                {
                    Expect('}');
                    break;
                }
                Expect(',');
            }

            return obj;
        }

        private JsonArray ParseArray(int depth)
        {
            Expect('[');
            var arr = new JsonArray();
            SkipWhitespace();

            if (Peek(']'))
            {
                Expect(']');
                return arr;
            }

            while (true)
            {
                SkipWhitespace();
                JsonNode value = ParseValue(depth);
                arr.Add(value);

                SkipWhitespace();
                if (Peek(']'))
                {
                    Expect(']');
                    break;
                }
                Expect(',');
            }

            return arr;
        }

        private string ParseString()
        {
            Expect('"');
            var sb = new StringBuilder();

            while (_index < _length)
            {
                char ch = _json[_index++];
                _column++;

                if (ch == '"')
                {
                    return sb.ToString();
                }

                if (ch == '\\')
                {
                    if (_index >= _length)
                    {
                        throw CreateException("Sequência de escape inacabada em string JSON.");
                    }

                    char esc = _json[_index++];
                    _column++;

                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_index + 4 > _length)
                            {
                                throw CreateException("Escape Unicode \\u incompleto.");
                            }
                            string hex = _json.Substring(_index, 4);
                            _index += 4;
                            _column += 4;
                            int codePoint;
                            if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))
                            {
                                sb.Append((char)codePoint);
                            }
                            else
                            {
                                throw CreateException(string.Format("Escape Unicode inválido: \\u{0}", hex));
                            }
                            break;
                        default:
                            throw CreateException(string.Format("Escape desconhecido: \\{0}", esc));
                    }
                }
                else
                {
                    if (ch < 0x20)
                    {
                        throw CreateException(string.Format("Caractere de controle sem escape (0x{0:X2}) não permitido em string JSON.", (int)ch));
                    }
                    sb.Append(ch);
                }
            }

            throw CreateException("Fim inesperado ao ler string JSON (aspas de fechamento não encontradas).");
        }

        private JsonNumber ParseNumber()
        {
            int startIndex = _index;

            if (Peek('-')) Advance();

            if (Peek('0'))
            {
                Advance();
                if (_index < _length && _json[_index] >= '0' && _json[_index] <= '9')
                {
                    throw CreateException("Zeros à esquerda não são permitidos em números JSON.");
                }
            }
            else if (_index < _length && _json[_index] >= '1' && _json[_index] <= '9')
            {
                while (_index < _length && _json[_index] >= '0' && _json[_index] <= '9') Advance();
            }
            else
            {
                throw CreateException("Dígito numérico esperado.");
            }

            if (Peek('.'))
            {
                Advance();
                if (_index >= _length || _json[_index] < '0' || _json[_index] > '9')
                {
                    throw CreateException("Pelo menos um dígito é obrigatório após o ponto decimal.");
                }
                while (_index < _length && _json[_index] >= '0' && _json[_index] <= '9') Advance();
            }

            if (Peek('e') || Peek('E'))
            {
                Advance();
                if (Peek('+') || Peek('-')) Advance();
                if (_index >= _length || _json[_index] < '0' || _json[_index] > '9')
                {
                    throw CreateException("Pelo menos um dígito é obrigatório no expoente numérico.");
                }
                while (_index < _length && _json[_index] >= '0' && _json[_index] <= '9') Advance();
            }

            string numText = _json.Substring(startIndex, _index - startIndex);
            double val;
            if (double.TryParse(numText, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                return new JsonNumber(val, numText);
            }

            throw CreateException(string.Format("Não foi possível interpretar o número: {0}", numText));
        }

        private JsonBoolean ParseBoolean()
        {
            if (Match("true")) return new JsonBoolean(true);
            if (Match("false")) return new JsonBoolean(false);
            throw CreateException("Booleano inválido.");
        }

        private JsonNull ParseNull()
        {
            if (Match("null")) return JsonNull.Value;
            throw CreateException("Literal 'null' inválido.");
        }

        private void SkipWhitespace()
        {
            while (_index < _length)
            {
                char ch = _json[_index];
                if (ch == ' ' || ch == '\t')
                {
                    _index++;
                    _column++;
                }
                else if (ch == '\r')
                {
                    _index++;
                    if (_index < _length && _json[_index] == '\n') _index++;
                    _line++;
                    _column = 1;
                }
                else if (ch == '\n')
                {
                    _index++;
                    _line++;
                    _column = 1;
                }
                else
                {
                    break;
                }
            }
        }

        private bool Peek(char ch)
        {
            return _index < _length && _json[_index] == ch;
        }

        private void Advance()
        {
            _index++;
            _column++;
        }

        private void Expect(char expected)
        {
            if (_index >= _length || _json[_index] != expected)
            {
                char actual = _index < _length ? _json[_index] : '\0';
                throw CreateException(string.Format("Esperado '{0}', mas encontrado '{1}'.", expected, actual == '\0' ? "EOF" : actual.ToString()));
            }
            Advance();
        }

        private bool Match(string word)
        {
            if (_index + word.Length > _length) return false;
            for (int i = 0; i < word.Length; i++)
            {
                if (_json[_index + i] != word[i]) return false;
            }
            _index += word.Length;
            _column += word.Length;
            return true;
        }

        private CorruptDataException CreateException(string message)
        {
            return new CorruptDataException(string.Format(
                "Erro de sintaxe JSON na linha {0}, coluna {1}: {2}", _line, _column, message));
        }
    }
}
