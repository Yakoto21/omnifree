# ESPECIFICAÇÃO TÉCNICA NORMATIVA — MOTOR DE ÁUDIO PCM RIFF/WAVE PROPRIETÁRIO
**Projeto:** Lodino Native Core  
**Módulo:** `src/core/formats/audio/` e `src/core/formats/audio/wav/`  
**Data:** 19 de Outubro de 2026  
**Status:** Hardened, Auditado e Integrado (Fase 11B — PCM Q31 Bit-Perfect)  
**Conformidade:** IBM/Microsoft RIFF/WAVE Specification (1991), Microsoft Multimedia Standards Update, RFC 2361.

---

## 1. Visão Geral e Princípios Arquiteturais

O motor de áudio do Lodino foi projetado do zero em C# (.NET Framework 4.0 BCL) para fornecer suporte nativo e autônomo ao formato de áudio PCM não comprimido (RIFF/WAVE), eliminando integralmente a necessidade de ferramentas externas (como FFmpeg) ou bibliotecas de terceiros (como NAudio, Bass ou DirectX).

### 1.1 Princípios Inegociáveis
1. **Zero Dependências Externas:** O assembly `Lodino.Core.dll` depende estritamente de `mscorlib.dll`, `System.dll` e `System.Core.dll`.
2. **Isolamento de Domínio:** O modelo de dados de áudio (`AudioBuffer`) é 100% desacoplado do modelo raster de imagens (`PixelBuffer`). Conceitos visuais e acústicos não se misturam.
3. **Fidelidade Matemática Estrita (Bit-Perfect Q31):** Amostras de 8, 16, 24 e 32 bits são armazenadas internamente em ponto fixo planar Q31 (`int[][]`), garantindo preservação exata byte a byte (bit-perfect) em operações de round-trip e passthrough sem perdas de arredondamento inerentes a ponto flutuante de 32 bits.
4. **Resiliência e Segurança em Profundidade:** Rejeição estrita de variantes não suportadas (ADPCM, IEEE Float, A-law, Mu-law), cabeçalhos truncados, chunks `fmt ` duplicados, chunks auxiliares desmesurados em fluxos não-navegáveis e proteção de memória via tetos estritos em `SecurityLimits`.

---

## 2. Estrutura Binária do Contêiner RIFF/WAVE

O contêiner RIFF (*Resource Interchange File Format*) utiliza ordenação de bytes *Little-Endian* em blocos denominados *chunks*.

### 2.1 Cabeçalho Principal RIFF (12 bytes)
| Offset | Tamanho | Campo | Tipo | Valor Esperado / Descrição |
|---|---|---|---|---|
| `0x00` | 4 bytes | `FourCC` | ASCII | `"RIFF"` (`0x46464952`) |
| `0x04` | 4 bytes | `RiffSize` | uint32 (LE) | Tamanho total do arquivo menos 8 bytes (`4 + ChunksSize`) |
| `0x08` | 4 bytes | `Format` | ASCII | `"WAVE"` (`0x45564157`) |

**Delimitação do contêiner:** o parser utiliza `RiffEnd = 8 + RiffSize` como fronteira exclusiva. Todo chunk, incluindo seu byte de preenchimento, deve terminar dentro dessa fronteira. Em streams navegáveis, um `RiffEnd` superior ao tamanho físico disponível é rejeitado como truncamento. Bytes adicionais depois de `RiffEnd` são permitidos e ignorados, pois não pertencem ao contêiner declarado; essa política aceita tags ou dados externos ao RIFF sem tratá-los como áudio. Em streams não navegáveis, `Stream.Length` não é exigido: os limites declarados são aplicados durante a leitura e o truncamento físico é detectado quando os bytes correspondentes são consumidos. O conteúdo posterior ao chunk `data` não é varrido pela decodificação.

### 2.2 Estrutura Geral de Chunks
Cada chunk RIFF possui o seguinte leiaute:
```
+-------------------------------------------------------+
| ChunkId (4 bytes FourCC)                              |
+-------------------------------------------------------+
| ChunkSize (4 bytes uint32 LE)                         |
+-------------------------------------------------------+
| Data (ChunkSize bytes)                                |
+-------------------------------------------------------+
| [Pad Byte 0x00 se ChunkSize % 2 != 0]                 |
+-------------------------------------------------------+
```
> **Regra Normativa de Alinhamento (Padding):** Todo chunk cujo `ChunkSize` seja um número ímpar deve ser seguido imediatamente por 1 byte de preenchimento (`0x00`) para manter o alinhamento de 16 bits (Word Alignment). Esse byte de preenchimento não é contabilizado no campo `ChunkSize`.

