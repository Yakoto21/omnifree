# PLANO DE TESTES PRÁTICOS E INTEROPERABILIDADE — DEFLATE DINÂMICO (FASE 12.6)
**Arquitetura e Comprovação de Blocos: Dynamic (BTYPE 10), Fixed (BTYPE 01) e Stored (BTYPE 00)**

**Data:** 19 de Outubro de 2026  
**Autor:** Agente D — Especialista em Interoperabilidade e Testes da Fase 12.6 do Lodino  
**Ambiente:** Windows 10/11 x64, .NET Framework 4.8 / CLR 4.0, PowerShell 5.1, Python 3.15.0, bsdtar 3.8.9 (libarchive / zlib 1.2.5)  
**Diretório de Artefatos:** `c:\Projetos\lodino\scratch\deflate_dynamic\`  
**Status da Auditoria:** **100% APROVADO** (119/119 asserções internas + 136/136 asserções externas SHA-256)

---

## 1. Resumo Executivo e Objetivos

O objetivo da Fase 12.6 do Lodino é dotar o motor DEFLATE de suporte a **Huffman Dinâmico (BTYPE 10)**, complementando as implementações prévias de **Stored (BTYPE 00)** e **Fixed Huffman (BTYPE 01)**. A missão do **Agente D** é assegurar que a transição não seja apenas teórica, mas comprovada empiricamente através de:

1. **Classificação rigorosa de fixtures:** Arquivos reais e reproduzíveis calibrados para que cada tipo de bloco (00, 01, 10) seja a escolha estritamente ótima de acordo com a teoria da informação e a RFC 1951.
2. **Inspeção analítica direta de bitstream em C#:** Leitura no nível de bits dos primeiros 3 bits (`BFINAL` e `BTYPE`) e dos cabeçalhos estruturais para comprovar a emissão real de cada modo.
3. **Equalização de Kraft para conformidade universal:** Normalização completa de árvores de Huffman para compatibilidade total com decodificadores rigorosos baseados em tabelas de busca (`zlib`, `bsdtar`, `libarchive`).
4. **Validação criptográfica bidirecional:** Garantia de 100% de identidade bit-a-bit (SHA-256) em extrações cruzadas com o Windows `Expand-Archive`, Python `zipfile` e `tar.exe`.
5. **Medição empírica de desempenho e ganhos:** Comparação de tempo de compressão ($\mu s$), tamanho de saída e ganho percentual entre os 3 modos.

---

## 2. Ferramentas Disponíveis no Ambiente de Testes

| Ferramenta | Caminho no Sistema | Detalhes de Versão / Engine | Função no Teste |
|---|---|---|---|
| **PowerShell Expand-Archive** | Módulo nativo `Microsoft.PowerShell.Archive` | PowerShell 5.1 (Build 19041) sobre CLR 4.0 | Extração nativa padrão do Windows de contêineres ZIP gerados pelo Lodino |
| **tar.exe (bsdtar)** | `C:\WINDOWS\system32\tar.exe` | `bsdtar 3.8.9 - libarchive 3.8.9 zlib/1.2.5.f-ipp` | Extração via utilitário de linha de comando padrão do Windows |
| **Python zipfile / zlib** | `C:\Users\Pinchu\AppData\Local\Microsoft\WindowsApps\python.exe` | `Python 3.15.0 (AMD64)` com módulos `zipfile` e `zlib` | Gerador e validador bidirecional de DEFLATE e contêineres PKWARE |
| **Microsoft C# Compiler (csc.exe)** | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` | Compilador nativo C# v4.8.9232.0 | Compilação direta dos executáveis e bibliotecas de teste sem dependências externas |
| **Lodino Core Engine** | `c:\Projetos\lodino\build\Lodino.Core.dll` | Motor nativo puro C# Lodino (Phase 12) | Descompressor nativo `DeflateDecompressor` e leitor `ZipReader` |

---

## 3. Matriz de Casos de Teste para Diferenciação de Blocos

Para comprovar a inteligência adaptativa do motor DEFLATE, foram geradas **17 fixtures calibradas**, divididas em 4 matrizes:

