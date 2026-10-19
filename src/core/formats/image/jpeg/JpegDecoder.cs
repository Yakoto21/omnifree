using System;
using System.Collections.Generic;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Decodificador proprietário de imagens JPEG Baseline Sequential DCT (ISO/IEC 10918-1 / ITU-T T.81).
    /// Suporta subamostragem cromática 4:4:4, 4:2:0, 4:2:2 e imagens monocromáticas (Grayscale).
    /// Rejeita com exceções tipadas formatos progressivos, aritméticos e variantes não suportadas.
    /// </summary>
    public static class JpegDecoder
    {
        private class ComponentInfo
        {
            public int Id;
            public int H;
            public int V;
            public int QTableId;
            public int DcTableId;
            public int AcTableId;
        }

        public static PixelBuffer Decode(Stream stream, long maxStreamBytes = SecurityLimits.DefaultMaxImageFileSize)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException("Stream deve suportar leitura.", "stream");

            try
            {
                long length = stream.Length;
                SecurityLimits.AssertFileSize(length, maxStreamBytes);
            }
            catch (NotSupportedException)
            {
                // Se o stream não suporta Length (não-seekable), copia com limite estrito de bytes
                using (var ms = new MemoryStream())
                {
                    byte[] copyBuf = new byte[8192];
                    int read;
                    long totalCopied = 0;
                    while ((read = stream.Read(copyBuf, 0, copyBuf.Length)) > 0)
                    {
                        totalCopied += read;
                        if (totalCopied > maxStreamBytes)
                        {
                            throw new ResourceLimitException(string.Format(
                                "Fluxo JPEG excede o tamanho máximo permitido de {0:N0} bytes.",
                                maxStreamBytes));
                        }
                        ms.Write(copyBuf, 0, read);
                    }
                    ms.Position = 0;
                    return Decode(ms, maxStreamBytes);
                }
            }

            // 1. Valida SOI (Start of Image 0xFFD8)
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            if (b1 != 0xFF || b2 != 0xD8)
            {
                throw new CorruptDataException("Assinatura JPEG inválida: marcador SOI (0xFFD8) não encontrado no início do arquivo.");
            }

            var qTables = new JpegQuantizationTable[4];
            var dcTables = new JpegHuffmanTable[4];
            var acTables = new JpegHuffmanTable[4];

            int width = 0;
            int height = 0;
            int precision = 0;
            int numComponents = 0;
            var components = new List<ComponentInfo>();
            int restartInterval = 0;

            bool foundSof0 = false;
            bool foundSos = false;

            // 2. Loop de leitura de marcadores
            while (!foundSos)
            {
                int marker = ReadNextMarker(stream);
                if (marker == JpegMarker.EOI)
                {
                    throw new CorruptDataException("Marcador EOI encontrado antes dos dados de varredura (SOS).");
                }

                if (marker == JpegMarker.DQT)
                {
                    ReadDqt(stream, qTables);
                }
                else if (marker == JpegMarker.DHT)
                {
                    ReadDht(stream, dcTables, acTables);
                }
                else if (marker == JpegMarker.DRI)
                {
                    restartInterval = ReadDri(stream);
                }
                else if (marker == JpegMarker.SOF0)
                {
                    if (foundSof0)
                    {
                        throw new CorruptDataException("Múltiplos quadros SOF0 encontrados no mesmo fluxo.");
                    }
                    ReadSof0(stream, out width, out height, out precision, out numComponents, components);
                    foundSof0 = true;
                }
                else if (marker == JpegMarker.SOF1 || marker == JpegMarker.SOF2 || marker == JpegMarker.SOF3 ||
                         marker == JpegMarker.SOF5 || marker == JpegMarker.SOF6 || marker == JpegMarker.SOF7 ||
                         marker == JpegMarker.SOF9 || marker == JpegMarker.SOF10 || marker == JpegMarker.SOF11 ||
                         marker == JpegMarker.SOF13 || marker == JpegMarker.SOF14 || marker == JpegMarker.SOF15)
                {
                    throw new UnsupportedFormatException(string.Format(
                        "Perfil JPEG '{0}' não é suportado pelo núcleo do Lodino. Apenas Baseline Sequential DCT (SOF0) é aceito.",
                        JpegMarker.GetName((ushort)marker)));
                }
                else if (marker == JpegMarker.SOS)
                {
                    if (!foundSof0)
                    {
                        throw new CorruptDataException("Marcador SOS encontrado antes do cabeçalho de quadro SOF0.");
                    }
                    ReadSos(stream, components);
                    foundSos = true;
                    break;
                }
                else
                {
                    // Pula marcador auxiliar (APP0..APP15, COM, etc.)
                    SkipMarkerSegment(stream, marker);
                }
            }

            // 3. Valida se tabelas de quantização e Huffman foram devidamente carregadas
            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];
                if (qTables[comp.QTableId] == null)
                {
                    throw new CorruptDataException(string.Format("Tabela de quantização {0} requerida pelo componente {1} não foi definida.", comp.QTableId, comp.Id));
                }
                if (dcTables[comp.DcTableId] == null)
                {
                    throw new CorruptDataException(string.Format("Tabela Huffman DC {0} requerida pelo componente {1} não foi definida.", comp.DcTableId, comp.Id));
                }
                if (acTables[comp.AcTableId] == null)
                {
                    throw new CorruptDataException(string.Format("Tabela Huffman AC {0} requerida pelo componente {1} não foi definida.", comp.AcTableId, comp.Id));
                }
            }

            // 4. Decodificação da Varredura (Scan)
            return DecodeScan(stream, width, height, numComponents, components, qTables, dcTables, acTables, restartInterval);
        }

        private static int ReadNextMarker(Stream stream)
        {
            int b;
            do
            {
                b = stream.ReadByte();
                if (b < 0) throw new CorruptDataException("Fim de fluxo inesperado ao procurar marcador JPEG.");
            } while (b != 0xFF);

            // Descarta bytes 0xFF consecutivos de preenchimento
            do
            {
                b = stream.ReadByte();
                if (b < 0) throw new CorruptDataException("Fim de fluxo inesperado após prefixo 0xFF.");
            } while (b == 0xFF);

            return (0xFF << 8) | b;
        }

        private static int ReadBigEndian16(Stream stream)
        {
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            if (b1 < 0 || b2 < 0) throw new CorruptDataException("Fim de arquivo prematuro ao ler inteiro de 16 bits.");
            return (b1 << 8) | b2;
        }

        private static void SkipMarkerSegment(Stream stream, int marker)
        {
            if (!JpegMarker.HasLength((ushort)marker)) return;
            int length = ReadBigEndian16(stream);
            if (length < 2) throw new CorruptDataException("Comprimento de segmento JPEG inválido (< 2).");
            int payload = length - 2;

            if (stream.CanSeek)
            {
                stream.Seek(payload, SeekOrigin.Current);
            }
            else
            {
                byte[] skipBuf = new byte[Math.Min(payload, 4096)];
                int remaining = payload;
                while (remaining > 0)
                {
                    int read = stream.Read(skipBuf, 0, Math.Min(remaining, skipBuf.Length));
                    if (read <= 0) throw new CorruptDataException("Fim de fluxo ao pular segmento de marcador.");
                    remaining -= read;
                }
            }
        }

        private static void ReadDqt(Stream stream, JpegQuantizationTable[] qTables)
        {
            int length = ReadBigEndian16(stream);
            if (length < 2)
            {
                throw new CorruptDataException("Comprimento de segmento DQT inválido (< 2).");
            }
            int remaining = length - 2;
            if (remaining == 0)
            {
                throw new CorruptDataException("Segmento DQT vazio (comprimento 2 sem tabelas).");
            }

            while (remaining > 0)
            {
                if (remaining < 65) throw new CorruptDataException("Segmento DQT incompleto.");
                int header = stream.ReadByte();
                remaining--;

                int precision = (header >> 4) & 0x0F;
                int tableId = header & 0x0F;

                if (precision != 0)
                {
                    throw new UnsupportedFormatException("Tabelas de quantização de 16 bits não são suportadas pelo perfil Baseline.");
                }
                if (tableId < 0 || tableId > 3)
                {
                    throw new CorruptDataException(string.Format("Identificador de tabela de quantização inválido: {0}.", tableId));
                }

                byte[] zigzag = new byte[64];
                int read = 0;
                while (read < 64)
                {
                    int r = stream.Read(zigzag, read, 64 - read);
                    if (r <= 0) throw new CorruptDataException("Fim de fluxo ao ler tabela DQT.");
                    read += r;
                }
                remaining -= 64;

                qTables[tableId] = JpegQuantizationTable.FromZigzag(zigzag);
            }
        }

        private static void ReadDht(Stream stream, JpegHuffmanTable[] dcTables, JpegHuffmanTable[] acTables)
        {
            int length = ReadBigEndian16(stream);
            if (length < 2)
            {
                throw new CorruptDataException("Comprimento de segmento DHT inválido (< 2).");
            }
            int remaining = length - 2;
            if (remaining == 0)
            {
                throw new CorruptDataException("Segmento DHT vazio (comprimento 2 sem tabelas).");
            }

            while (remaining > 0)
            {
                if (remaining < 17) throw new CorruptDataException("Segmento DHT incompleto.");
                int header = stream.ReadByte();
                remaining--;

                int tableClass = (header >> 4) & 0x0F; // 0 = DC, 1 = AC
                int tableId = header & 0x0F;

                if (tableClass > 1 || tableId > 3)
                {
                    throw new CorruptDataException(string.Format("Parâmetros de tabela DHT inválidos: classe {0}, ID {1}.", tableClass, tableId));
                }

                byte[] bits16 = new byte[16];
                int totalSymbols = 0;
                for (int i = 0; i < 16; i++)
                {
                    int b = stream.ReadByte();
                    if (b < 0) throw new CorruptDataException("Fim de fluxo ao ler contagens DHT.");
                    bits16[i] = (byte)b;
                    totalSymbols += b;
                }
                remaining -= 16;

                if (remaining < totalSymbols) throw new CorruptDataException("Contagem de símbolos DHT excede o comprimento do segmento.");

                byte[] values = new byte[totalSymbols];
                int read = 0;
                while (read < totalSymbols)
                {
                    int r = stream.Read(values, read, totalSymbols - read);
                    if (r <= 0) throw new CorruptDataException("Fim de fluxo ao ler valores DHT.");
                    read += r;
                }
                remaining -= totalSymbols;

                var table = new JpegHuffmanTable(bits16, values);
                if (tableClass == 0)
                {
                    dcTables[tableId] = table;
                }
                else
                {
                    acTables[tableId] = table;
                }
            }
        }

        private static int ReadDri(Stream stream)
        {
            int length = ReadBigEndian16(stream);
            if (length != 4) throw new CorruptDataException("Comprimento de segmento DRI inválido.");
            return ReadBigEndian16(stream);
        }

        private static void ReadSof0(Stream stream, out int width, out int height, out int precision, out int numComponents, List<ComponentInfo> components)
        {
            int length = ReadBigEndian16(stream);
            precision = stream.ReadByte();
            if (precision != 8)
            {
                throw new UnsupportedFormatException(string.Format("Precisão de amostra JPEG de {0} bits não é suportada. Apenas 8 bits é aceito.", precision));
            }

            height = ReadBigEndian16(stream);
            width = ReadBigEndian16(stream);

            SecurityLimits.AssertImageDimensions(width, height);

            numComponents = stream.ReadByte();
            if (numComponents != 1 && numComponents != 3)
            {
                throw new UnsupportedFormatException(string.Format(
                    "JPEG com {0} componentes de cor não é suportado. Apenas 1 (Monocromático/Grayscale) ou 3 (YCbCr) são suportados.",
                    numComponents));
            }

            int expectedLength = 8 + numComponents * 3;
            if (length != expectedLength)
            {
                throw new CorruptDataException(string.Format("Comprimento de segmento SOF0 inválido ({0} bytes, esperado {1}).", length, expectedLength));
            }

            components.Clear();
            for (int i = 0; i < numComponents; i++)
            {
                int id = stream.ReadByte();
                int sampling = stream.ReadByte();
                int qTableId = stream.ReadByte();

                int h = (sampling >> 4) & 0x0F;
                int v = sampling & 0x0F;

                // D-01: ISO/IEC 10918-1 §B.2.2: Hi e Vi devem estar no intervalo 1..4
                if (h < 1 || h > 4 || v < 1 || v > 4)
                {
                    throw new CorruptDataException(string.Format(
                        "Fatores de amostragem inválidos para componente {0}: H={1}, V={2} (devem estar entre 1 e 4).",
                        id, h, v));
                }

                // D-02: Seletor de tabela de quantização deve estar no intervalo 0..3
                if (qTableId < 0 || qTableId > 3)
                {
                    throw new CorruptDataException(string.Format(
                        "Seletor de tabela de quantização inválido para componente {0}: {1} (deve estar entre 0 e 3).",
                        id, qTableId));
                }

                components.Add(new ComponentInfo
                {
                    Id = id,
                    H = h,
                    V = v,
                    QTableId = qTableId
                });
            }
        }

        private static void ReadSos(Stream stream, List<ComponentInfo> components)
        {
            int length = ReadBigEndian16(stream);
            int numScanComponents = stream.ReadByte();

            if (numScanComponents != components.Count)
            {
                throw new UnsupportedFormatException("Varreduras parciais ou progressivas não são suportadas em JPEG Baseline.");
            }

            int expectedLength = 6 + numScanComponents * 2;
            if (length != expectedLength)
            {
                throw new CorruptDataException(string.Format("Comprimento de segmento SOS inválido ({0} bytes, esperado {1}).", length, expectedLength));
            }

            for (int i = 0; i < numScanComponents; i++)
            {
                int compId = stream.ReadByte();
                int tables = stream.ReadByte();

                var comp = components.Find(c => c.Id == compId);
                if (comp == null)
                {
                    throw new CorruptDataException(string.Format("Componente SOS {0} não corresponde aos componentes definidos no SOF0.", compId));
                }

                int dcTableId = (tables >> 4) & 0x0F;
                int acTableId = tables & 0x0F;

                // D-02: Seletores de tabelas Huffman devem estar no intervalo 0..3
                if (dcTableId < 0 || dcTableId > 3 || acTableId < 0 || acTableId > 3)
                {
                    throw new CorruptDataException(string.Format(
                        "Seletores de tabela Huffman inválidos para componente {0}: DC={1}, AC={2} (devem estar entre 0 e 3).",
                        compId, dcTableId, acTableId));
                }

                comp.DcTableId = dcTableId;
                comp.AcTableId = acTableId;
            }

            int ss = stream.ReadByte();
            int se = stream.ReadByte();
            int approx = stream.ReadByte();

            if (ss != 0 || se != 63 || approx != 0)
            {
                throw new UnsupportedFormatException("Parâmetros espectrais SOS não conformes com o perfil Baseline (Ss=0, Se=63, Ah=Al=0).");
            }
        }

        private static PixelBuffer DecodeScan(Stream stream, int width, int height, int numComponents,
            List<ComponentInfo> components, JpegQuantizationTable[] qTables,
            JpegHuffmanTable[] dcTables, JpegHuffmanTable[] acTables, int restartInterval)
        {
            // Valida pré-requisitos: existência das tabelas de quantização e Huffman referenciadas
            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];
                if (qTables[comp.QTableId] == null)
                {
                    throw new CorruptDataException(string.Format(
                        "Tabela de quantização {0} referenciada pelo componente {1} não foi definida no fluxo.",
                        comp.QTableId, comp.Id));
                }
                if (dcTables[comp.DcTableId] == null)
                {
                    throw new CorruptDataException(string.Format(
                        "Tabela Huffman DC {0} referenciada pelo componente {1} não foi definida no fluxo.",
                        comp.DcTableId, comp.Id));
                }
                if (acTables[comp.AcTableId] == null)
                {
                    throw new CorruptDataException(string.Format(
                        "Tabela Huffman AC {0} referenciada pelo componente {1} não foi definida no fluxo.",
                        comp.AcTableId, comp.Id));
                }
            }

            int maxH = 1;
            int maxV = 1;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].H > maxH) maxH = components[i].H;
                if (components[i].V > maxV) maxV = components[i].V;
            }

            // D-01: Valida que maxH e maxV são múltiplos exatos dos fatores de amostragem do componente
            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];
                if ((maxH % comp.H) != 0 || (maxV % comp.V) != 0)
                {
                    throw new CorruptDataException(string.Format(
                        "Fatores de amostragem incompatíveis para componente {0}: maxH={1}, compH={2}, maxV={3}, compV={4}.",
                        comp.Id, maxH, comp.H, maxV, comp.V));
                }
            }

            int mcuWidth = maxH * 8;
            int mcuHeight = maxV * 8;
            int mcuCols = (width + mcuWidth - 1) / mcuWidth;
            int mcuRows = (height + mcuHeight - 1) / mcuHeight;

            int paddedWidth = mcuCols * mcuWidth;
            int paddedHeight = mcuRows * mcuHeight;

            byte[] planeY = new byte[paddedWidth * paddedHeight];
            byte[] planeCb = (numComponents == 3) ? new byte[paddedWidth * paddedHeight] : null;
            byte[] planeCr = (numComponents == 3) ? new byte[paddedWidth * paddedHeight] : null;

            var reader = new JpegBitReader(stream);
            int[] dcPredictors = new int[components.Count];
            int mcuIndex = 0;
            int expectedRestartMarker = JpegMarker.RST0;

            int[] blockZigzag = new int[64];
            float[] blockFloat = new float[64];
            byte[] blockDecoded = new byte[64];

            for (int mcuY = 0; mcuY < mcuRows; mcuY++)
            {
                for (int mcuX = 0; mcuX < mcuCols; mcuX++)
                {
                    // Tratamento de Restart Interval (DRI / RSTm)
                    if (restartInterval > 0 && mcuIndex > 0 && (mcuIndex % restartInterval) == 0)
                    {
                        reader.AlignToByte();
                        // Lê próximo marcador de reinício se não foi capturado
                        int rstMarker = reader.UnreadMarker;
                        if (rstMarker == 0)
                        {
                            rstMarker = ReadNextMarker(stream);
                        }

                        if (rstMarker != expectedRestartMarker)
                        {
                            throw new CorruptDataException(string.Format("Marcador de reinício esperado 0x{0:X4}, encontrado 0x{1:X4}.", expectedRestartMarker, rstMarker));
                        }

                        // Reinicia DC predictors e avança esperado RSTm
                        for (int c = 0; c < dcPredictors.Length; c++) dcPredictors[c] = 0;
                        reader.ResetRestart();

                        expectedRestartMarker++;
                        if (expectedRestartMarker > JpegMarker.RST7)
                        {
                            expectedRestartMarker = JpegMarker.RST0;
                        }
                    }

                    // Processa componentes da MCU
                    for (int c = 0; c < components.Count; c++)
                    {
                        var comp = components[c];
                        var qTable = qTables[comp.QTableId];
                        var dcTable = dcTables[comp.DcTableId];
                        var acTable = acTables[comp.AcTableId];

                        for (int v = 0; v < comp.V; v++)
                        {
                            for (int h = 0; h < comp.H; h++)
                            {
                                // Decodifica bloco 8x8 de coeficientes
                                DecodeBlock(reader, dcTable, acTable, ref dcPredictors[c], blockZigzag);

                                // Desquantização: dezigzag e multiplica pela tabela Q
                                for (int z = 0; z < 64; z++)
                                {
                                    int naturalIndex = JpegQuantizationTable.ZigzagToNatural[z];
                                    blockFloat[naturalIndex] = blockZigzag[z] * qTable.Elements[naturalIndex];
                                }

                                // IDCT 2D
                                FastDct.InverseTransform(blockFloat, blockDecoded);

                                // Armazena amostras decodificadas no plano (D-09: mapeamento explícito por componente)
                                byte[] targetPlane = (c == 0) ? planeY : (c == 1 ? planeCb : planeCr);
                                if (comp.H == maxH && comp.V == maxV)
                                {
                                    // Bloco com amostragem total
                                    int blockPixelX = mcuX * mcuWidth + h * 8;
                                    int blockPixelY = mcuY * mcuHeight + v * 8;
                                    CopyBlockToPlane(blockDecoded, targetPlane, blockPixelX, blockPixelY, paddedWidth);
                                }
                                else
                                {
                                    // Bloco subamostrado (ex: Cb ou Cr em 4:2:0 ou 4:2:2)
                                    int scaleX = maxH / comp.H;
                                    int scaleY = maxV / comp.V;
                                    int blockPixelX = mcuX * mcuWidth + h * (8 * scaleX);
                                    int blockPixelY = mcuY * mcuHeight + v * (8 * scaleY);
                                    CopyAndUpsampleBlockToPlane(blockDecoded, targetPlane, blockPixelX, blockPixelY, paddedWidth, scaleX, scaleY);
                                }
                            }
                        }
                    }

                    mcuIndex++;
                }
            }

            // 5. Monta PixelBuffer RGB reconstruído
            var pixelBuffer = new PixelBuffer(width, height);

            if (numComponents == 1)
            {
                // Monocromático (Grayscale)
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * paddedWidth;
                    for (int x = 0; x < width; x++)
                    {
                        byte lum = planeY[rowOffset + x];
                        pixelBuffer.SetPixel(x, y, new PixelColor(lum, lum, lum, 255));
                    }
                }
            }
            else
            {
                // YCbCr 3 componentes
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * paddedWidth;
                    for (int x = 0; x < width; x++)
                    {
                        byte yVal = planeY[rowOffset + x];
                        byte cbVal = planeCb[rowOffset + x];
                        byte crVal = planeCr[rowOffset + x];

                        byte r, g, b;
                        JpegColorConverter.YCbCrToRgb(yVal, cbVal, crVal, out r, out g, out b);
                        pixelBuffer.SetPixel(x, y, new PixelColor(r, g, b, 255));
                    }
                }
            }

            return pixelBuffer;
        }

        private static void DecodeBlock(JpegBitReader reader, JpegHuffmanTable dcTable, JpegHuffmanTable acTable, ref int dcPredictor, int[] blockZigzag)
        {
            Array.Clear(blockZigzag, 0, 64);

            // 1. Decodifica coeficiente DC
            int dcCategory = dcTable.Decode(reader);
            int dcDiff = 0;
            if (dcCategory > 0)
            {
                int val = reader.Receive(dcCategory);
                dcDiff = JpegBitReader.Extend(val, dcCategory);
            }
            dcPredictor += dcDiff;
            blockZigzag[0] = dcPredictor;

            // 2. Decodifica coeficientes AC (zigzag 1..63)
            int k = 1;
            while (k < 64)
            {
                int symbol = acTable.Decode(reader);
                if (symbol == 0x00)
                {
                    // EOB (End of Block): todos os coeficientes restantes são zero
                    break;
                }

                if (symbol == 0xF0)
                {
                    // D-07: ZRL (Zero Run Length): 16 zeros. O bloco possui 64 coeficientes (0..63).
                    if (k + 16 > 64)
                    {
                        throw new CorruptDataException(string.Format(
                            "Símbolo ZRL (16 zeros) ultrapassa os limites do bloco 8x8 na posição k={0}.", k));
                    }
                    k += 16;
                    continue;
                }

                int run = (symbol >> 4) & 0x0F;
                int size = symbol & 0x0F;

                k += run;
                if (k >= 64)
                {
                    throw new CorruptDataException("Índice de coeficiente AC fora dos limites do bloco 8x8.");
                }

                int val = reader.Receive(size);
                int acVal = JpegBitReader.Extend(val, size);
                blockZigzag[k] = acVal;
                k++;
            }
        }

        private static void CopyBlockToPlane(byte[] block, byte[] plane, int startX, int startY, int planeStride)
        {
            for (int y = 0; y < 8; y++)
            {
                int planeRow = (startY + y) * planeStride + startX;
                int blockRow = y * 8;
                for (int x = 0; x < 8; x++)
                {
                    plane[planeRow + x] = block[blockRow + x];
                }
            }
        }

        private static void CopyAndUpsampleBlockToPlane(byte[] block, byte[] plane, int startX, int startY, int planeStride, int scaleX, int scaleY)
        {
            for (int y = 0; y < 8; y++)
            {
                int blockRow = y * 8;
                for (int sy = 0; sy < scaleY; sy++)
                {
                    int planeRow = (startY + y * scaleY + sy) * planeStride + startX;
                    for (int x = 0; x < 8; x++)
                    {
                        byte sample = block[blockRow + x];
                        for (int sx = 0; sx < scaleX; sx++)
                        {
                            plane[planeRow + x * scaleX + sx] = sample;
                        }
                    }
                }
            }
        }
    }
}