### 2.3 Chunk Obrigatório de Formatação (`"fmt "`)
Identificador: `"fmt "` (`0x20746D66`).
Para PCM linear não comprimido, o tamanho padrão é de 16 bytes:
| Campo | Tamanho | Descrição | Restrição Lodino Core |
|---|---|---|---|
| `wFormatTag` | 2 bytes | Código do formato | `1` (`WAVE_FORMAT_PCM`) ou `0xFFFE` (`EXTENSIBLE`) |
| `nChannels` | 2 bytes | Número de canais | `1` (Mono) ou `2` (Estéreo) (suporte até 8) |
| `nSamplesPerSec` | 4 bytes | Taxa de amostragem (Hz) | `8.000 Hz` a `192.000 Hz` |
| `nAvgBytesPerSec`| 4 bytes | Taxa de transferência em bytes/s | `nSamplesPerSec * nBlockAlign` |
| `nBlockAlign` | 2 bytes | Alinhamento de quadro em bytes | `nChannels * (wBitsPerSample / 8)` |
| `wBitsPerSample` | 2 bytes | Bits por amostra por canal | `8`, `16`, `24` ou `32` bits |

> **Hardening D-WAV-01:** O decodificador rejeita explicitamente fluxos com múltiplos chunks `"fmt "`, impedindo confusão de estado ou ataques de polimorfismo de cabeçalho.

#### Suporte a `WAVE_FORMAT_EXTENSIBLE` (`0xFFFE`) e Hardening D-WAV-05
Arquivos WAV gravados por estações de áudio modernas (DAWs) frequentemente utilizam a tag `0xFFFE` mesmo para áudio PCM, estendendo o chunk `"fmt "` para 40 bytes com um GUID de 16 bytes:
- SubFormat GUID do PCM: `00000001-0000-0010-8000-00AA00389B71`.
- **Validações Obrigatórias de Hardening:**
  1. O campo `cbSize` deve ser $\ge 22$ bytes (`validBits` [2] + `channelMask` [4] + `subFormat` GUID [16]). Se `cbSize < 22`, o arquivo é sumariamente rejeitado com `CorruptDataException`.
  2. O campo `cbSize` não pode exceder os bytes de extensão disponíveis no chunk `fmt ` após o próprio campo `cbSize`; bytes adicionais além da extensão declarada são ignorados para preservar compatibilidade.
  3. O campo `wValidBitsPerSample` não pode exceder `wBitsPerSample`. Se `validBits > BitsPerSample`, o cabeçalho é rejeitado como corrompido.
  4. Se o GUID corresponder ao PCM, o formato é normalizado para PCM canônico.

### 2.4 Chunk Obrigatório de Amostras (`"data"`) e Hardening D-WAV-07
Identificador: `"data"` (`0x61746164`).
- `ChunkSize`: Comprimento total em bytes dos dados de áudio brutos.
- Contagem total de quadros de amostra: $\text{FrameCount} = \frac{\text{ChunkSize}}{\text{BlockAlign}}$.
- Duração em segundos: $\text{Duration} = \frac{\text{FrameCount}}{\text{SampleRate}}$.
- **Diferenciação Semântica de Limites (D-WAV-07):** O limite `MaxSingleChunkSize` (64 MB) aplica-se estritamente aos blocos auxiliares de metadados, nunca ao bloco primário de áudio `"data"`. O chunk de áudio é delimitado por:
  1. `SecurityLimits.DefaultMaxAudioFileSize` (500 MB) para tamanho em disco / container RIFF.
  2. `SecurityLimits.MaxAudioMemoryBytes` (400 MB) para expansão descompactada em memória RAM (`canais * quadros * sizeof(int)`), validado preventivamente antes de qualquer alocação.
- **Validação de Alinhamento de Quadros:** O tamanho em bytes do chunk `"data"` deve ser um múltiplo exato do alinhamento de bloco (`DataSize % BlockAlign == 0`). Se houver bytes residuais ou incompletos, o arquivo é imediatamente rejeitado com `CorruptDataException`.

