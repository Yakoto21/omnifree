# LODINO NATIVE SUITE — RELATÓRIO DE VALIDAÇÃO FINAL PÓS-MERGE NA MAIN (FASE 14.3.1)

**Documento:** Relatório Técnico de Smoke Test e Validação de Publicação do Lodino Native Suite na Branch `main`  
**Data:** 20 de Outubro de 2026  
**Repositório:** `C:\Projetos\lodino`  
**Branch Auditada:** `main`  
**Commit-Base Auditado:** `b8c78601c207256bb440950ce9d73a5fd258e1ea` (HEAD da `main` após commit documental da Fase 14.3)  
**Commit de Merge Referenciado:** `52c87000b323d2a4b2a2ea20a784012ddb80e7dc`  
**Ambiente de Homologação:** Microsoft Windows 10 Pro x64 (Build 19045), .NET CLR v4.0.30319.42000, PowerShell 5.1  
**Compilador Oficial:** NSIS v3.13 (`C:\Program Files (x86)\NSIS\Bin\makensis.exe`)  
**Veredito Oficial:**  
$$\mathbf{VEREDITO:\ MAIN\ VALIDADA\ PARA\ PUBLICA\C\tilde{A}O\ CONTROLADA}$$

---

## 1. Integridade Inicial e Estado do Repositório

### 1.1 Confirmação de Branch e Rastreabilidade Git
- **Branch:** `main`
- **HEAD Confirmado:** `b8c78601c207256bb440950ce9d73a5fd258e1ea`
- **Ancestrais Diretos:**
  - `52c87000b323d2a4b2a2ea20a784012ddb80e7dc` (Merge da branch `feature/release-packaging` com `--no-ff`)
  - `f1a212b3c2e7fa09ef90c03f63defeed53cc799f` (Base anterior da `main`)
- **Status do Working Tree:** Arquivos rastreados 100% limpos. Arquivos e relatórios de `scratch/` integralmente preservados.
- **Relação com o Remoto:** A branch `main` local está 28 commits à frente de `origin/main` (política de **zero push** estritamente cumprida).

---

## 2. Relatório da Revisão Multiagente

### 2.1 Agente A — Execução Técnica e Evidências

O Agente A executou sequencialmente todas as 7 etapas de validação requeridas na branch `main`:

