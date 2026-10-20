using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Registry;

namespace Lodino.Desktop
{
    /// <summary>
    /// Gerenciador assíncrono e thread-safe da fila de conversões da aplicação desktop.
    /// Conecta a interface gráfica com os motores nativos do Lodino.Core.
    /// </summary>
    public class ConversionManager
    {
        private readonly List<ConversionJob> _jobs;
        private readonly object _lock = new object();
        private int _nextId = 1;
        private volatile bool _isRunning = false;
        private volatile bool _cancelRequested = false;
        private IntPtr _targetWindow = IntPtr.Zero;

        public event Action<ConversionJob> JobAdded;
        public event Action<ConversionJob> JobProgress;
        public event Action<ConversionJob> JobCompleted;
        public event Action<int, int, int> BatchFinished; // total, success, failed
        public event Action<string> LogMessage;

        public bool IsRunning { get { return _isRunning; } }
        public bool CancelRequested { get { return _cancelRequested; } }

        public IReadOnlyList<ConversionJob> Jobs
        {
            get
            {
                lock (_lock)
                {
                    return _jobs.ToArray();
                }
            }
        }

        public ConversionManager()
        {
            _jobs = new List<ConversionJob>();
        }

        public void BindWindow(IntPtr hWnd)
        {
            _targetWindow = hWnd;
        }

        public void Log(string message)
        {
            string formatted = string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, message);
            var handler = LogMessage;
            if (handler != null)
            {
                handler(formatted);
            }
        }

        public ConversionJob AddFile(string path, string defaultTargetFormat = null)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            ConversionJob job;
            lock (_lock)
            {
                string ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                string target = defaultTargetFormat;
                if (string.IsNullOrEmpty(target) || target.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    target = SuggestTargetFormat(ext);
                }
                else if (!CanConvert(ext, target))
                {
                    // Se o formato alvo pedido não puder ser aplicado ao arquivo, adota rota inteligente nativa
                    target = SuggestTargetFormat(ext);
                }

                job = new ConversionJob(_nextId++, path, target);
                _jobs.Add(job);
            }

            Log(string.Format("Arquivo adicionado: {0} ({1})", job.FileName, job.FormatSize(job.SourceSizeBytes)));

            var handler = JobAdded;
            if (handler != null)
            {
                handler(job);
            }

