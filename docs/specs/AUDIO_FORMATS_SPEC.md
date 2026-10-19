# ESPECIFICAÇÃO TÉCNICA NORMATIVA — MOTOR DE ÁUDIO PCM RIFF/WAVE PROPRIETÁRIO
**Projeto:** Lodino Native Core  
**Módulo:** `src/core/formats/audio/` e `src/core/formats/audio/wav/`  
**Data:** 19 de Outubro de 2026  
**Status:** Implementado, Auditado e Integrado  
**Conformidade:** IBM/Microsoft RIFF/WAVE Specification (1991), Microsoft Multimedia Standards Update, RFC 2361.

---

## 1. Visão Geral e Princípios Arquiteturais

O motor de áudio do Lodino foi projetado do zero em C# (.NET Framework 4.0 BCL) para fornecer suporte nativo e autônomo ao formato de áudio PCM não comprimido (RIFF/WAVE), eliminando integralmente a necessidade de ferramentas externas (como FFmpeg) ou bibliotecas de terceiros (como NAudio, Bass ou DirectX).

### 1.1 Princípios Inegociáveis
1. **Zero Dependências Externas:** O assembly `Lodino.Core.dll` depende estritamente de `mscorlib.dll`, `System.dll` e `System.Core.dll`.
2. **Isolamento de Domínio:** O modelo de dados de áudio (`AudioBuffer`) é 100% desacoplado do modelo raster de imagens (`PixelBuffer`). Conceitos visuais e acústicos não se misturam.
3. **Fidelidade Matemática Estrita:** Amostras de 8, 16, 24 e 32 bits são decodificadas e recodificadas sem transformações com perdas não solicitadas, com verificação de limites numéricos e teste de reversibilidade analítica.
4. **Resiliência e Segurança:** Rejeição estrita de variantes não suportadas (ADPCM, IEEE Float, A-law, Mu-law), cabeçalhos truncados, chunks fora de ordem e fluxos com tamanhos superiores aos tetos configurados em `SecurityLimits`.

---

## 2. Estrutura Binária do Contêiner RIFF/WAVE

O contêiner RIFF (*Resource Interchange File Format*) utiliza ordenação de bytes *Little-Endian* em blocos denominados *chunks*.

### 2.1 Cabeçalho Principal RIFF (12 bytes)
| Offset | Tamanho | Campo | Tipo | Valor Esperado / Descrição |
|---|---|---|---|---|
| `0x00` | 4 bytes | `FourCC` | ASCII | `"RIFF"` (`0x46464952`) |
| `0x04` | 4 bytes | `RiffSize` | uint32 (LE) | Tamanho total do arquivo menos 8 bytes (`4 + ChunksSize`) |
| `0x08` | 4 bytes | `Format` | ASCII | `"WAVE"` (`0x45564157`) |

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

#### Suporte a `WAVE_FORMAT_EXTENSIBLE` (`0xFFFE`)
Arquivos WAV gravados por estações de áudio modernas (DAWs) frequentemente utilizam a tag `0xFFFE` mesmo para áudio PCM, estendendo o chunk `"fmt "` para 40 bytes com um GUID de 16 bytes:
- SubFormat GUID do PCM: `00000001-0000-0010-8000-00AA00389B71`.
- O decodificador `WavHeader` e `WavDecoder` valida a assinatura do GUID e normaliza o formato internamente para PCM, garantindo compatibilidade universal.

### 2.4 Chunk Obrigatório de Amostras (`"data"`)
Identificador: `"data"` (`0x61746164`).
- `ChunkSize`: Comprimento total em bytes dos dados de áudio brutos.
- Contagem total de quadros de amostra: $\text{FrameCount} = \frac{\text{ChunkSize}}{\text{BlockAlign}}$.
- Duração em segundos: $\text{Duration} = \frac{\text{FrameCount}}{\text{SampleRate}}$.

### 2.5 Tratamento de Chunks Auxiliares
Chunks como `"LIST"`, `"JUNK"`, `"bext"`, `"id3 "` ou chunks de metadados informativos são saltados de maneira segura com base no seu `ChunkSize` (acrescido do pad byte quando ímpar) e indexados no dicionário de metadados da inspeção (`FormatInspection.Metadata`).

---

## 3. Codificação Numérica de Amostras PCM

O Lodino implementa a decodificação e codificação rigorosa dos quatro níveis de quantização inteira padronizados pela especificação RIFF/WAVE:

### 3.1 PCM 8 bits (Unsigned, centro em 128)
- **Representação em disco:** Inteiro não-sinalizado de 8 bits no intervalo $[0, 255]$.
- Silêncio analógico: $128$ (`0x80`).
- Conversão para Ponto Flutuante:
  $$s_{\text{float}} = \frac{b - 128}{128.0}$$