#### 2.1.1 Build Release e Testes Unitários do Core (`scripts/build-core.ps1 -Release`)
- **Compilação:** Assemblies compilados com otimização ativa (`/optimize+`).
- **Suíte de Testes:** **227 / 227 testes unitários APROVADOS (100% PASS)** cobrindo todos os módulos:
  - CSV (RFC 4180), JSON (RFC 8259), INI, Matriz de Rotas 9x9 e Resolução Dinâmica;
  - BMP próprio (24/32-bit, BGRX, redimensionamento, recorte, marca d'água);
  - PNG próprio (filtros 0-4, IDAT padding, blindagem contra bombas de descompressão);
  - JPEG próprio (baseline sequential, multiscan, conversão cruzada);
  - WAV próprio (PCM Q31, 8/16/24/32-bit, mono/estéreo, blindagem RIFF);
  - ZIP próprio (EOCD resiliente, hardening Win32, Huffman Dinâmico DEFLATE);
  - GZIP próprio (RFC 1952, streaming O(1), múltiplos membros, CRC-32 e ISIZE);
  - Fila Gráfica Desktop (sugestão inteligente, guarda de sobrescrita e lote misto).
- **Código de Saída:** `0`.

#### 2.1.2 Suíte de Interoperabilidade Externa GZIP (`scratch/gzip_interop/test_gzip_interop.ps1`)
- **Matriz Positiva:** 13 / 13 cenários aprovados com SHA-256 idêntico.
- **Matriz Bidirecional Cruzada (Python, tar, PowerShell):** 52 / 52 cenários aprovados.
- **Matriz Adversarial de Segurança:** 10 / 10 ataques bloqueados com Exit Code 2.
- **Total:** **75 / 75 testes aprovados (100% PASS)**. Código de Saída: `0`.

#### 2.1.3 Fila Mista e Regras de Negócio da GUI RC2 (`run_rc2_gui_mixed_queue_test.ps1`)
- Inicialização da janela Win32 nativa (`HWND` válido) e encerramento limpo via WM_CLOSE.
- Processamento de lotes mistos com dados (CSV), imagens (BMP $\rightarrow$ PNG) e contêineres (ZIP e GZIP).
- Total de asserções: **84 / 84 aprovadas (100% PASS)**. Código de Saída: `0`.

#### 2.1.4 Pipeline de Publicação (`scripts/publish.ps1 -Version 1.0.0-rc.2`)
- **Preservação Absoluta do RC1:** O arquivo `dist/Lodino-v1.0.0-rc.1-Portable-win-x64.zip` permaneceu intacto e inalterado:
  `SHA256: 0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` *(Preservado)*
- **Reconstrução do Pacote Portátil RC2:** Montagem isolada dos 7 arquivos e geração de `dist/Lodino-v1.0.0-rc.2-Portable-win-x64.zip` (134.739 bytes, SHA-256: `931ee594ed9ce1090094107fb9b79db52d4877f0cb685aa1cca7dfea9a8f7128`).
- **Código de Saída:** `0`.

#### 2.1.5 Compilação Real do Instalador NSIS (`scripts/build-installer.ps1 -Version 1.0.0-rc.2`)
- Compilador: NSIS v3.13 (`makensis.exe`)
- Modo de compressão: `zlib`
- Arquivo gerado: `dist/Lodino-v1.0.0-rc.2-Setup-win-x64.exe` (216.215 bytes, SHA-256: `38593eb8b2fce844d83e2c32634fecb8e4933213030429ae720b4a69456c4107`).
- **Código de Saída:** `0`.

#### 2.1.6 Homologação Funcional Completa do Instalador Reconstruído (`test_nsis_installer.ps1`)
- Instalação silenciosa em `%TEMP%` descartável sem elevação UAC: **Sucesso (Exit 0)**.
- Comparação dos 7 arquivos instalados contra o staging: **7 / 7 hashes 100% idênticos**.
- Atalhos no Menu Iniciar (`$SMPROGRAMS`): Criados com sucesso.
- Execução de CLI instalada (`list-engines`, conversão CSV $\rightarrow$ JSON, GZIP round-trip, ZIP round-trip): **100% Aprovados**.
- Ciclo de vida da GUI instalada: **Aprovado**.
- Desinstalação silenciosa (`uninstall.exe /S`): **Arquivos e atalhos removidos**.
- **Preservação de Dados do Usuário (CWE-459):** Arquivos `documento_do_usuario.txt` e pasta `MinhasExportacoes\planilha_final.csv` intencionalmente criados na pasta do app foram **100% preservados**.

---

### 2.2 Agente B — Revisão Independente e Análise de Divergência de Hashes

O Agente B analisou a variação dos hashes dos artefatos reconstruídos na Fase 14.3.1 em comparação com os registrados na Fase 14.2 / 14.2.3:

1. **Causa da Variação do Hash do ZIP RC2:**
   - Na Fase 14.2.2, o arquivo [src/distribution/docs/THIRD_PARTY_NOTICES.txt](file:///c:/Projetos/lodino/src/distribution/docs/THIRD_PARTY_NOTICES.txt) foi atualizado para incluir a atribuição legal ao NSIS sob licença zlib/libpng.
   - O pipeline `publish.ps1` copiou o novo `THIRD_PARTY_NOTICES.txt` (+572 bytes) para o staging.
   - Adicionalmente, o compilador `csc.exe` do .NET Framework 4.0 gera um novo GUID MVID (Module Version ID) no cabeçalho COFF dos binários PE a cada compilação, e o `System.IO.Compression.ZipFile` embute o carimbo de data/hora atual nos cabeçalhos locais do ZIP.
   - O conteúdo interno é 100% consistente: exatamente os 3 binários de produção e os 4 documentos oficiais, sem qualquer resíduo espúrio.
2. **Causa da Variação do Hash do Instalador NSIS:**
   - O instalador NSIS empacota os 7 arquivos do diretório de staging atualizado. Como `THIRD_PARTY_NOTICES.txt` continha a nova cláusula de licença do NSIS, o executável resultante teve seu tamanho ajustado de 215.839 bytes para 216.215 bytes (+376 bytes comprimidos).
   - A compilação pelo NSIS 3.13 é **100% determinística** a partir do mesmo staging: reexecuções consecutivas de `build-installer.ps1` produzem exatamente o hash `38593eb8b2fce844d83e2c32634fecb8e4933213030429ae720b4a69456c4107`.
3. **Preservação de Artefatos:**
   - O pacote portátil RC1 (`0bbcc757...`) não sofreu nenhuma alteração.
   - O manifesto `dist/Lodino-v1.0.0-rc.2-Portable-win-x64-manifest.txt` foi sincronizado perfeitamente com o novo ZIP RC2 e seus arquivos constituintes.

---

## 3. Matriz Consolidada de Evidências

| Item Auditado | Ferramenta / Caminho | Resultado | Código de Saída |
|---|---|:---:|:---:|
| **Suíte Unitária do Core** | `scripts/build-core.ps1 -Release` | **227 / 227 PASS** | `0` |
| **Interoperabilidade GZIP** | `scratch/gzip_interop/test_gzip_interop.ps1` | **75 / 75 PASS** | `0` |
| **Fila Gráfica Desktop (GUI)** | `run_rc2_gui_mixed_queue_test.ps1` | **84 / 84 PASS** | `0` |
| **Pipeline de Publicação Portátil** | `scripts/publish.ps1 -Version 1.0.0-rc.2` | **ZIP RECONSTRUÍDO** | `0` |
| **Compilador NSIS Oficial** | `scripts/build-installer.ps1 -CheckOnly` | **AVAILABLE (v3.13)** | `0` |
| **Compilação Real do Instalador** | `scripts/build-installer.ps1 -Version 1.0.0-rc.2` | **EXECUTÁVEL GERADO** | `0` |
| **Instalação Per-User sem UAC** | `test_nsis_installer.ps1` | **CONCLUÍDA** | `0` |
| **Testes da CLI Instalada** | `Lodino.exe` (9 motores, CSV/JSON, ZIP, GZIP) | **TODOS PASS** | `0` |
| **Ciclo de Vida da GUI Instalada** | `Lodino.App.exe` (Win32 HWND) | **APROVADO** | `0` |
| **Desinstalação Limpa da Aplicação** | `uninstall.exe /S` | **ARQUIVOS EXPURGADOS** | `0` |
| **Preservação de Dados do Usuário** | Checagem de integridade pós-desinstalação | **PRESERVADOS (100%)** | `0` |

---

## 4. Tabela Oficial de Hashes de Distribuição

| Artefato de Distribuição | Tamanho Físico | Checksum SHA-256 | Estado |
|---|:---:|---|:---:|
| `Lodino-v1.0.0-rc.1-Portable-win-x64.zip` | 134.410 bytes | `0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` | **Preservado (Histórico RC1)** |
| `Lodino-v1.0.0-rc.2-Portable-win-x64.zip` | 134.739 bytes | `931ee594ed9ce1090094107fb9b79db52d4877f0cb685aa1cca7dfea9a8f7128` | **Reconstruído e Validado (RC2)** |
| `Lodino-v1.0.0-rc.2-Setup-win-x64.exe` | 216.215 bytes | `38593eb8b2fce844d83e2c32634fecb8e4933213030429ae720b4a69456c4107` | **Compilado e Validado (RC2 Setup)** |

---

## 5. Falhas, Ressalvas e Pendências

- **Falhas Técnicas:** Zero falhas nos testes unitários, nos testes de interoperabilidade ou no ciclo de vida do instalador.
- **Ressalva de SmartScreen:** Os binários gerados não possuem assinatura digital Authenticode de um certificado comercial, disparando aviso preventivo no Windows SmartScreen em máquinas clientes não confiadas.
- **Pendência de Publicação:** Nenhuma tag de versão ou push remoto foi executado, mantendo a governança do repositório estritamente sob controle local.

---

## 6. Veredito Final

$$\mathbf{VEREDITO:\ MAIN\ VALIDADA\ PARA\ PUBLICA\C\tilde{A}O\ CONTROLADA}$$

A branch `main` está completamente consolidada, funcional e estável, com todos os testes passando em 100% e com os artefatos Release Candidate 2 (pacote portátil e instalador NSIS) plenamente operacionais e homologados.
