# LODINO — ESPECIFICAÇÃO TÉCNICA DO MOTOR DE IMAGENS (BMP)
**Módulo:** `src/core/formats/image/`  
**Status:** Implementado, Auditado e Testado  
**Identificador do Motor:** `image-bmp`

---

## 1. Visão Geral

O motor de imagem do Lodino foi desenvolvido inteiramente em código original C#, operando sobre buffers lineares de bytes puros. Ele substitui dependências externas de conversão e processamento de imagens (como GDI+, `System.Drawing`, `sharp`, `ImageMagick` ou wrappers executáveis), fornecendo controle total sobre conformidade binária, alocação de memória e segurança.

---

## 2. Padrão BMP Suportado

O motor implementa o padrão Microsoft Windows Device Independent Bitmap (DIB), com foco estrito nas variantes de 24 e 32 bits não comprimidas.

### 2.1 Estruturas Binárias

1. **`BITMAPFILEHEADER` (14 bytes):**
   - `bfType` (2 bytes): Identificador mágico `0x4D42` ("BM" em little-endian).
   - `bfSize` (4 bytes): Tamanho total do arquivo em bytes.
   - `bfReserved1` / `bfReserved2` (4 bytes): Reservados (0).
   - `bfOffBits` (4 bytes): Deslocamento absoluto até o início da matriz de pixels (padrão: 54 bytes).

2. **`BITMAPINFOHEADER` (40 bytes):**
   - `biSize` (4 bytes): Tamanho do cabeçalho DIB (deve ser exatamente 40).
   - `biWidth` (4 bytes): Largura da imagem em pixels ($> 0$).
   - `biHeight` (4 bytes): Altura da imagem:
     - Positiva ($> 0$): Orientação padrão **Bottom-up** (origem no canto inferior esquerdo).
     - Negativa ($< 0$): Orientação **Top-down** (origem no canto superior esquerdo).
   - `biPlanes` (2 bytes): Planos de cor (deve ser 1).
   - `biBitCount` (2 bytes): Profundidade de bits (24 ou 32 bits).
   - `biCompression` (4 bytes): Compressão (deve ser 0 = `BI_RGB`).
   - `biSizeImage` (4 bytes): Tamanho em bytes da área de pixels ($stride \times |height|$).
   - `biXPelsPerMeter` / `biYPelsPerMeter` (8 bytes): Resolução física (~2835 ppm / 72 DPI).
   - `biClrUsed` / `biClrImportant` (8 bytes): Tabela de cores (0 para BI_RGB).

### 2.2 Cálculo de Stride e Padding

Cada scanline do BMP deve ser alinhada em múltiplos de 4 bytes (limite de 32 bits):

$$\text{stride} = \left\lfloor \frac{\text{width} \times \text{bitCount} + 31}{32} \right\rfloor \times 4$$
$$\text{rowDataBytes} = \text{width} \times \frac{\text{bitCount}}{8}$$
$$\text{paddingBytes} = \text{stride} - \text{rowDataBytes}$$

- **24-bit (BGR):** 3 bytes por pixel. O padding varia entre 0 e 3 bytes por linha.
- **32-bit (BGRA/BGRX):** 4 bytes por pixel. O stride é sempre $4 \times \text{width}$, com zero padding.

### 2.3 Tratamento de Orientação e Semântica de 32 bits

1. **Orientação:**
   - **Bottom-up:** O scanline 0 do arquivo corresponde à linha $H - 1$ da imagem renderizada. A leitura inverte a ordem vertical.
   - **Top-down:** O scanline 0 do arquivo corresponde à linha 0 da imagem. A leitura segue a ordem direta.
2. **Semântica Estrita de 32-bit BI_RGB (BGRX Opaco):**
   - Na especificação padrão de `BITMAPINFOHEADER` (40 bytes) com compressão `BI_RGB` (0), os pixels de 32 bits são definidos pelo padrão Windows DIB como `BGRX`, onde o quarto byte (`X`) é um byte de alinhamento reservado e não define um canal alfa normativo.
   - O decodificador trata todos os pixels de BMP 32-bit `BI_RGB` como estritamente opacos, normalizando o canal alfa em memória para 255 independentemente de o byte reservado conter `0x00`, `0xFF` ou qualquer valor intermediário. Nenhuma heurística ad-hoc é empregada como regra de conformidade.
   - O suporte a transparência em BMP fica explicitamente reservado para uma etapa posterior, na qual variantes com máscaras de bits (`BI_BITFIELDS`) ou cabeçalhos estendidos (`BITMAPV4HEADER` / `BITMAPV5HEADER`) forem devidamente implementadas e validadas.

