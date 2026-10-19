using System;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Árvore canônica de Huffman para descompressão DEFLATE (RFC 1951).
    /// Constrói a árvore diretamente a partir do vetor de comprimentos de código (code lengths)
    /// e decodifica símbolos navegando bit a bit no fluxo de entrada.
    /// </summary>
    public class HuffmanTree
    {
        private const int MaxNodes = 2048;
        private readonly int[] _left = new int[MaxNodes];
        private readonly int[] _right = new int[MaxNodes];
        private readonly int[] _symbols = new int[MaxNodes];
        private int _nodeCount;

        private HuffmanTree()
        {
            for (int i = 0; i < MaxNodes; i++)
            {
                _symbols[i] = -1;
            }
            _nodeCount = 1; // 0 é o nó raiz
        }

        public static HuffmanTree Build(int[] codeLengths)
        {
            if (codeLengths == null) throw new ArgumentNullException("codeLengths");

            var tree = new HuffmanTree();

            // 1. Encontra o comprimento máximo e conta os códigos de cada tamanho
            int maxBits = 0;
            for (int i = 0; i < codeLengths.Length; i++)
            {
                if (codeLengths[i] > maxBits)
                {
                    maxBits = codeLengths[i];
                }
            }

            if (maxBits == 0)
            {
                // Árvore vazia (nenhum símbolo utilizado)
                return tree;
            }

            int[] blCount = new int[maxBits + 1];
            for (int i = 0; i < codeLengths.Length; i++)
            {
                int len = codeLengths[i];
                if (len > 0)
                {
                    blCount[len]++;
                }
            }

            // 2. Calcula o código inicial para cada comprimento de bit (RFC 1951 seção 3.2.2)
            int code = 0;
            blCount[0] = 0;
            int[] nextCode = new int[maxBits + 1];
            for (int bits = 1; bits <= maxBits; bits++)
            {
                code = (code + blCount[bits - 1]) << 1;
                nextCode[bits] = code;
            }

            // 3. Insere cada símbolo na árvore binária
            for (int n = 0; n < codeLengths.Length; n++)
            {
                int len = codeLengths[n];
                if (len != 0)
                {
                    int c = nextCode[len]++;
                    tree.InsertCode(c, len, n);
                }
            }

            return tree;
        }

        private void InsertCode(int code, int length, int symbol)
        {
            int current = 0;
            // Bits são navegados do MSB para o LSB do código canônico
            for (int i = length - 1; i >= 0; i--)
            {
                int bit = (code >> i) & 1;
                if (bit == 0)
                {
                    if (_left[current] == 0)
                    {
                        if (_nodeCount >= MaxNodes)
                        {
                            throw new CorruptDataException("Árvore Huffman excedeu o número máximo de nós permitidos.");
                        }
                        _left[current] = _nodeCount++;
                    }
                    current = _left[current];
                }
                else
                {
                    if (_right[current] == 0)
                    {
                        if (_nodeCount >= MaxNodes)
                        {
                            throw new CorruptDataException("Árvore Huffman excedeu o número máximo de nós permitidos.");
                        }
                        _right[current] = _nodeCount++;
                    }
                    current = _right[current];
                }
            }

            _symbols[current] = symbol;
        }

        public int DecodeSymbol(DeflateBitReader reader)
        {
            int current = 0;
            while (_symbols[current] < 0)
            {
                int bit = reader.ReadBit();
                current = (bit == 0) ? _left[current] : _right[current];
                if (current == 0)
                {
                    throw new CorruptDataException("Código de Huffman não reconhecido ou inválido no fluxo DEFLATE.");
                }
            }

            return _symbols[current];
        }

        #region Árvores Fixas Predefinidas (RFC 1951 Seção 3.2.6)

        private static HuffmanTree _fixedLiteralTree;
        private static HuffmanTree _fixedDistanceTree;
        private static readonly object _fixedLock = new object();

        public static HuffmanTree GetFixedLiteralTree()
        {
            lock (_fixedLock)
            {
                if (_fixedLiteralTree == null)
                {
                    int[] lengths = new int[288];
                    for (int i = 0; i <= 143; i++) lengths[i] = 8;
                    for (int i = 144; i <= 255; i++) lengths[i] = 9;
                    for (int i = 256; i <= 279; i++) lengths[i] = 7;
                    for (int i = 280; i <= 287; i++) lengths[i] = 8;
                    _fixedLiteralTree = Build(lengths);
                }
                return _fixedLiteralTree;
            }
        }

        public static HuffmanTree GetFixedDistanceTree()
        {
            lock (_fixedLock)
            {
                if (_fixedDistanceTree == null)
                {
                    int[] lengths = new int[32];
                    for (int i = 0; i < 32; i++) lengths[i] = 5;
                    _fixedDistanceTree = Build(lengths);
                }
                return _fixedDistanceTree;
            }
        }

        #endregion
    }
}