```
fixtures/
├── dynamic_btype10/   (5 arquivos: texto natural, português, JSON REST, código C, SVG)
├── fixed_btype01/     (5 arquivos: payload pequeno, log curto, JSON minimalista, uniform ASCII)
├── stored_btype00/    (4 arquivos: CSPRNG alta entropia, AES-256, JPEG IDAT slice, tabela SHA-256)
└── mixed_bench/       (3 arquivos: CSV misto, página HTML completa, runs repetitivos)
```

### Tabela Analítica das 17 Fixtures

| Categoria | Arquivo | Tamanho | Entropia | BTYPE Esperado | BTYPE Escolhido | Racional Teórico e Econômico |
|---|---|---|---|---|---|---|
| `dynamic_btype10` | `dyn_english_prose.txt` | 8.460 B | 4,59 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Assimetria acentuada de caracteres ('e', 't', 'a', espaço); Dynamic economiza **20,63%** sobre Fixed (amortiza amplamente o cabeçalho de árvore). |
| `dynamic_btype10` | `dyn_portuguese_corpus.txt` | 5.215 B | 4,64 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Frequência de artigos/preposições e acentos UTF-8; Dynamic economiza **19,37%** sobre Fixed. |
| `dynamic_btype10` | `dyn_rest_payload.json` | 23.147 B | 4,44 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Chaves repetitivas (`uuid`, `username`, `status`, `region`); Dynamic economiza **16,28%** sobre Fixed (ganho de 245 bytes). |
| `dynamic_btype10` | `dyn_source_code.c` | 7.396 B | 5,04 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Palavras-chave de sintaxe C e indentação; Dynamic economiza **12,24%** sobre Fixed. |
| `dynamic_btype10` | `dyn_svg_vector.svg` | 4.603 B | 4,92 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Tags XML e coordenadas vetoriais; Dynamic economiza **15,80%** sobre Fixed. |
| `fixed_btype01` | `fixed_short_log.log` | 51 B | 4,78 b/B | **01 (Fixed)** | **01 (Fixed)** | Payload de 51 bytes; sobrecarga de árvore dinâmica (259 bits = ~32 B) não compensa; Fixed é **19,62% mais eficiente**. |
| `fixed_btype01` | `fixed_small_greeting.txt` | 38 B | 4,32 b/B | **01 (Fixed)** | **01 (Fixed)** | Mensagem curta de 38 bytes; cabeçalho dinâmico (240 bits) supera ganho; Fixed é **31,53% mais eficiente**. |
| `fixed_btype01` | `fixed_small_json.json` | 75 B | 4,10 b/B | **01 (Fixed)** | **01 (Fixed)** | Configuração mínima; Fixed custa 518 bits vs 540 bits de Dynamic (**4,25% mais eficiente**). |
| `fixed_btype01` | `fixed_tiny_phrase.txt` | 44 B | 4,49 b/B | **01 (Fixed)** | **01 (Fixed)** | Frase curta de 44 bytes; Fixed custa 353 bits vs 391 bits de Dynamic (**10,76% mais eficiente**). |
| `fixed_btype01` | `fixed_uniform_ascii.txt` | 95 B | 6,57 b/B | **01 (Fixed)** | **01 (Fixed)** | Distribuição quase uniforme na faixa ASCII 32..126; Fixed é **12,08% mais eficiente** que Dynamic. |
| `stored_btype00` | `stored_aes_ciphertext.bin` | 2.048 B | 7,91 b/B | **00 (Stored)** | **00 (Stored)** | Alta entropia (incompressível); Fixed expande 5,6% e Dynamic expande 2,4%; Stored impõe estritamente 5 bytes de cabeçalho. |
| `stored_btype00` | `stored_compressed_jpeg_slice.bin` | 8.192 B | 7,98 b/B | **00 (Stored)** | **00 (Stored)** | Dados pré-comprimidos; Fixed expande 5,5% (8.641 B); Stored preserva o tamanho original (+5 bytes). |
| `stored_btype00` | `stored_random_high_entropy.bin` | 4.096 B | 7,96 b/B | **00 (Stored)** | **00 (Stored)** | Bytes pseudo-aleatórios criptográficos; Fixed expande 5,7%; Stored é o único que evita inflação. |
| `stored_btype00` | `stored_sha256_table.dat` | 4.096 B | 7,96 b/B | **00 (Stored)** | **00 (Stored)** | 128 digests SHA-256 binários concatenados; Stored é ótimo (evita inflação de 226 bytes do Fixed). |
| `mixed_bench` | `mixed_csv_table.csv` | 13.022 B | 4,82 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Tabela de dados tabulares; Dynamic economiza **25,15%** (1.098 bytes a menos que Fixed!). |
| `mixed_bench` | `mixed_html_page.html` | 6.564 B | 5,08 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Marcação HTML + CSS + JS; Dynamic economiza **14,43%** sobre Fixed. |
| `mixed_bench` | `mixed_repetitive_runs.txt` | 33.400 B | 5,04 b/B | **10 (Dynamic)** | **10 (Dynamic)** | Runs periódicos repetitivos; Dynamic atinge **97,8% de compressão** (742 bytes!), economizando **29,98%** sobre Fixed. |

