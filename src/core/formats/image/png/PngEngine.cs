using System;
using System.Diagnostics;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Motor proprietário de processamento, inspeção e conversão de imagens PNG.
    /// Suporta transformações completas de PixelBuffer (recorte em 9 posições, redimensionamento
    /// nearest/bilinear, marca d'água de texto e marca d'água de imagem com canal alfa),
    /// além de conversão bidirecional entre PNG e BMP.
    /// </summary>
    public class PngEngine : ICoreEngine
    {
        public string EngineId { get { return "image-png"; } }
        public string EngineLabel { get { return "Motor Nativo de Imagens PNG (Lodino Proprietary PNG Engine)"; } }
        public string[] SupportedInputs { get { return new[] { "png", "bmp" }; } }
        public string[] SupportedOutputs { get { return new[] { "png", "bmp" }; } }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;

            bool isSourcePng = sourceFormat.Equals("png", StringComparison.OrdinalIgnoreCase);
            bool isSourceBmp = sourceFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase);
            bool isTargetPng = targetFormat.Equals("png", StringComparison.OrdinalIgnoreCase);
            bool isTargetBmp = targetFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase);

            return (isSourcePng && (isTargetPng || isTargetBmp)) ||
                   (isSourceBmp && isTargetPng);
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            long length = inputStream.Length;
            SecurityLimits.AssertFileSize(length, SecurityLimits.DefaultMaxImageFileSize);

            var inspection = new FormatInspection
            {
                FormatId = "png",
                MimeType = "image/png",
                FileSizeBytes = length,
                IsSupported = false
            };

            long originalPos = inputStream.CanSeek ? inputStream.Position : 0;
            try
            {
                PngHeader.ValidateSignature(inputStream);

                PngChunk firstChunk = PngChunk.Read(inputStream);
                if (firstChunk == null)
                {
                    throw new CorruptDataException("Arquivo PNG truncado imediatamente após a assinatura.");
                }

                PngHeader header = PngHeader.FromChunk(firstChunk);
                inspection.IsSupported = true;
                inspection.Width = header.Width;
                inspection.Height = header.Height;

                inspection.Metadata["BitDepth"] = header.BitDepth.ToString();
                inspection.Metadata["ColorType"] = header.ColorType == PngHeader.ColorTypeTruecolorRgba
                    ? "Truecolor RGBA (6)"
                    : "Truecolor RGB (2)";
                inspection.Metadata["HasAlpha"] = header.HasAlpha ? "true" : "false";
                inspection.Metadata["Dimensions"] = string.Format("{0}x{1}", header.Width, header.Height);
                inspection.Metadata["Compression"] = "DEFLATE (0)";
                inspection.Metadata["FilterMethod"] = "Adaptive (0)";
                inspection.Metadata["Interlace"] = "None (0)";
            }
            finally
            {
                if (inputStream.CanSeek)
                {
                    inputStream.Position = originalPos;
                }
            }

            return inspection;
        }

        public ConvertResult Convert(ConvertRequest request, Action<int> onProgress)
        {
            if (request == null) throw new ArgumentNullException("request");
            var stopwatch = Stopwatch.StartNew();

            if (onProgress != null) onProgress(5);

            if (!File.Exists(request.InputPath))
            {
                throw new FileNotFoundException("Arquivo de entrada não encontrado: " + request.InputPath);
            }

            var fileInfo = new FileInfo(request.InputPath);
            SecurityLimits.AssertFileSize(fileInfo.Length, SecurityLimits.DefaultMaxImageFileSize);

            var settings = request.Settings;

            string sourceFormat = settings != null && settings.ContainsKey("sourceFormat")
                ? settings["sourceFormat"].ToLowerInvariant()
                : Path.GetExtension(request.InputPath).TrimStart('.').ToLowerInvariant();

            string targetFormat = !string.IsNullOrEmpty(request.TargetFormat)
                ? request.TargetFormat.ToLowerInvariant()
                : "png";

            PixelBuffer currentImage;
            using (var stream = File.OpenRead(request.InputPath))
            {
                if (sourceFormat == "bmp")
                {
                    currentImage = BmpDecoder.Decode(stream);
                }
                else
                {
                    currentImage = PngDecoder.Decode(stream);
                }
            }

            if (onProgress != null) onProgress(30);

            // 1. Recorte (Crop)
            if (settings != null)
            {
                if (settings.ContainsKey("cropWidth") && settings.ContainsKey("cropHeight"))
                {
                    int cw = int.Parse(settings["cropWidth"]);
                    int ch = int.Parse(settings["cropHeight"]);
                    string pos = settings.ContainsKey("cropPosition") ? settings["cropPosition"] : "center";
                    currentImage = currentImage.CropQuadrant(cw, ch, pos);
                }
                else if (settings.ContainsKey("cropX") && settings.ContainsKey("cropY") &&
                         settings.ContainsKey("cropW") && settings.ContainsKey("cropH"))
                {
                    int cx = int.Parse(settings["cropX"]);
                    int cy = int.Parse(settings["cropY"]);
                    int cw = int.Parse(settings["cropW"]);
                    int ch = int.Parse(settings["cropH"]);
                    currentImage = currentImage.Crop(cx, cy, cw, ch);
                }

                if (onProgress != null) onProgress(50);

                // 2. Redimensionamento (Resize)
                bool hasWidth = settings.ContainsKey("resizeWidth") || settings.ContainsKey("width");
                bool hasHeight = settings.ContainsKey("resizeHeight") || settings.ContainsKey("height");

                if (hasWidth || hasHeight)
                {
                    int targetW = currentImage.Width;
                    int targetH = currentImage.Height;

                    if (settings.ContainsKey("resizeWidth")) targetW = int.Parse(settings["resizeWidth"]);
                    else if (settings.ContainsKey("width")) targetW = int.Parse(settings["width"]);

                    if (settings.ContainsKey("resizeHeight")) targetH = int.Parse(settings["resizeHeight"]);
                    else if (settings.ContainsKey("height")) targetH = int.Parse(settings["height"]);

                    string filter = "bilinear";
                    if (settings.ContainsKey("resizeFilter")) filter = settings["resizeFilter"];
                    else if (settings.ContainsKey("filter")) filter = settings["filter"];

                    if (filter.Equals("nearest", StringComparison.OrdinalIgnoreCase))
                    {
                        currentImage = currentImage.ResizeNearestNeighbor(targetW, targetH);
                    }
                    else
                    {
                        currentImage = currentImage.ResizeBilinear(targetW, targetH);
                    }
                }

                if (onProgress != null) onProgress(70);

                // 3. Marca d'água por imagem (Overlay)
                string wmImgPath = null;
                if (settings.ContainsKey("watermarkImagePath")) wmImgPath = settings["watermarkImagePath"];
                else if (settings.ContainsKey("watermarkImage")) wmImgPath = settings["watermarkImage"];

                if (!string.IsNullOrEmpty(wmImgPath) && File.Exists(wmImgPath))
                {
                    PixelBuffer wmBuffer;
                    using (var wmStream = File.OpenRead(wmImgPath))
                    {
                        string wmExt = Path.GetExtension(wmImgPath).TrimStart('.').ToLowerInvariant();
                        if (wmExt == "bmp")
                        {
                            wmBuffer = BmpDecoder.Decode(wmStream);
                        }
                        else
                        {
                            wmBuffer = PngDecoder.Decode(wmStream);
                        }
                    }

                    int wx = settings.ContainsKey("watermarkX") ? int.Parse(settings["watermarkX"]) : 10;
                    int wy = settings.ContainsKey("watermarkY") ? int.Parse(settings["watermarkY"]) : 10;
                    float opacity = 0.5f;
                    if (settings.ContainsKey("watermarkOpacity"))
                    {
                        float.TryParse(settings["watermarkOpacity"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out opacity);
                    }

                    currentImage.ApplyImageWatermark(wmBuffer, wx, wy, opacity);
                }

                // 4. Marca d'água de texto
                string wmText = null;
                if (settings.ContainsKey("watermarkText")) wmText = settings["watermarkText"];
                else if (settings.ContainsKey("watermark")) wmText = settings["watermark"];

                if (!string.IsNullOrEmpty(wmText))
                {
                    int tx = settings.ContainsKey("watermarkX") ? int.Parse(settings["watermarkX"]) : 10;
                    int ty = settings.ContainsKey("watermarkY") ? int.Parse(settings["watermarkY"]) : 10;
                    int scale = settings.ContainsKey("watermarkScale") ? int.Parse(settings["watermarkScale"]) : 2;
                    string colorStr = settings.ContainsKey("watermarkColor") ? settings["watermarkColor"] : "#FFFFFF";
                    PixelColor color = PixelColor.ParseHex(colorStr, PixelColor.White);

                    currentImage.ApplyTextWatermark(wmText, tx, ty, color, scale);
                }
            }

            if (onProgress != null) onProgress(85);

            byte[] outputBytes;
            if (targetFormat == "bmp")
            {
                ushort outBpp = 24;
                if (settings != null && settings.ContainsKey("bitDepth"))
                {
                    ushort parsedBpp;
                    if (ushort.TryParse(settings["bitDepth"], out parsedBpp) && (parsedBpp == 24 || parsedBpp == 32))
                    {
                        outBpp = parsedBpp;
                    }
                }
                outputBytes = BmpEncoder.Encode(currentImage, outBpp);
            }
            else
            {
                bool hasAlpha = PngEncoder.HasTransparency(currentImage);
                if (settings != null && settings.ContainsKey("hasAlpha"))
                {
                    bool forcedAlpha;
                    if (bool.TryParse(settings["hasAlpha"], out forcedAlpha))
                    {
                        hasAlpha = forcedAlpha;
                    }
                }

                byte filterType = PngFilter.FilterNone;
                if (settings != null && settings.ContainsKey("pngFilter"))
                {
                    byte parsedFilter;
                    if (byte.TryParse(settings["pngFilter"], out parsedFilter) && parsedFilter <= 4)
                    {
                        filterType = parsedFilter;
                    }
                }

                outputBytes = PngEncoder.Encode(currentImage, hasAlpha, filterType);
            }

            string outDir = string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : request.OutputDirectory;

            string baseFileName = string.IsNullOrWhiteSpace(request.OutputFileName)
                ? Path.GetFileNameWithoutExtension(request.InputPath)
                : request.OutputFileName;

            string finalPath = SafeFileWriter.GetUniqueOutputPath(
                outDir,
                baseFileName,
                targetFormat);

            File.WriteAllBytes(finalPath, outputBytes);

            if (onProgress != null) onProgress(100);
            stopwatch.Stop();

            return ConvertResult.Success(
                request.JobId,
                finalPath,
                fileInfo.Length,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
