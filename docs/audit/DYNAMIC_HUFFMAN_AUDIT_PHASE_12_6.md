# PARECER TÉCNICO ADVERSARIAL E DE ARQUITETURA — FASE 12.6
## Huffman Dinâmico no Motor DEFLATE do Lodino (`DeflateCompressor.cs`)

**Autor:** Agente E — Revisor Adversarial e de Arquitetura  
**Data:** 19 de Outubro de 2026  
**Status:** AUDITADO — DIRETRIZES MANDATÓRIAS E REQUISITOS DE CONFORMIDADE  
**Destinatário:** Equipe de Engenharia de Núcleo (Agentes A, B, C e D)  
**Escopo:** `DeflateCompressor.cs`, `DeflateDecompressor.cs`, `DeflateBitWriter.cs`, `ZipWriter.cs`, `ZlibStream.cs`, `PngEncoder.cs`

---

## 1. Sumário Executivo

A implementação de **Huffman Dinâmico (BTYPE 10, RFC 1951 §3.2.7)** representa o ápice da taxa de compressão do motor proprietário do Lodino, permitindo que contêineres ZIP e fluxos ZLIB atinjam ratios comparáveis aos da biblioteca de referência Info-ZIP / zlib.

No entanto, a codificação de árvores dinâmicas introduz **armadilhas algorítmicas críticas (gotchas)** que, se tratadas ingenuamente, causam:
1. Loops infinitos em distribuições assimétricas de símbolos;
2. Corrupção imediata de bitstream por emissão imprópria de códigos de repetição RLE;
3. Rejeição sumária por decodificadores estritos (incluindo o Python `zipfile`, .NET `DeflateStream`, Info-ZIP e o próprio `DeflateDecompressor` do Lodino);
4. Regressão de inflação de tamanho (*negative compression*) em payloads pequenos;
5. Degradação de memória por alocações excessivas no *Large Object Heap* (LOH).

Este parecer estabelece as **diretrizes inegociáveis** que devem reger o design e a codificação do compressor dinâmico.

---

## 2. Auditoria Conceitual e Armadilhas Técnicas (Gotchas)

### 2.1 Algoritmo de Limitação de Comprimento (Length-Limited Huffman)
- **Restrição Normativa (RFC 1951):**
  - Árvore de Literais/Comprimentos: profundidade máxima de **15 bits** (até 286 símbolos).
  - Árvore de Distâncias: profundidade máxima de **15 bits** (até 30/32 símbolos).
  - Árvore de Comprimentos de Código: profundidade máxima de **7 bits** (19 símbolos).
- **Vulnerabilidade Identificada:**
  Uma árvore clássica de Huffman sobre dados com frequências assimétricas (distribuição exponencial ou Fibonacci) pode atingir profundidades de até $N - 1$ bits (neste caso, até 285 bits). Cortar cegamente em 15 bits (`len = Math.Min(len, 15)`) viola a desigualdade de Kraft ($\sum 2^{-l} \le 1$), gerando uma árvore *oversubscribed* que faz qualquer decodificador estrito falhar com erro fatal.
- **Risco de Loop Infinito:**
  Algoritmos de rebalanceamento que iteram empurrando nós mais profundos para níveis superiores e aumentando comprimentos inferiores podem oscilar e entrar em **loop infinito** se não houver prova matemática de monotonicidade e contagem estrita de nós por nível.
- **DIRETRIZ MANDATÓRIA (Agente C):**
  1. O algoritmo deve operar sobre um histograma de contagem de comprimentos `bl_count[1..15]`.
  2. Aplicar o algoritmo canônico de Kraft com teto de iterações garantido:
     - Se algum nó exceder 15 bits, ajustar o histograma de contagens de bit decrementando os níveis com excesso e promovendo nós para garantir $\sum_{l=1}^{15} bl\_count[l] \cdot 2^{15 - l} \le 2^{15}$.
     - O laço de ajuste do Kraft histogram é estritamente limitado a no máximo 15 passos ($O(1)$).
  3. **Caso degenerado de 1 único símbolo ativo:** Um código prefixo precisa de pelo menos 1 bit. Se apenas 1 símbolo tiver frequência $> 0$, o compressor deve emitir 2 símbolos com comprimento 1 (ou atribuir comprimento 1 ao símbolo ativo e comprimento 1 a um símbolo dummy vizinho não utilizado) para que a árvore seja matematicamente válida na RFC 1951.

