# Lodino — Inventário Funcional Detalhado

Este documento deriva das inspeções realizadas diretamente no código-fonte de `src/main/conversion-catalog.js`, `src/main/conversion-service.js`, `src/main/data-converter.js`, `src/main/pdf-service.js`, `src/main/recognition-service.js`, `src/main/media-service.js` e `src/renderer/renderer.js`.

---

## 1. Classificação das Funcionalidades

Para cada funcionalidade, categorizamos o estado real de implementação:
- **Funcional (Biblioteca Terceirizada):** Código em JavaScript que depende de biblioteca npm embutida.
- **Dependente de Binário Externo:** Código que apenas invoca executável de terceiros via `spawn`.
- **Parcial / Misto:** Combina código interno com ferramentas de apoio.
- **Apenas Anunciada / Não Implementada:** Declarada em manifestos ou interface sem código funcional de conversão.
- **Shell / Aplicação:** Funcionalidades de suporte, interface e integração desktop.

---

## 2. Matriz de Motores de Conversão

### A. Grupo Imagem
- **Entradas:** `png`, `jpg`, `jpeg`, `webp`, `tif`, `tiff`, `avif`, `heic`, `heif`, `bmp`, `svg`, `psd`, `gif` (13 formatos)
- **Saídas:** `png`, `jpg`, `webp`, `avif`, `tiff` (5 formatos)
- **Serviço Responsável:** `conversion-service.js` (linhas 21-22)
- **Motor Utilizado:** Biblioteca `sharp` (v0.35.4) sobre `libvips`
- **Opções Suportadas:**
  - `width` / `height`: redimensionamento proporcional ou por recorte.
  - `cropImage`: ajuste `cover` vs `inside`.
  - `cropPosition`: grade de 9 quadrantes (`northwest`, `centre`, `southeast`, etc.).
  - `watermark`: texto inserido via sobreposição de markup SVG gerado dinamicamente.
  - `watermarkImage`: imagem de sobreposição com opacidade 72% e gravidade sudeste.
  - `removeMetadata`: inclusão ou remoção de tags EXIF/IPTC (`withMetadata()`).
  - `quality`: compressão 20 a 100%.
  - `batchSizes`: tamanhos múltiplos especificados por texto (ex.: `800x600, 1920x1080`).
- **Comportamento em Falha:** Captura de erro com retorno de mensagem via IPC.
- **Situação:** *Funcional via biblioteca de terceiros*.

### B. Grupo Áudio e Vídeo
- **Entradas:** `mp4`, `mkv`, `mov`, `avi`, `webm`, `flv`, `wmv`, `m4v`, `mpg`, `mpeg`, `mts`, `m2ts`, `3gp`, `ts`, `vob`, `ogv`, `mp3`, `wav`, `aac`, `m4a`, `ogg`, `flac`, `wma`, `aiff`, `opus`, `amr`, `ac3`, `gif` (28 formatos)
- **Saídas:** `mp4`, `mkv`, `mov`, `avi`, `webm`, `gif`, `mp3`, `wav`, `ogg`, `flac`, `m4a`, `opus` (12 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 23)
- **Motor Utilizado:** `fluent-ffmpeg` (v2.1.3) + executável `ffmpeg-static` (v5.3.0)
- **Opções Suportadas:**
  - `startTime` / `duration`: corte de trechos.
  - `audioOnly`: descarta fluxo de vídeo (`noVideo()`).
  - `noAudio`: remove fluxo de áudio (`noAudio()`).
  - `removeMetadata`: remove metadados (`-map_metadata -1`).
  - `quality`: mapeamento numérico para CRF (`-crf Math.max(0, Math.min(51, 51 - quality/2))`).
  - Progresso percentual emitido por evento do FFmpeg.
  - Cancelamento por sinal `SIGKILL` no processo filho.
- **Situação:** *Dependente de binário externo (FFmpeg GPL)*.

### C. Grupo Documentos
- **Entradas:** `pdf`, `doc`, `docx`, `docm`, `odt`, `rtf`, `txt`, `md`, `html`, `htm`, `xps` (11 formatos)
- **Saídas:** `pdf`, `docx`, `odt`, `rtf`, `txt`, `html` (6 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 12)
- **Motor Utilizado:** Executável `soffice.exe` (LibreOffice)
- **Comando Executado:** `soffice --headless --convert-to <target> --outdir <tmp> <input>`
- **Potenciais Perdas de Informação:** A conversão entre formatos distintos (ex: RTF -> HTML ou PDF -> TXT) perde estilos complexos, diagramação, paginação e fontes embutidas.
- **Situação:** *Dependente de binário externo (LibreOffice)*. Se não instalado, falha imediatamente com ENOENT.

### D. Grupo Planilhas
- **Entradas:** `xlsx`, `xls`, `xlsm`, `xlsb`, `ods`, `csv`, `tsv`, `dbf` (8 formatos)
- **Saídas:** `xlsx`, `ods`, `csv`, `tsv`, `pdf` (5 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 12)
- **Motor Utilizado:** Executável `soffice.exe` (LibreOffice)
- **Situação:** *Dependente de binário externo (LibreOffice)*.
- **Nota Especial:** Formato `csv` também pode ser resolvido pelo motor de dados quando o destino for `json`, `xml`, `yaml`, `toml` ou `ini`.

### E. Grupo Apresentações
- **Entradas:** `ppt`, `pptx`, `pptm`, `pps`, `ppsx`, `pot`, `potx`, `odp` (8 formatos)
- **Saídas:** `pptx`, `odp`, `pdf` (3 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 12)
- **Motor Utilizado:** Executável `soffice.exe` (LibreOffice)
- **Situação:** *Dependente de binário externo (LibreOffice)*.

