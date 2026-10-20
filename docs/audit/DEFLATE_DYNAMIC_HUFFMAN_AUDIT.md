# LODINO — PARECER DE AUDITORIA DE SEGURANÇA E CASOS LIMÍTROFES DEFLATE (FASE 12.6)
## Huffman Dinâmico (RFC 1951), Riscos de Transbordamento, Patologias e Blindagem

**Data:** 19 de Outubro de 2026  
**Auditor Responsável:** Agente C — Auditor de Casos Limítrofes e Segurança DEFLATE  
**Escopo de Auditoria:** `DeflateCompressor.cs`, `DeflateDecompressor.cs`, `HuffmanTree.cs`, `DeflateBitWriter.cs`, `DeflateBitReader.cs`, `SecurityLimits.cs`  
**Destinatário:** Agente Coordenador (Parent) e Equipe de Implementação da Fase 12.6  

---

### Resumo Executivo

No escopo da **Fase 12.6** do Lodino (implementação e maturação da compressão com **Huffman Dinâmico — BTYPE 10** e endurecimento de decodificadores), este relatório apresenta a análise crítica e exaustiva dos seguintes eixos:
1. **Riscos de Transbordamento e Overflow Numérico** em arquivos de até 500 MB e dimensionamento ideal de blocos;
2. **Casos Patológicos de Frequência** (ruído branco, entropia mínima, sequências de Fibonacci e símbolos ausentes);
3. **Eficiência e Alocação de Memória no Runtime C#** (padrão zero-alloc no heap e determinismo bit-a-bit);
4. **Vulnerabilidades e Blindagem no Decodificador** (`DeflateDecompressor.cs` e `HuffmanTree.cs`), prevenindo descompressões adversariais e incompatibilidades com descompressores externos padrão (`zlib`, `.NET DeflateStream`, `Python zipfile`, `7-Zip`).

---

## 1. Riscos de Transbordamento e Overflow Numérico

### 1.1 Análise da Capacidade dos Contadores de Frequência (Teto de 500 MB)
- **Teto do Sistema:** `SecurityLimits.MaxZipEntrySize = 500 * 1024 * 1024 = 524.288.000` bytes (~500 MB).
- **Tipo de Dados em C#:** O tipo `int` é um inteiro de 32 bits com sinal em complemento de dois, com capacidade máxima de:
  $$2^{31} - 1 = 2.147.483.647$$
- **Cenário Extremo de Monossímbolo (Entropia Mínima Monolítica):**
  Se uma entrada de 500 MB contiver exclusivamente repetições do mesmo byte (ex: `0x00`), o contador individual para esse símbolo acumularia no máximo $524.288.000$.
  $$\frac{524.288.000}{2.147.483.647} \approx 24,41\% \text{ da capacidade máxima de um } int$$
  Portanto, um contador individual de 32 bits **não transborda** mesmo na hipótese teórica de processamento sem blocagem.

- **Somas de Frequência na Construção da Árvore de Huffman:**
  Durante a fusão de nós no algoritmo guloso ($W_{\text{pai}} = W_{\text{filho1}} + W_{\text{filho2}}$), a raiz da árvore acumula a soma total de todas as frequências das folhas presentes no bloco.
  Em um bloco monolítico contendo todos os dados, a soma na raiz é:
  $$\sum f_i = N_{\text{tokens}} + 1 (\text{EOB}) \le 524.288.001$$
  Como $524.288.001 < 2^{31} - 1$, o peso da raiz também não causa overflow em `int32`.