---

## 4. Inspeção Analítica e Comprovação de Emissão de BTYPE

Para certificar programaticamente se os arquivos gerados contêm os blocos corretos, projetamos a classe C# `DeflateBlockInspector` (`DeflateBlockInspector.cs`).

### 4.1 Mapeamento de Bits Conforme RFC 1951

No algoritmo DEFLATE, os bits são lidos em ordem **Least Significant Bit (LSB) first**.
O primeiro byte do fluxo codifica o cabeçalho do primeiro bloco:

- **Bit 0:** `BFINAL` (1 se for o último bloco do fluxo, 0 se houver blocos subsequentes).
- **Bits 1..2:** `BTYPE` (o tipo de codificação do bloco):
  - `00`: **Stored** (sem compressão).
  - `01`: **Fixed Huffman** (árvores pré-definidas na RFC 1951 § 3.2.6).
  - `10`: **Dynamic Huffman** (árvores canônicas personalizadas na RFC 1951 § 3.2.7).
  - `11`: **Reservado / Inválido** (proibido pela norma).

### 4.2 Tabela de Assinaturas de Bits Iniciais (Com `BFINAL = 1`)

| BFINAL | BTYPE | Bits (LSB -> MSB) | Primeiro Byte (Hex) | Modo Reconhecido |
|:---:|:---:|:---:|:---:|---|
| `1` | `00` | `...001` | `0x01` (+ LEN/NLEN alinhados) | **Stored (BTYPE 00)** |
| `1` | `01` | `...011` | `0x03` | **Fixed Huffman (BTYPE 01)** |
| `1` | `10` | `...101` | `0x05` (+ 14 bits HLIT/HDIST/HCLEN) | **Dynamic Huffman (BTYPE 10)** |

### 4.3 Trecho do Inspecionador Analítico em C#

```csharp
public DeflateBlockInfo InspectBlockHeader(byte[] stream)
{
    int firstByte = stream[0];
    int bfinal = firstByte & 1;
    int btype = (firstByte >> 1) & 3;

    var info = new DeflateBlockInfo
    {
        BFinal = (bfinal == 1),
        BType = btype
    };

    switch (btype)
    {
        case 0:
            info.BTypeName = "Stored (BTYPE 00)";
            // Alinha para byte boundary e lê LEN e NLEN (16 bits little-endian)
            ushort len = (ushort)(stream[1] | (stream[2] << 8));
            ushort nlen = (ushort)(stream[3] | (stream[4] << 8));
            info.Details = string.Format("LEN={0}, NLEN=0x{1:X4} (Válido: {2})", len, nlen, (ushort)~len == nlen);
            break;

        case 1:
            info.BTypeName = "Fixed Huffman (BTYPE 01)";
            info.Details = "Árvore canônica pré-definida RFC 1951 § 3.2.6";
            break;

        case 2:
            info.BTypeName = "Dynamic Huffman (BTYPE 10)";
            // Inspeciona os 14 bits seguintes: HLIT (5b), HDIST (5b), HCLEN (4b)
            int hlit = ReadBits(stream, bitOffset: 3, count: 5);
            int hdist = ReadBits(stream, bitOffset: 8, count: 5);
            int hclen = ReadBits(stream, bitOffset: 13, count: 4);
            info.Details = string.Format("HLIT={0} ({1} literais), HDIST={2} ({3} distâncias), HCLEN={4} ({5} comprimentos)",
                hlit, hlit + 257, hdist, hdist + 1, hclen, hclen + 4);
            break;
    }
    return info;
}
```

