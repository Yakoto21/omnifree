# Especificações Técnicas dos Formatos de Dados — Piloto 1

**Agente Responsável:** Agente 3 — SPEC-RESEARCH  
**Módulo:** Motor de Dados Estruturados (`Lodino.Core.Formats.Data`)  
**Formatos Cobertos:** CSV, JSON, INI  

---

## 1. CSV (Comma-Separated Values)

### Especificação Base
- **Padrão:** IETF RFC 4180 (*Common Format and MIME Type for Comma-Separated Values (CSV) Files*).
- **MIME Type:** `text/csv`.

### Regras Gramaticais e Comportamentais
1. **Delimitador de Registro:** Padrão `CRLF` (`\r\n`), com tolerância a `LF` (`\n`) e `CR` (`\r`) isolados em leituras. O último registro pode ou não ser seguido por quebra de linha.
2. **Cabeçalho:** A primeira linha contém nomes de campos opcionais. No Lodino, o leitor infere a presença de cabeçalho ou aceita flag explícita `hasHeader`.
3. **Delimitador de Campo:** Vírgula (`,`).
4. **Campos Citados (Quoted Fields):**
   - Campos contendo quebras de linha (`\r`, `\n`), vírgulas (`,`) ou aspas duplas (`"`) **devem** ser envolvidos por aspas duplas.
   - Espaços em branco adjacentes às aspas fazem parte do campo.
5. **Escape de Aspas:** Aspas duplas literais dentro de um campo citado devem ser representadas por duas aspas duplas consecutivas (`""`).
6. **Campos Multilinha:** Quando um campo está entre aspas, qualquer quebra de linha interna pertence ao valor do campo e não encerra o registro.
7. **Codificação:** UTF-8 com ou sem BOM (`0xEF, 0xBB, 0xBF`). O BOM deve ser descartado pelo parser.
8. **Registros Inconsistentes e Modos de Conformidade:**
   - **Modo Estrito (`StrictRfc4180 = true`):**
     - Exige número idêntico de colunas em todas as linhas (RFC 4180 §2.4). Divergências lançam `CorruptDataException`.
     - Rejeita caracteres arbitrários após aspas de fechamento (RFC 4180 §2.7).
     - Rejeita aspas não fechadas no fim do arquivo com `CorruptDataException`.
   - **Modo Permissivo (`StrictRfc4180 = false`, Padrão do Lodino):**
     - Linhas com menos campos são preenchidas com string vazia `""`.
     - Linhas com mais campos expandem o cabeçalho automaticamente com `ExtraColumn_N`, evitando truncamento e perda silenciosa de dados.
     - Aspas não fechadas no final do arquivo são toleradas e o registro é preservado.

---

## 2. JSON (JavaScript Object Notation)

### Especificação Base
- **Padrão:** ECMA-404 (*The JSON data interchange syntax*, 2nd Edition) e IETF RFC 8259.
- **MIME Type:** `application/json`.

### Regras Gramaticais e Tipos Suportados
1. **Tipos Primitivos:**
   - `null` (literal exato).
   - `true` / `false` (booleanos literais).
   - `number`: inteiros (`0`, `42`, `-10`), ponto flutuante (`3.14159`), notação científica (`1e10`, `2.5E-3`). Rejeita zeros à esquerda inválidos (`0123`).
   - `string`: sequência de caracteres Unicode entre aspas duplas (`"`), com escapes suportados:
     - `\"` (aspas)
     - `\\` (barra invertida)
     - `\/` (barra)
     - `\b` (backspace)
     - `\f` (form feed)
     - `\n` (line feed)
     - `\r` (carriage return)
     - `\t` (tabulação horizontal)
     - `\uXXXX` (código hexadecimal de 4 dígitos para UTF-16, com suporte a pares substitutos / surrogate pairs).
2. **Tipos Estruturados:**
   - `array`: lista ordenada `[ valor, ... ]`.
   - `object`: conjunto não ordenado de pares chave/valor `{ "chave": valor, ... }`. Chaves devem ser sempre strings entre aspas.
3. **Limites de Segurança:**
   - Profundidade máxima de aninhamento: 128 níveis (previne estouro de pilha por recursão profunda).
   - Tamanho máximo de string individual: 32 MB.

---

## 3. INI (Initialization / Configuration Format)

### Dialeto Formal Adotado pelo Lodino
Como o formato INI não possui uma RFC oficial única, definimos formalmente o dialeto suportado:

1. **Seções:**
   - Delimitadas por colchetes: `[NomeDaSecao]`.
   - Espaços antes ou depois dos colchetes são ignorados.
   - Parâmetros definidos antes do primeiro cabeçalho pertencem à seção global `[__global__]`.
2. **Propriedades (Chave e Valor):**
   - Sintaxe: `chave = valor` ou `chave: valor`.
   - O delimitador principal de escrita é `=`.
   - Chave e valor têm espaços em branco nas extremidades removidos (`trim()`), a menos que o valor esteja delimitado por aspas (`"valor"`).
3. **Comentários:**
   - Linhas iniciadas por ponto-e-vírgula (`;`) ou cerquilha (`#`) são tratadas como comentários e ignoradas pelo leitor.
   - Comentários inline após valores não delimitados por aspas são suportados se precedidos por espaço e `;`.
4. **Valores Vazios:**
   - `chave =` resulta em uma string vazia `""`.
5. **Chaves Duplicadas:**
   - Se uma chave for repetida dentro da mesma seção, o último valor prevalece.

---

## 4. Matriz de Conversão e Regras de Perda de Informação

| Conversão | Mapeamento Estrutural | Perdas Potenciais / Avisos de Fidelidade |
| :--- | :--- | :--- |
| **CSV -> JSON** | Cria uma lista de objetos JSON usando os cabeçalhos como chaves. | Tipagem: todos os campos do CSV são lidos como strings a menos que inferência numérica/booleana esteja ativada. |
| **JSON -> CSV** | Se o JSON for array de objetos: unifica todas as chaves em colunas. | Objetos aninhados ou arrays internos dentro de um registro são serializados como JSON embutido na célula. Informação hierárquica é aplanada. |
| **INI -> JSON** | Cada seção torna-se um objeto JSON; propriedades viram pares chave/valor. | Comentários e ordem exata das seções podem não ser preservados no destino. |
| **JSON -> INI** | Objeto JSON de 2 níveis: chaves de nível 1 viram seções `[secao]`, nível 2 viram propriedades. | Arrays ou objetos com mais de 2 níveis de profundidade não têm representação nativa em INI e geram alerta ou serialização em string. |
| **CSV -> INI** | Cada linha vira uma seção nomeada pelo índice ou pelo valor da primeira coluna. | Ineficiente para tabelas grandes; requer que cada registro tenha identificador único. |
| **INI -> CSV** | Gera tabela com 3 colunas padrão: `Section`, `Key`, `Value`. | Reestrutura a apresentação original do arquivo. |
