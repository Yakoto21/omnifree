using System;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Implementação da Transformada Discreta de Cosseno 2D (FDCT) e Transformada Inversa (IDCT)
    /// para blocos 8x8 padrão JPEG (ISO/IEC 10918-1 / ITU-T T.81).
    /// Utiliza decomposição 1D separável ortogonal precalculada, garantindo precisão matemática rigorosa.
    /// </summary>
    public static class FastDct
    {
        // Matriz de transformação ortogonal 8x8 precalculada M[u, x]
        // M[u, x] = 0.5 * C(u) * cos((2x + 1) * u * PI / 16)
        private static readonly float[,] M = new float[8, 8];

        static FastDct()
        {
            double sqrt2Inv = 1.0 / Math.Sqrt(2.0);
            for (int u = 0; u < 8; u++)
            {
                double cu = (u == 0) ? sqrt2Inv : 1.0;
                for (int x = 0; x < 8; x++)
                {
                    double angle = (2 * x + 1) * u * Math.PI / 16.0;
                    M[u, x] = (float)(0.5 * cu * Math.Cos(angle));
                }
            }
        }

        /// <summary>
        /// Aplica FDCT 2D em um bloco 8x8 de amostras: F = M * (input - 128) * M^T.
        /// input: matriz 8x8 row-major [0..255].
        /// output: matriz 8x8 de coeficientes de frequência DCT.
        /// </summary>
        public static void Transform(float[] input, float[] output)
        {
            // Passo 1: T = (input - 128) * M^T
            float[] temp = new float[64];

            for (int r = 0; r < 8; r++)
            {
                int rOffset = r * 8;
                for (int v = 0; v < 8; v++)
                {
                    float sum = 0.0f;
                    for (int c = 0; c < 8; c++)
                    {
                        sum += (input[rOffset + c] - 128.0f) * M[v, c];
                    }
                    temp[rOffset + v] = sum;
                }
            }

            // Passo 2: F = M * T
            for (int u = 0; u < 8; u++)
            {
                int uOffset = u * 8;
                for (int v = 0; v < 8; v++)
                {
                    float sum = 0.0f;
                    for (int r = 0; r < 8; r++)
                    {
                        sum += M[u, r] * temp[r * 8 + v];
                    }
                    output[uOffset + v] = sum;
                }
            }
        }

        /// <summary>
        /// Aplica IDCT 2D em um bloco 8x8 de coeficientes: A = M^T * input * M + 128.
        /// input: coeficientes 8x8 row-major.
        /// output: amostras espaciais 8x8 com deslocamento de nível +128 e clamp para [0..255].
        /// </summary>
        public static void InverseTransform(float[] input, byte[] output, int outOffset = 0, int outStride = 8)
        {
            // Passo 1: T = input * M
            float[] temp = new float[64];

            for (int u = 0; u < 8; u++)
            {
                int uOffset = u * 8;
                for (int c = 0; c < 8; c++)
                {
                    float sum = 0.0f;
                    for (int v = 0; v < 8; v++)
                    {
                        sum += input[uOffset + v] * M[v, c];
                    }
                    temp[uOffset + c] = sum;
                }
            }

            // Passo 2: A = M^T * T + 128
            for (int r = 0; r < 8; r++)
            {
                int lineStart = outOffset + r * outStride;
                for (int c = 0; c < 8; c++)
                {
                    float sum = 0.0f;
                    for (int u = 0; u < 8; u++)
                    {
                        sum += M[u, r] * temp[u * 8 + c];
                    }

                    int sample = (int)Math.Round(sum + 128.0f);
                    if (sample < 0) sample = 0;
                    else if (sample > 255) sample = 255;

                    output[lineStart + c] = (byte)sample;
                }
            }
        }

        /// <summary>
        /// Aplica IDCT 2D diretamente em um buffer float de saída (sem clamp para byte),
        /// útil para análises numéricas e testes de precisão.
        /// </summary>
        public static void InverseTransformFloat(float[] input, float[] output)
        {
            // Passo 1: T = input * M
            float[] temp = new float[64];

            for (int u = 0; u < 8; u++)
            {
                int uOffset = u * 8;
                for (int c = 0; c < 8; c++)
                {
                    float sum = 0.0f;
                    for (int v = 0; v < 8; v++)
                    {
                        sum += input[uOffset + v] * M[v, c];
                    }
                    temp[uOffset + c] = sum;
                }
            }

            // Passo 2: A = M^T * T + 128
            for (int r = 0; r < 8; r++)
            {
                int rOffset = r * 8;
                for (int c = 0; c < 8; c++)
                {
                    float sum = 0.0f;
                    for (int u = 0; u < 8; u++)
                    {
                        sum += M[u, r] * temp[u * 8 + c];
                    }
                    output[rOffset + c] = sum + 128.0f;
                }
            }
        }
    }
}
