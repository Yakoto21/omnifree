using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio.Wav
{
    /// <summary>
    /// Motor proprietário de áudio RIFF/WAVE PCM do Lodino.
    /// Suporta inspeção profunda de metadados, reamostragem, conversão mono/estéreo,
    /// conversão entre profundidades de bits (8, 16, 24, 32), ganho e normalização.
    /// </summary>
    public class WavEngine : ICoreEngine
    {
        public string EngineId { get { return "audio-wav"; } }
        public string EngineLabel { get { return "Motor Nativo de Áudio RIFF/WAVE PCM (Lodino Proprietary Audio Engine)"; } }
        public string[] SupportedInputs { get { return new[] { "wav", "wave" }; } }
        public string[] SupportedOutputs { get { return new[] { "wav", "wave" }; } }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;
            bool srcOk = sourceFormat.Equals("wav", StringComparison.OrdinalIgnoreCase) ||
                         sourceFormat.Equals("wave", StringComparison.OrdinalIgnoreCase);
            bool dstOk = targetFormat.Equals("wav", StringComparison.OrdinalIgnoreCase) ||
                         targetFormat.Equals("wave", StringComparison.OrdinalIgnoreCase);
            return srcOk && dstOk;
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            long length = inputStream.CanSeek ? inputStream.Length : 0;
            if (inputStream.CanSeek)
            {
                SecurityLimits.AssertFileSize(length, SecurityLimits.DefaultMaxAudioFileSize);
            }

            var inspection = new FormatInspection
            {
                FormatId = "wav",
                MimeType = "audio/wav",
                FileSizeBytes = length,
                IsSupported = false
            };

            long origPos = inputStream.CanSeek ? inputStream.Position : 0;
            try
            {
                WavHeader header = WavHeader.Read(inputStream);
                inspection.IsSupported = true;
                inspection.Metadata["AudioFormat"] = "PCM (Linear Não Comprimido)";
                inspection.Metadata["Channels"] = header.Channels == 1 ? "1 (Mono)" : (header.Channels == 2 ? "2 (Estéreo)" : header.Channels.ToString());
                inspection.Metadata["ChannelCount"] = header.Channels.ToString();
                inspection.Metadata["SampleRate"] = header.SampleRate.ToString() + " Hz";
                inspection.Metadata["BitsPerSample"] = header.BitsPerSample.ToString() + " bits";
                inspection.Metadata["ByteRate"] = header.ByteRate.ToString() + " bytes/s";
                inspection.Metadata["BlockAlign"] = header.BlockAlign.ToString() + " bytes";
                inspection.Metadata["FrameCount"] = header.FrameCount.ToString("N0", CultureInfo.InvariantCulture);
                inspection.Metadata["DurationSeconds"] = header.DurationSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " s";

                foreach (var pair in header.AuxiliaryChunks)
                {
                    inspection.Metadata["Chunk_" + pair.Key] = pair.Value;
                }
            }
            finally
            {
                if (inputStream.CanSeek)
                {
                    inputStream.Position = origPos;
                }
            }

            return inspection;
        }

        public ConvertResult Convert(ConvertRequest request, Action<int> onProgress)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (string.IsNullOrEmpty(request.InputPath)) throw new ArgumentException("InputPath não especificado.");
            if (!File.Exists(request.InputPath)) throw new FileNotFoundException("Arquivo de entrada não encontrado.", request.InputPath);

            var sw = Stopwatch.StartNew();
            if (onProgress != null) onProgress(5);

            string targetExt = request.TargetFormat;
            if (string.IsNullOrEmpty(targetExt)) targetExt = "wav";
            targetExt = targetExt.TrimStart('.').ToLowerInvariant();

            if (!CanConvert(Path.GetExtension(request.InputPath).TrimStart('.'), targetExt))
            {
                throw new UnsupportedFormatException(string.Format("Rota de áudio não suportada para o arquivo {0} -> .{1}",
                    request.InputPath, targetExt));
            }

            // 1. Decodifica o áudio de entrada
            AudioBuffer buffer;
            using (var inStream = File.OpenRead(request.InputPath))
            {
                buffer = WavDecoder.Decode(inStream, pct =>
                {
                    if (onProgress != null) onProgress(5 + (int)(pct * 0.45));
                });
            }

            // 2. Aplica transformações de áudio conforme Settings
            if (request.Settings != null)
            {
                // Conversão de taxa de amostragem (Resample)
                string sampleRateStr;
                if (request.Settings.TryGetValue("sampleRate", out sampleRateStr) ||
                    request.Settings.TryGetValue("rate", out sampleRateStr))
                {
                    int targetRate;
                    if (int.TryParse(sampleRateStr, out targetRate) && targetRate > 0)
                    {
                        buffer = buffer.Resample(targetRate);
                    }
                }

                // Conversão de canais (Mono / Estéreo)
                string channelsStr;
                if (request.Settings.TryGetValue("channels", out channelsStr))
                {
                    int targetChannels;
                    if (int.TryParse(channelsStr, out targetChannels) && (targetChannels == 1 || targetChannels == 2))
                    {
                        buffer = buffer.ConvertChannels(targetChannels);
                    }
                }

                // Conversão de profundidade de bits (8, 16, 24, 32)
                string bitsStr;
                if (request.Settings.TryGetValue("bits", out bitsStr) ||
                    request.Settings.TryGetValue("bitsPerSample", out bitsStr))
                {
                    int targetBits;
                    if (int.TryParse(bitsStr, out targetBits) &&
                        (targetBits == 8 || targetBits == 16 || targetBits == 24 || targetBits == 32))
                    {
                        buffer.BitsPerSample = targetBits;
                    }
                }

                // Ganho de amplitude (Volume)
                string gainStr;
                if (request.Settings.TryGetValue("gain", out gainStr) ||
                    request.Settings.TryGetValue("volume", out gainStr))
                {
                    float gain;
                    if (float.TryParse(gainStr, NumberStyles.Float, CultureInfo.InvariantCulture, out gain))
                    {
                        if (float.IsNaN(gain) || float.IsInfinity(gain))
                        {
                            throw new ArgumentOutOfRangeException("gain", "O ganho deve ser um número finito.");
                        }
                        if (gain > 0f)
                        {
                            buffer.ApplyGain(gain);
                        }
                    }
                }

                // Normalização de pico
                string normStr;
                if (request.Settings.TryGetValue("normalize", out normStr))
                {
                    if (normStr.Equals("true", StringComparison.OrdinalIgnoreCase) || normStr == "1")
                    {
                        buffer.Normalize(0.99f);
                    }
                }
            }

            if (onProgress != null) onProgress(55);

            // 3. Define caminho de saída seguro
            string outDir = string.IsNullOrEmpty(request.OutputDirectory)
                ? Path.GetDirectoryName(request.InputPath)
                : request.OutputDirectory;

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string baseFileName = string.IsNullOrWhiteSpace(request.OutputFileName)
                ? Path.GetFileNameWithoutExtension(request.InputPath)
                : Path.GetFileNameWithoutExtension(request.OutputFileName);

            string finalPath = SafeFileWriter.GetUniqueOutputPath(
                outDir,
                baseFileName,
                targetExt);

            // 4. Gravação segura em disco
            using (var outStream = File.Create(finalPath))
            {
                WavEncoder.Encode(buffer, outStream, pct =>
                {
                    if (onProgress != null) onProgress(55 + (int)(pct * 0.45));
                });
            }

            sw.Stop();
            if (onProgress != null) onProgress(100);

            long outSize = File.Exists(finalPath) ? new FileInfo(finalPath).Length : 0;

            return ConvertResult.Success(
                request.JobId,
                finalPath,
                outSize,
                sw.ElapsedMilliseconds);
        }
    }
}