---

## 5. Normalização de Árvores de Huffman e Compatibilidade zlib / bsdtar

Durante os testes de estresse contra ferramentas nativas, foi descoberta uma discrepância sutil e crítica de interoperabilidade entre diferentes implementações do DEFLATE:

### O Desafio
O descompressor nativo do .NET (`System.IO.Compression.DeflateStream`) e o descompressor do Lodino (`DeflateDecompressor.cs`) navegam árvores de Huffman bit a bit através de árvores binárias pontuais. Se a árvore for **incompleta** (ou seja, a soma de Kraft $\sum 2^{-\text{len}} < 1.0$), ramos não utilizados simplesmente não existem na árvore e nenhuma exceção ocorre se os símbolos não forem emitidos.

Contudo, motores derivados da **zlib** (como o `bsdtar 3.8.9` e Python `zlib`) constroem tabelas de busca indexadas diretas (`inflate_table` em `inftrees.c`). Na zlib:
```c
if (type == LENS && (left > 0 || max != 1))
    return -1; /* Z_DATA_ERROR: invalid code lengths set */
```
Se a árvore de comprimentos de código (`cl_lens`, com até 19 símbolos) for incompleta (`left > 0`), a zlib aborta imediatamente a extração!

### A Solução Implementada
Implementamos no `DynamicDeflateCompressor.cs` o algoritmo de **equalização estrita de Kraft**:
1. Após a geração da árvore de Huffman e a limitação de comprimentos (`ClampLengths`), verificamos a soma de Kraft: $\sum 2^{-\text{length}}$.
2. Se a soma for menor que $1.0$ (`totalKraft < maxKraft`) em alfabetos com mais de 1 símbolo, o algoritmo decrementa progressivamente os maiores comprimentos de código até que a soma de Kraft seja **exatamente $1.0$**.
3. **Resultado:** Além de tornar as árvores 100% completas e satisfazer a zlib, a redução de comprimentos encurtou os códigos de prefixo mais frequentes, gerando uma redução adicional no tamanho do fluxo comprimido!

---

## 6. Resultados de Desempenho e Ganho de Compressão

Avaliamos as 17 fixtures nos modos **Stored**, **Fixed**, **Dynamic** e **Adaptive Auto**. Os resultados compilados a partir de `scratch/deflate_dynamic/reports/dynamic_deflate_benchmark.csv` são resumidos abaixo:

### 6.1 Tabela Comparativa de Tamanho e Desempenho

