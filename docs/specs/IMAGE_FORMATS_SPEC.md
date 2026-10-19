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

## 5. Limites de Segurança

- **Dimensão Máxima por Eixo:** 16.384 pixels.
- **Contagem Máxima de Pixels:** 64 Megapixels (~256 MB em buffer RGBA).
- **Tamanho Máximo de Arquivo de Imagem:** 250 MB.
- **Proteção contra sobrescrita:** `SafeFileWriter` com sufixo incremental determinístico `(1)`, `(2)`, etc.
