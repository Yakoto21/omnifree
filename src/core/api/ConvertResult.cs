using System;

namespace Lodino.Core.Api
{
    public class ConvertResult
    {
        public string JobId { get; set; }
        public string Status { get; set; } // "completed", "cancelled", "failed"
        public string OutputPath { get; set; }
        public string ErrorMessage { get; set; }
        public long BytesProcessed { get; set; }
        public long DurationMs { get; set; }

        public ConvertResult()
        {
            JobId = string.Empty;
            Status = "failed";
            OutputPath = string.Empty;
            ErrorMessage = string.Empty;
        }

        public static ConvertResult Success(string jobId, string outputPath, long bytesProcessed, long durationMs)
        {
            return new ConvertResult
            {
                JobId = jobId,
                Status = "completed",
                OutputPath = outputPath,
                BytesProcessed = bytesProcessed,
                DurationMs = durationMs
            };
        }

        public static ConvertResult Cancelled(string jobId, long durationMs)
        {
            return new ConvertResult
            {
                JobId = jobId,
                Status = "cancelled",
                ErrorMessage = "Operação cancelada pelo usuário.",
                DurationMs = durationMs
            };
        }

        public static ConvertResult Failure(string jobId, string error, long durationMs)
        {
            return new ConvertResult
            {
                JobId = jobId,
                Status = "failed",
                ErrorMessage = error,
                DurationMs = durationMs
            };
        }
    }
}