| Arquivo | Tamanho Original | Stored (B) | Fixed (B) | Dynamic (B) | **Auto (B)** | Ganho Dyn vs Fixed (%) | Taxa de Compressão Final (%) | Tempo Dynamic ($\mu s$) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `dyn_english_prose.txt` | 8.460 | 8.465 | 1.002 | **795** | **795** | **+20,66%** | **90,60%** | 9.024 |
| `dyn_portuguese_corpus.txt` | 5.215 | 5.220 | 808 | **651** | **651** | **+19,43%** | **87,52%** | 1.060 |
| `dyn_rest_payload.json` | 23.147 | 23.152 | 1.506 | **1.261** | **1.261** | **+16,27%** | **94,55%** | 1.751 |
| `dyn_source_code.c` | 7.396 | 7.401 | 941 | **826** | **826** | **+12,22%** | **88,83%** | 1.630 |
| `dyn_svg_vector.svg` | 4.603 | 4.608 | 1.347 | **1.134** | **1.134** | **+15,81%** | **75,36%** | 1.417 |
| `fixed_short_log.log` | 51 | 56 | **53** | 63 | **53** | *-18,87%* | **-3,92%** | 626 |
| `fixed_small_greeting.txt` | 38 | 43 | **40** | 52 | **40** | *-30,00%* | **-5,26%** | 592 |
| `fixed_small_json.json` | 75 | 80 | **65** | 68 | **65** | *-4,62%* | **13,33%** | 324 |
| `fixed_tiny_phrase.txt` | 44 | 49 | **45** | 49 | **45** | *-8,89%* | **-2,27%** | 365 |
| `fixed_uniform_ascii.txt` | 95 | 100 | **97** | 108 | **97** | *-11,34%* | **-2,11%** | 1.201 |
| `stored_aes_ciphertext.bin` | 2.048 | **2.053** | 2.163 | 2.098 | **2.053** | *+3,01%* | **-0,24%** | 7.942 |
| `stored_compressed_jpeg_slice.bin` | 8.192 | **8.197** | 8.641 | 8.232 | **8.197** | *+4,73%* | **-0,06%** | 7.549 |
| `stored_random_high_entropy.bin` | 4.096 | **4.101** | 4.329 | 4.146 | **4.101** | *+4,23%* | **-0,12%** | 7.335 |
| `stored_sha256_table.dat` | 4.096 | **4.101** | 4.322 | 4.144 | **4.101** | *+4,12%* | **-0,12%** | 10.240 |
| `mixed_csv_table.csv` | 13.022 | 13.027 | 4.364 | **3.266** | **3.266** | **+25,16%** | **74,92%** | 1.945 |
| `mixed_html_page.html` | 6.564 | 6.569 | 1.332 | **1.140** | **1.140** | **+14,41%** | **82,63%** | 1.859 |
| `mixed_repetitive_runs.txt` | 33.400 | 33.405 | 1.059 | **742** | **742** | **+29,98%** | **97,78%** | 1.425 |

### 6.2 Análise Comparativa dos Contêineres ZIP Gerados

| Contêiner ZIP | Modo de Compressão DEFLATE | Tamanho Total do ZIP | Comportamento Observado |
|---|---|---|---|
| `lodino_all_stored_btype00.zip` | Stored (BTYPE 00 forçado) | 123.201 bytes | Sem compressão; payload preservado com 5 bytes de overhead por arquivo. |
| `lodino_all_fixed_btype01.zip` | Fixed Huffman (BTYPE 01 forçado) | 34.688 bytes | Compressão razoável, porém sofre expansão de ~5,5% nos 4 arquivos incompressíveis. |
| `lodino_all_dynamic_btype10.zip` | Dynamic Huffman (BTYPE 10 forçado) | 31.349 bytes | Excelente compressão nos textos; sofre ligeira sobrecarga nos 5 arquivos pequenos (<100 B). |
| `lodino_adaptive_auto.zip` | **Adaptive Auto (Decisão Ótima)** | **31.141 bytes** | **O MENOR DE TODOS:** Combina o melhor dos 3 mundos (Dynamic em textos, Fixed em pequenos, Stored em aleatórios). |

---

## 7. Matriz de Interoperabilidade Bidirecional (Round-Trip SHA-256)

Foram executadas **136 extrações criptograficamente auditadas** via script mestre (`run_deflate_dynamic_suite.ps1`), cobrindo todos os fluxos de trabalho e sentidos:

| Sentido | Ferramenta Criadora | Ferramenta Extratora | Modo Testado | Total Arquivos | Aprovados SHA-256 | Taxa |
|---|---|---|---|:---:|:---:|:---:|
| **Lodino -> Externa** | Lodino Engine | PowerShell `Expand-Archive` | Dynamic (BTYPE 10) | 17 | **17** | **100%** |
| **Lodino -> Externa** | Lodino Engine | `tar.exe` (bsdtar 3.8.9) | Dynamic (BTYPE 10) | 17 | **17** | **100%** |
| **Lodino -> Externa** | Lodino Engine | Python `zipfile` | Dynamic (BTYPE 10) | 17 | **17** | **100%** |
| **Lodino -> Externa** | Lodino Engine | PowerShell `Expand-Archive` | Fixed (BTYPE 01) | 17 | **17** | **100%** |
| **Lodino -> Externa** | Lodino Engine | PowerShell `Expand-Archive` | Stored (BTYPE 00) | 17 | **17** | **100%** |
| **Lodino -> Externa** | Lodino Engine | PowerShell `Expand-Archive` | Adaptive Auto | 17 | **17** | **100%** |
| **Externa -> Lodino** | Python `zipfile` (Dynamic Deflate) | Lodino Core (`ZipReader`) | Dynamic (BTYPE 10) | 17 | **17** | **100%** |
| **Externa -> Lodino** | PowerShell `Compress-Archive` | Lodino Core (`ZipReader`) | Deflate Padrão | 17 | **17** | **100%** |