---

### 2.2 Tratamento do Símbolo 256 (EOB - End of Block)
- **Vulnerabilidade Identificada:**
  O símbolo 256 delimita o final do bloco Deflate. Como o LZ77 consome bytes e emite literais (0..255) ou comprimentos (257..285), o símbolo 256 **nunca aparece no payload de dados brutos**.
  Se o compressor computar frequências apenas a partir do fluxo de tokens do LZ77, `freq[256]` permanecerá zero.
  Uma árvore construída com `freq[256] == 0` não atribuirá código ao símbolo 256. Quando o compressor tentar emitir o EOB, o bitstream ficará truncado ou corrompido, e o decodificador entrará em loop de leitura ou lançará exceção de EOF inesperado.
- **DIRETRIZ MANDATÓRIA (Agentes B e C):**
  - `freq[256]` deve ser **incondicionalmente forçado para pelo menos 1** (`freq[256] = Math.Max(1, freq[256]);`) antes de qualquer cálculo de árvore.

---

### 2.3 Caso de Zero Casamentos LZ77 (Nenhuma Distância Utilizada)
- **Armadilha de Interoperabilidade Crítica:**
  A RFC 1951 §3.2.7 determina categoricamente:
  > *"One distance code of length 1 must be transmitted if no distance codes are used, because some decoders complain otherwise."*
- **Análise Adversarial de Decodificadores:**
  - O próprio `DeflateDecompressor.cs` do Lodino constrói `distTree = HuffmanTree.Build(distLengths)`. Se `distLengths` tiver apenas zeros, `maxBits == 0` e a árvore fica sem nós folha.
  - O decodificador clássico da Zlib (`inflate.c` / `inftrees.c`), adotado pelo Python `zipfile`, lança `Z_DATA_ERROR` imediato ("invalid distances set") se todos os comprimentos de distância transmitidos forem 0.
  - O `DeflateStream` do .NET Framework 4.0 rejeita o stream com `InvalidDataException`.
- **DIRETRIZ MANDATÓRIA (Agentes A e C):**
  - Se nenhum casamento LZ77 for encontrado no bloco (isto é, todas as distâncias têm frequência 0):
    1. Fixar `HDIST = 1` (transmitido no cabeçalho como 5 bits com valor `0`, correspondendo a `HDIST - 1 = 0`).
    2. Atribuir artificialmente `distLengths[0] = 1`.
    3. Codificar esse comprimento 1 no fluxo de comprimentos de código.
  - Como o payload conterá apenas literais (0..255) e o EOB (256), esse código de distância jamais será invocado pelo decodificador, mas satisfará 100% dos motores do ecossistema.

---

### 2.4 Codificação RLE dos Comprimentos de Código (Símbolos 16, 17, 18)
- **Armadilha do Símbolo 16 na Posição Zero:**
  O símbolo 16 significa *"repita o comprimento anterior 3 a 6 vezes"*.
  Se a sequência combinada de comprimentos (`litLengths` + `distLengths`) iniciar com valores repetidos e o compressor tentar emitir o símbolo 16 no índice 0:
  O descompressor do Lodino (`DeflateDecompressor.cs` linha 168) possui o seguinte teste explícito:
  ```csharp
  if (sym == 16)
  {
      if (index == 0)
      {
          throw new CorruptDataException("Código de repetição 16 no início do fluxo de comprimentos de código.");
      }
      ...
  }
  ```
  Isso resultará em **falha imediata e quebra de integridade**!
- **DIRETRIZ MANDATÓRIA (Agente C):**
  1. O símbolo 16 **NUNCA** pode ser emitido para o primeiro comprimento do fluxo (índice 0). O primeiro elemento deve sempre ser emitido como um comprimento literal (0..15).
  2. Sequências de valor zero:
     - 1 ou 2 zeros: emitir literais `0`.
     - 3 a 10 zeros: emitir símbolo 17 (com 3 bits extras: `count - 3`).
     - 11 a 138 zeros: emitir símbolo 18 (com 7 bits extras: `count - 11`).
     - Acima de 138 zeros: decompor em múltiplos símbolos 18 / 17.
  3. Sequências de valor não-zero repetido:
     - 1 ou 2 repetições: emitir literais individuais.
     - 3 a 6 repetições: emitir o valor literal inicial seguido pelo símbolo 16 (com 2 bits extras: `count - 3`).
     - Acima de 6 repetições: emitir múltiplos símbolos 16 em cascata.