### F. Grupo Arquivos Compactados
- **Entradas:** `zip`, `7z`, `tar`, `gz`, `bz2`, `xz`, `cab`, `iso`, `rar` (9 formatos)
- **Saídas:** `zip`, `7z`, `tar`, `gz`, `bz2`, `xz` (6 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 13)
- **Motor Utilizado:** Executável `7z.exe` (7-Zip)
- **Comandos Executados:** `7z x <input> -o<tmp> -y` seguido de `7z a -t<ext> <output> <tmp>/*`
- **Risco de Segurança:** Ausência de checagem contra *path traversal* (extração de arquivos com `../`).
- **Situação:** *Dependente de binário externo (7-Zip)*.

### G. Grupo E-books
- **Entradas:** `epub`, `mobi`, `azw3`, `fb2`, `lrf`, `pdb`, `cbz`, `cbr` (8 formatos)
- **Saídas:** `epub`, `mobi`, `azw3`, `pdf` (4 formatos)
- **Serviço Responsável:** `conversion-service.js` (linha 24)
- **Motor Utilizado:** Executável `ebook-convert.exe` (Calibre)
- **Situação:** *Dependente de binário externo (Calibre GPL-3.0)*.

### H. Grupo Dados Estruturados
- **Entradas:** `json`, `csv`, `xml`, `yaml`, `yml`, `toml`, `ini` (7 formatos)
- **Saídas:** `json`, `csv`, `xml`, `yaml`, `toml`, `ini` (6 formatos)
- **Serviço Responsável:** `data-converter.js`
- **Motor Utilizado:** Código JavaScript próprio para CSV e INI; bibliotecas npm para YAML (`yaml`), TOML (`@iarna/toml`) e XML (`fast-xml-parser`).
- **Fidelidade e Limitações:**
  - CSV aceita apenas tabelas planas; converter JSON aninhado para CSV aplana apenas campos de primeiro nível ou gera representações incompletas.
  - INI suporta apenas chave=valor e seções de 1 nível de profundidade.
- **Situação:** *Parcialmente próprio / Bibliotecas JS*.

---

## 3. Ferramentas Especializadas e Operações em PDF

| Recurso | Arquivo Fonte | Mecanismo Atual | Situação Real |
| :--- | :--- | :--- | :--- |
| **Mesclar PDFs (Merge)** | `pdf-service.js` (l. 11) | `pdf-lib` (`PDFDocument.create`, `copyPages`) | Funcional via biblioteca JS |
| **Separar PDF (Split)** | `pdf-service.js` (l. 12) | `pdf-lib` (cria 1 arquivo por página) | Funcional via biblioteca JS |
| **Extrair Páginas PDF** | `pdf-service.js` (l. 13) | `pdf-lib` + parser manual de ranges `1, 3-5` | Funcional via biblioteca JS |
| **Girar PDF (Rotate)** | `pdf-service.js` (l. 14) | `pdf-lib` (adiciona 90° com `degrees`) | Funcional via biblioteca JS |
| **Proteger PDF por Senha** | `pdf-service.js` (l. 15) | Executável `qpdf --encrypt` | Dependência de binário externo |
| **Compactar/Otimizar PDF** | `pdf-service.js` (l. 16) | Executável `gswin64c` (Ghostscript) ou `qpdf` | Dependência de binário externo |
| **Extrair Imagens de PDF** | `pdf-service.js` (l. 17) | Executável `pdfimages -all` (Poppler) | Dependência de binário externo |
| **Página Web para PDF** | `pdf-service.js` (l. 18) | Electron `BrowserWindow.webContents.printToPDF` | Funcional via Chromium/Electron |
| **Extrair Legendas** | `media-service.js` (l. 8) | FFmpeg (`-map 0:s:0`) para `.srt`, `.vtt` ou `.ass` | Dependência de binário externo |
| **Gerar Thumbnail de Vídeo** | `media-service.js` (l. 9) | FFmpeg (`seekInput(time)`, 1 frame) para `.jpg` | Dependência de binário externo |
| **Ler QR Code / Código de Barras** | `recognition-service.js` (l. 11) | `@zxing/library` + Sharp (`removeAlpha().raw()`) | Funcional via biblioteca JS (+ Poppler se PDF) |
| **OCR (Imagem e PDF para Texto)** | `recognition-service.js` (l. 12) | Executável `tesseract` (+ `pdftoppm` se PDF) | Dependência de binário externo |
| **Pandoc** | Manifesto / `package.json` | Nenhuma chamada no código | **Apenas anunciado / Não implementado** |

---

## 4. Recursos de Gestão e Aplicação Desktop (Shell)

1. **Fila de Conversão em Lote:** Suporta múltiplos arquivos, reordenação via arrastar e soltar, cancelamento individual/geral e tentativa unitária de arquivos com erro (`retryOnlyFiles`).
2. **Pastas Monitoradas (Regras Automáticas):** Utiliza `fs.watch(rule.folder)` para detectar novos arquivos e disparar conversão automática para formato e pasta de destino configurados.
3. **Perfis de Conversão:** Armazenamento em `localStorage` com pré-ajustes (`balanced`, `small`, `quality`, `whatsapp`, `instagram`, `youtube`, `print`) e personalização de usuário.
4. **Histórico Local e Modo de Privacidade:** Registro das últimas 50 operações, exportação para CSV ou JSON e limpeza automática opcional no encerramento da janela (`beforeunload`).
5. **Renomeação em Lote:** Injeção de prefixo, sufixo, substituição textual, carimbo de data ISO e numeração sequencial formatada (`_001`).
6. **Mecanismo de Atualização:** Integração com GitHub Releases via `electron-updater` (checagem, download e reinicialização para instalação de setup NSIS).
