namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Métodos de compressão suportados pelo motor próprio ZIP.
    /// </summary>
    public enum ZipCompressionMethod : ushort
    {
        /// <summary>
        /// Armazenamento puro sem compressão (Método 0).
        /// </summary>
        Store = 0,

        /// <summary>
        /// Compressão DEFLATE sem cabeçalhos ZLIB (RFC 1951 / Método 8).
        /// </summary>
        Deflate = 8
    }

    /// <summary>
    /// Nível de compressão desejado ao criar arquivos ZIP.
    /// </summary>
    public enum ZipCompressionLevel
    {
        /// <summary>
        /// Sem compressão (Método Store 0). Mais rápido, zero consumo de CPU em compressão.
        /// </summary>
        Store = 0,

        /// <summary>
        /// Compressão rápida com blocos DEFLATE Stored (Método 8).
        /// </summary>
        Fast = 1,

        /// <summary>
        /// Compressão ótima com DEFLATE Fixed Huffman e casamento LZ77 (Método 8).
        /// </summary>
        Optimal = 2
    }
}