- **PONTOS VULNERÁVEIS DE OVERFLOW OCULTO IDENTIFICADOS:**
  1. **Empacotamento de Chaves de Ordenação (Bit-Packed Heap Keys):**  
     Rotinas de alto desempenho frequentemente evitam alocação no heap combinando peso e identificador num escalar de 64 bits: `key = ((ulong)weight << 32) | (ulong)nodeIndex`. Se o desenvolvedor utilizar `long` com sinal acidentalmente, `(long)weight << 32` produz número negativo se `weight >= 2^{31}`. Em blocos fatiados (32 KB/64 KB), o peso máximo é $\le 65.536$, tornando o empacotamento imune a estouros em `long` ou `ulong`.
  2. **Multiplicadores e Normalizações de Peso:**  
     Se o algoritmo de limitação de profundidade ou desempate multiplicar frequências por constantes (ex: `weight * 5`), em blocos monolíticos $524.288.000 \times 5 = 2.621.440.000 > 2^{31} - 1$, provocando overflow negativo imediato no C# em contexto não-checked.
  3. **Mitigação Definitiva:**  
     O fatiamento obrigatório dos dados em blocos de até 64 KB restringe a frequência máxima a 65.536, eliminando qualquer risco de transbordamento aritmético.

### 1.2 Dimensionamento de Blocos DEFLATE (Overhead de Cabeçalho vs Adaptabilidade Estatística)
- **Cálculo da Sobrecarga Fixa do Cabeçalho Huffman Dinâmico:**
  - `BFINAL` (1 bit) + `BTYPE` (2 bits) = 3 bits;
  - `HLIT` (5 bits, 257..286) + `HDIST` (5 bits, 1..32) + `HCLEN` (4 bits, 4..19) = 14 bits;
  - Tabela de comprimentos da árvore de comprimentos: $HCLEN \times 3$ bits (12 a 57 bits);
  - Sequência RLE dos comprimentos da árvore de literais e distâncias: tipicamente de 120 a 350 bits;
  - **Overhead total típico:** **~150 a 450 bits (19 a 56 bytes)** por bloco dinâmico.

- **Avaliação dos Tamanhos de Bloco:**
  - **Blocos Pequenos (< 8 KB):** Uma sobrecarga de ~40 bytes sobre 2.048 bytes representa **~2,0% de perda pura** de compressão. Além disso, amostras curtas apresentam alta variância estatística, gerando árvores subótimas que perdem para o Huffman Fixo.
  - **Blocos Grandes (> 128 KB até Monolítico de 500 MB):**
    - Exige manter em memória RAM todos os tokens emitidos pelo LZ77 antes de construir a árvore e emitir o cabeçalho. Um bloco de 100 MB exigiria ~100 a 400 MB de buffers transitórios.
    - Anula a adaptabilidade local: arquivos mistos (ex: ZIP contendo texto, código e binário) têm estatísticas conflitantes; uma árvore média global degrada a taxa de compressão dos trechos de alta redundância.
  - **Tamanho Ideal Recomendado para o Lodino:** **32 KB (32.768 bytes) a 64 KB (65.536 bytes)** de dados descompactados por bloco.
    - 32 KB alinha-se perfeitamente com a janela máxima do LZ77 (32.768 bytes);
    - Em 32 KB, a sobrecarga de ~40 bytes representa insignificantes **0,12%**, amplamente compensada pelos ganhos de 10% a 25% do Huffman Dinâmico;
    - O buffer de tokens LZ77 consome no máximo 128 KB de RAM, totalmente pré-alocável e estático.

---

## 2. Casos Patológicos de Frequência

### 2.1 Dados Aleatórios / Ruído Branco (Entropia Máxima)
- **Comportamento:** Todos os 256 bytes ocorrem com frequências homogêneas. A árvore de Huffman balanceada atribui 8 ou 9 bits por byte.
- **Risco:** O stream comprimido resulta em tamanho maior do que a entrada original (expansão negativa de compressão) devido à soma dos bits extras do símbolo 256 (EOB) e ao cabeçalho da árvore dinâmica (~40 bytes).
- **Blindagem do Lodino:**
  O compressor DEVE simular os custos em bits antes de despachar o bloco:
  - Se $Bits_{\text{Dynamic}} \ge N \times 8$ e $Bits_{\text{Fixed}} \ge N \times 8$, chavear automaticamente para **Bloco Stored (BTYPE 00)**.
  - Em bloco Stored, o teto de overhead é estritamente limitado a 5 bytes a cada 65.535 bytes (0,0076%).

