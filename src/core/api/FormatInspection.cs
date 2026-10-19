using System;
using System.Collections.Generic;

namespace Lodino.Core.Api
{
    public class FormatInspection
    {
        public string FormatId { get; set; }
        public string MimeType { get; set; }
        public bool IsSupported { get; set; }
        public long FileSizeBytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int PageCount { get; set; }
        public Dictionary<string, string> Metadata { get; set; }

        public FormatInspection()
        {
            FormatId = string.Empty;
            MimeType = "application/octet-stream";
            IsSupported = false;
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