Todos os 136 hashes individuais constam no relatório `scratch/deflate_dynamic/reports/interop_bidirectional_matrix.csv`.

---

## 8. Estrutura de Artefatos Gerados

Todos os arquivos foram criados estritamente dentro de `scratch/deflate_dynamic/`, sem poluir `src/` ou `tests/`:

```
c:\Projetos\lodino\scratch\deflate_dynamic\
│
├── DEFLATE_DYNAMIC_TEST_PLAN.md      # Este relatório técnico formal
├── generate_dynamic_fixtures.py      # Script Python gerador das 17 fixtures determinísticas
├── fixtures_manifest.json            # Manifesto JSON com tamanhos, entropia e hashes SHA-256
├── fixtures_manifest.txt             # Manifesto em texto tabular para consulta rápida
│
├── DeflateBlockInspector.cs          # Inspetor analítico bit-level C# (BFINAL, BTYPE, HLIT, HDIST, HCLEN)
├── DeflateBlockInspector.exe         # Executável CLI do inspecionador de bitstream
├── DynamicDeflateCompressor.cs       # Implementação de referência C# DEFLATE com Kraft-equalization
├── ZipPackageHelper.cs               # Empacotador PKWARE ZIP nativo em C# puro
├── DeflateDynamicHarness.cs          # Harness C# master com 119 asserções analíticas
├── DeflateDynamicHarness.exe         # Executável compilado do harness
├── Lodino.DeflateDynamic.dll         # Biblioteca de classes compilada para testes
├── Lodino.Core.dll                   # Cópia local de runtime do núcleo nativo Lodino
│
├── run_deflate_dynamic_suite.ps1     # Orquestrador mestre automatizado em PowerShell
│
├── fixtures/                         # Diretório das 17 fixtures nas 4 matrizes
│   ├── dynamic_btype10/
│   ├── fixed_btype01/
│   ├── stored_btype00/
│   └── mixed_bench/
│
├── output/                           # Arquivos ZIP gerados e diretórios de extração cruzada
│   ├── lodino_all_dynamic_btype10.zip
│   ├── lodino_all_fixed_btype01.zip
│   ├── lodino_all_stored_btype00.zip
│   ├── lodino_adaptive_auto.zip
│   ├── py_created_dynamic.zip
│   └── ps_created_archive.zip
│
└── reports/                          # Relatórios tabulares CSV com todas as métricas
    ├── btype_decision_matrix.csv
    ├── dynamic_deflate_benchmark.csv
    ├── lodino_roundtrip_sha256.csv
    └── interop_bidirectional_matrix.csv
```

---

## 9. Recomendações Técnicas para a Implementação em `src/`

Com base nos testes e descobertas do Agente D, recomendamos ao Agente de Implementação (Fase 12.6):

1. **Adotar a Heurística Analítica de Custo de Bloco:**  
   Em vez de apenas usar tamanho arbitrário, implementar a rotina `EvaluateBlockCosts` antes da emissão. Como os tokens de LZ77 já estão calculados, o custo exato em bits de Stored, Fixed e Dynamic pode ser obtido em menos de $15\,\mu s$, garantindo sempre a menor saída.
2. **Implementar a Equalização Estrita de Kraft para `clLens`:**  
   Assegurar que a árvore do alfabeto de comprimentos de código (símbolos 0..18) nunca seja subscrita ($\sum 2^{-\text{len}} = 1.0$), prevenindo que `tar.exe` ou `zlib` recusem arquivos com `Z_DATA_ERROR (-3)`.
3. **Incorporar o `DeflateBlockInspector` nos Testes Unitários Oficiais:**  
   Adicionar os métodos de teste do `DeflateBlockInspector` à suíte `Lodino.Tests.exe` para validar programaticamente as propriedades de bitstream em cada build de integração contínua.