### 2.2 Único Símbolo Repetido (Entropia Mínima)
- **Comportamento:** O LZ77 converte 64 KB de repetições em um único literal inicial seguido de repetidas tuplas `(Length=258, Distance=1)`.
- **Risco de Árvore Incompleta (Seção 3.2.7 da RFC 1951):**
  - Apenas 1 símbolo de distância é utilizado (distância 1 $\to$ símbolo 0).
  - A RFC 1951 permite que uma árvore com apenas 1 símbolo codifique-o com comprimento 1 (código `0`).
  - No entanto, pela desigualdade de Kraft-McMillan, $\sum 2^{-1} = 0,5 < 1,0$. Decodificadores rígidos (ou versões antigas do zlib) podem falhar ou exigir árvores completas.
  - Quando um bloco é puramente literal (0 matches LZ77), a árvore de distâncias possui **zero símbolos ativos**. Contudo, o campo HDIST tem 5 bits e representa `HDIST = bits + 1`, sendo obrigatoriamente $\ge 1$.
- **Blindagem do Lodino:**
  - Garantir que a árvore de literais contenha sempre pelo menos 2 símbolos (o literal ativo + EOB 256);
  - Se apenas 1 símbolo de distância for usado: emitir comprimento 1 para o símbolo 0 e comprimento 1 para o símbolo 1 (dummy nunca emitido), preenchendo a desigualdade de Kraft ($2^{-1} + 2^{-1} = 1,0$). Isso satisfaz 100% dos decodificadores canônicos estritos;
  - Se nenhuma distância for usada: emitir `HDIST = 1` com comprimento 0 para o código 0, ou chavear o bloco para Fixed Huffman / Stored quando for mais eficiente.

### 2.3 Distribuição Fibonacci (Árvores Degeneradas e Teto de 15 bits)
- **Comportamento:** Frequências na razão de Fibonacci ($1, 1, 2, 3, 5, 8, 13, 21, \dots$) forçam o algoritmo de Huffman tradicional a criar uma árvore em cadeia linear desbalanceada, onde a profundidade da folha $k$ é igual a $k$. Para 286 símbolos, a profundidade máxima alcançaria **285 bits**.
- **Violação da RFC 1951:**
  - A especificação DEFLATE proíbe códigos com comprimento superior a **15 bits** para literais e distâncias, e superior a **7 bits** para a árvore de comprimentos de código (`HCLEN`).
  - Decodificadores padrão (.NET, zlib, Python) abortam imediatamente com erro fatal se encontrarem comprimentos $> 15$.
- **Blindagem do Lodino (Requisito Mandatório do Gerador):**
  O gerador de árvore de Huffman do Lodino DEVE conter etapa de limitação de comprimento:
  1. Calcular comprimentos preliminares;
  2. Truncar todo comprimento $> 15$ para 15 (e $> 7$ para a árvore de comprimentos);
  3. Rebalancear comprimentos para restaurar a desigualdade canônica de Kraft-McMillan $\sum 2^{-\text{len}_i} \le 1$.

### 2.4 Símbolos Ausentes e Esparsidade Severa
- **Comportamento:** Arquivos de texto ASCII ou JSON deixam > 200 símbolos literais com frequência zero.
- **Risco:** Transmitir comprimentos zero explicitamente no cabeçalho desperdiça espaço precioso.
- **Blindagem do Lodino:**
  - Truncar `HLIT` no último símbolo não-nulo: `HLIT = Math.Max(257, ultimoSimboloNaoNulo + 1)`;
  - Truncar `HDIST` no último símbolo de distância não-nulo: `HDIST = Math.Max(1, ultimaDistanciaNaoNula + 1)`;
  - Codificar sequências de zeros intermediárias via RLE 17 (3..10 zeros) e RLE 18 (11..138 zeros).
  - Nunca emitir código 16 (repetição do anterior) como primeiro símbolo do fluxo de comprimentos.

