# Lodino — Baseline Técnico Inicial

**Data da Auditoria:** 19 de Outubro de 2026  
**Repositório de Origem:** `Yakoto21/omnifree`  
**Identificador Local:** `c:\Projetos\lodino`  
**Branch de Trabalho:** `feature/proprietary-core` (derivada da `main` no commit `e9638f3`)  
**Commit de Referência (HEAD da main):** `e9638f3` (*Prepare bundled engine runtime*)  
**Estado da Árvore:** Limpa, sem arquivos não rastreados antes da inicialização desta documentação.

---

## 1. Identidade do Projeto

Conforme registrado em `package.json`:
- **Nome interno (`name`):** `meu-conversor`
- **Nome do produto (`productName`):** `OmniFree`
- **Versão:** `1.0.14`
- **Descrição:** `Conversor local de arquivos OmniFree.`
- **App ID:** `com.omnifree.app`
- **Destino de Atualizações:** GitHub Releases em `Yakoto21/omnifree`
- **Ponto de Entrada:** `src/main/main.js`

---

## 2. Inventário de Arquivos do Repositório (32 Arquivos)

| Caminho | Linhas / Tamanho | Função |
| :--- | :--- | :--- |
| `.gitignore` | 73 B | Ignora `node_modules`, `dist`, `.DS_Store` e arquivos temporários. |
| `package.json` | 67 linhas (1.701 B) | Manifesto npm, scripts de build, dependências diretas e configurações do electron-builder. |
| `package-lock.json` | 400 pacotes (189.830 B) | Árvore resolvida de dependências transitivas (lockfileVersion 3). |
| `README.md` | 27 linhas (863 B) | Instruções sucintas de desenvolvimento e publicação de releases. |
| `THIRD_PARTY_NOTICES.md` | 6 linhas (583 B) | Aviso sobre componentes de terceiros e futuras adições em `engines/`. |
| `.github/workflows/release.yml` | 32 linhas (701 B) | Pipeline de CI/CD no GitHub Actions disparado por tags `v*`. |
| `build/omnifree-icon.ico` | 285.478 B | Ícone multi-resolução para o executável Windows. |
| `build/omnifree-icon.png` | 1.239.719 B | Imagem fonte do ícone. |
| `engines/manifest.json` | 16 linhas (584 B) | Declaração de caminhos relativos para 8 motores portáteis esperados. |
| `engines/README.md` | 10 linhas (758 B) | Orientação sobre inclusão manual de cópias portáteis dos motores. |
| `scripts/create-icon.js` | 6 linhas (212 B) | Script de geração do arquivo `.ico` a partir de `omnifree-icon.png`. |
| `src/main/conversion-catalog.js` | 27 linhas (2.744 B) | Catálogo de formatos suportados, agrupamento por categorias e resolução de motores. |
| `src/main/conversion-service.js` | 32 linhas (5.173 B) | Orquestrador de conversão conectando `sharp`, `ffmpeg`, `soffice`, `7z`, `calibre` e `data`. |
| `src/main/data-converter.js` | 43 linhas (3.302 B) | Conversor de formatos estruturados (JSON, CSV, XML, YAML, TOML, INI). |
| `src/main/error-messages.js` | 10 linhas (797 B) | Mapeamento de erros de sistema (ENOENT, EACCES, ENOSPC) para mensagens amigáveis. |
| `src/main/main.js` | 62 linhas (6.658 B) | Ciclo de vida Electron, criação da janela, isolamento de contexto e handlers IPC. |
| `src/main/media-service.js` | 13 linhas (951 B) | Extração de legendas e thumbnails usando FFmpeg. |
| `src/main/output-paths.js` | 17 linhas (681 B) | Geração de nomes únicos de saída com sufixo incremental `(1)` para evitar sobrescrita. |
| `src/main/pdf-service.js` | 22 linhas (4.665 B) | Operações em PDF: merge, split, extract, rotate, protect, optimize, extract images, webToPdf. |
| `src/main/preferences-service.js`| 16 linhas (1.618 B) | Persistência de configurações e monitoramento de pastas via `fs.watch`. |
| `src/main/recognition-service.js`| 16 linhas (2.244 B) | Leitura de QR/Barras (ZXing + Sharp) e OCR (Tesseract + Poppler). |
| `src/main/system-tools.js` | 36 linhas (4.029 B) | Resolução de PATH para ferramentas portáteis em `engines/` ou instaladas no Windows. |
| `src/main/update-service.js` | 49 linhas (3.310 B) | Verificação, download e instalação de updates via `electron-updater`. |
| `src/preload/preload.js` | 38 linhas (3.083 B) | Ponte segura IPC via `contextBridge.exposeInMainWorld('conversorAPI', ...)`. |
| `src/renderer/index.html` | 12 linhas (13.125 B) | Estrutura HTML da interface (layout em 3 colunas, modais e controles). |
| `src/renderer/renderer.js` | 556 linhas (70.851 B) | Lógica completa de UI: fila, drag-and-drop, filtros, histórico, perfis, traduções. |
| `src/renderer/style.css` | 14.036 B | Folha de estilos, suporte a tema escuro/claro e componentes de controle. |
| `test/catalog.test.js` | 24 linhas (973 B) | 4 testes do catálogo de conversão. |
| `test/data-converter.test.js` | 14 linhas (670 B) | 1 teste de conversão JSON -> YAML. |
| `test/engines.test.js` | 86 linhas (4.166 B) | 7 testes dos motores externos e bibliotecas (Sharp, FFmpeg, PDF-lib, LibreOffice, 7z, Calibre). |
| `test/output-paths.test.js` | 15 linhas (697 B) | 1 teste de resolução de caminho único. |
| `test/reliability.test.js` | 64 linhas (4.678 B) | 5 testes de confiabilidade, comparação de versões, erros e canais IPC. |

---

## 3. Ambiente do Sistema e Ferramentas Detectadas

A inspeção do ambiente Windows local registrou:
- **Git:** Git 2.56.0 instalado em `C:\Program Files\Git\cmd\git.exe`.
- **Python:** Python 3.15.0 funcional via terminal.
- **Compilador C# / .NET Framework Nativo:** `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` e `msbuild.exe` presentes e utilizáveis para geração de executáveis nativos Windows (`.exe` e `.dll`) sem necessidade de pacotes externos.
- **Node.js:** Não configurado no PATH do sistema.
- **Pasta `node_modules`:** Inexistente no diretório clonado.
- **Pasta `engines/`:** Contém apenas documentação e manifesto, vazia de binários portáteis.

---

## 4. Limitações de Validação na Fase Baseline

1. **Execução Direta do Electron Legado:** Devido à ausência de `node_modules` e de Node.js no PATH do shell, a aplicação legada em Electron não é executável neste host sem instalação prévia.
2. **Dependência de Ferramentas Externas:** Os testes que validam integração com LibreOffice, 7-Zip, Ghostscript, Poppler, Tesseract e Calibre são pulados (`t.skip()`) quando os binários não se encontram instalados na máquina do desenvolvedor.
3. **Decisão:** O repositório legado serve como documentação de contratos, interface e comportamento funcional a ser integralmente preservado na nova arquitetura nativa independente.
