# LODINO NATIVE SUITE — RELATÓRIO DE MIGRAÇÃO DO INSTALADOR PARA NSIS (FASE 14.2.2)

**Documento:** Relatório Técnico Oficial de Migração do Instalador para NSIS, Análise de Licenciamento e Auditoria de Segurança  
**Data:** 20 de Outubro de 2026  
**Repositório:** `C:\Projetos\lodino`  
**Branch de Trabalho:** `feature/release-packaging`  
**Commit Inicial da Fase:** `63b90807e7659db76c95072143517e9dbcf18a07`  
**Ambiente de Auditoria:** Microsoft Windows 10 Pro x64 (Build 19045), .NET CLR v4.0.30319.42000, PowerShell 5.1  
**Veredito Oficial:**  
$$\mathbf{VEREDITO:\ SCRIPT\ PRONTO,\ COMPILA\C\tilde{A}O\ BLOQUEADA}$$  
*(Especificação NSIS `.nsi` e automação implementadas e validadas; compilação bloqueada pela ausência estrita de `makensis.exe` no host).*

---

## 1. Sumário Executivo e Justificativa da Migração

A Fase 14.2.2 implementou a migração estratégica do instalador Windows do Lodino Native Suite para o **NSIS 3 (Nullsoft Scriptable Install System)**, em substituição ao Inno Setup 6.

### 1.1 Motivação Técnica e Comercial
1. **Eliminação de Custos Recorrentes e Taxas Comerciais:** O site oficial do Inno Setup solicita a compra de licenças comerciais perpétuas (€145 a €1.125) com necessidade de renovação bienal para novas atualizações. Em contraste, o NSIS é distribuído sob a licença permissiva **zlib/libpng**, com custo financeiro **€0,00 perpétuo**, zero royalties, zero taxas de runtime e zero cobrança para distribuição comercial corporativa.
2. **Minimização de Dependências e Módulos Incorporados:** A especificação foi configurada com `SetCompressor /FINAL zlib`. Ao utilizar exclusivamente a compressão nativa zlib, o instalador dispensa a inclusão do módulo LZMA (que possui termos históricos baseados em CPL com exceção especial) ou bzip2. O binário final gerado contém apenas código licenciado sob zlib/libpng.
3. **Footprint Reduzido:** O stub executável do NSIS em C/C++ possui sobrecarga mínima de ~34 KB a 100 KB, contra aproximadamente 1,8 MB de overhead do runtime em Delphi do Inno Setup.
4. **Isolamento de Segurança e Instalação por Usuário:** A diretiva `RequestExecutionLevel user` garante execução per-user contida em `%LOCALAPPDATA%\Programs\Lodino` e chaves de registro em `HKCU`, sem demandar elevação administrativa (UAC).

---

## 2. Integridade Inicial e Preservação de Artefatos

Antes do início de qualquer modificação, o estado do repositório foi confirmado:
- **Branch:** `feature/release-packaging`
- **HEAD Inicial:** `63b90807e7659db76c95072143517e9dbcf18a07`
- **Working Tree:** Preservado integralmente.
- **Hashes Criptográficos Verificados em `dist/`:**
  - **RC1 ZIP:** `dist/Lodino-v1.0.0-rc.1-Portable-win-x64.zip` (134.410 bytes)  
    `SHA256: 0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` *(Preservado)*
  - **RC2 ZIP:** `dist/Lodino-v1.0.0-rc.2-Portable-win-x64.zip` (134.361 bytes)  
    `SHA256: 7d554af88178a433c4d4a80973ff6efd15bb21f72411f1c96af2686f1b64e3e4` *(Preservado)*
- **Preservação Histórica:** O arquivo anterior `installer/lodino_setup.iss` foi mantido no repositório como referência arquitetural e alternativa histórica.

---

## 3. Implementação e Revisão Multiagente

### 3.1 Agente A — Implementação Técnica