- Codificação para Byte:
  $$b = \text{clamp}\left(\text{round}\left((s_{\text{float}} + 1.0) \times 127.5\right), 0, 255\right)$$

### 3.2 PCM 16 bits (Signed, Little-Endian)
- **Representação em disco:** Inteiro sinalizado de 16 bits em complemento de dois no intervalo $[-32.768, +32.767]$.
- Silêncio analógico: $0$.
- Conversão para Ponto Flutuante:
  $$s_{\text{float}} = \begin{cases} \frac{s}{32768.0}, & \text{se } s < 0 \\ \frac{s}{32767.0}, & \text{se } s \ge 0 \end{cases}$$
- Codificação para Int16:
  $$s = \text{clamp}\left(\text{round}\left(s_{\text{float}} < 0 ? s_{\text{float}} \times 32768.0 : s_{\text{float}} \times 32767.0\right), -32768, 32767\right)$$

### 3.3 PCM 24 bits (Signed Packed 3-Bytes, Little-Endian)
- **Representação em disco:** 3 bytes consecutivos empacotados por amostra no intervalo $[-8.388.608, +8.388.607]$.
- Extensão de sinal:
  $$\text{val} = b_0 \mid (b_1 \ll 8) \mid (b_2 \ll 16); \quad \text{se } (\text{val} \ \& \ 0x800000) \ne 0 \implies \text{val} \mid= 0xFF000000$$
- Conversão para Ponto Flutuante:
  $$s_{\text{float}} = \begin{cases} \frac{\text{val}}{8388608.0}, & \text{se } \text{val} < 0 \\ \frac{\text{val}}{8388607.0}, & \text{se } \text{val} \ge 0 \end{cases}$$

### 3.4 PCM 32 bits (Signed Int32, Little-Endian)
- **Representação em disco:** Inteiro sinalizado de 32 bits em complemento de dois no intervalo $[-2.147.483.648, +2.147.483.647]$.
- Conversão para Ponto Flutuante:
  $$s_{\text{float}} = \begin{cases} \frac{\text{val}}{2147483648.0}, & \text{se } \text{val} < 0 \\ \frac{\text{val}}{2147483647.0}, & \text{se } \text{val} \ge 0 \end{cases}$$

---

## 4. O Modelo de Domínio `AudioBuffer` e Operações DSP

A classe `AudioBuffer` desacopla a representação interna dos detalhes de empacotamento em disco, armazenando as amostras em arranjos planares normalizados `float[][] _channelData`.

### 4.1 Conversão de Canais (Mono $\leftrightarrow$ Estéreo)
- **Estéreo $\to$ Mono (Downmixing):**
  $$M[i] = \frac{L[i] + R[i]}{2.0}$$
- **Mono $\to$ Estéreo (Upmixing / Duplicação):**
  $$L[i] = M[i], \quad R[i] = M[i]$$

### 4.2 Reamostragem (Resampling por Interpolação Linear)
Dado um sinal com taxa $S_{\text{in}}$ e quadros $N_{\text{in}}$, a conversão para $S_{\text{out}}$ resulta em:
$$N_{\text{out}} = \text{round}\left(N_{\text{in}} \times \frac{S_{\text{out}}}{S_{\text{in}}}\right)$$
Para cada quadro de saída $j \in [0, N_{\text{out}}-1]$:
$$t = j \times \frac{S_{\text{in}}}{S_{\text{out}}}, \quad i = \lfloor t \rfloor, \quad \alpha = t - i$$
$$s_{\text{out}}[j] = (1.0 - \alpha) \times s_{\text{in}}[i] + \alpha \times s_{\text{in}}[\min(i + 1, N_{\text{in}} - 1)]$$

### 4.3 Ajuste Linear de Ganho e Normalização de Pico
- **Ganho Linear:** Multiplicação uniforme de cada amostra por um fator $G > 0$, com clamping em $[-1.0, +1.0]$.
- **Normalização de Pico:** Localização do valor absoluto máximo em todos os canais:
  $$P = \max_{c, i} |s_c[i]|$$
  Se $P > 0$, todas as amostras são escalonadas por $\frac{P_{\text{target}}}{P}$ (onde $P_{\text{target}} = 0.99$).

---

## 5. Limites de Segurança (`SecurityLimits`)

O subsistema de áudio estabelece tetos rígidos para evitar exaustão de memória ou ataques DoS:
- `DefaultMaxAudioFileSize`: 500 MB.
- `MaxAudioChannels`: 8 canais (com 1 e 2 testados em perfil estrito).
- `MinAudioSampleRate`: 8.000 Hz.
- `MaxAudioSampleRate`: 192.000 Hz.
- `MaxAudioFrames`: 100.000.000 quadros (~35 minutos de áudio estéreo a 48 kHz).
- `AssertAudioParameters`: Método centralizado que valida a consistência de canais, taxa, bits e quadros antes de qualquer alocação de buffer.

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