---

## 3. Variantes Não Suportadas e Política de Rejeição

O motor rejeita deliberadamente variantes não implementadas, emitindo exceções tipadas:

| Variante / Cenário | Código / Indicador | Tratamento / Exceção |
|---|---|---|
| Imagem com Paleta Indexada | 1 bpp, 4 bpp, 8 bpp | `UnsupportedFormatException` |
| BMP de 16 bits | 16 bpp | `UnsupportedFormatException` |
| Compressão RLE | `BI_RLE8` (1), `BI_RLE4` (2) | `UnsupportedFormatException` |
| Bitfields / Huffman / PNG | `BI_BITFIELDS` (3), `BI_JPEG` (4), `BI_PNG` (5) | `UnsupportedFormatException` |
| Cabeçalho DIB não-Windows | `biSize != 40` (OS/2, V4, V5) | `UnsupportedFormatException` |
| Assinatura Inválida | `bfType != 0x4D42` | `CorruptDataException` |
| Dimensões Inválidas | Largura $\le 0$ ou Altura $= 0$ | `CorruptDataException` |
| Arquivo Truncado | `Length < bfOffBits + stride * H` | `CorruptDataException` |
| Dimensão Excessiva | Largura ou Altura $> 16.384$ px | `ResourceLimitException` |

---

## 4. Algoritmos de Processamento Gráfico

### 4.1 Redimensionamento

1. **Vizinho Mais Próximo (Nearest-Neighbor):**
   - Mapeamento direto de coordenadas inteiras:
     $$x_{src} = \left\lfloor \frac{x_{dst} \times W_{src}}{W_{dst}} \right\rfloor, \quad y_{src} = \left\lfloor \frac{y_{dst} \times H_{src}}{H_{dst}} \right\rfloor$$
   - Ideal para pixel art, diagramas e gráficos com arestas duras.

2. **Interpolação Bilinear Contínua:**
   - Mapeamento centrado no pixel:
     $$u = (x + 0.5) \times \frac{W_{src}}{W_{dst}} - 0.5, \quad v = (y + 0.5) \times \frac{H_{src}}{H_{dst}} - 0.5$$
   - Ponderação de 4 vizinhos com clamp nos limites:
     $$P(x,y) = (1-dx)(1-dy)P_{00} + dx(1-dy)P_{10} + (1-dx)dy P_{01} + dx dy P_{11}$$
   - Suaviza gradientes e reduz artefatos de serrilhado.

### 4.2 Recorte (Cropping)

- **Retângulo Arbitrário:** `Crop(x, y, width, height)` com validação estrita de fronteiras.
- **9 Quadrantes:** `top-left`, `top-center`, `top-right`, `middle-left`, `center`, `middle-right`, `bottom-left`, `bottom-center`, `bottom-right`.

### 4.3 Marca d'Água Textual (BitmapFont 8x8)

- Rasterizador bitmap monocromático embutido cobrindo a faixa ASCII imprimível (códigos 32 a 126).
- Suporte a escalas inteiras ($1\times, 2\times, 3\times, \dots$).
- Renderização com mesclagem alfa direta sobre o fundo.

### 4.4 Marca d'Água de Imagem (Composição Alfa)

- Implementação da fórmula de composição Porter-Duff *Source-Over*:
  $$A_{fg} = \alpha_{watermark} \times opacity$$
  $$A_{out} = A_{fg} + A_{bg} \times (1 - A_{fg})$$
  $$C_{out} = \frac{C_{fg} \times A_{fg} + C_{bg} \times A_{bg} \times (1 - A_{fg})}{A_{out}}$$

---

## 5. Limites de Segurança e Proteções de Hardening