---

## 3. Eficiência e Alocação de Memória no Runtime C#

### 3.1 Padrão Zero-Alloc no Loop de Blocos (Prevenção de Pressão no GC)
- **Problema de Alocação Repetitiva:**
  Em um arquivo de 500 MB fatiado em blocos de 32 KB, são processados **~16.000 blocos**. Se cada bloco instanciar arrays para literais, distâncias, comprimentos de código, heap e nós, são geradas **mais de 190.000 alocações de arrays no Gen 0**, provocando pausas de Garbage Collection e fragmentação.
- **Estrutura Recomendada: `DynamicDeflateContext` (Zero-Alloc):**
  Uma instância de contexto pré-alocada uma única vez no início da compressão:
  - `int[288] LitFreq`, `int[32] DistFreq`, `int[19] CodeLenFreq`
  - `int[288] LitLens`, `int[32] DistLens`, `int[19] CodeLenLens`
  - `int[288] LitCodesReversed`, `int[32] DistCodesReversed`, `int[19] CodeLenCodesReversed`
  - `int[576] Heap`, `int[576] Left`, `int[576] Right`, `int[576] Weight` (fila e árvore plana sem objetos!)
  - `int[32768] TokenLitOrLen`, `int[32768] TokenDist` (buffer de tokens LZ77)
  - A cada bloco, reinicializar com `Array.Clear(...)`.
  - **Resultado:** **Zero alocações no heap por bloco processado**.

### 3.2 Critério Determinístico e Bit-a-Bit Reproduzível (Stored vs Fixed vs Dynamic)
- **Fórmula de Contagem Exata de Bits:**
  $$B_{\text{stored}} = \text{bits\_alinhamento} + 3 + 32 + (N_{\text{bytes}} \times 8)$$
  $$B_{\text{fixed}} = 3 + \sum (\text{FixedLitLens}[s] + \text{extra}) + \sum (\text{FixedDistLens}[d] + \text{extra}) + \text{FixedLitLens}[256]$$
  $$B_{\text{dynamic}} = 3 + 14 + (HCLEN \times 3) + B_{\text{RLE}} + \sum (\text{DynLitLens}[s] + \text{extra}) + \sum (\text{DynDistLens}[d] + \text{extra}) + \text{DynLitLens}[256]$$
- **Regras Estritas de Desempate (Tie-Breaking Determinístico):**
  1. Se $B_{\text{dynamic}} < B_{\text{fixed}}$ E $B_{\text{dynamic}} < B_{\text{stored}}$ $\implies$ **Dynamic (BTYPE 10)**.
  2. Se $B_{\text{fixed}} \le B_{\text{dynamic}}$ E $B_{\text{fixed}} < B_{\text{stored}}$ $\implies$ **Fixed (BTYPE 01)**.
  3. Caso contrário $\implies$ **Stored (BTYPE 00)**.
  - Em caso de empate entre Fixed e Dynamic: **Fixed prevalece** (elimina tempo de construção de árvore no descompressor).
  - Em caso de empate entre Stored e Fixed: **Stored prevalece** (descompressão instantânea por cópia de bloco).
- **Estabilidade da Fila de Prioridade de Huffman:**
  Em caso de pesos iguais entre dois nós $A$ e $B$:
  `if (Weight[a] == Weight[b]) return a.CompareTo(b);`
  Garante determinismo absoluto e reproduzibilidade binária de hash SHA-256 entre plataformas.

---

## 4. Riscos no Decodificador e Recomendações de Blindagem

### 4.1 Auditoria do Decodificador Atual (`DeflateDecompressor.cs` / `HuffmanTree.cs`)

