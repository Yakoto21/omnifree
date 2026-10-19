using System;
using System.Diagnostics;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image
{
    /// <summary>
    /// Motor proprietário de processamento e transformação de imagens BMP.
    /// Implementa redimensionamento, recorte em 9 posições e marcas d'água de texto e imagem.
    /// </summary>
    public class BmpEngine : ICoreEngine
    {
        public string EngineId { get { return "image-bmp"; } }
        public string EngineLabel { get { return "Motor Nativo de Imagens BMP (Lodino Proprietary Bitmap Engine)"; } }
        public string[] SupportedInputs { get { return new[] { "bmp" }; } }
        public string[] SupportedOutputs { get { return new[] { "bmp" }; } }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;
            return sourceFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase) &&
                   targetFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase);
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            long length = inputStream.Length;
            SecurityLimits.AssertFileSize(length, SecurityLimits.DefaultMaxImageFileSize);

            var inspection = new FormatInspection
            {
                FormatId = "bmp",
                MimeType = "image/bmp",
                FileSizeBytes = length,
                IsSupported = false
            };

            long originalPos = inputStream.CanSeek ? inputStream.Position : 0;
            try
            {
                using (var reader = new BinaryReader(inputStream, System.Text.Encoding.ASCII, true))
                {
                    BmpHeader header = BmpHeader.Read(reader, length);
                    inspection.IsSupported = true;
                    inspection.Width = header.Width;
                    inspection.Height = header.Height;

                    inspection.Metadata["BitDepth"] = header.BitCount.ToString();
                    inspection.Metadata["Orientation"] = header.IsTopDown ? "Top-down" : "Bottom-up";
                    inspection.Metadata["Compression"] = "BI_RGB (Não comprimido)";
                    inspection.Metadata["Dimensions"] = string.Format("{0}x{1}", header.Width, header.Height);
                    inspection.Metadata["Stride"] = header.Stride.ToString();
                    inspection.Metadata["OffBits"] = header.OffBits.ToString();
                }
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

            PixelBuffer currentImage;
            using (var stream = File.OpenRead(request.InputPath))
            {
                currentImage = BmpDecoder.Decode(stream);
            }

            if (onProgress != null) onProgress(30);

            var settings = request.Settings;

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
                        wmBuffer = BmpDecoder.Decode(wmStream);
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

            // Resolução da profundidade de bits de saída
            ushort outBpp = 24;
            if (settings != null && settings.ContainsKey("bitDepth"))
            {
                ushort parsedBpp;
                if (ushort.TryParse(settings["bitDepth"], out parsedBpp) && (parsedBpp == 24 || parsedBpp == 32))
                {
                    outBpp = parsedBpp;
                }
            }

            byte[] outputBytes = BmpEncoder.Encode(currentImage, outBpp);

            string outDir = string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : request.OutputDirectory;

            string baseFileName = string.IsNullOrWhiteSpace(request.OutputFileName)
                ? Path.GetFileNameWithoutExtension(request.InputPath)
                : request.OutputFileName;

            string finalPath = SafeFileWriter.GetUniqueOutputPath(
                outDir,
                baseFileName,
                "bmp");

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