- **Dimensão Máxima por Eixo:** 16.384 pixels (`MaxImageDimension`).
- **Contagem Máxima de Pixels:** 64 Megapixels (`MaxImagePixelCount` = 67.108.864 pixels, ~256 MB em buffer RGBA).
- **Tamanho Máximo de Arquivo de Imagem:** 250 MB (`DefaultMaxImageFileSize` para arquivos no disco ou em fluxo de entrada).
- **Teto Máximo de Raster Descomprimido:** ~268,44 MB (`MaxDecompressedRasterBytes` = $\text{MaxImagePixelCount} \times 4 + \text{MaxImageDimension}$), contemplando o byte de tipo de filtro por scanline em imagens até 16.384 linhas.
- **Proteção Ativa contra Decompression Bombs (Zip Bombs):**
  - O `PngDecoder` calcula de forma prévia e estrita os bytes necessários para o raster através de `SecurityLimits.CalculateExpectedScanlineBytes(width, height, bytesPerPixel)`.
  - Esse total esperado é repassado ao `ZlibStream.Decompress` como teto máximo de descompressão (`maxOutputBytes`), impedindo que payloads maliciosos altamente comprimidos expandam memória descontroladamente para além do necessário para as dimensões declaradas. Fluxos anômalos são abortados imediatamente com `ResourceLimitException`.
- **Particionamento de Chunks IDAT na Codificação:**
  - O `PngEncoder` particiona o fluxo comprimido ZLIB em chunks `IDAT` consecutivos de no máximo 32 KB (32.768 bytes), garantindo conformidade com a recomendação da ISO/IEC 15948 e eliminando chunks monolíticos gigantes.
- **Tamanho Máximo por Chunk PNG:** 64 MB (`MaxChunkSize` em `PngChunk.cs`).
- **Resiliência a Fluxos Fragmentados e Não-Seekable:**
  - Leitura contínua em laço via `StreamUtils.ReadExactOrThrow`, garantindo que streams lentos, de rede ou fragmentados não disparem falsos erros de integridade.
  - Navegação em streams sem alocação de buffers arbitrários via `StreamUtils.SkipBytes` (buffer pequeno fixo de 4 KB em streams não-navegáveis).
- **Proteção Contra Overflow em Recorte:**
  - `PixelBuffer.Crop` utiliza validação por subtração (`x > _width - width`) imune a wrap-around aritmético em valores elevados de coordenadas e lança `ArgumentOutOfRangeException`.
- **Proteção contra sobrescrita:** `SafeFileWriter` com sufixo incremental determinístico `(1)`, `(2)`, etc.

---

## 6. Especificação Técnica do Motor PNG (`image-png`)

**Módulo:** `src/core/formats/image/png/`  
**Status:** Implementado, Auditado e Testado  
**Identificador do Motor:** `image-png`  
**Normas de Referência:** ISO/IEC 15948:2004 / W3C PNG 1.2, RFC 1950 (ZLIB), RFC 1951 (DEFLATE).

### 6.1 Perfil Suportado e Conformidade
1. **Assinatura e Validação:**
   - Assinatura mágica obrigatória de 8 bytes: `0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A` (`\x89PNG\r\n\x1a\n`).
2. **Perfis de Cor Alvo (8 bits por canal):**
   - **Truecolor RGB (ColorType 2):** 3 canais de 8 bits (24 bpp). Totalmente opaco ($A=255$).
   - **Truecolor RGBA (ColorType 6):** 4 canais de 8 bits (32 bpp). Canal alfa contínuo $[0, 255]$ preservado com exatidão.
3. **Não-Entrelaçado:** Apenas imagens de varredura linear convencional (`InterlaceMethod = 0`). Adam7 é rejeitado estritamente com `UnsupportedFormatException`.

### 6.2 Integridade e Algoritmos de Checksum
- **CRC-32 (ISO 3309 / ITU-T V.42):**
  - Polinômio padrão `0xEDB88320` (representação invertida de $x^{32} + x^{26} + x^{23} + \dots$).
  - Tabela pré-computada de 256 entradas de 32 bits.
  - Calculado sobre o tipo do chunk (4 bytes) concatenado com os dados do chunk.