---

### 2.5 Otimização de Transmissão de Cabeçalhos (Economia de até 45 bits)
- **Truncamento de HCLEN:**
  A tabela de comprimentos de código é transmitida na ordem de permutação canônica:
  `{ 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 }`
  O cabeçalho grava `HCLEN = (número de códigos transmitidos) - 4` (4 bits).
  Cada comprimento consome 3 bits no fluxo.
  Se os símbolos finais na permutação tiverem comprimento 0 (por exemplo, os símbolos 15, 1, 14, 2, 13, 3, 12 não foram utilizados), truncamos `HCLEN` no maior índice com comprimento $> 0$.
  - Economia: até $(19 - 4) \times 3 = \mathbf{45\text{ bits}}$.
- **Truncamento de HLIT e HDIST:**
  - `HLIT` pode ser truncado no último código literal/comprimento com tamanho $> 0$, respeitando o piso estrito `HLIT >= 257` (devido ao símbolo 256). Economia de até 29 códigos de comprimento.
  - `HDIST` pode ser truncado na última distância com tamanho $> 0$, respeitando o piso estrito `HDIST >= 1`. Economia de até 31 códigos de distância.
- **DIRETRIZ MANDATÓRIA (Agente A e C):**
  - Implementar varredura retroativa obrigatória em `clLengths`, `litLengths` e `distLengths` para calcular os mínimos exatos de `HCLEN`, `HLIT` e `HDIST`.

---

### 2.6 Decisão Econômica de Bloco (Anti-Negative Compression)
- **Problema de Compressão Negativa:**
  O cabeçalho de Huffman Dinâmico impõe um overhead fixo de 200 a 450 bits (cabeçalhos de tamanho, árvore CL de 19 símbolos e bitstream RLE).
  Em arquivos pequenos (ex: $< 512$ bytes) ou dados altamente incompressíveis (entropia alta), o Dynamic Huffman produzirá um payload **maior** do que o Fixed Huffman ou o Stored!
- **DIRETRIZ MANDATÓRIA (Agente A e B):**
  - O compressor deve implementar estimativa de custo em bits:
    $$\text{Custo}_{\text{Dynamic}} = \text{Bits}_{\text{Headers}} + \text{Bits}_{\text{Trees}} + \text{Bits}_{\text{Tokens}}$$
    $$\text{Custo}_{\text{Fixed}} = 3 + \text{Bits}_{\text{FixedTokens}}$$
  - Se $\text{Custo}_{\text{Dynamic}} \ge \text{Custo}_{\text{Fixed}}$, o compressor deve automaticamente emitir bloco **Fixed Huffman** (ou **Stored** se ambos expandirem os dados).

---

## 3. Avaliação de Impacto e Não-Regressão

### 3.1 Subsistema PNG (`PngEncoder.cs` e `ZlibStream.cs`)
- Atualmente, o `PngEncoder.cs` invoca `ZlibStream.Compress(filteredData)`.
- O `ZlibStream.Compress` codifica o IDAT com blocos Stored (BTYPE 00) envelopados por cabeçalho ZLIB (CMF `0x78`, FLG `0x01`) e checksum Adler-32 de 4 bytes.
- O `DeflateCompressor.cs` está localizado no namespace `Lodino.Core.Formats.Archive.Zip` e emite fluxos DEFLATE brutos (*raw deflate*).
- **Avaliação de Impacto:**
  1. Alterações em `DeflateCompressor.cs` **não afetam** o subsistema PNG de forma imediata, garantindo zero regressão na suíte de testes de imagem atual.
  2. Para o roadmap futuro de otimização de PNG: o `DeflateCompressor` poderá ser aproveitado pelo `ZlibStream.Compress`, mas **deve ser mantido o envelope estrito da RFC 1950** (cabeçalho de 2 bytes e trailer Adler-32).

### 3.2 Subsistema ZIP (`ZipWriter.cs` e `ZipEngine.cs`)
- A API pública de `ZipCompressionLevel` (`Store`, `Fast`, `Optimal`) deve ser preservada intacta.
- Em `ZipCompressionLevel.Optimal`, o compressor deve acionar a seleção inteligente entre Dynamic, Fixed e Stored.
- Em `ZipCompressionLevel.Fast`, manter a geração de blocos Stored (BTYPE 00) para manter velocidade extrema quando exigido.