| Cenário Inspecionado | Comportamento Atual no Lodino | Gravidade | Recomendação de Blindagem |
|---|---|---|---|
| **HLIT > 286** | Lê 5 bits + 257 (valores 257..288). Não valida se valor é $> 286$. Pode alocar 287/288 posições e quebrar tardiamente ao ler literais. | **Alto** | Inserir validação preventiva: `if (hlit > 286) throw new CorruptDataException("HLIT inválido: " + hlit);` |
| **HDIST > 30** | Lê 5 bits + 1 (valores 1..32). A RFC 1951 só define 30 distâncias (`DistanceBase.Length == 30`). Valores 31 ou 32 disparam exceção em tempo de execução ao acessar `DistanceBase`. | **Alto** | Inserir validação preventiva: `if (hdist > 30) throw new CorruptDataException("HDIST inválido: " + hdist);` |
| **RLE 16, 17, 18 além de `totalCodes`** | Linhas 173-200 já contêm checagens estritas `if (index + repeat > totalCodes) throw ...`. | **Blindado** | Nenhuma alteração necessária; código 100% resiliente a buffer overrun. |
| **RLE 16 no início** | Linha 167 já contém verificação `if (index == 0) throw ...`. | **Blindado** | Totalmente protegido contra uso de símbolo anterior inexistente. |
| **Comprimentos > 15 bits em `HuffmanTree.cs`** | `HuffmanTree.Build` calcula `1 << len`. Se `len >= 31`, ocorre overflow de sinal em `int32`. | **Médio** | Inserir em `HuffmanTree.Build`: `if (maxBits > 15) throw new CorruptDataException("Comprimento de código Huffman > 15 bits.");` |
| **Árvore de Literais sem EOB 256** | Se `litLengths.Length <= 256` ou `litLengths[256] == 0`, o bloco dinâmico jamais poderá ser terminado normalmente. | **Médio** | Validar `if (litLengths.Length <= 256 \|\| litLengths[256] == 0) throw new CorruptDataException("Símbolo EOB 256 ausente na árvore.");` |

### 4.2 Padrões Gerados que Podem Quebrar Decodificadores Externos (zlib, .NET, Python)
1. **Ordem dos Bits dos Códigos Canônicos (Bit-Reversal):**
   - No DEFLATE, bits de controle entram LSB-first. Códigos de Huffman canônicos são ordenados do MSB para LSB.
   - O gerador DEVE inverter a ordem de bits do código canônico antes de enviá-lo ao `DeflateBitWriter`:
     `int revCode = ReverseBits(code, len); writer.WriteBits(revCode, len);`
2. **Árvores de Distância com 0 Códigos:**
   - Se o bloco for puramente literal, emitir `HDIST = 1` com código 0 de comprimento 0, ou preferir emitir o bloco como Huffman Fixo / Stored.
3. **Ordem dos Códigos de Comprimento (`CodeLengthOrder`):**
   - Transmissão obrigatória na sequência permutada da RFC 1951:
     `{ 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 }`.

---

## 5. Diretrizes e Checklist de Implementação para os Agentes A e B

- [ ] **Blocagem Fatiada:** Fatiar dados em blocos de no máximo 32.768 bytes (32 KB) para casar perfeitamente com a janela LZ77.
- [ ] **Reutilização de Contexto:** Utilizar buffers estáticos/reutilizados para tabelas de frequência e heaps, eliminando alocações repetitivas no GC.
- [ ] **Limite de Comprimento a 15 bits:** Implementar algoritmo de limitação de profundidade na construção da árvore dinâmica de Huffman.
- [ ] **Inversão de Bits Canônicos:** Inverter bits dos códigos de Huffman antes de emitir com `DeflateBitWriter`.
- [ ] **Seleção Determinística:** Implementar cálculo comparativo de bits entre Stored, Fixed e Dynamic com regras estritas de desempate.
- [ ] **Hardening no Decodificador:** Adicionar validações de `hlit <= 286`, `hdist <= 30`, `maxBits <= 15` e presença de EOB 256 em `DeflateDecompressor.cs` e `HuffmanTree.cs`.