- **Adler-32 (RFC 1950):**
  - Checksum aritmético de 32 bits composto por duas somas módulo 65521:
    $$s_1 = \left( 1 + \sum_{i=1}^{n} D_i \right) \bmod 65521, \quad s_2 = \left( n + \sum_{i=1}^{n} (n - i + 1) D_i \right) \bmod 65521$$
    $$\text{Adler32} = (s_2 \ll 16) \mid s_1$$
  - Acumulação por blocos de até 5552 bytes sem overflow de 32 bits antes da redução modular.

### 6.3 Descompressão e Compressão DEFLATE / ZLIB Próprias
1. **Camada ZLIB (RFC 1950):**
   - Validação estrita do cabeçalho de 2 bytes:
     - `CMF`: Método de compressão deve ser DEFLATE (`CM = 8`), tamanho da janela $\le 32$ KB (`CINFO <= 7`).
     - `FLG`: Verificação `(CMF * 256 + FLG) % 31 == 0` (FCHECK). Dicionários predefinidos (`FDICT != 0`) são rejeitados conforme a norma PNG.
   - Verificação do checksum Adler-32 de 4 bytes no final do fluxo após a descompressão de todos os blocos.
2. **Descompressão DEFLATE (RFC 1951):**
   - Leitor de bits LSB-first com buffer de 64 bits e alinhamento de bytes para blocos armazenados.
   - **Blocos Não Comprimidos / Armazenados (BTYPE 00):** Validação de `NLEN == ~LEN` e cópia direta para a saída e histórico circular de 32 KB.
   - **Blocos com Huffman Fixo (BTYPE 01):** Árvore estática pré-computada conforme RFC 1951 § 3.2.6 (literais 0..143 de 8 bits, 144..255 de 9 bits, 256..279 de 7 bits, 280..287 de 8 bits; distâncias 0..31 de 5 bits).
   - **Blocos com Huffman Dinâmico (BTYPE 10):**
     - Leitura de `HLIT` (257..286), `HDIST` (1..32) e `HCLEN` (4..19).
     - Árvore de comprimentos de código gerada a partir da ordem de permutação canônica de 19 símbolos (`16, 17, 18, 0, 8, 7, ...`).
     - Decodificação dos comprimentos de literais e distâncias com suporte aos códigos de repetição 16 (3..6 vezes), 17 (3..10 zeros) e 18 (11..138 zeros).
     - Construção canônica de árvores binárias para literais e distâncias.
     - Decodificação LZ77 com janela deslizante de 32 KB e tabelas completas de bits extras de comprimento (códigos 257..285) e distância (códigos 0..29).
3. **Compressão ZLIB / DEFLATE Proprietária:**
   - Emissão de cabeçalho ZLIB padronizado (`CMF = 0x78`, `FLG = 0x01`).
   - Fragmentação em blocos DEFLATE não comprimidos (BTYPE 00) de até 65.535 bytes com `LEN` e `NLEN`.
   - Cálculo e escrita do checksum Adler-32 Big-Endian no encerramento do fluxo.

### 6.4 Filtros de Scanline (Reconstrução e Codificação)
A especificação PNG define 5 filtros aplicados scanline a scanline sobre bytes prévios:

| Tipo | Nome | Fórmula de Defiltragem (Decodificação) | Fórmula de Filtragem (Codificação) |
|---|---|---|---|
| 0 | **None** | $\text{Recon}(x) = \text{Filt}(x)$ | $\text{Filt}(x) = \text{Orig}(x)$ |
| 1 | **Sub** | $\text{Recon}(x) = \text{Filt}(x) + \text{Recon}(x - bpp)$ | $\text{Filt}(x) = \text{Orig}(x) - \text{Orig}(x - bpp)$ |
| 2 | **Up** | $\text{Recon}(x) = \text{Filt}(x) + \text{Prior}(x)$ | $\text{Filt}(x) = \text{Orig}(x) - \text{Prior}(x)$ |
| 3 | **Average** | $\text{Recon}(x) = \text{Filt}(x) + \lfloor (\text{Recon}(x - bpp) + \text{Prior}(x)) / 2 \rfloor$ | $\text{Filt}(x) = \text{Orig}(x) - \lfloor (\text{Orig}(x - bpp) + \text{Prior}(x)) / 2 \rfloor$ |
| 4 | **Paeth** | $\text{Recon}(x) = \text{Filt}(x) + \text{PaethPredictor}(a, b, c)$ | $\text{Filt}(x) = \text{Orig}(x) - \text{PaethPredictor}(a, b, c)$ |

