using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Png
{
    /// <summary>
    /// Validador de assinatura e cabeçalho de imagem PNG (IHDR) conforme perfil alvo do Lodino.
    /// Perfil suportado: 8-bit Truecolor RGB (ColorType 2) e 8-bit Truecolor RGBA (ColorType 6), não entrelaçado.
    /// </summary>
    public class PngHeader
    {
        public static readonly byte[] PngSignature = new byte[8]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
        };

        public const int ColorTypeGrayscale = 0;
        public const int ColorTypeTruecolorRgb = 2;
        public const int ColorTypeIndexed = 3;
        public const int ColorTypeGrayscaleAlpha = 4;
        public const int ColorTypeTruecolorRgba = 6;

        public int Width { get; set; }
        public int Height { get; set; }
        public byte BitDepth { get; set; }
        public byte ColorType { get; set; }
        public byte CompressionMethod { get; set; }
        public byte FilterMethod { get; set; }
        public byte InterlaceMethod { get; set; }

        public bool HasAlpha
        {
            get { return ColorType == ColorTypeTruecolorRgba; }
        }

        public int BytesPerPixel
        {
            get { return HasAlpha ? 4 : 3; }
        }

        public static void ValidateSignature(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            byte[] sig = new byte[8];
            int read = stream.Read(sig, 0, 8);
            if (read < 8)
            {
                throw new CorruptDataException("Arquivo truncado ou menor que a assinatura padrão PNG (8 bytes).");
            }

            for (int i = 0; i < 8; i++)
            {
                if (sig[i] != PngSignature[i])
                {
                    throw new CorruptDataException(string.Format(
                        "Assinatura PNG inválida no byte {0}: 0x{1:X2} (esperado 0x{2:X2}).",
                        i, sig[i], PngSignature[i]));
                }
            }
        }

        public static void WriteSignature(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            stream.Write(PngSignature, 0, 8);
        }

        public static PngHeader FromChunk(PngChunk chunk)
        {
            if (chunk == null) throw new ArgumentNullException("chunk");
            if (chunk.Type != "IHDR")
            {
                throw new CorruptDataException(string.Format(
                    "O primeiro chunk do arquivo PNG deve ser 'IHDR', mas foi encontrado '{0}'.", chunk.Type));
            }
            if (chunk.Data == null || chunk.Data.Length != 13)
            {
                throw new CorruptDataException(string.Format(
                    "Chunk 'IHDR' deve possuir exatamente 13 bytes de dados (encontrado {0}).",
                    chunk.Data != null ? chunk.Data.Length : 0));
            }

            byte[] d = chunk.Data;
            var header = new PngHeader();

            uint w = ((uint)d[0] << 24) | ((uint)d[1] << 16) | ((uint)d[2] << 8) | (uint)d[3];
            uint h = ((uint)d[4] << 24) | ((uint)d[5] << 16) | ((uint)d[6] << 8) | (uint)d[7];

            if (w == 0 || w > int.MaxValue || h == 0 || h > int.MaxValue)
            {
                throw new CorruptDataException(string.Format("Dimensões PNG inválidas: {0}x{1}.", w, h));
            }

            header.Width = (int)w;
            header.Height = (int)h;
            SecurityLimits.AssertImageDimensions(header.Width, header.Height);

            header.BitDepth = d[8];
            header.ColorType = d[9];
            header.CompressionMethod = d[10];
            header.FilterMethod = d[11];
            header.InterlaceMethod = d[12];

            // Validação de conformidade estrita com o perfil anunciado
            if (header.BitDepth != 8)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Profundidade de bits {0} não é suportada no perfil inicial PNG do Lodino. Apenas 8 bits por canal são suportados.",
                    header.BitDepth));
            }

            if (header.ColorType == ColorTypeIndexed)
            {
                throw new UnsupportedFormatException("PNG indexado com paleta de cores (ColorType 3) não é suportado no perfil inicial.");
            }
            if (header.ColorType == ColorTypeGrayscale || header.ColorType == ColorTypeGrayscaleAlpha)
            {
                throw new UnsupportedFormatException(string.Format(
                    "PNG em escala de cinza (ColorType {0}) não é suportado no perfil inicial.", header.ColorType));
            }
            if (header.ColorType != ColorTypeTruecolorRgb && header.ColorType != ColorTypeTruecolorRgba)
            {
                throw new UnsupportedFormatException(string.Format(
                    "ColorType PNG desconhecido ou não suportado: {0}.", header.ColorType));
            }

            if (header.CompressionMethod != 0)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Método de compressão PNG desconhecido: {0} (apenas DEFLATE = 0 é permitido pela norma).",
                    header.CompressionMethod));
            }

            if (header.FilterMethod != 0)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Método de filtro PNG desconhecido: {0} (apenas Adaptive Filtering = 0 é permitido pela norma).",
                    header.FilterMethod));
            }

            if (header.InterlaceMethod != 0)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Entrelaçamento PNG (Adam7 / método {0}) não é suportado no perfil inicial.",
                    header.InterlaceMethod));
            }

            return header;
        }

        public static PngChunk CreateIhdrChunk(int width, int height, bool hasAlpha)
        {
            SecurityLimits.AssertImageDimensions(width, height);

            byte[] data = new byte[13];
            data[0] = (byte)((width >> 24) & 0xFF);
            data[1] = (byte)((width >> 16) & 0xFF);
            data[2] = (byte)((width >> 8) & 0xFF);
            data[3] = (byte)(width & 0xFF);

            data[4] = (byte)((height >> 24) & 0xFF);
            data[5] = (byte)((height >> 16) & 0xFF);
            data[6] = (byte)((height >> 8) & 0xFF);
            data[7] = (byte)(height & 0xFF);

            data[8] = 8; // 8 bits por canal
            data[9] = hasAlpha ? (byte)ColorTypeTruecolorRgba : (byte)ColorTypeTruecolorRgb;
            data[10] = 0; // Compressão DEFLATE padrão
            data[11] = 0; // Filtros adaptativos padrão
            data[12] = 0; // Sem entrelaçamento

            return new PngChunk("IHDR", data);
        }
    }
}
