using System;
using System.IO;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Codificador proprietário de imagens JPEG Baseline Sequential DCT (ISO/IEC 10918-1 / ITU-T T.81).
    /// Gera arquivos em conformidade rigorosa com JFIF 1.01, com suporte a qualidade configurável (1..100)
    /// e subamostragem cromática 4:2:0 (padrão fotográfico) ou 4:4:4 (máxima fidelidade de cor).
    /// </summary>
    public static class JpegEncoder
    {
        public static void Encode(PixelBuffer image, Stream outputStream, int quality = 85, bool useSubsampling420 = true)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            if (quality < 1) quality = 1;
            if (quality > 100) quality = 100;

            int width = image.Width;
            int height = image.Height;

            // 1. Escala tabelas de quantização pelo fator de qualidade
            var qTableLum = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardLuminance, quality);
            var qTableChr = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardChrominance, quality);

            // Tabelas de Huffman padrão do Anexo K
            var dcTableLum = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            var acTableLum = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);
            var dcTableChr = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
            var acTableChr = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);

            // 2. Marcador SOI (Start of Image 0xFFD8)
            outputStream.WriteByte(0xFF);
            outputStream.WriteByte(0xD8);

            // 3. Marcador APP0 (JFIF 1.01)
            WriteApp0(outputStream);

            // 4. Marcador DQT (Define Quantization Tables)
            WriteDqt(outputStream, qTableLum, qTableChr);

            // 5. Marcador SOF0 (Baseline Sequential DCT Frame Header)
            WriteSof0(outputStream, width, height, useSubsampling420);

            // 6. Marcadores DHT (Define Huffman Tables)
            WriteDht(outputStream, 0, 0, JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            WriteDht(outputStream, 1, 0, JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);
            WriteDht(outputStream, 0, 1, JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
            WriteDht(outputStream, 1, 1, JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);

            // 7. Marcador SOS (Start of Scan)
            WriteSos(outputStream);

            // 8. Codificação da Varredura (Scan)
            EncodeScan(image, outputStream, qTableLum, qTableChr, dcTableLum, acTableLum, dcTableChr, acTableChr, useSubsampling420);

            // 9. Marcador EOI (End of Image 0xFFD9)
            outputStream.WriteByte(0xFF);
            outputStream.WriteByte(0xD9);
            outputStream.Flush();
        }

        private static void WriteBigEndian16(Stream stream, int value)
        {
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        private static void WriteApp0(Stream stream)
        {
            stream.WriteByte(0xFF);
            stream.WriteByte(0xE0);
            WriteBigEndian16(stream, 16); // Comprimento (16 bytes incluindo os 2 do próprio comprimento)

            // Identificador JFIF nulo-terminado
            stream.WriteByte((byte)'J');
            stream.WriteByte((byte)'F');
            stream.WriteByte((byte)'I');
            stream.WriteByte((byte)'F');
            stream.WriteByte(0x00);

            // Versão 1.1
            stream.WriteByte(0x01);
            stream.WriteByte(0x01);

            // Unidade de densidade: 1 = pontos por polegada (DPI)
            stream.WriteByte(0x01);

            // Densidade X e Y (72 DPI)
            WriteBigEndian16(stream, 72);
            WriteBigEndian16(stream, 72);

            // Dimensões de miniatura (0x0)
            stream.WriteByte(0x00);
            stream.WriteByte(0x00);
        }

        private static void WriteDqt(Stream stream, JpegQuantizationTable qLum, JpegQuantizationTable qChr)
        {
            stream.WriteByte(0xFF);
            stream.WriteByte(0xDB);
            WriteBigEndian16(stream, 2 + (1 + 64) * 2); // 132 bytes

            // Tabela 0: Luminância (precisão 8-bit, ID 0)
            stream.WriteByte(0x00);
            byte[] lumZigzag = qLum.ToZigzagBytes();
            stream.Write(lumZigzag, 0, 64);

            // Tabela 1: Crominância (precisão 8-bit, ID 1)
            stream.WriteByte(0x01);
            byte[] chrZigzag = qChr.ToZigzagBytes();
            stream.Write(chrZigzag, 0, 64);
        }

        private static void WriteSof0(Stream stream, int width, int height, bool useSubsampling420)
        {
            stream.WriteByte(0xFF);
            stream.WriteByte(0xC0);
            WriteBigEndian16(stream, 2 + 1 + 2 + 2 + 1 + 3 * 3); // 17 bytes

            stream.WriteByte(0x08); // Precisão de 8 bits por amostra
            WriteBigEndian16(stream, height);
            WriteBigEndian16(stream, width);
            stream.WriteByte(0x03); // 3 componentes (Y, Cb, Cr)

            if (useSubsampling420)
            {
                // Componente Y: ID 1, Subsampling 2x2 (0x22), QTable 0
                stream.WriteByte(0x01);
                stream.WriteByte(0x22);
                stream.WriteByte(0x00);

                // Componente Cb: ID 2, Subsampling 1x1 (0x11), QTable 1
                stream.WriteByte(0x02);
                stream.WriteByte(0x11);
                stream.WriteByte(0x01);

                // Componente Cr: ID 3, Subsampling 1x1 (0x11), QTable 1
                stream.WriteByte(0x03);
                stream.WriteByte(0x11);
                stream.WriteByte(0x01);
            }
            else
            {
                // 4:4:4
                // Componente Y: ID 1, Subsampling 1x1 (0x11), QTable 0
                stream.WriteByte(0x01);
                stream.WriteByte(0x11);
                stream.WriteByte(0x00);

                // Componente Cb: ID 2, Subsampling 1x1 (0x11), QTable 1
                stream.WriteByte(0x02);
                stream.WriteByte(0x11);
                stream.WriteByte(0x01);

                // Componente Cr: ID 3, Subsampling 1x1 (0x11), QTable 1
                stream.WriteByte(0x03);
                stream.WriteByte(0x11);
                stream.WriteByte(0x01);
            }
        }

        private static void WriteDht(Stream stream, int tableClass, int tableId, byte[] bits16, byte[] values)
        {
            stream.WriteByte(0xFF);
            stream.WriteByte(0xC4);
            int length = 2 + 1 + 16 + values.Length;
            WriteBigEndian16(stream, length);

            int header = (tableClass << 4) | (tableId & 0x0F);
            stream.WriteByte((byte)header);
            stream.Write(bits16, 0, 16);
            stream.Write(values, 0, values.Length);
        }

        private static void WriteSos(Stream stream)
        {
            stream.WriteByte(0xFF);
            stream.WriteByte(0xDA);
            WriteBigEndian16(stream, 2 + 1 + 3 * 2 + 3); // 12 bytes

            stream.WriteByte(0x03); // 3 componentes

            // Y: ID 1, DC 0, AC 0
            stream.WriteByte(0x01);
            stream.WriteByte(0x00);

            // Cb: ID 2, DC 1, AC 1
            stream.WriteByte(0x02);
            stream.WriteByte(0x11);

            // Cr: ID 3, DC 1, AC 1
            stream.WriteByte(0x03);
            stream.WriteByte(0x11);

            // Baseline parameters
            stream.WriteByte(0x00); // Ss = 0
            stream.WriteByte(0x3F); // Se = 63
            stream.WriteByte(0x00); // Ah = 0, Al = 0
        }

        private static void EncodeScan(PixelBuffer image, Stream outputStream,
            JpegQuantizationTable qLum, JpegQuantizationTable qChr,
            JpegHuffmanTable dcLum, JpegHuffmanTable acLum,
            JpegHuffmanTable dcChr, JpegHuffmanTable acChr,
            bool useSubsampling420)
        {
            int width = image.Width;
            int height = image.Height;

            // Extrai componentes Y, Cb, Cr de toda a imagem em buffers planares
            byte[] planeY = new byte[width * height];
            byte[] planeCb = new byte[width * height];
            byte[] planeCr = new byte[width * height];

            byte[] pixels = image.Pixels;
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                int pixelRowOffset = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int pIdx = pixelRowOffset + x * 4;
                    byte r = pixels[pIdx];
                    byte g = pixels[pIdx + 1];
                    byte b = pixels[pIdx + 2];

                    byte yVal, cbVal, crVal;
                    JpegColorConverter.RgbToYCbCr(r, g, b, out yVal, out cbVal, out crVal);

                    int idx = rowOffset + x;
                    planeY[idx] = yVal;
                    planeCb[idx] = cbVal;
                    planeCr[idx] = crVal;
                }
            }

            var writer = new JpegBitWriter(outputStream);
            int prevDcY = 0;
            int prevDcCb = 0;
            int prevDcCr = 0;

            float[] blockInput = new float[64];
            float[] blockDct = new float[64];
            int[] blockZigzag = new int[64];

            if (useSubsampling420)
            {
                int mcuCols = (width + 15) / 16;
                int mcuRows = (height + 15) / 16;

                for (int mcuY = 0; mcuY < mcuRows; mcuY++)
                {
                    for (int mcuX = 0; mcuX < mcuCols; mcuX++)
                    {
                        // 1. Quatro blocos de Luminância Y (2x2)
                        // Bloco Y0 (0,0)
                        ExtractBlock8x8(planeY, width, height, mcuX * 16, mcuY * 16, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qLum, dcLum, acLum, ref prevDcY);

                        // Bloco Y1 (1,0)
                        ExtractBlock8x8(planeY, width, height, mcuX * 16 + 8, mcuY * 16, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qLum, dcLum, acLum, ref prevDcY);

                        // Bloco Y2 (0,1)
                        ExtractBlock8x8(planeY, width, height, mcuX * 16, mcuY * 16 + 8, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qLum, dcLum, acLum, ref prevDcY);

                        // Bloco Y3 (1,1)
                        ExtractBlock8x8(planeY, width, height, mcuX * 16 + 8, mcuY * 16 + 8, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qLum, dcLum, acLum, ref prevDcY);

                        // 2. Um bloco Cb subamostrado 2x2 a partir do retângulo 16x16
                        ExtractDownsampledBlock8x8(planeCb, width, height, mcuX * 16, mcuY * 16, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qChr, dcChr, acChr, ref prevDcCb);

                        // 3. Um bloco Cr subamostrado 2x2 a partir do retângulo 16x16
                        ExtractDownsampledBlock8x8(planeCr, width, height, mcuX * 16, mcuY * 16, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qChr, dcChr, acChr, ref prevDcCr);
                    }
                }
            }
            else
            {
                // 4:4:4 (MCUs de 8x8 pixels)
                int mcuCols = (width + 7) / 8;
                int mcuRows = (height + 7) / 8;

                for (int mcuY = 0; mcuY < mcuRows; mcuY++)
                {
                    for (int mcuX = 0; mcuX < mcuCols; mcuX++)
                    {
                        // Bloco Y
                        ExtractBlock8x8(planeY, width, height, mcuX * 8, mcuY * 8, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qLum, dcLum, acLum, ref prevDcY);

                        // Bloco Cb
                        ExtractBlock8x8(planeCb, width, height, mcuX * 8, mcuY * 8, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qChr, dcChr, acChr, ref prevDcCb);

                        // Bloco Cr
                        ExtractBlock8x8(planeCr, width, height, mcuX * 8, mcuY * 8, blockInput);
                        EncodeBlock(writer, blockInput, blockDct, blockZigzag, qChr, dcChr, acChr, ref prevDcCr);
                    }
                }
            }

            writer.Flush();
        }

        private static void ExtractBlock8x8(byte[] plane, int width, int height, int startX, int startY, float[] blockOut)
        {
            for (int y = 0; y < 8; y++)
            {
                int py = Math.Min(startY + y, height - 1);
                int rowOffset = py * width;
                int blockRow = y * 8;

                for (int x = 0; x < 8; x++)
                {
                    int px = Math.Min(startX + x, width - 1);
                    blockOut[blockRow + x] = plane[rowOffset + px];
                }
            }
        }

        private static void ExtractDownsampledBlock8x8(byte[] plane, int width, int height, int startX, int startY, float[] blockOut)
        {
            for (int y = 0; y < 8; y++)
            {
                int py0 = Math.Min(startY + y * 2, height - 1);
                int py1 = Math.Min(py0 + 1, height - 1);
                int row0 = py0 * width;
                int row1 = py1 * width;
                int blockRow = y * 8;

                for (int x = 0; x < 8; x++)
                {
                    int px0 = Math.Min(startX + x * 2, width - 1);
                    int px1 = Math.Min(px0 + 1, width - 1);

                    int p00 = plane[row0 + px0];
                    int p01 = plane[row0 + px1];
                    int p10 = plane[row1 + px0];
                    int p11 = plane[row1 + px1];

                    blockOut[blockRow + x] = (float)((p00 + p01 + p10 + p11 + 2) >> 2);
                }
            }
        }

        private static void EncodeBlock(JpegBitWriter writer, float[] input, float[] dctOut, int[] zigzagOut,
            JpegQuantizationTable qTable, JpegHuffmanTable dcTable, JpegHuffmanTable acTable, ref int prevDc)
        {
            // 1. FDCT 2D
            FastDct.Transform(input, dctOut);

            // 2. Quantização e reordenação Zigzag
            int[] qElements = qTable.Elements;
            for (int u = 0; u < 8; u++)
            {
                int uOffset = u * 8;
                for (int v = 0; v < 8; v++)
                {
                    int naturalIndex = uOffset + v;
                    float coeff = dctOut[naturalIndex];
                    int q = qElements[naturalIndex];
                    int qVal = (int)Math.Round(coeff / q);

                    int zIdx = JpegQuantizationTable.NaturalToZigzag[naturalIndex];
                    zigzagOut[zIdx] = qVal;
                }
            }

            // 3. Codificação do coeficiente DC diferencial
            int dcVal = zigzagOut[0];
            int diff = dcVal - prevDc;
            prevDc = dcVal;
            writer.WriteSignedValue(diff, dcTable);

            // 4. Codificação dos coeficientes AC (zigzag 1..63)
            int zeroRun = 0;
            for (int k = 1; k < 64; k++)
            {
                int acVal = zigzagOut[k];
                if (acVal == 0)
                {
                    zeroRun++;
                }
                else
                {
                    while (zeroRun >= 16)
                    {
                        writer.WriteSymbol(0xF0, acTable); // ZRL (16 zeros)
                        zeroRun -= 16;
                    }

                    int cat = JpegBitWriter.GetCategory(acVal);
                    int symbol = (zeroRun << 4) | cat;
                    writer.WriteSymbol(symbol, acTable);

                    int bits = acVal;
                    if (acVal < 0)
                    {
                        bits = acVal + (1 << cat) - 1;
                    }
                    writer.WriteBits(bits, cat);

                    zeroRun = 0;
                }
            }

            if (zeroRun > 0)
            {
                writer.WriteSymbol(0x00, acTable); // EOB (End of Block)
            }
        }
    }
}
