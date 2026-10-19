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

