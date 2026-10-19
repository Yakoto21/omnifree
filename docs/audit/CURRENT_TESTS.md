# Lodino — Análise dos Testes Existentes e Cobertura

Avaliação detalhada da suíte de testes legada do repositório em `test/`.

---

## 1. Inventário dos Testes Existentes (5 Arquivos / 18 Casos de Teste)

| Arquivo | Quantidade | O que é Efetivamente Testado | Limitações Observadas |
| :--- | :--- | :--- | :--- |
| `test/catalog.test.js` | 4 testes | - Identificação de formato de mídia (`.mp4` suporta `.mp3`).<br>- Seleção do motor para CSV (`data` para json, `libreoffice` para xlsx).<br>- Rejeição de extensão desconhecida (`.xyz`).<br>- Garantia de rota existente para o primeiro formato de cada categoria. | Não testa combinações cruzadas inválidas, arquivos sem extensão ou com múltiplos pontos (`arquivo.tar.gz`). |
| `test/data-converter.test.js` | 1 teste | - Conversão de JSON plano para YAML (`{"nome":"OmniFree"}`). | **Cobertura ínfima.** Não testa CSV, INI, XML, TOML, Unicode, estruturas aninhadas, quebras de linha ou caracteres especiais. |
| `test/engines.test.js` | 7 testes | - Resolução de pastas em `engines/`.<br>- Sharp convertendo SVG para PNG.<br>- FFmpeg gerando tom senoidal WAV (`lavfi`).<br>- PDF-lib criando e relendo 1 página.<br>- LibreOffice convertendo TXT para PDF (condicional com `t.skip`).<br>- 7-Zip gerando `.zip` (condicional com `t.skip`).<br>- Calibre gerando `.epub` a partir de HTML (condicional com `t.skip`). | Testes de integração dependem de binários externos instalados no host. Se ausentes, são ignorados silenciosamente sem validar nada. |
| `test/output-paths.test.js` | 1 teste | - Verificação de que arquivo existente recebe sufixo `(1)` para evitar sobrescrita. | Não testa comportamento sob concorrência ou falta de permissão de escrita no diretório. |
| `test/reliability.test.js` | 5 testes | - Comparação semântica de versões de atualização (sem downgrade).<br>- Tradução de erros técnicos (404, ENOENT, EACCES) em mensagens amigáveis.<br>- Simulação do fluxo de download e instalação do updater.<br>- Parser de intervalos de páginas de PDF (`1, 3-5`).<br>- Verificação estática de código-fonte (assegura que canais IPC, botões e labels existem nos arquivos). | O teste de canais é uma checagem sintática de texto (regex no código), não uma validação funcional ponta a ponta. |

---

## 2. Lacunas Críticas de Teste

A suíte legada não possui cobertura para:
1. **Entradas Malformadas:** Nenhum teste com arquivos corrompidos, buffers truncados, cabeçalhos falsos ou valores fora dos limites.
2. **Transformações de Imagem:** Nenhum teste para algoritmos de interpolação, recorte, coordenadas de marca-d'água ou compressão.
3. **Casos Limítrofes de Dados:**
   - No CSV: vírgulas dentro de aspas, quebras de linha no conteúdo (CRLF dentro do campo citado), aspas escapadas como `""`.
   - No INI: comentários `#` vs `;`, linhas em branco, seções vazias, caracteres especiais nas chaves.
   - No JSON: profundidade excessiva, números gigantescos, escapes Unicode `\uXXXX`.
4. **Fidelidade de Conversão e Round-Trip:** Ausência total de testes diferenciais de entrada e saída.
5. **Concorrência e Cancelamento:** Nenhum teste valida se o cancelamento de uma tarefa libera recursos e remove arquivos temporários imediatamente.

---

## 3. Diretrizes para a Nova Infraestrutura de Testes do Lodino

A nova plataforma exigirá:
- **Fixtures Determinísticas:** Arquivos reais de teste versionados em `tests/fixtures/`, com licenças explícitas e tamanhos controlados.
- **Testes Negativos Obrigatórios:** Todo parser novo deve conter testes com arquivos vazios, corrompidos e hostis.
- **Oráculos de Teste:** Comparação matemática dos resultados com validação de invariantes (ex: decodificar imagem e verificar dimensões exatas e histograma de cor).
- **Sem Dependência de Janela Gráfica:** 100% dos motores devem ser testáveis via console/linha de comando em ambiente headless e em CI.

---

## 4. Estado Atual da Suíte de Testes Proprietária do Lodino (`build/Lodino.Tests.exe`)

**Total de Testes Aprovados:** 87 | **Falhas:** 0 | **Dependências Externas:** 0

| Módulo / Motor | Testes | Cobertura Chave |
|---|---|---|
| **Piloto 1: Dados Estruturados** (`lodino.engine.data`) | 40 | RFC 4180 estrito/permissivo (aspas, multilinha, vírgulas, CRLF/LF, BOM, colunas inconsistentes), ECMA-404 JSON (primitivos, Unicode escapes, surrogate pairs, precisão numérica, DoS depth), dialeto formal INI (comentários `#`/`;`, quoted values, seções), 9 rotas cruzadas (CSV <-> JSON <-> INI), caminhos seguros anti-sobrescrita e EngineRegistry. |
| **Piloto 2: Imagens BMP** (`image-bmp`) | 32 | Padrão Windows DIB 24-bit e 32-bit `BI_RGB` (semântica BGRX normalizada opaca $A=255$), cálculo de stride/padding (0, 1, 2 e 3 bytes), Bottom-Up vs Top-Down, redimensionamento vizinho mais próximo e bilinear, recorte em 9 quadrantes, marca d'água textual 8x8 e imagem com Porter-Duff Source-Over, fixtures independentes montadas byte a byte, limites de segurança e detecção de overflow. |
| **Piloto 3: Imagens PNG** (`image-png`) | 15 | ISO/IEC 15948 8-bit Truecolor RGB (ColorType 2) e RGBA (ColorType 6), não-entrelaçado; ISO 3309 CRC-32 e RFC 1950 Adler-32; descompressor DEFLATE (RFC 1951) puro com blocos armazenados (00), Huffman fixo (01) e dinâmico (10) com LZ77 e janela de 32 KB; compressor ZLIB puro; reversibilidade dos 5 filtros de linha (None, Sub, Up, Average, Paeth); fixtures independentes RGB/RGBA; fragmentação em múltiplos IDATs; rejeição estrita (indexado, escala de cinza, 16-bit, Adam7, corrupção, DoS); inspeção de metadados; pipeline de transformações; conversão bidirecional PNG <-> BMP. |

