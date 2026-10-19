# ESPECIFICAÇÃO TÉCNICA NORMATIVA — MOTOR DE CONTÊINERES ZIP E COMPRESSÃO DEFLATE PROPRIETÁRIO
**Projeto:** Lodino Native Core  
**Módulo:** `src/core/formats/archive/zip/` e `src/core/io/`  
**Data:** 19 de Outubro de 2026  
**Status:** Implementado, Auditado e Integrado (Fase 12 — Motor ZIP e DEFLATE 100% Próprio)  
**Conformidade Normativa:** PKWARE PKZIP Application Note (APPNOTE.TXT v6.3.9), RFC 1951 (DEFLATE Compressed Data Format Specification v1.3), ISO 3309 / IEEE 802.3 (CRC-32).

---

## 1. Visão Geral e Princípios Arquiteturais

O motor de arquivamento e compressão do Lodino foi desenvolvido inteiramente em C# (.NET Framework 4.0 BCL) para fornecer suporte autônomo e de alto desempenho à criação, inspeção, listagem e extração segura de contêineres ZIP padrão da indústria.

### 1.1 Princípios Inegociáveis
1. **Zero Dependências Externas em Runtime:** O assembly `Lodino.Core.dll` não utiliza bibliotecas de terceiros (como SharpZipLib, DotNetZip ou 7-Zip) nem classes do runtime de compressão do framework (`System.IO.Compression.ZipArchive` ou `DeflateStream` em tempo de execução). Depende exclusivamente de `mscorlib.dll`, `System.dll` e `System.Core.dll`.
2. **Separação Rigorosa de Camadas:** 
   - A camada de contêiner (`ZipReader`, `ZipWriter`, `ZipEntry`, `ZipConstants`) lida com a estrutura binária PKWARE, alinhamento de diretório central e metadados.
   - A camada de compressão (`DeflateCompressor`, `DeflateDecompressor`, `DeflateBitWriter`, `DeflateBitReader`) opera de forma puramente algorítmica sobre streams binários brutos conforme a RFC 1951.
3. **Segurança em Profundidade (Security by Design):**
   - Imunidade total a ataques de travessia de diretório (**Zip Slip**) e injeção de fluxos NTFS (*Alternate Data Streams*).
   - Defesa ativa contra arquivos descompactadores exaustivos (**Zip Bombs**) via tetos cumulativos de extração.
   - Extração atômica com arquivos temporários para impedir escritas parciais em caso de corrupção ou interrupção.
4. **Interoperabilidade Total:** Arquivos gerados pelo Lodino são abertos e extraídos com integridade criptográfica perfeita (SHA-256) por qualquer utilitário de mercado (Windows Explorer, PowerShell `Expand-Archive`, Info-ZIP, 7-Zip, WinRAR), e o Lodino extrai perfeitamente arquivos gerados por essas ferramentas.

---

## 2. Estrutura Binária do Contêiner ZIP (PKWARE APPNOTE.TXT)

O contêiner ZIP é organizado em uma sequência de entradas compostas por cabeçalhos locais e payloads de dados, finalizada por um índice consolidado denominado **Diretório Central** (*Central Directory*), localizado antes do registro de encerramento (**EOCD**).

```
[Local File Header 1] [Dados / Payload 1]
[Local File Header 2] [Dados / Payload 2]
...
[Central Directory Header 1]
[Central Directory Header 2]
...
[End of Central Directory Record (EOCD)]
```

### 2.1 Registro End of Central Directory (EOCD)
O leitor (`ZipReader`) localiza o Diretório Central de forma canônica buscando de trás para frente a assinatura mágica de 4 bytes `0x06054B50`.

| Offset | Tamanho | Campo | Descrição |
|---|---|---|---|
| `0x00` | 4 bytes | `Signature` | `0x06054B50` (`"PK\x05\x06"`) |
| `0x04` | 2 bytes | `DiskNumber` | Número deste disco (deve ser 0) |
| `0x06` | 2 bytes | `StartDisk` | Disco com início do CD (deve ser 0) |
| `0x08` | 2 bytes | `EntriesOnDisk` | Entradas no CD neste disco |
| `0x0A` | 2 bytes | `TotalEntries` | Total de entradas no arquivo |
| `0x0C` | 4 bytes | `CentralDirectorySize` | Tamanho do Diretório Central em bytes |
| `0x10` | 4 bytes | `CentralDirectoryOffset`| Deslocamento do início do CD |
| `0x14` | 2 bytes | `CommentLength` | Tamanho do comentário do arquivo ($L_c$) |
| `0x16` | $L_c$ bytes | `Comment` | Texto do comentário (UTF-8 ou codificação padrão) |