### 3.3 Gestão de Memória e Large Object Heap (LOH)
- **Teto do LOH:** No .NET Framework 4.0, qualquer array $\ge 85.000$ bytes é alocado diretamente no Large Object Heap, causando fragmentação irreversível de memória e coletas Gen2 caras.
- **Diretrizes de Alocação:**
  1. As tabelas de frequências e árvores (`int[288]`, `int[32]`, `int[19]`) ocupam menos de 2 KB.
  2. O buffer intermediário de tokens LZ77 não deve ser alocado de forma ilimitada para o arquivo inteiro.
  3. Particionar a compressão em blocos DEFLATE de até **64 KB** ou **128 KB**:
     - Cada bloco gera no máximo 65.536 tokens.
     - Um array plano de tokens empacotados (`int[]` ou struct de 4 bytes) consome $\le 256\text{ KB}$ em blocos particionados ou pode usar blocos de 32 KB com alocação $< 85.000$ bytes para ficar estritamente no Gen0/Gen1 do GC.
     - Permite emitir múltiplos blocos (`BFINAL = 0` nos blocos intermediários e `BFINAL = 1` no bloco final), o que melhora a taxa de compressão ao adaptar os códigos de Huffman à localidade de dados do arquivo.

---

## 4. Verificação de Proveniência e Pureza de Código

Conforme as diretrizes inegociáveis de arquitetura do Lodino:
1. **Zero dependências externas:** O código não pode importar bibliotecas externas (nem SharpZipLib, DotNetZip ou zlib nativa).
2. **Zero `System.IO.Compression` em runtime:** É terminantemente proibido o uso de `DeflateStream` ou `ZipArchive` no código de produção de `Lodino.Core.dll`.
3. **C# Puro com Tipos Primitivos:** Toda a árvore, a ordenação e a emissão de bits devem operar sobre arrays primitivos (`int[]`, `byte[]`, `ushort[]`) e manipulação de bits em `DeflateBitWriter.cs`.

---

## 5. Checklist Mandatório para Aprovação da Fase 12.6

Para que a implementação do Huffman Dinâmico seja considerada aprovada pela revisão adversarial, todos os itens abaixo devem ser rigorosamente atendidos:

| Item | Requisito Mandatório | Agente Responsável | Critério de Validação |
|---|---|---|---|
| **REQ-01** | Algoritmo de limitação a 15 bits com convergência garantida sem loops infinitos | Agente C | Testar fixture com frequências exponenciais (Fibonacci) |
| **REQ-02** | Garantia de frequência $\ge 1$ para o símbolo 256 (EOB) | Agentes B e C | Bloco vazio e payload de 1 byte geram bitstream válido |
| **REQ-03** | Emissão obrigatória de 1 código de distância com comprimento 1 se distâncias = 0 | Agentes A e C | Arquivo sem repetências passa em Python `zipfile`, .NET e Lodino |
| **REQ-04** | Proibição estrita do símbolo 16 no índice 0 da codificação RLE | Agente C | Fixture com comprimentos iniciais idênticos não quebra `DeflateDecompressor` |
| **REQ-05** | Truncamento ótimo de `HCLEN` (economia de até 45 bits), `HLIT` e `HDIST` | Agentes A e C | Verificação bit a bit do cabeçalho do bloco dinâmico |
| **REQ-06** | Fallback econômico para Fixed/Stored em payloads pequenos ou entropia alta | Agente A | Arquivos de 10 bytes a 500 bytes não sofrem inflação de tamanho |
| **REQ-07** | Particionamento de blocos sem estouro no Large Object Heap ($< 85\text{ KB}$) | Agentes B e A | Monitoramento de perfil de memória GC durante compressão de 10 MB |
| **REQ-08** | Interoperabilidade total com Python `zipfile`, `Expand-Archive` e `DeflateDecompressor` | Agente D | 100% de integridade SHA-256 bit-a-bit nos testes de ida e volta |
| **REQ-09** | 100% dos 191 testes existentes continuam aprovados (Zero Regressão) | Agente D | Execução com sucesso de `Lodino.Tests.exe` |

---
**Conclusão do Parecer:**  
A proposta de Huffman Dinâmico é **arquiteturalmente viável e altamente desejável**, desde que as salvaguardas e requisitos mandatórios acima sejam formalmente incorporados ao código antes do merge.