            if (_targetWindow != IntPtr.Zero)
            {
                NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_QUEUE_UPDATED, (IntPtr)job.Id, IntPtr.Zero);
            }

            return job;
        }

        public int AddFiles(IEnumerable<string> paths, string defaultTargetFormat = null)
        {
            int count = 0;
            if (paths == null) return 0;
            foreach (var p in paths)
            {
                if (AddFile(p, defaultTargetFormat) != null)
                {
                    count++;
                }
            }
            return count;
        }

        public bool RemoveJob(int id)
        {
            ConversionJob removed = null;
            lock (_lock)
            {
                for (int i = 0; i < _jobs.Count; i++)
                {
                    if (_jobs[i].Id == id)
                    {
                        removed = _jobs[i];
                        _jobs.RemoveAt(i);
                        break;
                    }
                }
            }

            if (removed != null)
            {
                Log("Removido da fila: " + removed.FileName);
                if (_targetWindow != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_QUEUE_UPDATED, IntPtr.Zero, IntPtr.Zero);
                }
                return true;
            }
            return false;
        }

        public void Clear()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _jobs.Clear();
            }
            Log("Fila limpa.");
            if (_targetWindow != IntPtr.Zero)
            {
                NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_QUEUE_UPDATED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public bool StartBatch(string overrideTargetFormat = null, string outputDirectory = null, IDictionary<string, string> commonParameters = null)
        {
            lock (_lock)
            {
                if (_isRunning) return false;
                if (_jobs.Count == 0) return false;

                _isRunning = true;
                _cancelRequested = false;

                // Atualiza formato e parâmetros se fornecidos
                foreach (var job in _jobs)
                {
                    if (job.Status == JobStatus.Completed) continue;

                    job.Status = JobStatus.Queued;
                    job.ProgressPercent = 0;
                    job.ErrorMessage = null;

                    if (!string.IsNullOrEmpty(overrideTargetFormat) && !overrideTargetFormat.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        string cleanOverride = overrideTargetFormat.Trim().ToLowerInvariant().TrimStart('.');
                        if (CanConvert(job.SourceFormat, cleanOverride))
                        {
                            job.TargetFormat = cleanOverride;
                        }
                        else
                        {
                            // Preserva a operação original se for válida; caso contrário, sugere operação compatível
                            if (!CanConvert(job.SourceFormat, job.TargetFormat))
                            {
                                job.TargetFormat = SuggestTargetFormat(job.SourceFormat);
                            }
                        }
                    }

                    if (commonParameters != null)
                    {
                        foreach (var kvp in commonParameters)
                        {
                            job.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            Log("Iniciando lote de conversões...");
            ThreadPool.QueueUserWorkItem(ProcessBatchWorker, outputDirectory);
            return true;
        }

        public void Cancel()
        {
            if (_isRunning)
            {
                _cancelRequested = true;
                Log("Cancelamento solicitado pelo usuário.");
            }
        }

        private void ProcessBatchWorker(object state)
        {
            string outputDirectory = state as string;
            int total = 0;
            int success = 0;
            int failed = 0;

            try
            {
                ConversionJob[] pendingJobs;
                lock (_lock)
                {
                    pendingJobs = _jobs.ToArray();
                    total = pendingJobs.Length;
                }

                for (int i = 0; i < pendingJobs.Length; i++)
                {
                    var job = pendingJobs[i];

                    if (_cancelRequested)
                    {
                        lock (_lock)
                        {
                            if (job.Status == JobStatus.Queued)
                            {
                                job.Status = JobStatus.Cancelled;
                                job.ErrorMessage = "Cancelado pelo usuário.";
                            }
                        }
                        NotifyJobProgress(job, 0);
                        NotifyJobCompleted(job);
                        continue;
                    }

                    if (job.Status == JobStatus.Completed)
                    {
                        success++;
                        continue;
                    }

                    ProcessSingleJob(job, outputDirectory);

                    if (job.Status == JobStatus.Completed)
                    {
                        success++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Erro inesperado no lote: " + ex.Message);
            }
            finally
            {
                _isRunning = false;
                Log(string.Format("Lote finalizado. Total: {0}, Sucesso: {1}, Falhas: {2}", total, success, failed));

                var finishHandler = BatchFinished;
                if (finishHandler != null)
                {
                    finishHandler(total, success, failed);
                }

                if (_targetWindow != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_BATCH_FINISH, (IntPtr)success, (IntPtr)failed);
                }
            }
        }

        private void ProcessSingleJob(ConversionJob job, string outputDirectory)
        {
            job.Status = JobStatus.Processing;
            job.ProgressPercent = 10;
            NotifyJobProgress(job, 10);
            Log(string.Format("Processando [{0}]: {1} -> {2}...", job.Id, job.FileName, job.TargetFormat.ToUpperInvariant()));

            try
            {
                if (!File.Exists(job.SourcePath))
                {
                    throw new FileNotFoundException("Arquivo de entrada não encontrado.", job.SourcePath);
                }

                // Determina pasta de destino
                string targetDir = outputDirectory;
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = Path.GetDirectoryName(job.SourcePath);
                }
                else
                {
                    try
                    {
                        if (!Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new IOException(string.Format("Não foi possível acessar ou criar o diretório de destino '{0}': {1}", targetDir, ex.Message), ex);
                    }
                }

                // Resolve o motor no Core Registry
                var registry = EngineRegistry.Default;
                var engine = registry.Resolve(job.SourceFormat, job.TargetFormat);
                if (engine == null)
                {
                    throw new UnsupportedFormatException(string.Format(
                        "Rota de conversão '{0}' -> '{1}' não é suportada pelo motor próprio Lodino.",
                        job.SourceFormat, job.TargetFormat));
                }

                job.ProgressPercent = 25;
                NotifyJobProgress(job, 25);

                if (_cancelRequested)
                {
                    job.Status = JobStatus.Cancelled;
                    job.ErrorMessage = "Operação cancelada.";
                    return;
                }

                var request = new ConvertRequest
                {
                    InputPath = Path.GetFullPath(job.SourcePath),
                    TargetFormat = job.TargetFormat,
                    OutputDirectory = Path.GetFullPath(targetDir)
                };

                if (job.Parameters != null)
                {
                    foreach (var kvp in job.Parameters)
                    {
                        request.Settings[kvp.Key] = kvp.Value;
                    }
                }

                var result = engine.Convert(request, pct =>
                {
                    job.ProgressPercent = Math.Max(25, Math.Min(95, pct));
                    NotifyJobProgress(job, job.ProgressPercent);
                });

                if (result == null || result.Status != "completed")
                {
                    string err = (result != null && !string.IsNullOrEmpty(result.ErrorMessage))
                        ? result.ErrorMessage
                        : "Falha na conversão do arquivo.";
                    throw new LodinoException(err);
                }

                job.DestinationPath = result.OutputPath;
                if (File.Exists(result.OutputPath))
                {
                    job.OutputSizeBytes = new FileInfo(result.OutputPath).Length;
                }
                else
                {
                    job.OutputSizeBytes = result.BytesProcessed;
                }

                job.FinishedAt = DateTime.Now;
                job.Status = JobStatus.Completed;
                job.ProgressPercent = 100;

                Log(string.Format("Concluído: {0} ({1}) salvo em {2}",
                    Path.GetFileName(result.OutputPath),
                    job.FormatSize(job.OutputSizeBytes),
                    result.OutputPath));
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                Log(string.Format("Erro ao converter {0}: {1}", job.FileName, ex.Message));
            }
            finally
            {
                NotifyJobCompleted(job);
            }
        }

        private void NotifyJobProgress(ConversionJob job, int percent)
        {
            var handler = JobProgress;
            if (handler != null)
            {
                handler(job);
            }

            if (_targetWindow != IntPtr.Zero)
            {
                NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_JOB_PROGRESS, (IntPtr)job.Id, (IntPtr)percent);
            }
        }

        private void NotifyJobCompleted(ConversionJob job)
        {
            var handler = JobCompleted;
            if (handler != null)
            {
                handler(job);
            }

            if (_targetWindow != IntPtr.Zero)
            {
                int statusInt = (int)job.Status;
                NativeMethods.PostMessage(_targetWindow, NativeMethods.WM_APP_JOB_COMPLETE, (IntPtr)job.Id, (IntPtr)statusInt);
            }
        }

        private string GenerateUniqueFilePath(string directory, string baseName, string extension)
        {
            string target = Path.Combine(directory, baseName + extension);
            if (!File.Exists(target))
            {
                return target;
            }

            for (int i = 1; i <= 1000; i++)
            {
                string candidate = Path.Combine(directory, string.Format("{0} ({1}){2}", baseName, i, extension));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(directory, string.Format("{0}_{1}{2}", baseName, Guid.NewGuid().ToString("N").Substring(0, 8), extension));
        }

        public static bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;
            string src = sourceFormat.Trim().ToLowerInvariant().TrimStart('.');
            string tgt = targetFormat.Trim().ToLowerInvariant().TrimStart('.');

            if (tgt == "auto") return true;

            var engines = EngineRegistry.Default.GetAll();
            foreach (var engine in engines)
            {
                if (engine.CanConvert(src, tgt))
                {
                    return true;
                }
            }
            return false;
        }

        public static string SuggestTargetFormat(string inputExt)
        {
            if (string.IsNullOrEmpty(inputExt)) return "png";
            switch (inputExt.Trim().ToLowerInvariant().TrimStart('.'))
            {
                case "bmp": return "png";
                case "png": return "jpg";
                case "jpg":
                case "jpeg": return "png";
                case "csv": return "json";
                case "json": return "csv";
                case "ini": return "json";
                case "wav":
                case "wave": return "wav";
                case "zip": return "extract";
                case "gz":
                case "gzip": return "extract";
                default: return "zip";
            }
        }

        public void ProcessBatchSynchronously(string overrideTargetFormat = null, string outputDirectory = null, IDictionary<string, string> commonParameters = null)
        {
            lock (_lock)
            {
                if (_jobs.Count == 0) return;
                _isRunning = true;
                _cancelRequested = false;

                foreach (var job in _jobs)
                {
                    if (job.Status == JobStatus.Completed) continue;
                    job.Status = JobStatus.Queued;
                    job.ProgressPercent = 0;
                    job.ErrorMessage = null;

                    if (!string.IsNullOrEmpty(overrideTargetFormat) && !overrideTargetFormat.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        string cleanOverride = overrideTargetFormat.Trim().ToLowerInvariant().TrimStart('.');
                        if (CanConvert(job.SourceFormat, cleanOverride))
                        {
                            job.TargetFormat = cleanOverride;
                        }
                        else
                        {
                            if (!CanConvert(job.SourceFormat, job.TargetFormat))
                            {
                                job.TargetFormat = SuggestTargetFormat(job.SourceFormat);
                            }
                        }
                    }

                    if (commonParameters != null)
                    {
                        foreach (var kvp in commonParameters)
                        {
                            job.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            ProcessBatchWorker(outputDirectory);
        }
    }
}
