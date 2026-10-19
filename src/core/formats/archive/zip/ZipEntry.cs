using System;

namespace Lodino.Core.Formats.Archive.Zip
{
    /// <summary>
    /// Representa os metadados de uma entrada (arquivo ou pasta) no contêiner ZIP.
    /// </summary>
    public class ZipEntry
    {
        public string Name { get; set; }
        public ZipCompressionMethod Method { get; set; }
        public uint Crc32 { get; set; }
        public long CompressedSize { get; set; }
        public long UncompressedSize { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string Comment { get; set; }
        public byte[] ExtraField { get; set; }
        public long LocalHeaderOffset { get; set; }
        public uint ExternalAttributes { get; set; }
        public ushort GeneralPurposeFlags { get; set; }
        public ushort VersionNeeded { get; set; }
        public ushort VersionMadeBy { get; set; }

        public bool IsDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return false;
                if (Name.EndsWith("/")) return true;
                // Atributo DOS de diretório (0x10) ou atributo Unix de diretório (0040000)
                if ((ExternalAttributes & ZipConstants.FileAttributeDirectoryWindows) != 0) return true;
                if ((ExternalAttributes & 0x40000000) != 0) return true;
                return false;
            }
        }

        public ZipEntry()
        {
            Method = ZipCompressionMethod.Deflate;
            LastWriteTime = DateTime.Now;
            Comment = string.Empty;
            ExtraField = new byte[0];
            VersionNeeded = ZipConstants.VersionDefaultNeeded;
            VersionMadeBy = ZipConstants.VersionMadeByUnix;
            GeneralPurposeFlags = ZipConstants.FlagUtf8;
        }

        public ZipEntry(string name) : this()
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            Name = NormalizeEntryName(name);
            if (Name.EndsWith("/"))
            {
                Method = ZipCompressionMethod.Store;
                ExternalAttributes = ZipConstants.FileAttributeDirectoryWindows | ZipConstants.FileAttributeDirectoryUnix;
            }
            else
            {
                ExternalAttributes = ZipConstants.FileAttributeRegularUnix;
            }
        }

        public static string NormalizeEntryName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            string normalized = name.Replace('\\', '/');
            while (normalized.StartsWith("/"))
            {
                normalized = normalized.Substring(1);
            }
            return normalized;
        }

        public static ushort ToDosTime(DateTime dt)
        {
            return (ushort)((dt.Hour << 11) | (dt.Minute << 5) | (dt.Second >> 1));
        }

        public static ushort ToDosDate(DateTime dt)
        {
            int year = dt.Year;
            if (year < 1980) year = 1980;
            if (year > 2107) year = 2107;
            return (ushort)(((year - 1980) << 9) | (dt.Month << 5) | dt.Day);
        }

        public static DateTime FromDosDateTime(ushort dosDate, ushort dosTime)
        {
            int year = ((dosDate >> 9) & 0x7F) + 1980;
            int month = (dosDate >> 5) & 0x0F;
            int day = dosDate & 0x1F;

            int hour = (dosTime >> 11) & 0x1F;
            int minute = (dosTime >> 5) & 0x3F;
            int second = (dosTime & 0x1F) * 2;

            if (month < 1 || month > 12) month = 1;
            int maxDays = DateTime.DaysInMonth(year, month);
            if (day < 1) day = 1;
            if (day > maxDays) day = maxDays;

            if (hour > 23) hour = 23;
            if (minute > 59) minute = 59;
            if (second > 59) second = 59;

            try
            {
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return new DateTime(1980, 1, 1, 0, 0, 0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] ({2:N0} bytes -> {3:N0} bytes)",
                Name, Method, UncompressedSize, CompressedSize);
        }
    }
}
