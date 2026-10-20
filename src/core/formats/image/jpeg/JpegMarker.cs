using System;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Constantes e utilitários para identificadores de marcadores JPEG (ITU-T T.81 / ISO/IEC 10918-1).
    /// Todos os marcadores JPEG consistem no byte de prefixo 0xFF seguido de um código de marcador não nulo.
    /// </summary>
    public static class JpegMarker
    {
        public const byte Prefix = 0xFF;

        // Marcadores de início e fim de quadro / imagem
        public const ushort SOI = 0xFFD8;   // Start of Image
        public const ushort EOI = 0xFFD9;   // End of Image
        public const ushort SOS = 0xFFDA;   // Start of Scan

        // Tabelas e Parâmetros
        public const ushort DQT = 0xFFDB;   // Define Quantization Table(s)
        public const ushort DHT = 0xFFC4;   // Define Huffman Table(s)
        public const ushort DAC = 0xFFCC;   // Define Arithmetic Coding conditioning
        public const ushort DRI = 0xFFDD;   // Define Restart Interval
        public const ushort DNL = 0xFFDC;   // Define Number of Lines

        // Quadros (Start of Frame)
        public const ushort SOF0 = 0xFFC0;  // Baseline DCT (huffman)
        public const ushort SOF1 = 0xFFC1;  // Extended Sequential DCT (huffman)
        public const ushort SOF2 = 0xFFC2;  // Progressive DCT (huffman)
        public const ushort SOF3 = 0xFFC3;  // Lossless (sequential, huffman)
        public const ushort SOF5 = 0xFFC5;  // Differential Sequential DCT (huffman)
        public const ushort SOF6 = 0xFFC6;  // Differential Progressive DCT (huffman)
        public const ushort SOF7 = 0xFFC7;  // Differential Lossless (huffman)
        public const ushort JPG = 0xFFC8;   // Reserved for JPEG extensions
        public const ushort SOF9 = 0xFFC9;  // Extended Sequential DCT (arithmetic)
        public const ushort SOF10 = 0xFFCA; // Progressive DCT (arithmetic)
        public const ushort SOF11 = 0xFFCB; // Lossless (arithmetic)
        public const ushort SOF13 = 0xFFCD; // Differential Sequential DCT (arithmetic)
        public const ushort SOF14 = 0xFFCE; // Differential Progressive DCT (arithmetic)
        public const ushort SOF15 = 0xFFCF; // Differential Lossless (arithmetic)

        // Marcadores de Reinício (Restart)
        public const ushort RST0 = 0xFFD0;
        public const ushort RST1 = 0xFFD1;
        public const ushort RST2 = 0xFFD2;
        public const ushort RST3 = 0xFFD3;
        public const ushort RST4 = 0xFFD4;
        public const ushort RST5 = 0xFFD5;
        public const ushort RST6 = 0xFFD6;
        public const ushort RST7 = 0xFFD7;

        // Marcadores de Aplicação (APP0..APP15)
        public const ushort APP0 = 0xFFE0;  // JFIF / JFXX
        public const ushort APP1 = 0xFFE1;  // EXIF / XMP
        public const ushort APP2 = 0xFFE2;  // ICC Profile / FlashPix
        public const ushort APP14 = 0xFFEE; // Adobe color transform
        public const ushort APP15 = 0xFFEF;

        // Comentários e outros
        public const ushort COM = 0xFFFE;   // Comment
        public const ushort TEM = 0xFF01;   // Temporary

        public static bool IsRestart(ushort marker)
        {
            return marker >= RST0 && marker <= RST7;
        }

        public static bool IsApp(ushort marker)
        {
            return marker >= APP0 && marker <= APP15;
        }

        public static bool HasLength(ushort marker)
        {
            // Marcadores independentes que NÃO possuem campo de comprimento de 2 bytes
            if (marker == SOI || marker == EOI || marker == TEM || IsRestart(marker))
            {
                return false;
            }
            return true;
        }

        public static string GetName(ushort marker)
        {
            switch (marker)
            {
                case SOI: return "SOI (Start of Image)";
                case EOI: return "EOI (End of Image)";
                case SOS: return "SOS (Start of Scan)";
                case DQT: return "DQT (Define Quantization Table)";
                case DHT: return "DHT (Define Huffman Table)";
                case DRI: return "DRI (Define Restart Interval)";
                case SOF0: return "SOF0 (Baseline Sequential DCT)";
                case SOF1: return "SOF1 (Extended Sequential DCT)";
                case SOF2: return "SOF2 (Progressive DCT)";
                case SOF3: return "SOF3 (Lossless Huffman)";
                case SOF9: return "SOF9 (Extended Sequential Arithmetic)";
                case SOF10: return "SOF10 (Progressive Arithmetic)";
                case SOF11: return "SOF11 (Lossless Arithmetic)";
                case APP0: return "APP0 (JFIF)";
                case APP1: return "APP1 (EXIF)";
                case APP2: return "APP2 (ICC)";
                case APP14: return "APP14 (Adobe)";
                case COM: return "COM (Comment)";
                default:
                    if (IsRestart(marker)) return string.Format("RST{0}", marker - RST0);
                    if (IsApp(marker)) return string.Format("APP{0}", marker - APP0);
                    return string.Format("0x{0:X4}", marker);
            }
        }
    }
}
