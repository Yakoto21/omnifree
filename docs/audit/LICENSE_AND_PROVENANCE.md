# Lodino — Auditoria de Licenças e Procedência

Auditoria de conformidade jurídica, riscos de redistribuição e diretrizes de propriedade intelectual para o desenvolvimento de tecnologia própria.

---

## 1. Riscos Críticos de Licenciamento no Modelo Atual

A aplicação em seu estado legado distribui ou prepara a inclusão de componentes externos que apresentam severas incompatibilidades ou obrigações com produtos fechados/proprietários:

### A. GPL-3.0-or-later em `ffmpeg-static`
- **Diagnóstico:** O pacote `ffmpeg-static` fornece uma compilação pré-construída do FFmpeg sob a licença **GNU General Public License v3.0**.
- **Impacto Jurídico:** A GPL v3 é uma licença copyleft estrita. Se o executável for empacotado no mesmo instalador do aplicativo e integrado diretamente ao seu fluxo de valor comercial, há forte jurisprudência de que a obra combinada distribuída deve fornecer a totalidade de seu código-fonte sob os termos da GPL-3.0.
- **Resolução:** A substituição gradual por decodificadores e multiplexadores próprios elimina a dependência desse executável.

### B. AGPL-3.0 em `ghostscript`
- **Diagnóstico:** Declarado em `engines/manifest.json` e executado em `src/main/pdf-service.js` (linha 16) para otimização de PDFs.
- **Impacto Jurídico:** A **GNU Affero General Public License (AGPL v3)** é a modalidade de copyleft mais restritiva existente, cobrindo inclusive interação remota via rede. A distribuição de binários do Ghostscript sem aquisição de licença comercial da Artifex Software impõe obrigações severas de fornecimento de código-fonte.
- **Resolução:** A otimização e compressão de PDF deve ser executada exclusivamente pelo nosso motor interno de re-escrita e compressão de streams de objetos.

### C. GPL-2.0-or-later em `poppler` (`pdfimages` e `pdftoppm`)
- **Diagnóstico:** Utilizado para extrair imagens e renderizar páginas de PDF para o OCR e leitor de códigos em `pdf-service.js` e `recognition-service.js`.
- **Impacto Jurídico:** Licenciado sob GPL v2, inviabilizando integração em produto de código fechado caso embutido no instalador.

### D. GPL-3.0 em `calibre` (`ebook-convert`)
- **Diagnóstico:** Utilizado para conversões de livros digitais em `conversion-service.js`.
- **Impacto Jurídico:** O Calibre é inteiramente GPL v3.

---

## 2. Incertezas de Licenciamento na Árvore Transitiva

A auditoria automatizada do `package-lock.json` identificou duas dependências com licença `UNSPECIFIED` (não informada no manifesto):

1. **`node_modules/async` (v0.2.10):**
   - Transiente puxado por `node-pandoc` (`^0.3.0`).
   - Versão legada com manifesto incompleto.
   - **Mitigação:** Eliminação sumária de `node-pandoc` do projeto.
2. **`node_modules/parse-cache-control` (v1.0.1):**
   - Transiente de `builder-util-runtime` via `electron-updater`.
   - Repositório público original é MIT, mas a tag SPID do npm não veio preenchida na versão em cache.
   - **Mitigação:** Será removido quando a aplicação for portada para o shell nativo Windows.

---

## 3. Protocolo de Procedência do Código Próprio Lodino

Para cada módulo ou motor introduzido nas branches do Lodino:

1. **Autoria Direta:** Todo código deve ser escrito especificamente para o projeto, sem transposição de blocos de implementações externas.
2. **Registro de Fontes de Especificação:** Devem ser documentados em cabeçalho os padrões públicos utilizados (ex: RFC 4180 para CSV, ECMA-404 para JSON, ISO 32000-1 para PDF, Microsoft BMP Specification, etc.).
3. **Isolamento de Oráculos de Teste:** Caso ferramentas de referência sejam utilizadas para validar conformidade diferencial em testes, elas devem rodar estritamente fora do runtime de produção e seus binários jamais serão versionados no núcleo.
4. **Sem Fallback Oculto:** O código próprio não deve fingir sucesso chamando ferramentas de terceiros em segundo plano.
