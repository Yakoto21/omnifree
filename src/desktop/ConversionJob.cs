using System;
using System.Collections.Generic;
using System.IO;

namespace Lodino.Desktop
{
    /// <summary>
    /// Estados possíveis de um item na fila de conversão.
    /// </summary>
    public enum JobStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Representa um trabalho individual de conversão na fila da aplicação.
    /// </summary>
    public class ConversionJob
    {
        public int Id { get; private set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public string SourceFormat { get; set; }
        public string TargetFormat { get; set; }
        public long SourceSizeBytes { get; set; }
        public long OutputSizeBytes { get; set; }
        public JobStatus Status { get; set; }
        public int ProgressPercent { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime QueuedAt { get; private set; }
        public DateTime? FinishedAt { get; set; }
        public Dictionary<string, string> Parameters { get; private set; }

        public string FileName
        {
            get { return Path.GetFileName(SourcePath); }
        }

        public string OperationDescription
        {
            get
            {
                string tgt = (TargetFormat ?? "").ToLowerInvariant();
                string src = (SourceFormat ?? "").ToLowerInvariant();

                if (tgt == "extract" || tgt == "ungz" || tgt == "decompress" || tgt == "dir" || tgt == "folder")
                {
                    return "EXTRAIR";
                }
                if (tgt == "zip")
                {
                    return src == "zip" ? "VERIFICAR ZIP" : "COMPACTAR (ZIP)";
                }
                if (tgt == "gz" || tgt == "gzip")
                {
                    return (src == "gz" || src == "gzip") ? "RECOMPRIMIR (GZ)" : "COMPACTAR (GZ)";
                }
                return "CONVERTER -> " + tgt.ToUpperInvariant();
            }
        }

        public ConversionJob(int id, string sourcePath, string targetFormat)
        {
            Id = id;
            SourcePath = sourcePath;
            TargetFormat = (targetFormat ?? "").Trim().ToLowerInvariant().TrimStart('.');
            SourceFormat = Path.GetExtension(sourcePath).Trim().ToLowerInvariant().TrimStart('.');
            Status = JobStatus.Queued;
            ProgressPercent = 0;
            QueuedAt = DateTime.Now;
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (File.Exists(sourcePath))
                {
                    SourceSizeBytes = new FileInfo(sourcePath).Length;
                }
            }
            catch
            {
                SourceSizeBytes = 0;
            }
        }

        public string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
        }

        public override string ToString()
        {
            string statusStr = Status.ToString();
            switch (Status)
            {
                case JobStatus.Queued: statusStr = "Na fila"; break;
                case JobStatus.Processing: statusStr = string.Format("Processando ({0}%)", ProgressPercent); break;
                case JobStatus.Completed: statusStr = "Concluído"; break;
                case JobStatus.Failed: statusStr = "Falha"; break;
                case JobStatus.Cancelled: statusStr = "Cancelado"; break;
            }
            return string.Format("{0} [{1}] — {2}", FileName, OperationDescription, statusStr);
        }
    }
}