### 2.2 Cabeçalho Central Directory Header (CDH)
Cada entrada no arquivo possui um registro de 46 bytes no Diretório Central:

| Offset | Tamanho | Campo | Valor / Descrição |
|---|---|---|---|
| `0x00` | 4 bytes | `Signature` | `0x02014B50` (`"PK\x01\x02"`) |
| `0x04` | 2 bytes | `VersionMadeBy` | Versão e sistema criador (`0x0314` = Unix / 2.0) |
| `0x06` | 2 bytes | `VersionNeeded` | Versão mínima para extração (`10` para Store, `20` para Deflate) |
| `0x08` | 2 bytes | `GeneralPurposeBitFlag` | Bit 0: Criptografia; Bit 3: Descritor; Bit 11: UTF-8 (`0x0800`) |
| `0x0A` | 2 bytes | `CompressionMethod` | `0` (Store) ou `8` (DEFLATE) |
| `0x0C` | 2 bytes | `LastModTime` | Hora DOS (hora:5, min:6, seg/2:5) |
| `0x0E` | 2 bytes | `LastModDate` | Data DOS ((ano-1980):7, mês:4, dia:5) |
| `0x10` | 4 bytes | `Crc32` | Checksum CRC-32 do payload descompactado |
| `0x14` | 4 bytes | `CompressedSize` | Tamanho do payload compactado |
| `0x18` | 4 bytes | `UncompressedSize` | Tamanho do arquivo original |
| `0x1C` | 2 bytes | `FileNameLength` | Comprimento do nome do arquivo ($L_n$) |
| `0x1E` | 2 bytes | `ExtraFieldLength`| Comprimento de campos extras ($L_e$) |
| `0x20` | 2 bytes | `FileCommentLength`| Comprimento do comentário da entrada ($L_k$) |
| `0x22` | 2 bytes | `DiskNumberStart` | Número do disco inicial (0) |
| `0x24` | 2 bytes | `InternalAttributes`| Atributos internos (ex: arquivo texto) |
| `0x26` | 4 bytes | `ExternalAttributes`| Atributos de sistema DOS/Windows (`0x10` para pasta) e Unix (`0x81A40000` / `0x41ED0000`) |
| `0x2A` | 4 bytes | `LocalHeaderOffset` | Posição absoluta do cabeçalho local no fluxo |
| `0x2E` | $L_n$ bytes | `FileName` | Caminho relativo do arquivo (UTF-8 normalizado com `/`) |
| `0x2E + Ln`| $L_e$ bytes | `ExtraField` | Campos de extensão PKWARE |
| `0x2E + Ln + Le` | $L_k$ bytes | `FileComment` | Comentário individual da entrada |

### 2.3 Cabeçalho Local File Header (LFH)
Precede imediatamente os dados binários de cada arquivo:

| Offset | Tamanho | Campo | Valor / Descrição |
|---|---|---|---|
| `0x00` | 4 bytes | `Signature` | `0x04034B50` (`"PK\x03\x04"`) |
| `0x04` | 2 bytes | `VersionNeeded` | Versão necessária para extrair |
| `0x06` | 2 bytes | `GeneralPurposeBitFlag` | Flags (Bit 11: UTF-8) |
| `0x08` | 2 bytes | `CompressionMethod` | `0` (Store) ou `8` (DEFLATE) |
| `0x0A` | 2 bytes | `LastModTime` | Hora DOS |
| `0x0C` | 2 bytes | `LastModDate` | Data DOS |
| `0x0E` | 4 bytes | `Crc32` | Checksum CRC-32 |
| `0x12` | 4 bytes | `CompressedSize` | Tamanho compactado |
| `0x16` | 4 bytes | `UncompressedSize` | Tamanho descompactado |
| `0x1A` | 2 bytes | `FileNameLength` | Comprimento do nome ($L_n$) |
| `0x1C` | 2 bytes | `ExtraFieldLength`| Comprimento de campos extras ($L_e$) |
| `0x1E` | $L_n$ bytes | `FileName` | Nome do arquivo |
| `0x1E + Ln`| $L_e$ bytes | `ExtraField` | Campos extras locais |
| `0x1E + Ln + Le` | Variável | `DataPayload` | Bytes de dados (armazenados ou comprimidos via DEFLATE) |

