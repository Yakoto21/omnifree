using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.IO;

namespace Lodino.Scratch.DeflateDynamic
{
    public class CustomZipEntryInfo
    {
        public string Name { get; set; }
        public byte[] UncompressedData { get; set; }
        public byte[] CompressedPayload { get; set; }
        public ushort Method { get; set; } // 0 = Store, 8 = Deflate
        public uint Crc32 { get; set; }
        public long LocalHeaderOffset { get; set; }
    }

    /// <summary>
    /// Gerador de contêineres ZIP padrão PKWARE para testes de interoperabilidade.
    /// Permite empacotar fluxos DEFLATE brutos contendo blocos BTYPE 10 (Dynamic),
    /// BTYPE 01 (Fixed) ou BTYPE 00 (Stored) e testar ferramentas externas.
    /// </summary>
    public static class ZipPackageHelper
    {
        public static void CreateZipWithPayloads(string zipPath, List<CustomZipEntryInfo> entries)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                // 1. Grava Local File Headers e Payloads
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    entry.LocalHeaderOffset = fs.Position;
                    byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name.Replace('\\', '/'));

                    bw.Write((uint)0x04034B50); // Signature
                    bw.Write((ushort)20);        // Version needed (2.0)
                    bw.Write((ushort)0x0800);    // Bit flag (UTF-8)
                    bw.Write(entry.Method);      // Method (0 ou 8)
                    bw.Write((ushort)0x7420);    // DOS time
                    bw.Write((ushort)0x5D45);    // DOS date (2026-10-19)
                    bw.Write(entry.Crc32);
                    bw.Write((uint)entry.CompressedPayload.Length);
                    bw.Write((uint)entry.UncompressedData.Length);
                    bw.Write((ushort)nameBytes.Length);
                    bw.Write((ushort)0);         // Extra field length
                    bw.Write(nameBytes);
                    bw.Write(entry.CompressedPayload);
                }

                // 2. Grava Central Directory Headers
                long cdStart = fs.Position;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    byte[] nameBytes = Encoding.UTF8.GetBytes(entry.Name.Replace('\\', '/'));

                    bw.Write((uint)0x02014B50); // Signature
                    bw.Write((ushort)0x031E);    // Version made by (Unix 3.0)
                    bw.Write((ushort)20);        // Version needed (2.0)
                    bw.Write((ushort)0x0800);    // Bit flag (UTF-8)
                    bw.Write(entry.Method);
                    bw.Write((ushort)0x7420);
                    bw.Write((ushort)0x5D45);
                    bw.Write(entry.Crc32);
                    bw.Write((uint)entry.CompressedPayload.Length);
                    bw.Write((uint)entry.UncompressedData.Length);
                    bw.Write((ushort)nameBytes.Length);
                    bw.Write((ushort)0);         // Extra length
                    bw.Write((ushort)0);         // Comment length
                    bw.Write((ushort)0);         // Disk start
                    bw.Write((ushort)0);         // Internal attrs
                    bw.Write((uint)0x81A40000);  // External attrs (-rw-r--r--)
                    bw.Write((uint)entry.LocalHeaderOffset);
                    bw.Write(nameBytes);
                }

                long cdEnd = fs.Position;
                uint cdSize = (uint)(cdEnd - cdStart);

                // 3. Grava End of Central Directory (EOCD)
                bw.Write((uint)0x06054B50);     // Signature
                bw.Write((ushort)0);            // Disk number
                bw.Write((ushort)0);            // CD disk
                bw.Write((ushort)entries.Count);// Entries on disk
                bw.Write((ushort)entries.Count);// Total entries
                bw.Write(cdSize);               // CD size
                bw.Write((uint)cdStart);        // CD offset
                bw.Write((ushort)0);            // Comment length
            }
        }
    }
}