- O preditor de Paeth calcula $p = a + b - c$ e seleciona o vizinho mais próximo de $p$ com desempate determinístico em $a$, depois $b$, depois $c$.
- Todas as operações aritméticas são calculadas em módulo 256 sem overflow de tipo.

### 6.5 Ciclo de Vida de Chunks e Fragmentação de IDAT
- **`IHDR`:** Obrigatoriamente o primeiro chunk após a assinatura de 8 bytes. Define largura, altura, profundidade de bits, tipo de cor, compressão (0), filtro (0) e entrelaçamento (0).
- **Múltiplos Chunks `IDAT` Consecutivos:** A norma PNG permite que o fluxo ZLIB comprimido seja fatiado em múltiplos blocos `IDAT`. O `PngDecoder` acumula continuamente os dados de todos os chunks `IDAT` consecutivos até o encerramento do fluxo, validando que nenhum chunk `IDAT` surja após outros chunks intermediários.
- **`IEND`:** Chunk final de terminação com 0 bytes de dados (`CRC = 0xAE426082`).
- **Chunks Críticos vs. Auxiliares:** Qualquer chunk desconhecido com a primeira letra maiúscula (crítico) é rejeitado com `UnsupportedFormatException`. Chunks auxiliares (primeira letra minúscula) são ignorados de forma segura.

### 6.6 Política de Rejeição e Segurança do Motor PNG

| Cenário / Variante | Indicador / Causa | Exceção Lançada |
|---|---|---|
| Assinatura PNG Inválida | Primeiros 8 bytes $\ne$ `89 50 4E 47 0D 0A 1A 0A` | `CorruptDataException` |
| Arquivo Truncado | Fim inesperado do stream durante cabeçalho ou chunks | `CorruptDataException` |
| Falha de Integridade CRC-32 | CRC calculado $\ne$ CRC registrado no chunk | `CorruptDataException` |
| Falha de Integridade Adler-32 | Checksum Adler do ZLIB $\ne$ valor final esperado | `CorruptDataException` |
| Paleta Indexada | `ColorType = 3` | `UnsupportedFormatException` |
| Escala de Cinza / Cinza+Alfa | `ColorType = 0` ou `ColorType = 4` | `UnsupportedFormatException` |
| Profundidade $\ne$ 8 bits | `BitDepth` 1, 2, 4 ou 16 bits | `UnsupportedFormatException` |
| Entrelaçamento Adam7 | `InterlaceMethod != 0` | `UnsupportedFormatException` |
| Dimensões Inválidas | Largura $\le 0$ ou Altura $\le 0$ | `CorruptDataException` |
| Dimensão Excessiva | Largura ou Altura $> 16.384$ px | `ResourceLimitException` |
| Chunk Gigante (DoS) | Tamanho do chunk $> 64$ MB | `ResourceLimitException` |
| Chunk Crítico Desconhecido | Tipo FourCC com primeira letra maiúscula não suportado | `UnsupportedFormatException` |
| Chunk `IHDR` Duplicado | Mais de um chunk `IHDR` no mesmo arquivo | `CorruptDataException` |
| Ausência de Chunks `IDAT` | Arquivo sem dados de imagem comprimidos | `CorruptDataException` |

### 6.7 Rotas de Conversão e Integração no Registro
O motor `image-png` integra-se ao `EngineRegistry`, habilitando:
1. `png -> png`: Re-filtragem, compressão, recorte, redimensionamento (nearest/bilinear) e marcas d'água de texto/imagem.
2. `bmp -> png`: Conversão nativa de bitmaps Windows (24-bit e 32-bit BGRX) para PNG Truecolor com suporte a marcas d'água.
3. `png -> bmp`: Conversão de PNG Truecolor (RGB e RGBA) para BMP não comprimido de 24 bits ou 32 bits com fidelidade visual completa.



---