---

## 3. Algoritmo de Compressão e Descompressão DEFLATE (RFC 1951)

O formato ZIP armazena fluxos DEFLATE brutos (*raw deflate*), ao contrário do PNG que utiliza envelope ZLIB (RFC 1950 com cabeçalho de 2 bytes e trailer Adler-32 de 4 bytes).

### 3.1 Descompressão (`DeflateDecompressor.cs`)
O descompressor implementado no Lodino suporta os três tipos de blocos da RFC 1951:
- **BTYPE 00 (Stored):** Dados copiados diretamente com alinhamento de byte e validação de complemento de tamanho (`LEN` e `NLEN`).
- **BTYPE 01 (Fixed Huffman):** Códigos literais/comprimentos (0..287) e distâncias (0..31) pré-definidos pela especificação.
- **BTYPE 10 (Dynamic Huffman):** Construção dinâmica de árvores a partir da transmissão de comprimentos de código com permutação canônica de 19 símbolos e repetições RLE (símbolos 16, 17 e 18).
- **Buffer Circular LZ77:** Janela deslizante de 32 KB com indexação mascarada `(historyPos - distance) & 32767`.

### 3.2 Compressor Proprietário (`DeflateCompressor.cs`)
O motor oferece três níveis configuráveis de compressão:
1. **`Store` (Método 0):** Gravação direta dos bytes sem compressão. Ideal para dados já comprimidos (JPEG, PNG, MP4, etc.).
2. **`Fast` (Método 8 - DEFLATE Stored):** Emissão de blocos não-comprimidos (BTYPE 00) divididos em fatias máximas de 65.535 bytes, em conformidade com o envelope DEFLATE.
3. **`Optimal` (Método 8 - DEFLATE Fixed Huffman + LZ77):**
   - **Localizador de Casamentos LZ77:** Janela histórica deslizante de 32 KB ($2^{15}$ bytes) utilizando uma tabela hash de 65.536 entradas (`head`) e encadeamento linear (`prev`).
   - Identificação de correspondências de comprimento mínimo 3 e máximo 258 bytes dentro da distância máxima de 32.768 bytes.
   - Emissão de blocos Fixed Huffman (BTYPE 01) com inversão de bits canônicos pré-computada em lookup tables (`FixedLitReversed` e `FixedDistReversed`) para escrita contínua bit a bit LSB-first (`DeflateBitWriter.cs`).
   - Símbolo de fim de bloco EOB (256) e alinhamento final de byte.

---

## 4. Checksum e Integridade Criptográfica (CRC-32)

O módulo compartilhado `src/core/io/Crc32.cs` implementa o algoritmo canônico ISO 3309 / IEEE 802.3:
- **Polinômio Gerador Revertido:** `0xEDB88320` ($x^{32} + x^{26} + x^{23} + \dots$).
- **Valor Inicial:** `0xFFFFFFFF`.
- **Finalização (XOR):** `crc ^ 0xFFFFFFFF`.
- A classe é reutilizada de forma unificada pelo subsistema PNG (`PngCrc32.cs`) e pelo subsistema ZIP (`ZipReader.cs` e `ZipWriter.cs`), garantindo integridade centralizada e zero duplicação de tabelas.

---

## 5. Arquitetura de Segurança e Defesa em Profundidade

O subsistema de arquivos compactados incorpora barreiras determinísticas contra os vetores de ataque mais prevalentes em descompactadores:

