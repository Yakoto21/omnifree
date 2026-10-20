using System;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image
{
    /// <summary>
    /// Matriz de pixels RGBA pura, desacoplada de GDI+ e bibliotecas externas.
    /// Armazena 4 bytes por pixel (R, G, B, A) em ordem linear scanline a scanline.
    /// </summary>
    public class PixelBuffer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly byte[] _pixels;

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public byte[] Pixels { get { return _pixels; } }

        public PixelBuffer(int width, int height)
        {
            SecurityLimits.AssertImageDimensions(width, height);
            _width = width;
            _height = height;
            _pixels = new byte[width * height * 4];
        }

        public PixelBuffer(int width, int height, byte[] pixels)
        {
            SecurityLimits.AssertImageDimensions(width, height);
            if (pixels == null) throw new ArgumentNullException("pixels");
            int expected = width * height * 4;
            if (pixels.Length != expected)
            {
                throw new CorruptDataException(string.Format(
                    "Tamanho do buffer ({0} bytes) incompatível com as dimensões {1}x{2} ({3} bytes esperados).",
                    pixels.Length, width, height, expected));
            }

            _width = width;
            _height = height;
            _pixels = pixels;
        }

        public PixelColor GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                throw new ArgumentOutOfRangeException(string.Format("Coordenada ({0},{1}) fora dos limites da imagem ({2}x{3}).", x, y, _width, _height));
            }

            int index = (y * _width + x) * 4;
            return new PixelColor(_pixels[index], _pixels[index + 1], _pixels[index + 2], _pixels[index + 3]);
        }

        public void SetPixel(int x, int y, PixelColor color)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return; // Ignora pixels fora do limite para permitir clipping natural
            }

            int index = (y * _width + x) * 4;
            _pixels[index] = color.R;
            _pixels[index + 1] = color.G;
            _pixels[index + 2] = color.B;
            _pixels[index + 3] = color.A;
        }

        public void Fill(PixelColor color)
        {
            for (int i = 0; i < _pixels.Length; i += 4)
            {
                _pixels[i] = color.R;
                _pixels[i + 1] = color.G;
                _pixels[i + 2] = color.B;
                _pixels[i + 3] = color.A;
            }
        }

        public PixelBuffer Clone()
        {
            byte[] copy = new byte[_pixels.Length];
            Buffer.BlockCopy(_pixels, 0, copy, 0, _pixels.Length);
            return new PixelBuffer(_width, _height, copy);
        }

        /// <summary>
        /// Redimensionamento por Vizinho Mais Próximo (Nearest-Neighbor).
        /// Ideal para pixel art, diagramas e alta velocidade.
        /// </summary>
        public PixelBuffer ResizeNearestNeighbor(int targetWidth, int targetHeight)
        {
            SecurityLimits.AssertImageDimensions(targetWidth, targetHeight);

            if (targetWidth == _width && targetHeight == _height)
            {
                return Clone();
            }

            PixelBuffer result = new PixelBuffer(targetWidth, targetHeight);
            byte[] targetPixels = result.Pixels;

            for (int y = 0; y < targetHeight; y++)
            {
                int srcY = (int)((long)y * _height / targetHeight);
                if (srcY >= _height) srcY = _height - 1;
                int srcRowOffset = srcY * _width * 4;
                int dstRowOffset = y * targetWidth * 4;

                for (int x = 0; x < targetWidth; x++)
                {
                    int srcX = (int)((long)x * _width / targetWidth);
                    if (srcX >= _width) srcX = _width - 1;

                    int srcIdx = srcRowOffset + srcX * 4;
                    int dstIdx = dstRowOffset + x * 4;

                    targetPixels[dstIdx] = _pixels[srcIdx];
                    targetPixels[dstIdx + 1] = _pixels[srcIdx + 1];
                    targetPixels[dstIdx + 2] = _pixels[srcIdx + 2];
                    targetPixels[dstIdx + 3] = _pixels[srcIdx + 3];
                }
            }

            return result;
        }

        /// <summary>
        /// Redimensionamento Bilinear contínuo com interpolação ponderada de 4 pontos.
        /// Suaviza transições sem pixelização grosseira.
        /// </summary>
        public PixelBuffer ResizeBilinear(int targetWidth, int targetHeight)
        {
            SecurityLimits.AssertImageDimensions(targetWidth, targetHeight);

            if (targetWidth == _width && targetHeight == _height)
            {
                return Clone();
            }

            PixelBuffer result = new PixelBuffer(targetWidth, targetHeight);
            byte[] targetPixels = result.Pixels;

            float xRatio = (float)_width / targetWidth;
            float yRatio = (float)_height / targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                float srcY = ((float)y + 0.5f) * yRatio - 0.5f;
                if (srcY < 0.0f) srcY = 0.0f;
                if (srcY > _height - 1) srcY = _height - 1;

                int y0 = (int)Math.Floor(srcY);
                int y1 = Math.Min(_height - 1, y0 + 1);
                float dy = srcY - y0;
                float invDy = 1.0f - dy;

                int row0Offset = y0 * _width * 4;
                int row1Offset = y1 * _width * 4;
                int dstRowOffset = y * targetWidth * 4;

                for (int x = 0; x < targetWidth; x++)
                {
                    float srcX = ((float)x + 0.5f) * xRatio - 0.5f;
                    if (srcX < 0.0f) srcX = 0.0f;
                    if (srcX > _width - 1) srcX = _width - 1;

                    int x0 = (int)Math.Floor(srcX);
                    int x1 = Math.Min(_width - 1, x0 + 1);
                    float dx = srcX - x0;
                    float invDx = 1.0f - dx;

                    float w00 = invDx * invDy;
                    float w10 = dx * invDy;
                    float w01 = invDx * dy;
                    float w11 = dx * dy;

                    int idx00 = row0Offset + x0 * 4;
                    int idx10 = row0Offset + x1 * 4;
                    int idx01 = row1Offset + x0 * 4;
                    int idx11 = row1Offset + x1 * 4;

                    float r = _pixels[idx00] * w00 + _pixels[idx10] * w10 + _pixels[idx01] * w01 + _pixels[idx11] * w11;
                    float g = _pixels[idx00 + 1] * w00 + _pixels[idx10 + 1] * w10 + _pixels[idx01 + 1] * w01 + _pixels[idx11 + 1] * w11;
                    float b = _pixels[idx00 + 2] * w00 + _pixels[idx10 + 2] * w10 + _pixels[idx01 + 2] * w01 + _pixels[idx11 + 2] * w11;
                    float a = _pixels[idx00 + 3] * w00 + _pixels[idx10 + 3] * w10 + _pixels[idx01 + 3] * w01 + _pixels[idx11 + 3] * w11;

                    int dstIdx = dstRowOffset + x * 4;
                    targetPixels[dstIdx] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(r)));
                    targetPixels[dstIdx + 1] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(g)));
                    targetPixels[dstIdx + 2] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(b)));
                    targetPixels[dstIdx + 3] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(a)));
                }
            }

            return result;
        }

        /// <summary>
        /// Recorte de retângulo arbitrário com validação segura de coordenadas e prevenção de overflow.
        /// </summary>
        public PixelBuffer Crop(int x, int y, int width, int height)
        {
            SecurityLimits.AssertImageDimensions(width, height);

            if (x < 0 || y < 0 || width <= 0 || height <= 0 || x > _width - width || y > _height - height)
            {
                throw new ArgumentOutOfRangeException(string.Format(
                    "Área de recorte ({0},{1},{2},{3}) ultrapassa os limites da imagem original ({4}x{5}).",
                    x, y, width, height, _width, _height));
            }

            PixelBuffer result = new PixelBuffer(width, height);
            int rowBytes = checked(width * 4);

            for (int r = 0; r < height; r++)
            {
                int srcOffset = checked(((y + r) * _width + x) * 4);
                int dstOffset = checked(r * rowBytes);
                Buffer.BlockCopy(_pixels, srcOffset, result.Pixels, dstOffset, rowBytes);
            }

            return result;
        }

        /// <summary>
        /// Recorte em nove posições (quadrantes) padronizadas:
        /// top-left, top-center, top-right,
        /// middle-left, center, middle-right,
        /// bottom-left, bottom-center, bottom-right.
        /// </summary>
        public PixelBuffer CropQuadrant(int cropWidth, int cropHeight, string position)
        {
            if (cropWidth > _width) cropWidth = _width;
            if (cropHeight > _height) cropHeight = _height;

            string pos = (position ?? "center").Trim().ToLowerInvariant();

            int x;
            int y;

            switch (pos)
            {
                case "top-left":
                    x = 0;
                    y = 0;
                    break;
                case "top-center":
                case "top":
                    x = (_width - cropWidth) / 2;
                    y = 0;
                    break;
                case "top-right":
                    x = _width - cropWidth;
                    y = 0;
                    break;

                case "middle-left":
                case "center-left":
                case "left":
                    x = 0;
                    y = (_height - cropHeight) / 2;
                    break;
                case "middle-center":
                case "center":
                case "middle":
                default:
                    x = (_width - cropWidth) / 2;
                    y = (_height - cropHeight) / 2;
                    break;
                case "middle-right":
                case "center-right":
                case "right":
                    x = _width - cropWidth;
                    y = (_height - cropHeight) / 2;
                    break;

                case "bottom-left":
                    x = 0;
                    y = _height - cropHeight;
                    break;
                case "bottom-center":
                case "bottom":
                    x = (_width - cropWidth) / 2;
                    y = _height - cropHeight;
                    break;
                case "bottom-right":
                    x = _width - cropWidth;
                    y = _height - cropHeight;
                    break;
            }

            return Crop(x, y, cropWidth, cropHeight);
        }

        /// <summary>
        /// Renderiza marca d'água textual sobre o buffer usando a fonte bitmap 8x8 proprietária.
        /// </summary>
        public void ApplyTextWatermark(string text, int x, int y, PixelColor color, int scale = 1)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (scale < 1) scale = 1;

            int cursorX = x;
            int glyphW = BitmapFont.GlyphWidth * scale;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    y += BitmapFont.GlyphHeight * scale + scale;
                    cursorX = x;
                    continue;
                }

                for (int gy = 0; gy < BitmapFont.GlyphHeight; gy++)
                {
                    for (int gx = 0; gx < BitmapFont.GlyphWidth; gx++)
                    {
                        if (BitmapFont.IsPixelSet(c, gx, gy))
                        {
                            for (int sy = 0; sy < scale; sy++)
                            {
                                int py = y + gy * scale + sy;
                                if (py < 0 || py >= _height) continue;

                                for (int sx = 0; sx < scale; sx++)
                                {
                                    int px = cursorX + gx * scale + sx;
                                    if (px < 0 || px >= _width) continue;

                                    PixelColor current = GetPixel(px, py);
                                    SetPixel(px, py, PixelColor.Blend(current, color));
                                }
                            }
                        }
                    }
                }

                cursorX += glyphW;
            }
        }

        /// <summary>
        /// Aplica marca d'água por imagem com composição alfa (Porter-Duff Source-Over) e opacidade ajustável.
        /// </summary>
        public void ApplyImageWatermark(PixelBuffer watermark, int x, int y, float opacity = 1.0f)
        {
            if (watermark == null) throw new ArgumentNullException("watermark");
            if (opacity <= 0.0f) return;

            int startX = Math.Max(0, x);
            int startY = Math.Max(0, y);
            int endX = Math.Min(_width, x + watermark.Width);
            int endY = Math.Min(_height, y + watermark.Height);

            for (int py = startY; py < endY; py++)
            {
                int wy = py - y;
                for (int px = startX; px < endX; px++)
                {
                    int wx = px - x;
                    PixelColor fg = watermark.GetPixel(wx, wy);
                    PixelColor bg = GetPixel(px, py);
                    SetPixel(px, py, PixelColor.Blend(bg, fg, opacity));
                }
            }
        }
    }
}