## 7. ESPECIFICAÇÃO TÉCNICA DO MOTOR DE IMAGENS JPEG

**Módulo:** `src/core/formats/image/jpeg/`  
**Status:** Implementado, Auditado e Testado  
**Identificador do Motor:** `image-jpeg`

### 7.1 Visão Geral e Normas Adotadas

O motor de imagem JPEG do Lodino foi projetado e desenvolvido inteiramente em código original C#, com independência absoluta de bibliotecas de terceiros (sem `libjpeg`, `stb_image`, `System.Drawing`, `WPF` ou executáveis externos).
Ele implementa o perfil **Baseline Sequential DCT** de 8 bits por amostra, em conformidade com:
1. **ISO/IEC 10918-1 / ITU-T T.81:** Compressão digital e codificação de imagens estáticas de tom contínuo.
2. **JFIF 1.01 (JPEG File Interchange Format):** Formatação de encapsulamento com segmento APP0 e densidade de amostragem.

### 7.2 Marcadores JPEG Suportados e Estrutura de Fluxo

O motor reconhece e processa os seguintes marcadores binários:

| Marcador | Código | Descrição | Tratamento no Lodino |
|---|---|---|---|
| `SOI` | `0xFFD8` | Start of Image | Validação obrigatória da assinatura nos 2 primeiros bytes |
| `EOI` | `0xFFD9` | End of Image | Marcação de encerramento da imagem e validação de término |
| `SOF0` | `0xFFC0` | Baseline Sequential DCT | Leitura de dimensões, componentes, precisão de 8 bits e fatores de amostragem |
| `DQT` | `0xFFDB` | Define Quantization Table(s) | Carregamento de tabelas de 8 bits (IDs 0..3) e desquantização Zigzag |
| `DHT` | `0xFFC4` | Define Huffman Table(s) | Carregamento de tabelas canônicas DC e AC (IDs 0..3) |
| `DRI` | `0xFFDD` | Define Restart Interval | Leitura de intervalo de reinício $Ri$ em unidades MCU |
| `RST0`..`RST7` | `0xFFD0`..`0xFFD7` | Restart Markers | Reinício síncrono dos preditores DC e alinhamento de bytes |
| `APP0` | `0xFFE0` | JFIF Application Segment | Escrita na codificação e salto seguro na decodificação |
| `APP1`..`APP15` | `0xFFE1`..`0xFFEF` | Marcadores de Aplicação (EXIF, ICC, etc.) | Salto seguro lendo o campo de comprimento de 16 bits |
| `COM` | `0xFFFE` | Comentário | Salto seguro lendo o campo de comprimento de 16 bits |
| `SOS` | `0xFFDA` | Start of Scan | Mapeamento de componentes para tabelas Huffman e início do fluxo de entropia |

### 7.3 Espaço de Cor e Fatores de Subamostragem

1. **Conversão de Espaço de Cores (ITU-R BT.601 / JFIF):**
   - **RGB para YCbCr (Codificação):**
     $$Y = 0.299000 R + 0.587000 G + 0.114000 B$$
     $$Cb = -0.168736 R - 0.331264 G + 0.500000 B + 128$$
     $$Cr = 0.500000 R - 0.418688 G - 0.081312 B + 128$$
   - **YCbCr para RGB (Decodificação):**
     $$R = \text{Clamp}(Y + 1.402000 (Cr - 128))$$
     $$G = \text{Clamp}(Y - 0.344136 (Cb - 128) - 0.714136 (Cr - 128))$$
     $$B = \text{Clamp}(Y + 1.772000 (Cb - 128))$$
2. **Esquemas de Subamostragem Suportados:**
   - **4:4:4:** Amostragem integral ($H=1, V=1$ para Y, Cb e Cr; MCUs de 8x8 pixels). Máxima fidelidade cromática.
   - **4:2:0:** Subamostragem $2 \times 2$ nos planos de crominância ($H=2, V=2$ para Y, $H=1, V=1$ para Cb/Cr; MCUs de 16x16 pixels). Padrão fotográfico com alta eficiência de compressão.
   - **4:2:2:** Subamostragem horizontal $2 \times 1$ ($H=2, V=1$ para Y; MCUs de 16x8 pixels).
   - **Grayscale (Monocromático):** 1 componente Y ($H=1, V=1$; MCUs de 8x8 pixels). R, G e B mapeados diretamente para a luminância com $A=255$.