### 2.5 Tratamento de Chunks Auxiliares e Hardening D-WAV-04
Chunks como `"LIST"`, `"JUNK"`, `"bext"`, `"id3 "` ou metadados informativos são saltados de maneira segura e indexados no dicionário de metadados da inspeção (`FormatInspection.Metadata`).
- **Teto Individual:** Nenhum chunk auxiliar individual pode exceder `SecurityLimits.MaxSingleChunkSize` (64 MB).
- **Teto Acumulado:** O total acumulado de bytes de chunks auxiliares antes do `"data"` não pode exceder `SecurityLimits.MaxAuxiliaryHeaderBytes` (16 MB).
- **Teto de Quantidade:** No máximo `SecurityLimits.MaxAudioChunkCount` (4.096) chunks são processados, incluindo `fmt `, `data` e auxiliares. O limite também cobre chunks vazios e restringe o custo de cabeçalhos, strings e entradas de metadados.
- **Resiliência a Fluxos Não-Navegáveis (`CanSeek = false`):** Na rotina `SkipBytes`, caso o stream encerre prematuramente (`read <= 0` com `remaining > 0`), o decodificador lança imediatamente `CorruptDataException`, eliminando qualquer risco de loop infinito ou travamento de I/O.

---

## 3. Codificação Numérica de Amostras PCM em Ponto Fixo Q31

Para eliminar por completo perdas de quantização e erros de arredondamento em ponto flutuante de 32 bits (onde a mantissa de 24 bits degradava amostras de 32 bits PCM e assimetrias em 8 bits), o Lodino adota a representação em ponto fixo **Q31** (`signed 32-bit int`, faixa $[-2^{31}, 2^{31}-1]$).

Essa representação consome exatamente os mesmos 4 bytes por amostra por canal que o `float`, porém permite mapeamento biunívoco exato via deslocamento de bits (bit-shifts).

### 3.1 PCM 8 bits (Unsigned, centro em 128) — Hardening D-WAV-02
- **Disco:** Inteiro não-sinalizado de 8 bits no intervalo $[0, 255]$.
- **Decodificação para Q31:**
  $$q = (b - 128) \ll 24$$
- **Codificação a partir de Q31:**
  $$b = \text{clamp}((q \gg 24) + 128, 0, 255)$$
- **Garantia Bit-Perfect:** Todos os 256 valores possíveis de byte $[0..255]$ são recuperados de forma exata e reversível ($b \to q \to b$), eliminando qualquer assimetria de truncamento.

### 3.2 PCM 16 bits (Signed, Little-Endian) — Hardening D-WAV-03
- **Disco:** Inteiro sinalizado de 16 bits em complemento de dois no intervalo $[-32.768, +32.767]$.
- **Decodificação para Q31:**
  $$q = s \ll 16$$
- **Codificação a partir de Q31:**
  $$s = \text{clamp}(q \gg 16, -32768, 32767)$$
- **Garantia Bit-Perfect:** Todos os 65.536 valores inteiros possíveis são preservados com erro zero ($s \to q \to s$).

### 3.3 PCM 24 bits (Signed Packed 3-Bytes, Little-Endian)
- **Disco:** 3 bytes consecutivos empacotados por amostra no intervalo $[-8.388.608, +8.388.607]$.
- **Extensão de sinal:**
  $$\text{val} = b_0 \mid (b_1 \ll 8) \mid (b_2 \ll 16); \quad \text{se } (\text{val} \ \& \ 0x800000) \ne 0 \implies \text{val} \mid= 0xFF000000$$
- **Decodificação para Q31:**
  $$q = \text{val} \ll 8$$
- **Codificação a partir de Q31:**
  $$\text{val} = \text{clamp}(q \gg 8, -8388608, 8388607)$$
- **Garantia Bit-Perfect:** Todos os $2^{24}$ valores são preservados de ponta a ponta sem qualquer degradação nos bits menos significativos.

### 3.4 PCM 32 bits (Signed Int32, Little-Endian) — Hardening D-WAV-06
- **Disco:** Inteiro sinalizado de 32 bits em complemento de dois no intervalo $[-2.147.483.648, +2.147.483.647]$.
- **Decodificação para Q31:**
  $$q = \text{val}$$
- **Codificação a partir de Q31:**
  $$\text{val} = q$$
- **Garantia Bit-Perfect:** Preservação estrita dos 32 bits inteiros em passthrough e round-trip, testada diretamente contra matrizes de inteiros arbitrários e extremos (`0x7FFFFFFF`, `0x80000000`, `0x12345678`, etc.) sem comparações circulares ou aproximações via float.

---

## 4. O Modelo de Domínio `AudioBuffer` e Operações DSP

A classe `AudioBuffer` desacopla a representação interna dos detalhes de empacotamento em disco, armazenando as amostras no leiaute planar `int[][] _channelData`.

### 4.1 Acessadores de Amostra
- `GetSampleQ31(channel, frame)` e `SetSampleQ31(channel, frame, int)`: Acesso direto de alta performance aos inteiros Q31 sem overhead.
- `GetSample(channel, frame)` e `SetSample(channel, frame, float)`: Acessadores de compatibilidade com ponto flutuante normalizado $[-1.0f, +1.0f]$ para interoperabilidade com rotinas existentes.

