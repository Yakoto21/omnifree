# Lodino — Inventário de Dependências e Ferramentas

Auditoria técnica das dependências declaradas em `package.json`, resolvidas em `package-lock.json` e ferramentas externas do sistema.

---

## 1. Classificação Estratégica (5 Categorias)

Conforme os critérios estabelecidos no Master Plan:

### Categoria 1: Necessária para o Shell ou Cadeia de Build
- `electron` (`^29.0.0` / resolvido `29.4.6`) — Licença MIT. Runtime desktop atual.
- `electron-builder` (`^24.13.3`) — Licença MIT. Gerador do instalador NSIS para Windows.
- `electron-updater` (`^6.8.9`) — Licença MIT. Cliente de atualização automática via GitHub.
- `png-to-ico` (`^3.0.2`) — Licença MIT. Utilitário de conversão de ícones no build.

*Meta arquitetural:* Substituir o shell Electron por aplicação nativa Windows (C++ ou C# nativo), eliminando toda essa cadeia do produto final.

---

### Categoria 2: Utilizada Apenas Durante Desenvolvimento e Testes
- Módulo nativo do Node.js `node:test` e `node:assert/strict`. Utilizado pelos arquivos em `test/`.
- Nenhuma biblioteca externa de testes (como Jest ou Mocha) está instalada.

---

### Categoria 3: Dependências do Núcleo de Processamento (ALVO DE ELIMINAÇÃO)
As seguintes bibliotecas npm são consumidas diretamente pelo código de conversão:

| Pacote | Versão | Licença | Uso Atual no Código | Estratégia de Eliminação |
| :--- | :--- | :--- | :--- | :--- |
| `sharp` | `0.35.4` | Apache-2.0 / LGPL-3.0+ (`libvips`) | Decodificação, redimensionamento, recorte, marca-d'água e conversão de imagens. | **Substituir por motor próprio de raster e manipulação de pixels em memória.** |
| `pdf-lib` | `1.17.1` | MIT | Manipulação estrutural de páginas de PDF (merge, split, rotação). | **Substituir por leitor e escritor próprio de objetos PDF (ISO 32000).** |
| `@zxing/library` | `0.21.3` | MIT | Decodificação de códigos de barras 1D e QR Codes 2D. | **Substituir por motor próprio de binarização e decodificação geométrica.** |
| `fast-xml-parser` | `5.11.1` | MIT | Parser e builder de documentos XML no `data-converter.js`. | **Substituir por parser de eventos XML próprio sem alocações desnecessárias.** |
| `yaml` | `2.9.1` | ISC | Parser e stringifier de documentos YAML. | **Substituir por leitor de estruturas hierárquicas YAML próprio.** |
| `@iarna/toml` | `2.2.5` | ISC | Parser de arquivos TOML. | **Substituir por parser léxico de TOML próprio.** |

---

### Categoria 4: Executáveis Externos de Conversão (ALVO DE ELIMINAÇÃO)
Ferramentas binárias que realizam o processamento pesado fora do processo Node.js:

| Executável | Pacote / Origem | Licença Declarada | Tamanho Estimado | Impacto no Projeto |
| :--- | :--- | :--- | :--- | :--- |
| `ffmpeg.exe` | `ffmpeg-static` (`^5.3.0`) | **GPL-3.0-or-later** | ~80 MB | Conversão de áudio, vídeo, legendas e thumbnails. Impõe risco legal estrito de copyleft. |
| `soffice.exe` | LibreOffice Portable / Sistema | MPL-2.0 / LGPL-3.0+ | ~350 MB a 1 GB | Conversão headless de documentos, planilhas e apresentações. |
| `7z.exe` | 7-Zip Portable / Sistema | LGPL-2.1+ / unRAR | ~3 MB | Extração e compressão de arquivos `.zip`, `.7z`, `.tar`, `.gz`. |
| `ebook-convert.exe` | Calibre Portable / Sistema | **GPL-3.0** | ~150 MB | Conversão de livros digitais e e-books. |
| `qpdf.exe` | QPDF Portable / Sistema | Apache-2.0 | ~15 MB | Proteção de PDFs com senha e linearização de streams. |
| `pdfimages.exe` / `pdftoppm.exe` | Poppler Portable / Sistema | **GPL-2.0-or-later** | ~30 MB | Extração de imagens e rasterização de páginas de PDF. |
| `gswin64c.exe` | Ghostscript Portable / Sistema | **AGPL-3.0** | ~50 MB | Otimização e compressão de PDFs. Licença AGPL de altíssimo risco. |
| `tesseract.exe` | Tesseract-OCR / Sistema | Apache-2.0 | ~60 MB | Reconhecimento óptico de caracteres (OCR). |

---

### Categoria 5: Recurso Opcional ou Não Utilizado (REMOÇÃO IMEDIATA)
- `node-pandoc` (`^0.3.0`):
  - Declarado em `package.json` (linha 21).
  - Traz dependência transitiva legada `async@0.2.10` sem licença explícita no npm.
  - **Não é importado em nenhum arquivo do projeto** (`conversion-catalog.js`, `conversion-service.js` ou `main.js`).
  - Totalmente inerte e candidato à remoção sem qualquer perda funcional.

---

## 2. Análise Quantitativa do `package-lock.json`

- **Versão do Lockfile:** 3
- **Total de Pacotes na Árvore:** 400 entradas
- **Distribuição de Licenças em Todas as Dependências:**
  - `MIT`: 289 pacotes
  - `ISC`: 45 pacotes
  - `Apache-2.0`: 23 pacotes
  - `LGPL-3.0-or-later`: 10 pacotes (módulos `@img/sharp-libvips-*`)
  - `BSD-2-Clause`: 6 pacotes
  - `BlueOak-1.0.0`: 5 pacotes
  - `BSD-3-Clause`: 5 pacotes
  - `Apache-2.0 AND LGPL-3.0-or-later`: 3 pacotes (`@img/sharp-win32-*`)
  - `GPL-3.0-or-later`: 1 pacote (`ffmpeg-static`)
  - `UNSPECIFIED`: 2 pacotes (`async@0.2.10` via `node-pandoc` e `parse-cache-control@1.0.1` via `builder-util-runtime`)
  - Outras permissivas: 11 pacotes