### 5.1 Prevenção contra Zip Slip e Path Traversal
- **Classe:** `src/core/formats/archive/zip/ZipSecurity.cs`
- **Exceção Dedicada:** `PathSecurityException` (derivada de `LodinoException`).
- **Bloqueios Aplicados:**
  1. Rejeição de segmentos de travessia `..` em qualquer nível de profundidade.
  2. Rejeição do caractere de dois pontos `:` para barrar ataques de *Alternate Data Streams* (ADS) do NTFS do Windows (`arquivo.txt:malicioso.exe`) e unidades absolutas (`C:\`).
  3. Remoção e saneamento de barras iniciais `/` ou `\`.
  4. Validação canônica de fronteira em `ValidateExtractionDestination`: confirmação de que `Path.GetFullPath(destino)` permanece estritamente contido no diretório raiz autorizado.

### 5.2 Defesa contra Zip Bomb e DoS de Recursos
- **Classe:** `src/core/safety/SecurityLimits.cs`
- **Tetos de Segurança:**
  - `DefaultMaxZipFileSize = 500 MB`: tamanho máximo do arquivo compactado de entrada.
  - `MaxZipEntries = 10.000`: número máximo de entradas por contêiner.
  - `MaxZipEntrySize = 500 MB`: limite máximo de bytes descompactados por entrada única.
  - `MaxZipTotalExtractedBytes = 1 GB`: teto cumulativo de bytes extraídos durante uma operação completa.
  - `MaxZipCommentLength = 65.535 bytes`: limite de comentário PKWARE.
- Caso o volume acumulado de bytes ou a contagem de entradas exceda os tetos, a operação é interrompida com `ResourceLimitException`.

### 5.3 Escrita Atômica de Arquivos
Durante a extração (`ZipReader.ExtractAll`), nenhum arquivo final de produção é escrito diretamente em seu destino definitivo. Os bytes são decodificados para um arquivo temporário exclusivo:
`[caminho_destino].lodinotmp_[guid8]`
Somente após a leitura completa, conferência do tamanho descompactado exato e validação do CRC-32 sem divergências, o arquivo temporário é movido atomicamente para o caminho definitivo. Se ocorrer qualquer erro, o arquivo temporário é excluído, impedindo resíduos parciais ou arquivos corrompidos no disco.

### 5.4 Rejeição Segura de Formatos Não Suportados
- **Arquivos Criptografados:** Detecção do bit 0 em `GeneralPurposeBitFlag` (`FlagEncrypted = 0x0001`) e emissão de `UnsupportedFormatException`.
- **Extensões ZIP64:** Detecção dos marcadores `0xFFFF` ou `0xFFFFFFFF` no EOCD e emissão de `UnsupportedFormatException` explicativa, sem comportamentos indefinidos.

---

## 6. Integração na Aplicação e Interfaces

### 6.1 Registro no Núcleo (`ZipEngine.cs`)
- `EngineId`: `"archive-zip"`.
- `EngineLabel`: `"Motor Nativo de Arquivos ZIP"`.
- Suporte dinâmico a rotas:
  - `dir -> zip` (empacotamento de diretórios).
  - `* -> zip` (empacotamento de arquivos avulsos).
  - `zip -> dir` / `zip -> extract` (extração segura para pasta).
  - `zip -> zip` (inspeção, validação de integridade e reempacotamento).

### 6.2 Linha de Comando (CLI - `Lodino.exe`)
```powershell
# Compactar diretório ou arquivo
Lodino.exe zip <entrada> [-o <saida.zip>] [-c store|fast|optimal]

# Listar entradas com método, tamanhos e proporção
Lodino.exe list-zip <arquivo.zip>

# Extrair arquivo ZIP para pasta
Lodino.exe unzip <arquivo.zip> [-o <pasta_destino>]

# Inspecionar metadados e taxas de compressão
Lodino.exe inspect <arquivo.zip>
```

### 6.3 Interface Desktop Win32 (`Lodino.App.exe`)
- Formato de destino `.zip` disponível diretamente no `ComboBox` de formatos.
- Suporte a empacotamento em lote com fila de conversão assíncrona multithread.
- Notificação de progresso e cancelamento cooperativo sem congelamento de interface.

---

## 7. Resultados dos Testes de Verificação e Interoperabilidade

| Categoria de Teste | Quantidade | Resultado |
|---|---|---|
| Suíte Histórica (CSV, JSON, INI, BMP, PNG, JPEG, WAV, Desktop) | 167 testes | 167 aprovados (0 regressões) |
| Novos Testes Unitários de ZIP e DEFLATE | 15 testes | 15 aprovados (0 falhas) |
| **Total da Suíte Interna (`Lodino.Tests.exe`)** | **182 testes** | **182 APROVADOS (100%)** |
| Interoperabilidade: Lodino -> Windows `Expand-Archive` | 3 arquivos + 1 subpasta | **100% SHA256 IDÊNTICO** |
| Interoperabilidade: Windows `Compress-Archive` -> Lodino `unzip` | 3 arquivos + 1 subpasta | **100% SHA256 IDÊNTICO** |
| Auditoria de Dependências de Runtime (`Lodino.Core.dll`) | Assemblies externos | **ZERO (apenas mscorlib/System)** |