### 4.2 Conversão de Canais (Mono $\leftrightarrow$ Estéreo)
- **Estéreo $\to$ Mono (Downmixing com Média Inteira de 64 bits):**
  $$M[i] = (\text{int})\left(\frac{((\text{long})L[i] + R[i])}{2}\right)$$
  Calculado com inteiros de 64 bits, garantindo ausência total de overflow e de arredondamento espúrio.
- **Mono $\to$ Estéreo (Upmixing / Duplicação Bit-Perfect):**
  $$L[i] = M[i], \quad R[i] = M[i]$$
  Executado via `Array.Copy` direto no array `int[]`.

### 4.3 Reamostragem (Resampling por Interpolação Linear)
Dado um sinal com taxa $S_{\text{in}}$ e quadros $N_{\text{in}}$, a conversão para $S_{\text{out}}$ resulta em:
$$N_{\text{out}} = \text{round}\left(N_{\text{in}} \times \frac{S_{\text{out}}}{S_{\text{in}}}\right)$$
Para cada quadro de saída $j \in [0, N_{\text{out}}-1]$:
$$t = j \times \frac{S_{\text{in}}}{S_{\text{out}}}, \quad i = \lfloor t \rfloor, \quad \alpha = t - i$$
$$\text{interpolated} = (1.0 - \alpha) \times s_{\text{in}}[i] + \alpha \times s_{\text{in}}[\min(i + 1, N_{\text{in}} - 1)]$$
O resultado em precisão `double` é submetido a clamping no intervalo $[-2^{31}, 2^{31}-1]$ e convertido para Q31 com `Math.Round`.

### 4.4 Ajuste Linear de Ganho e Normalização de Pico
- **Ganho Linear:** Multiplicação uniforme de cada amostra Q31 em precisão `double` com clamping estrito no intervalo de saturação Q31. O ganho deve ser finito; `NaN`, `Infinity` e `-Infinity` são rejeitados antes da modificação das amostras ou da criação da saída.
- **Normalização de Pico:** Localização do valor absoluto máximo em todos os canais:
  $$P = \max_{c, i} |s_c[i]|$$
  Se $P > 0$, as amostras são escalonadas para o pico alvo $P_{\text{target}}$ (padrão 0.99 / -0.1 dBFS).

---

## 5. Limites de Segurança Atualizados (`SecurityLimits`)

O subsistema de áudio estabelece tetos rígidos para evitar exaustão de memória ou ataques DoS:
- `DefaultMaxAudioFileSize`: 500 MB.
- `MaxAudioChannels`: 8 canais (com 1 e 2 testados em perfil estrito).
- `MinAudioSampleRate`: 8.000 Hz.
- `MaxAudioSampleRate`: 192.000 Hz.
- `MaxAudioFrames`: 100.000.000 quadros (~35 minutos de áudio estéreo a 48 kHz).
- `MaxSingleChunkSize`: 64 MB por chunk RIFF.
- `MaxAuxiliaryHeaderBytes`: 16 MB acumulados de chunks auxiliares antes do `"data"`.
- `MaxAudioChunkCount`: 4.096 chunks processados, incluindo chunks vazios, `fmt ` e `data`.
- `MaxAudioMemoryBytes`: 400 MB de consumo de memória estimada ($\text{canais} \times \text{quadros} \times 4$).
- `AssertAudioParameters`: Método centralizado que valida canais, taxas, bits, contagem de quadros e consumo de memória antes de qualquer alocação.

---

## 6. Integração na Aplicação e CLI

1. **`EngineRegistry`:** O `WavEngine` é registrado sob o identificador `"audio-wav"` e atende as extensões `"wav"` e `"wave"`.
2. **CLI (`Lodino.exe`):**
   - `Lodino.exe inspect audio.wav` exibe canais, taxa de amostragem, bits por amostra, byte rate, block align, contagem de quadros e duração formatada.
   - `Lodino.exe convert audio.wav wav [outDir] --sampleRate=48000 --channels=1 --bits=24 --gain=1.2 --normalize=true`
3. **Desktop Nativo Win32 (`Lodino.App.exe`):**
   - Formato disponível na lista suspensa: `"WAV (Áudio PCM Sem Perdas)"`.
   - Filtro da caixa de diálogo de abertura inclui `*.wav` e `*.wave`.
   - O `ConversionManager` processa lotes de áudio de forma assíncrona com reporte de progresso e cancelamento cooperativo.