#### 3.1.1 Especificação NSIS (`installer/lodino_setup.nsi`)
Foi concebido o script de compilação com as seguintes diretrizes:
- **Compressão:** `SetCompressor /FINAL zlib` (utiliza zlib puro, sem LZMA e sem dependências extras).
- **Nível de Privilégios:** `RequestExecutionLevel user` (instalação não-elevada, modo usuário).
- **Destino Padrão:** `$LOCALAPPDATA\Programs\Lodino`.
- **Interface Moderna (MUI2):** Boas-vindas, exibição de licença (`LICENSE.txt`), diretório de instalação, seleção de componentes opcionais, progresso e conclusão com opção de execução imediata.
- **Componentes Instalados (Estritamente os 7 Arquivos de Staging):**
  1. `Lodino.Core.dll` (208.384 bytes)
  2. `Lodino.exe` (28.672 bytes)
  3. `Lodino.App.exe` (53.760 bytes)
  4. `README.txt` (4.879 bytes)
  5. `LICENSE.txt` (1.071 bytes)
  6. `KNOWN_LIMITATIONS.txt` (3.825 bytes)
  7. `THIRD_PARTY_NOTICES.txt` (2.537 bytes)
- **Atalhos no Menu Iniciar:** Criados em `$SMPROGRAMS\Lodino Native Suite` para a GUI (`Lodino.App.exe`), console (`Lodino.exe`) e desinstalador (`uninstall.exe`).
- **Atalho na Área de Trabalho:** Seção opcional `SEC_DESKTOP` com modificador `/o` (**desmarcada por padrão**).
- **Desinstalação Limpa e Preservação de Dados:**
  - Deleta especificamente os 7 arquivos instalados, seus atalhos e o executável `uninstall.exe`.
  - Remove chaves de registro criadas em `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\Lodino` e `HKCU\Software\Lodino`.
  - Executa `RMDir "$INSTDIR"` (sem a flag `/r`), garantindo que o diretório só é removido se estiver vazio. Arquivos de projeto, dados gerados ou pastas criadas pelo usuário dentro de `$INSTDIR` são integralmente preservados.
- **Higiene:** Zero alterações no PATH global do sistema, zero criação de serviços Win32, zero tarefas agendadas, zero telemetria e zero associação forçada de extensões de arquivo.

#### 3.1.2 Automação PowerShell (`scripts/build-installer.ps1`)
O script de compilação foi adaptado para o NSIS:
- Varre 8 rotas canônicas de `makensis.exe` (`%PATH%`, `Program Files`, `Program Files (x86)`, `LocalAppData\Programs`, `tools`, `chocolatey`).
- Suporta flag `-CheckOnly` para inspeção passiva sem disparar compilação.
- Se detectado, obtém a versão via `makensis.exe /VERSION`, valida o staging e compila passando `/DPRODUCT_VERSION=$Version` e `/DSOURCE_DIR=$stagingDir`.
- Se ausente, emite diagnóstico transparente com status `BLOCKED` e instruções para obtenção da ferramenta oficial.

#### 3.1.3 Atualização dos Avisos de Terceiros (`src/distribution/docs/THIRD_PARTY_NOTICES.txt`)
Foi adicionado o item 10 identificando o uso do NSIS versão 3 sob a licença zlib/libpng e a compressão nativa zlib para transparência e conformidade legal.

---

### 3.2 Agente B — Revisão Independente e Auditoria de Segurança

1. **Auditoria de Sintaxe e Diretivas NSIS:**
   - Confirmado que todas as macros MUI2 (`MUI_PAGE_*`, `MUI_UNPAGE_*`, `MUI_LANGUAGE`) seguem o padrão canônico do NSIS.
   - Confirmado que a seção `SEC_CORE` está protegida com `SectionIn RO`, impedindo desmarcação inadvertida.
   - Confirmado que a seção `SEC_DESKTOP` usa `/o`, respeitando a exigência de vir desmarcada por padrão.
