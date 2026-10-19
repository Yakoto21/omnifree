using System;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Conversão de espaço de cores RGB <-> YCbCr (ITU-R BT.601 / JFIF)
    /// e subsampling/upsampling cromático para 4:4:4, 4:2:2 e 4:2:0.
    /// </summary>
    public static class JpegColorConverter
    {
        /// <summary>
        /// Converte amostras R, G, B em componentes Y, Cb, Cr.
        /// </summary>
        public static void RgbToYCbCr(byte r, byte g, byte b, out byte y, out byte cb, out byte cr)
        {
            // ITU-R BT.601
            double yVal  =  0.299000 * r + 0.587000 * g + 0.114000 * b;
            double cbVal = -0.168736 * r - 0.331264 * g + 0.500000 * b + 128.0;
            double crVal =  0.500000 * r - 0.418688 * g - 0.081312 * b + 128.0;

            y  = (byte)Clamp((int)Math.Round(yVal));
            cb = (byte)Clamp((int)Math.Round(cbVal));
            cr = (byte)Clamp((int)Math.Round(crVal));
        }

        /// <summary>
        /// Converte amostras Y, Cb, Cr em componentes R, G, B.
        /// </summary>
        public static void YCbCrToRgb(byte y, byte cb, byte cr, out byte r, out byte g, out byte b)
        {
            double c = y;
            double d = cb - 128.0;
            double e = cr - 128.0;

            double rVal = c + 1.402000 * e;
            double gVal = c - 0.344136 * d - 0.714136 * e;
            double bVal = c + 1.772000 * d;

            r = (byte)Clamp((int)Math.Round(rVal));
            g = (byte)Clamp((int)Math.Round(gVal));
            b = (byte)Clamp((int)Math.Round(bVal));
        }

        private static int Clamp(int val)
        {
            if (val < 0) return 0;
            if (val > 255) return 255;
            return val;
        }

        /// <summary>
        /// Realiza a subamostragem 4:2:0 de um plano de crominância (média de blocos 2x2).
        /// </summary>
        public static byte[] Subsample420(byte[] plane, int width, int height, out int subWidth, out int subHeight)
        {
            subWidth = (width + 1) / 2;
            subHeight = (height + 1) / 2;
            byte[] subPlane = new byte[subWidth * subHeight];

            for (int sy = 0; sy < subHeight; sy++)
            {
                int y0 = sy * 2;
                int y1 = Math.Min(y0 + 1, height - 1);

                for (int sx = 0; sx < subWidth; sx++)
                {
                    int x0 = sx * 2;
                    int x1 = Math.Min(x0 + 1, width - 1);

                    int p00 = plane[y0 * width + x0];
                    int p01 = plane[y0 * width + x1];
                    int p10 = plane[y1 * width + x0];
                    int p11 = plane[y1 * width + x1];

                    subPlane[sy * subWidth + sx] = (byte)((p00 + p01 + p10 + p11 + 2) >> 2);
                }
            }

            return subPlane;
        }

        /// <summary>
        /// Reconstitui (upsample) um plano de crominância subamostrado 4:2:0 para o tamanho original.
        /// </summary>
        public static byte[] Upsample420(byte[] subPlane, int subWidth, int subHeight, int targetWidth, int targetHeight)
        {
            byte[] fullPlane = new byte[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int sy = Math.Min(y >> 1, subHeight - 1);
                int subRowOffset = sy * subWidth;
                int fullRowOffset = y * targetWidth;

                for (int x = 0; x < targetWidth; x++)
                {
                    int sx = Math.Min(x >> 1, subWidth - 1);
                    fullPlane[fullRowOffset + x] = subPlane[subRowOffset + sx];
                }
            }

            return fullPlane;
        }

        /// <summary>
        /// Reconstitui (upsample) um plano de crominância subamostrado 4:2:2 (2x1) para o tamanho original.
        /// </summary>
        public static byte[] Upsample422(byte[] subPlane, int subWidth, int subHeight, int targetWidth, int targetHeight)
        {
            byte[] fullPlane = new byte[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int sy = Math.Min(y, subHeight - 1);
                int subRowOffset = sy * subWidth;
                int fullRowOffset = y * targetWidth;

                for (int x = 0; x < targetWidth; x++)
                {
                    int sx = Math.Min(x >> 1, subWidth - 1);
                    fullPlane[fullRowOffset + x] = subPlane[subRowOffset + sx];
                }
            }

            return fullPlane;
        }
    }
}