### 7.4 Transformada Discreta de Cosseno 2D (FDCT e IDCT)

A transformação 2D sobre blocos de 8x8 amostras utiliza decomposição matricial ortogonal separável:

$$F = M \cdot (A - 128) \cdot M^T \quad \text{(FDCT Direct)}$$
$$A = \text{Clamp}(M^T \cdot F \cdot M + 128) \quad \text{(IDCT Inverse)}$$

Onde a matriz de cossenos ortogonais $M[u, x]$ é pré-calculada com precisão de ponto flutuante:
$$M[u, x] = \frac{1}{2} C(u) \cos\left[\frac{(2x + 1) u \pi}{16}\right], \quad C(u) = \begin{cases} \frac{1}{\sqrt{2}} & \text{se } u = 0 \\ 1 & \text{se } u > 0 \end{cases}$$

Essa formulação garante reversibilidade exata com erro numérico inferior a $10^{-4}$ unidades float e elimina transposição acidental de eixos.

### 7.5 Quantização e Escalonamento de Qualidade (IJG Standard)

1. **Tabelas do Anexo K:** Utilização das tabelas de referência K.1 (Luminância) e K.2 (Crominância).
2. **Escala de Fator de Qualidade ($Q \in [1, 100]$):**
   - Se $Q < 50$: $\text{scale} = \lfloor 5000 / Q \rfloor$
   - Se $Q \ge 50$: $\text{scale} = 200 - 2 \times Q$
   - Elemento escalado: $\text{scaledVal} = \text{Clamp}\left(\left\lfloor \frac{\text{baseVal} \times \text{scale} + 50}{100} \right\rfloor, 1, 255\right)$
3. **Mapeamento Zigzag:** 64 coeficientes organizados diagonalmente por ordem crescente de frequência espacial. A ordenação Zigzag e seu inverso natural são demonstradamente bijetivos para todos os índices de 0 a 63.

### 7.6 Codificação por Entropia (Huffman) e Byte-Stuffing

1. **Manipulação de Bits:**
   - Leitor e escritor de fluxo de bits com orientação MSB-first.
   - **Byte-Stuffing:** Em qualquer emissão do byte `0xFF` no fluxo de entropia codificado, o byte nulo `0x00` é automaticamente inserido. Na decodificação, `0xFF 0x00` é reconvertido de forma transparente para `0xFF`.
   - **Encerramento da Varredura:** O escritor alinha o último byte à fronteira com bits `1` conforme ISO/IEC 10918-1 §B.1.1.5.
2. **Procedimento de Decodificação (ITU-T T.81 Anexo F):**
   - Decodificação canônica estruturada em tabelas `mincode`, `maxcode` e `valptr` para códigos de até 16 bits.
   - Decodificação diferencial DC com restauração por magnitude signed (`EXTEND`).
   - Decodificação AC com suporte a EOB (`0x00`) e salto de 16 zeros ZRL (`0xF0`).

### 7.7 Política de Rejeição e Segurança do Motor JPEG

| Cenário / Variante | Indicador / Causa | Exceção Lançada |
|---|---|---|
| Assinatura JPEG Ausente | Primeiros 2 bytes $\ne$ `0xFF 0xD8` (SOI) | `CorruptDataException` |
| Perfil Progressivo | `SOF2` (`0xFFC2`) ou `SOF6` (`0xFFC6`) | `UnsupportedFormatException` |
| Perfil Aritmético | `SOF9`, `SOF10`, `SOF11` | `UnsupportedFormatException` |
| Perfil Sem Perdas (Lossless) | `SOF3`, `SOF7` | `UnsupportedFormatException` |
| Precisão Diferente de 8 bits | $P \ne 8$ no cabeçalho SOF0 (ex: 12-bit, 16-bit) | `UnsupportedFormatException` |
| Formato 4 Componentes (CMYK/YCCK) | $N_f = 4$ em SOF0 | `UnsupportedFormatException` |
| Dimensões Inválidas | Largura $\le 0$ ou Altura $\le 0$ | `CorruptDataException` |
| Dimensão Excessiva | Largura ou Altura $> 16.384$ px | `ResourceLimitException` |
| Arquivo Truncado | Fim prematuro do stream antes de EOI ou dados de scan | `CorruptDataException` |
| Símbolo Huffman Inválido | Sequência de bits não contida na árvore canônica | `CorruptDataException` |
| Tabela de Quantização Inexistente | Componente referencia tabela Q não definida | `CorruptDataException` |