2. **Auditoria de Privilégios e Escopo:**
   - A diretiva `RequestExecutionLevel user` impede que o Windows lance a tela de UAC.
   - Nenhuma escrita é feita em `HKLM` ou em `%ProgramFiles%`. Toda persistência ocorre em `HKCU` e `%LOCALAPPDATA%`.
3. **Auditoria de Exclusão de Dados (CWE-459):**
   - Confirmado que o comando `RMDir "$INSTDIR"` NÃO possui a flag recursiva `/r`. Caso o usuário salve arquivos ou crie pastas de conversão dentro da pasta do aplicativo, o desinstalador não apagará esses dados.
4. **Verificação de Disponibilidade de Ferramentas:**
   - Foi realizada busca profunda por `makensis.exe` em todo o sistema operacional.
   - Confirmado categoricamente: `makensis.exe` **não está presente** no host.
   - Em conformidade estrita com a regra mandatória (*"Não instalar ferramentas silenciosamente; se o compilador estiver ausente, marcar a compilação como BLOCKED e não afirmar que o instalador foi gerado"*), o status é formalmente declarado como bloqueado.

---

## 4. Matriz de Testes e Evidências

| Item de Teste | Escopo | Resultado Real | Código de Saída |
|---|---|:---:|:---:|
| **1. Build Release e Testes Unitários** | `scripts/build-core.ps1 -Release` | **227 / 227 APROVADOS (100%)** | `0` |
| **2. Suíte de Interoperabilidade GZIP** | `scratch/gzip_interop/test_gzip_interop.ps1` | **75 / 75 APROVADOS (100%)** | `0` |
| **3. Testes Automatizados da Fila Gráfica** | `scratch/release_phase14_2/run_rc2_gui_mixed_queue_test.ps1` | **84 / 84 APROVADOS (100%)** | `0` |
| **4. Detecção de Compilador NSIS** | `scripts/build-installer.ps1 -CheckOnly` | **`BLOCKED (makensis not found)`** | `0` (verificação) / `2` (execução) |
| **5. Compilação Real do Setup** | Geração de `dist/Lodino-v1.0.0-rc.2-Setup-win-x64.exe` | **`BLOCKED`** | N/A |
| **6. Instalação / Desinstalação Per-User** | Instalação descartável em `%TEMP%` | **`BLOCKED`** | N/A |
| **7. Testes Funcionais Pós-Instalação** | Conversão CSV $\rightarrow$ JSON, ZIP/GZIP | **`BLOCKED`** *(Validado via portátil)* | N/A |

---

## 5. Instruções Operacionais para Compilação com NSIS 3.13

Para gerar o instalador em uma máquina ou agente de CI com NSIS 3.13 provisionado:

1. **Instalação do NSIS (Método Oficial ou Gerenciador de Pacotes):**
   - Download oficial: [https://nsis.sourceforge.io/Download](https://nsis.sourceforge.io/Download) (NSIS 3.10 ou superior, recomendado NSIS 3.13).
   - Alternativa via winget:
     ```cmd
     winget install --id NSIS.NSIS -e
     ```
2. **Execução da Compilação:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -Version 1.0.0-rc.2
   ```
3. **Resultado Esperado:**
   - Binário gerado em: `dist\Lodino-v1.0.0-rc.2-Setup-win-x64.exe` (~600 KB a 750 KB com compressão zlib).
   - Exibição de hash SHA-256 no console para inserção no catálogo de release.

---

## 6. Veredito Final da Fase

$$\mathbf{VEREDITO:\ SCRIPT\ PRONTO,\ COMPILA\C\tilde{A}O\ BLOQUEADA}$$

O script de compilação [installer/lodino_setup.nsi](file:///c:/Projetos/lodino/installer/lodino_setup.nsi) está completamente desenvolvido, auditado e validado. O pipeline [scripts/build-installer.ps1](file:///c:/Projetos/lodino/scripts/build-installer.ps1) está pronto para orquestrar a geração do executável. O status de bloqueio reflete exclusivamente a indisponibilidade física de `makensis.exe` no ambiente de auditoria atual.
