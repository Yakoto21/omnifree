namespace Lodino.Core.Compression.Deflate
{
    /// <summary>
    /// Níveis de compressão suportados pelo motor DEFLATE (RFC 1951).
    /// </summary>
    public enum DeflateCompressionLevel
    {
        /// <summary>
        /// Sem compressão (armazena blocos Stored BTYPE 00).
        /// </summary>
        Store = 0,

        /// <summary>
        /// Compressão rápida (blocos Stored ou Fixed Huffman BTYPE 01 com LZ77 rápido).
        /// </summary>
        Fast = 1,

        /// <summary>
        /// Compressão ótima (seleção adaptativa entre Stored, Fixed e Dynamic Huffman BTYPE 10 com LZ77 completo).
        /// </summary>
        Optimal = 2
    }
}
