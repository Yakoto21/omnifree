using System;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Constantes binárias e assinaturas do formato ZIP (PKWARE APPNOTE.TXT).
    /// </summary>
    public static class ZipConstants
    {
        // Assinaturas mágicas (Little-Endian de 4 bytes)
        public const uint LocalFileHeaderSignature = 0x04034b50;      // "PK\x03\x04"
        public const uint CentralDirectoryHeaderSignature = 0x02014b50; // "PK\x01\x02"
        public const uint EndOfCentralDirectorySignature = 0x06054b50;  // "PK\x05\x06"
        public const uint DataDescriptorSignature = 0x08074b50;         // "PK\x07\x08"
        public const uint Zip64EndOfCentralDirectorySignature = 0x06064b50;
        public const uint Zip64LocatorSignature = 0x07064b50;
        public const ushort Zip64Marker16 = 0xFFFF;
        public const uint Zip64Marker32 = 0xFFFFFFFF;

        // Métodos de Compressão
        public const ushort CompressionMethodStore = 0;   // Sem compressão (Stored)
        public const ushort CompressionMethodDeflate = 8; // DEFLATE (RFC 1951)

        // General Purpose Bit Flags
        public const ushort FlagEncrypted = 1 << 0;       // Bit 0: Arquivo criptografado
        public const ushort FlagDataDescriptor = 1 << 3;  // Bit 3: CRC e tamanhos no descritor de dados pós-payload
        public const ushort FlagUtf8 = 1 << 11;           // Bit 11: Nomes de arquivo e comentários em UTF-8

        // Versões
        public const ushort VersionDefaultNeeded = 20;    // 2.0 (DEFLATE / subpastas)
        public const ushort VersionStoreNeeded = 10;      // 1.0 (Store básico)
        public const ushort VersionMadeByWindows = 0x0A;  // Windows OS (10)
        public const ushort VersionMadeByUnix = 0x0314;   // Unix (3) + 2.0 (20)

        // Atributos de arquivo externos
        public const uint FileAttributeDirectoryWindows = 0x10; // Diretório DOS/Windows
        public const uint FileAttributeDirectoryUnix = 0x41ED0000; // drwxr-xr-x no Unix (0040755)
        public const uint FileAttributeRegularUnix = 0x81A40000;   // -rw-r--r-- no Unix (0100644)
    }
}
