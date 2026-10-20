using System;

namespace Lodino.Core.Formats.Archive.Gzip
{
    /// <summary>
    /// Constantes, máscaras e códigos numéricos do formato GZIP (RFC 1952).
    /// </summary>
    public static class GzipConstants
    {
        // Assinatura mágica de 2 bytes (RFC 1952 § 2.3.1)
        public const byte Id1 = 0x1F;
        public const byte Id2 = 0x8B;
        public const ushort MagicWord = 0x8B1F; // Little-endian

        // Método de compressão (RFC 1952 § 2.3.1)
        public const byte CompressionMethodDeflate = 8;

        // Máscaras de bits para o byte FLG (RFC 1952 § 2.3.1)
        public const byte FlagText = 0x01;       // Bit 0: FTEXT (sugestão de texto ASCII)
        public const byte FlagHeaderCrc = 0x02;  // Bit 1: FHCRC (CRC16 do cabeçalho presente)
        public const byte FlagExtra = 0x04;      // Bit 2: FEXTRA (campo extra presente)
        public const byte FlagFileName = 0x08;   // Bit 3: FNAME (nome original terminado em zero)
        public const byte FlagComment = 0x10;    // Bit 4: FCOMMENT (comentário terminado em zero)
        public const byte FlagReservedMask = 0xE0; // Bits 5, 6, 7: RESERVADOS (devem ser estritamente zero)

        // Flags extras de compressão XFL (RFC 1952 § 2.3.1)
        public const byte XflMaximumCompression = 2; // Compressor utilizou compressão máxima (mais lento)
        public const byte XflFastestCompression = 4; // Compressor utilizou compressão mais rápida

        // Identificadores de Sistema Operacional de Origem (OS)
        public const byte OsFat = 0;
        public const byte OsAmiga = 1;
        public const byte OsVms = 2;
        public const byte OsUnix = 3;
        public const byte OsVmCms = 4;
        public const byte OsAtari = 5;
        public const byte OsHpfs = 6;
        public const byte OsMacintosh = 7;
        public const byte OsZSystem = 8;
        public const byte OsCpM = 9;
        public const byte OsTops20 = 10;
        public const byte OsNtfs = 11;
        public const byte OsQdos = 12;
        public const byte OsAcorn = 13;
        public const byte OsUnknown = 255;

        // Tamanho mínimo do cabeçalho fixo (10 bytes) e do trailer fixo (8 bytes)
        public const int FixedHeaderLength = 10;
        public const int FixedTrailerLength = 8;
    }

    /// <summary>
    /// Código do sistema operacional de origem do arquivo GZIP.
    /// </summary>
    public enum GzipOperatingSystem : byte
    {
        Fat = GzipConstants.OsFat,
        Amiga = GzipConstants.OsAmiga,
        Vms = GzipConstants.OsVms,
        Unix = GzipConstants.OsUnix,
        VmCms = GzipConstants.OsVmCms,
        Atari = GzipConstants.OsAtari,
        Hpfs = GzipConstants.OsHpfs,
        Macintosh = GzipConstants.OsMacintosh,
        ZSystem = GzipConstants.OsZSystem,
        CpM = GzipConstants.OsCpM,
        Tops20 = GzipConstants.OsTops20,
        Ntfs = GzipConstants.OsNtfs,
        Qdos = GzipConstants.OsQdos,
        Acorn = GzipConstants.OsAcorn,
        Unknown = GzipConstants.OsUnknown
    }
}
