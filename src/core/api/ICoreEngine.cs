using System;
using System.IO;

namespace Lodino.Core.Api
{
    public interface ICoreEngine
    {
        string EngineId { get; }
        string EngineLabel { get; }
        string[] SupportedInputs { get; }
        string[] SupportedOutputs { get; }

        FormatInspection Inspect(Stream inputStream, string fileNameHint);
        bool CanConvert(string sourceFormat, string targetFormat);
        ConvertResult Convert(ConvertRequest request, Action<int> onProgress);
    }
}
