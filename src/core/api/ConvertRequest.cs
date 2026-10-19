using System;
using System.Collections.Generic;

namespace Lodino.Core.Api
{
    public class ConvertRequest
    {
        public string JobId { get; set; }
        public string InputPath { get; set; }
        public string TargetFormat { get; set; }
        public string OutputDirectory { get; set; }
        public string OutputFileName { get; set; }
        public Dictionary<string, string> Settings { get; set; }

        public ConvertRequest()
        {
            JobId = Guid.NewGuid().ToString("N");
            InputPath = string.Empty;
            TargetFormat = string.Empty;
            OutputDirectory = string.Empty;
            OutputFileName = string.Empty;
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