### 7.8 Rotas de Conversão e Interoperabilidade

O motor `image-jpeg` registra-se no `EngineRegistry` e atende:
1. `jpeg/jpg -> jpeg/jpg`: Recodificação, qualidade configurável, recorte em 9 posições, redimensionamento nearest/bilinear e marcas d'água de texto/imagem.
2. `jpeg/jpg -> bmp`: Decodificação nativa para BMP 24-bit ou 32-bit BGRX.
3. `bmp -> jpeg/jpg`: Codificação de bitmaps Windows para JPEG com qualidade e subamostragem selecionáveis.
4. `jpeg/jpg -> png`: Conversão para PNG Truecolor de 24 bits.
5. `png -> jpeg/jpg`: Conversão de PNG Truecolor (com mescla sobreposta de transparência) para JPEG.

### 7.9 Organização de Varreduras (Scans): Baseline Single-Scan e Multiscan Não-Intercalado

Conforme a norma ISO/IEC 10918-1 / ITU-T T.81 (§4.8, §4.10, §A.2.3 e §B.2.3), o processo Baseline Sequential DCT (SOF0) suporta dados organizados tanto em varredura única intercalada quanto em múltiplas varreduras independentes:

1. **Varredura Única Intercalada ($N_s = N_f$):**
   - Todos os componentes (Y, Cb, Cr) são multiplexados bloco a bloco em cada Unidade Mínima Codificada (MCU) espacial.
   - Padrão predominante em fotos digitais de alta compressão.
2. **Múltiplas Varreduras Não-Intercaladas ($N_s = 1$ por Scan):**
   - Cada componente do quadro SOF0 é codificado em sua própria varredura SOS independente (ex: Scan 1 = Y, Scan 2 = Cb, Scan 3 = Cr).
   - Em conformidade com ITU-T T.81 §A.2.3: Quando $N_s = 1$, **1 MCU equivale a exatamente 1 bloco 8x8 de dados**.
   - A grade de blocos de cada componente é calculada a partir de suas dimensões nativas:
     $$X_i = \left\lceil \frac{\text{width} \times H_i}{H_{max}} \right\rceil, \quad Y_i = \left\lceil \frac{\text{height} \times V_i}{V_{max}} \right\rceil$$
     $$\text{blocksX}_i = \left\lceil \frac{X_i}{8} \right\rceil, \quad \text{blocksY}_i = \left\lceil \frac{Y_i}{8} \right\rceil$$
3. **Preditores DC e Intervalos de Reinício (DRI / RSTm):**
   - Conforme ITU-T T.81 §F.1.1.5.1, o preditor DC (`PRED`) é obrigatoriamente inicializado em 0 no início de **cada** scan SOS.
   - Em varreduras não-intercaladas ($N_s = 1$), os marcadores de reinício (`RST0`..`RST7`) operam na cadência de $R_i$ blocos 8x8.
   - Em varreduras intercaladas ($N_s > 1$), operam na cadência de $R_i$ MCUs completas.
4. **Segmentos Intercalados entre Scans:**
   - Tabelas de quantização (`DQT`), tabelas de Huffman (`DHT`) e intervalos de reinício (`DRI`) podem surgir entre varreduras consecutivas, sendo absorvidos e atualizados dinamicamente pelo decodificador.
5. **Fronteira Arquitetural e Limitações Conhecidas:**
   - Scans intercalados parciais subamostrados ($1 < N_s < N_f$, como um scan isolado de Cb+Cr com subamostragem 4:2:0) utilizam a dimensão global de MCU do SOF0. Scans progressivos com parâmetros espectrais ($S_s \ne 0 \lor S_e \ne 63$) são formalmente rejeitados com `UnsupportedFormatException`.

