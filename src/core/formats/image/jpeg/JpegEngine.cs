using System;
using System.Diagnostics;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Motor proprietário de processamento, inspeção e conversão de imagens JPEG (ISO/IEC 10918-1 / ITU-T T.81).
    /// Suporta inspeção de metadados, transformações de PixelBuffer (recorte, redimensionamento nearest/bilinear,
    /// marcas d'água de texto e imagem) e conversão entre JPEG, BMP e PNG.
    /// </summary>
    public class JpegEngine : ICoreEngine
    {
        public string EngineId { get { return "image-jpeg"; } }
        public string EngineLabel { get { return "Motor Nativo de Imagens JPEG (Lodino Proprietary JPEG Engine)"; } }
        public string[] SupportedInputs { get { return new[] { "jpeg", "jpg", "bmp", "png" }; } }
        public string[] SupportedOutputs { get { return new[] { "jpeg", "jpg", "bmp", "png" }; } }

        public bool CanConvert(string sourceFormat, string targetFormat)
        {
            if (string.IsNullOrEmpty(sourceFormat) || string.IsNullOrEmpty(targetFormat)) return false;

            bool isSrcJpeg = IsJpegFormat(sourceFormat);
            bool isTgtJpeg = IsJpegFormat(targetFormat);

            bool isSrcBmp = sourceFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase);
            bool isTgtBmp = targetFormat.Equals("bmp", StringComparison.OrdinalIgnoreCase);

            bool isSrcPng = sourceFormat.Equals("png", StringComparison.OrdinalIgnoreCase);
            bool isTgtPng = targetFormat.Equals("png", StringComparison.OrdinalIgnoreCase);

            // Rotas atendidas pelo motor JPEG:
            // 1. JPEG -> JPEG
            // 2. JPEG -> BMP
            // 3. BMP  -> JPEG
            // 4. JPEG -> PNG
            // 5. PNG  -> JPEG
            return (isSrcJpeg && (isTgtJpeg || isTgtBmp || isTgtPng)) ||
                   (isSrcBmp && isTgtJpeg) ||
                   (isSrcPng && isTgtJpeg);
        }

        private static bool IsJpegFormat(string fmt)
        {
            return fmt.Equals("jpeg", StringComparison.OrdinalIgnoreCase) ||
                   fmt.Equals("jpg", StringComparison.OrdinalIgnoreCase);
        }

        public FormatInspection Inspect(Stream inputStream, string fileNameHint)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");

            long length = inputStream.Length;
            SecurityLimits.AssertFileSize(length, SecurityLimits.DefaultMaxImageFileSize);

            var inspection = new FormatInspection
            {
                FormatId = "jpeg",
                MimeType = "image/jpeg",
                FileSizeBytes = length,
                IsSupported = false
            };

            long originalPos = inputStream.CanSeek ? inputStream.Position : 0;
            try
            {
                int b1 = inputStream.ReadByte();
                int b2 = inputStream.ReadByte();
                if (b1 != 0xFF || b2 != 0xD8)
                {
                    throw new CorruptDataException("Marcador SOI (0xFFD8) não encontrado no início do arquivo JPEG.");
                }

                int restartInterval = 0;
                bool done = false;

                while (!done)
                {
                    int b;
                    do
                    {
                        b = inputStream.ReadByte();
                        if (b < 0) break;
                    } while (b != 0xFF);

                    if (b < 0) break;

                    do
                    {
                        b = inputStream.ReadByte();
                        if (b < 0) break;
                    } while (b == 0xFF);

                    if (b < 0) break;

                    int marker = (0xFF << 8) | b;
                    if (marker == JpegMarker.EOI || marker == JpegMarker.SOS)
                    {
                        break;
                    }

                    if (marker == JpegMarker.DRI)
                    {
                        int len = ReadBigEndian16(inputStream);
                        if (len == 4)
                        {
                            restartInterval = ReadBigEndian16(inputStream);
                        }
                    }
                    else if (marker == JpegMarker.SOF0)
                    {
                        int len = ReadBigEndian16(inputStream);
                        int precision = inputStream.ReadByte();
                        int height = ReadBigEndian16(inputStream);
                        int width = ReadBigEndian16(inputStream);
                        int numComponents = inputStream.ReadByte();

                        inspection.IsSupported = (precision == 8 && (numComponents == 1 || numComponents == 3));
                        inspection.Width = width;
                        inspection.Height = height;

                        inspection.Metadata["Format"] = "Baseline Sequential DCT (SOF0)";
                        inspection.Metadata["Dimensions"] = string.Format("{0}x{1}", width, height);
                        inspection.Metadata["BitDepth"] = precision.ToString();
                        inspection.Metadata["Precision"] = precision + "-bit";
                        inspection.Metadata["RestartInterval"] = restartInterval.ToString();

                        string compStr = (numComponents == 1) ? "1 (Grayscale)" : (numComponents == 3 ? "3 (YCbCr)" : numComponents.ToString());
                        inspection.Metadata["ColorComponents"] = compStr;

                        // Lê fatores de amostragem
                        string sampling = "Custom";
                        if (numComponents == 1)
                        {
                            sampling = "1:1 (Grayscale)";
                        }
                        else if (numComponents == 3 && len >= 8 + 3 * 3)
                        {
                            int yId = inputStream.ReadByte();
                            int ySamp = inputStream.ReadByte();
                            int yQ = inputStream.ReadByte();

                            int cbId = inputStream.ReadByte();
                            int cbSamp = inputStream.ReadByte();
                            int cbQ = inputStream.ReadByte();

                            int crId = inputStream.ReadByte();
                            int crSamp = inputStream.ReadByte();
                            int crQ = inputStream.ReadByte();

                            if (ySamp == 0x22 && cbSamp == 0x11 && crSamp == 0x11)
                            {
                                sampling = "4:2:0";
                            }
                            else if (ySamp == 0x11 && cbSamp == 0x11 && crSamp == 0x11)
                            {
                                sampling = "4:4:4";
                            }
                            else if (ySamp == 0x21 && cbSamp == 0x11 && crSamp == 0x11)
                            {
                                sampling = "4:2:2";
                            }
                        }
                        inspection.Metadata["Sampling"] = sampling;
                        done = true;
                    }
                    else if (marker == JpegMarker.SOF1 || marker == JpegMarker.SOF2 || marker == JpegMarker.SOF3 ||
                             marker == JpegMarker.SOF9 || marker == JpegMarker.SOF10)
                    {
                        inspection.IsSupported = false;
                        inspection.Metadata["UnsupportedProfile"] = JpegMarker.GetName((ushort)marker);
                        done = true;
                    }
                    else if (JpegMarker.HasLength((ushort)marker))
                    {
                        int len = ReadBigEndian16(inputStream);
                        if (len > 2)
                        {
                            if (inputStream.CanSeek) inputStream.Seek(len - 2, SeekOrigin.Current);
                            else
                            {
                                for (int i = 0; i < len - 2; i++) inputStream.ReadByte();
                            }
                        }
                    }
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

        private static int ReadBigEndian16(Stream stream)
        {
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            if (b1 < 0 || b2 < 0) throw new CorruptDataException("Fim de fluxo ao ler inteiro 16-bit.");
            return (b1 << 8) | b2;
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
                : "jpg";

            PixelBuffer currentImage;
            using (var stream = File.OpenRead(request.InputPath))
            {
                if (IsJpegFormat(sourceFormat))
                {
                    currentImage = JpegDecoder.Decode(stream);
                }
                else if (sourceFormat == "bmp")
                {
                    currentImage = BmpDecoder.Decode(stream);
                }
                else if (sourceFormat == "png")
                {
                    currentImage = PngDecoder.Decode(stream);
                }
                else
                {
                    throw new UnsupportedFormatException("Formato de origem não suportado pelo motor JPEG: " + sourceFormat);
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
                        else if (wmExt == "png")
                        {
                            wmBuffer = PngDecoder.Decode(wmStream);
                        }
                        else if (IsJpegFormat(wmExt))
                        {
                            wmBuffer = JpegDecoder.Decode(wmStream);
                        }
                        else
                        {
                            throw new UnsupportedFormatException("Formato de marca d'água não suportado: " + wmExt);
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
            else if (targetFormat == "png")
            {
                bool hasAlpha = PngEncoder.HasTransparency(currentImage);
                outputBytes = PngEncoder.Encode(currentImage, hasAlpha, PngFilter.FilterNone);
            }
            else
            {
                // JPEG / JPG
                int quality = 85;
                if (settings != null && settings.ContainsKey("quality"))
                {
                    int parsedQ;
                    if (int.TryParse(settings["quality"], out parsedQ))
                    {
                        quality = parsedQ;
                    }
                }

                bool useSub420 = true;
                if (settings != null && settings.ContainsKey("subsampling"))
                {
                    string sub = settings["subsampling"].Trim();
                    if (sub == "444" || sub == "4:4:4")
                    {
                        useSub420 = false;
                    }
                }

                using (var ms = new MemoryStream())
                {
                    JpegEncoder.Encode(currentImage, ms, quality, useSub420);
                    outputBytes = ms.ToArray();
                }
            }

            string outDir = string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : request.OutputDirectory;

            string baseFileName = string.IsNullOrWhiteSpace(request.OutputFileName)
                ? Path.GetFileNameWithoutExtension(request.InputPath)
                : request.OutputFileName;

            string finalExt = IsJpegFormat(targetFormat) ? "jpg" : targetFormat;
            string finalPath = SafeFileWriter.GetUniqueOutputPath(
                outDir,
                baseFileName,
                finalExt);

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
