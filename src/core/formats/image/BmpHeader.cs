using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image
{
    public class BmpHeader
    {
        // BITMAPFILEHEADER (14 bytes)
        public const ushort BmpMagic = 0x4D42; // "BM"
        public const int FileHeaderSize = 14;
        public const int InfoHeaderSize = 40;
        public const int StandardHeaderSize = 54; // 14 + 40

        public uint FileSize { get; set; }
        public ushort Reserved1 { get; set; }
        public ushort Reserved2 { get; set; }
        public uint OffBits { get; set; }

        // BITMAPINFOHEADER (40 bytes)
        public uint HeaderSize { get; set; }
        public int Width { get; set; }
        public int RawHeight { get; set; } // Pode ser negativo (top-down)
        public ushort Planes { get; set; }
        public ushort BitCount { get; set; }
        public uint Compression { get; set; }
        public uint SizeImage { get; set; }
        public int XPelsPerMeter { get; set; }
        public int YPelsPerMeter { get; set; }
        public uint ClrUsed { get; set; }
        public uint ClrImportant { get; set; }

        public int Height
        {
            get { return Math.Abs(RawHeight); }
        }

        public bool IsTopDown
        {
            get { return RawHeight < 0; }
        }

        public int Stride
        {
            get { return CalculateStride(Width, BitCount); }
        }

        public static int CalculateStride(int width, int bitCount)
        {
            return checked(((width * bitCount + 31) / 32) * 4);
        }

        public static BmpHeader Read(BinaryReader reader, long totalStreamLength)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (totalStreamLength < StandardHeaderSize)
            {
                throw new CorruptDataException(string.Format(
                    "Arquivo BMP corrompido ou truncado: tamanho total ({0} bytes) é insuficiente para cabeçalhos BMP válidos (mínimo {1} bytes).",
                    totalStreamLength, StandardHeaderSize));
            }

            var header = new BmpHeader();

            // 1. BITMAPFILEHEADER
            ushort magic = reader.ReadUInt16();
            if (magic != BmpMagic)
            {
                throw new CorruptDataException(string.Format(
                    "Assinatura BMP inválida: 0x{0:X4} (esperado 0x4D42 'BM').", magic));
            }

            header.FileSize = reader.ReadUInt32();
            header.Reserved1 = reader.ReadUInt16();
            header.Reserved2 = reader.ReadUInt16();
            header.OffBits = reader.ReadUInt32();

            if (header.OffBits < StandardHeaderSize)
            {
                throw new CorruptDataException(string.Format(
                    "Deslocamento de dados de pixel (bfOffBits = {0}) é menor que o cabeçalho mínimo ({1} bytes).",
                    header.OffBits, StandardHeaderSize));
            }

            if (header.OffBits >= totalStreamLength)
            {
                throw new CorruptDataException(string.Format(
                    "Deslocamento de dados de pixel (bfOffBits = {0}) aponta para fora do arquivo ({1} bytes).",
                    header.OffBits, totalStreamLength));
            }

            // 2. BITMAPINFOHEADER
            header.HeaderSize = reader.ReadUInt32();
            if (header.HeaderSize != InfoHeaderSize)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Variante de cabeçalho DIB com tamanho {0} bytes não é suportada. Apenas BITMAPINFOHEADER padrão Windows (40 bytes) é suportado.",
                    header.HeaderSize));
            }

            header.Width = reader.ReadInt32();
            header.RawHeight = reader.ReadInt32();

            if (header.Width <= 0)
            {
                throw new CorruptDataException(string.Format(
                    "Largura BMP inválida: {0} (deve ser estritamente positiva).", header.Width));
            }

            if (header.RawHeight == 0 || header.RawHeight == int.MinValue)
            {
                throw new CorruptDataException("Altura BMP inválida: não pode ser zero nem valor mínimo de inteiro.");
            }

            SecurityLimits.AssertImageDimensions(header.Width, header.Height);

            header.Planes = reader.ReadUInt16();
            if (header.Planes != 1)
            {
                throw new CorruptDataException(string.Format(
                    "Número de planos inválido no BMP: {0} (esperado 1).", header.Planes));
            }

            header.BitCount = reader.ReadUInt16();
            if (header.BitCount == 1 || header.BitCount == 4 || header.BitCount == 8)
            {
                throw new UnsupportedFormatException(string.Format(
                    "BMP indexado com paleta de cores ({0} bpp) não é suportado. Apenas variantes 24-bit e 32-bit BI_RGB são suportadas.",
                    header.BitCount));
            }

            if (header.BitCount != 24 && header.BitCount != 32)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Profundidade de cor {0} bpp não é suportada para BMP. Apenas 24 bits (BGR) e 32 bits (BGRA) não comprimidos são suportados.",
                    header.BitCount));
            }

            header.Compression = reader.ReadUInt32();
            if (header.Compression != 0) // BI_RGB = 0
            {
                string compName;
                switch (header.Compression)
                {
                    case 1: compName = "BI_RLE8"; break;
                    case 2: compName = "BI_RLE4"; break;
                    case 3: compName = "BI_BITFIELDS"; break;
                    case 4: compName = "BI_JPEG"; break;
                    case 5: compName = "BI_PNG"; break;
                    default: compName = "Código " + header.Compression; break;
                }
                throw new UnsupportedFormatException(string.Format(
                    "Compressão BMP '{0}' não é suportada. Apenas BI_RGB não comprimido (0) é suportado.",
                    compName));
            }

            header.SizeImage = reader.ReadUInt32();
            header.XPelsPerMeter = reader.ReadInt32();
            header.YPelsPerMeter = reader.ReadInt32();
            header.ClrUsed = reader.ReadUInt32();
            header.ClrImportant = reader.ReadUInt32();

            // Validação de overflow no cálculo de tamanho
            int stride = header.Stride;
            long requiredPixelBytes = checked((long)stride * header.Height);
            long totalRequired = checked((long)header.OffBits + requiredPixelBytes);

            if (totalRequired > totalStreamLength)
            {
                throw new CorruptDataException(string.Format(
                    "Arquivo BMP truncado: o arquivo possui {0} bytes, mas requer ao menos {1} bytes para os dados de pixel calculados ({2}x{3}, stride {4}).",
                    totalStreamLength, totalRequired, header.Width, header.Height, stride));
            }

            return header;
        }

        public void Write(BinaryWriter writer, int width, int height, ushort bitCount)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            SecurityLimits.AssertImageDimensions(width, height);
            if (bitCount != 24 && bitCount != 32)
            {
                throw new ArgumentException("Apenas 24 ou 32 bits são suportados para codificação BMP.", "bitCount");
            }

            int stride = CalculateStride(width, bitCount);
            uint pixelDataSize = checked((uint)(stride * height));
            uint offBits = StandardHeaderSize;
            uint totalFileSize = checked(offBits + pixelDataSize);

            // BITMAPFILEHEADER (14 bytes)
            writer.Write(BmpMagic);           // bfType (2)
            writer.Write(totalFileSize);      // bfSize (4)
            writer.Write((ushort)0);          // bfReserved1 (2)
            writer.Write((ushort)0);          // bfReserved2 (2)
            writer.Write(offBits);            // bfOffBits (4)

            // BITMAPINFOHEADER (40 bytes)
            writer.Write((uint)InfoHeaderSize); // biSize (4)
            writer.Write(width);              // biWidth (4)
            writer.Write(height);             // biHeight (4, positivo = bottom-up)
            writer.Write((ushort)1);          // biPlanes (2)
            writer.Write(bitCount);           // biBitCount (2)
            writer.Write((uint)0);            // biCompression = BI_RGB (4)
            writer.Write(pixelDataSize);      // biSizeImage (4)
            writer.Write(2835);               // biXPelsPerMeter (~72 DPI) (4)
            writer.Write(2835);               // biYPelsPerMeter (~72 DPI) (4)
            writer.Write((uint)0);            // biClrUsed (4)
            writer.Write((uint)0);            // biClrImportant (4)
        }
    }
}
