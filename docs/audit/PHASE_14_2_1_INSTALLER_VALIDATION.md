# LODINO NATIVE SUITE — RELATÓRIO DE HOMOLOGAÇÃO DO INSTALADOR RC2 (FASE 14.2.1)

**Documento:** Auditoria de Disponibilidade de Ferramentas, Análise de Licença e Prontidão de Instalação do Release Candidate 2  
**Data:** 20 de Outubro de 2026  
**Repositório:** `C:\Projetos\lodino`  
**Branch de Trabalho:** `feature/release-packaging`  
**Commit-Base Auditado:** `b9e1d8787109b682cb02d02e8289e474b05ce8d7`  
**Commit Ancestral de Origem:** `f1a212b3c2e7fa09ef90c03f63defeed53cc799f` (`main`)  
**Ambiente de Auditoria:** Microsoft Windows 10 Pro x64 (Build 19045), .NET CLR v4.0.30319.42000, PowerShell 5.1  
**Veredito Oficial:**  
$$\mathbf{VEREDITO:\ BLOQUEADO}$$  
*(Especificação Inno Setup `.iss` validada e harmonizada; compilação do executável instalador bloqueada por ausência do compilador `ISCC.exe` no host).*

---

## 1. Sumário Executivo e Integridade Inicial

A Fase 14.2.1 teve como missão desdobrar a homologação prática do instalador tradicional para o Lodino Native Suite (`Lodino-v1.0.0-rc.2-Setup-win-x64.exe`), projetado na Fase 14.2, assegurando a máxima conformidade arquitetural, de licenciamento e de segurança.

### 1.1 Estado do Repositório e Confirmação de Hash
- **Branch:** `feature/release-packaging`
- **HEAD Confirmado:** `b9e1d8787109b682cb02d02e8289e474b05ce8d7`
- **Status do Working Tree:** Limpo (com preservação de todos os artefatos históricos e de `scratch/`).
- **Verificação de Hashes em `dist/` (100% Bit-Identical aos Manifestos):**
  - **RC1 ZIP:** `dist/Lodino-v1.0.0-rc.1-Portable-win-x64.zip` (134.410 bytes)  
    `SHA256: 0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` *(Íntegro e Preservado)*
  - **RC2 ZIP:** `dist/Lodino-v1.0.0-rc.2-Portable-win-x64.zip` (134.361 bytes)  
    `SHA256: 7d554af88178a433c4d4a80973ff6efd15bb21f72411f1c96af2686f1b64e3e4` *(Íntegro e Preservado)*

### 1.2 Regra de Não-Proliferação e Não-Intervenção Silenciosa
Em conformidade estrita com as políticas de segurança do projeto Lodino, **nenhum binário, utilitário ou compilador foi baixado ou instalado silenciosamente** no host durante esta auditoria. A ausência de ferramentas de compilação é documentada de forma transparente e categórica, sem recorrer a falseamento de dados ou simulações indevidas.

---

## 2. Relatório da Revisão Multiagente

### 2.1 Agente A — Ferramenta, Licenciamento e Governança de Terceiros

#### 2.1.1 Auditoria de Presença do Compilador Inno Setup (`ISCC.exe`)
Foi executada uma varredura exaustiva no sistema host cobrindo todas as rotas padrão e canônicas de instalação:
1. Variável de ambiente `%PATH%` (via `Get-Command ISCC.exe`): Não localizado.
2. `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`: Inexistente.
3. `C:\Program Files\Inno Setup 6\ISCC.exe`: Inexistente.
4. `C:\Users\Pinchu\AppData\Local\Programs\Inno Setup 6\ISCC.exe`: Inexistente.
5. `C:\Program Files (x86)\Inno Setup 5\ISCC.exe`: Inexistente.
6. `C:\Program Files\Inno Setup 5\ISCC.exe`: Inexistente.
7. `C:\tools\InnoSetup\ISCC.exe`: Inexistente.
8. `C:\ProgramData\chocolatey\bin\ISCC.exe`: Inexistente.

Execução de verificação nativa via script:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -CheckOnly
# Retorno:
# Status: BLOCKED
# Reason: ISCC not found
```
**Conclusão da Disponibilidade:** O compilador Inno Setup (`ISCC.exe`) **não está instalado** no ambiente de execução.

#### 2.1.2 Análise Legal dos Termos de Licença do Inno Setup
Foi realizada uma análise jurídica e técnica da licença do Inno Setup (Jordan Russell / Martijn Laan):
- **Modelo de Licenciamento:** O Inno Setup possui seu código sob a Inno Setup License (permissiva, estilo BSD Modificada). Contudo, a política comercial e a distribuição oficial em seu site (jrsoftware.org) requerem atenção especial para entidades comerciais.
- **Uso Não Comercial:** Gratuito e sem royalties sob os termos da licença permissiva.
- **Uso Comercial e Termos Oficiais:** O site oficial do Inno Setup (jrsoftware.org) solicita expressamente que usuários e organizações que utilizem a ferramenta em contexto comercial adquiram uma licença comercial paga (licença perpétua com 2 anos de atualizações, custando a partir de ~€145 para Single-User até €1.125 para Enterprise). Portanto, **não se deve afirmar que o uso comercial é irrestritamente gratuito**: há uma solicitação formal dos mantenedores para pagamento de licença comercial, o que acarreta custos de aquisição e possíveis custos recorrentes de renovação para organizações que desejam manter o compilador atualizado com suporte e novas versões.
- **Compatibilidade com o Lodino (MIT License):** Tecnicamente compatível no aspecto de código (o stub gerado não impõe contaminação copyleft tipo GPL), porém o uso comercial institucional exige ponderação financeira e adesão ética ou contratual aos termos comerciais do mantenedor.
- **Versão Requerida e Fonte Oficial:**
  - Versão mínima recomendada: **Inno Setup 6.2.0** ou superior (suporta sintaxe moderna de 64 bits e melhorias de segurança de desinstalação per-user).
  - Distribuição oficial e segura: [https://jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php).

---

### 2.2 Agente B — Script e Especificação de Parâmetros

#### 2.2.1 Revisão e Harmonização do Script `.iss`
A especificação [installer/lodino_setup.iss](file:///c:/Projetos/lodino/installer/lodino_setup.iss) foi auditada linha a linha:
1. **Harmonização de Metadados:**
   - O campo `MyAppPublisher` foi atualizado de `"Lodino Native Suite Team"` para `"Lodino Project"`, estabelecendo 100% de paridade com `AssemblyCompany` e `AssemblyCopyright` de [CommonAssemblyInfo.cs](file:///c:/Projetos/lodino/src/common/CommonAssemblyInfo.cs).
2. **Isolamento de Privilégios e Escopo por Usuário:**
   - `PrivilegesRequired=lowest` e `PrivilegesRequiredOverridesAllowed=commandline` garantem que o instalador execute em espaço de usuário puro sem demandar prompt UAC de elevação administrativa.
   - Destino de instalação padrão: `{localappdata}\Programs\Lodino`.
3. **Atalhos e Experiência do Usuário:**
   - Criação de atalhos no Menu Iniciar para a interface gráfica (`Lodino.App.exe`) e console (`Lodino.exe`).
   - Atalho na Área de Trabalho condicionado à Task `desktopicon` com `Flags: unchecked` (desmarcado por padrão, respeitando a discrição do ambiente do usuário).
4. **Ciclo de Desinstalação Limpa e Segura:**
   - `UninstallDisplayIcon={app}\Lodino.App.exe`
   - O desinstalador gerencia e remove exclusivamente os 7 arquivos instalados e o subdiretório do aplicativo. Não apaga recursivamente diretórios externos ou dados processados pelo usuário em pastas de conversão.
5. **Pureza Arquitetural Soberana:**
   - Zero dependências de Node.js, Electron, Python runtime ou codecs proprietários.
   - Zero tarefas agendadas (`[Tasks]` de telemetria inexistentes), zero serviços em segundo plano (`[Services]` inexistente) e zero modificações forçadas em chaves globais do registro.

---

### 2.3 Agente C — Verificação do Staging de Pacotes

Foi auditado o diretório de staging `dist/Lodino-v1.0.0-rc.2-Portable-win-x64/`, que alimenta tanto a compilação do instalador quanto a montagem do arquivo ZIP portátil:

| Arquivo no Staging | Tamanho Real | Checksum SHA-256 | Validação de Manifesto |
|---|:---:|---|:---:|
| `Lodino.Core.dll` | 208.384 B | `4387053c2d6f5b0842884446c65c7201a2f27cc226791d3bded6fb65c6e6592d` | **CONFIRMADO (100%)** |
| `Lodino.App.exe` | 53.760 B | `5da0ee769c8c7d60fd8d1cd695d3730eb634cbbccebcc11c7eeb6a83a19d73f4` | **CONFIRMADO (100%)** |
| `Lodino.exe` | 28.672 B | `ec30d39d557d3fbf91c8ff39d7b8ffd90bff7f379af3254293a1813f9da79832` | **CONFIRMADO (100%)** |
| `README.txt` | 4.879 B | `73c80bdeaef74f73d719c9c171a257100d0f27d7e8671cc99d476523e08cc9de` | **CONFIRMADO (100%)** |
| `LICENSE.txt` | 1.071 B | `cb8504ef83f397eefff97c6906c4fbbbba095bf8df6d4d0811ddd20edc803a68` | **CONFIRMADO (100%)** |
| `KNOWN_LIMITATIONS.txt` | 3.825 B | `9d34a47c47554e09ce9e6f3914cf3c4b1fe8f5652111eb284d3acf8f6937d8b6` | **CONFIRMADO (100%)** |
| `THIRD_PARTY_NOTICES.txt` | 1.965 B | `bcc03d2f7a1603e370522df4089117fd603af8b64f6e7928f2d754ec5caf31ac` | **CONFIRMADO (100%)** |

**Total de Itens:** Exatamente 7 arquivos. Zero arquivos PDB, zero assemblies de teste, zero resíduos de compilação ou de diretórios `obj/`.

---

### 2.4 Agente D — Testes de Instalação e Execução

| Etapa de Homologação | Estado | Justificativa / Evidência |
|---|:---:|---|
| **Compilação do Executável do Instalador** | **`BLOCKED`** | Compilador `ISCC.exe` não detectado no host. |
| **Instalação Não-Elevada em `%LOCALAPPDATA%`** | **`BLOCKED`** | Dependência direta do executável compilado. |
| **Criação e Resolução de Atalhos** | **`BLOCKED`** | Dependência direta do instalador gerado. |
| **Execução Pós-Instalação (CLI / GUI)** | **`BLOCKED`** | Dependência direta da instalação no diretório `{app}`. |
| **Desinstalação e Limpeza de Disco** | **`BLOCKED`** | Dependência direta do desinstalador Inno Setup (`unins000.exe`). |
| **Testes de Regressão Unitários do Core** | **`APROVADO`** | 227 testes unitários validados no commit `b9e1d87` (100% PASS). |
| **Testes Automatizados de Fila Mista (GUI)** | **`APROVADO`** | 89 asserções de fila e regras de negócio da GUI aprovadas no commit `b9e1d87`. |

---

### 2.5 Agente E — Veredito e Auditoria de Segurança Independente

#### 2.5.1 Auditoria de Integridade e Requisitos
1. **Integridade de Relatório:** Nenhuma alegação de execução de instalador foi formulada sem o correspondente arquivo físico existente.
2. **Proteção contra Regressões:** O pacote portátil RC2 permanece intacto e perfeitamente operacional, atendendo a todos os casos de uso autônomos sem instalador.
3. **Avaliação da Especificação `.iss`:** O script está completamente pronto, parametrizado e auditado para gerar imediatamente `dist\Lodino-v1.0.0-rc.2-Setup-win-x64.exe` assim que o compilador `ISCC.exe` for provisionado no host ou no ambiente de CI/CD.

#### 2.5.2 Veredito Oficial
$$\mathbf{VEREDITO:\ BLOQUEADO}$$

*(O status é categoricamente registrado como **BLOQUEADO** exclusivamente pela indisponibilidade do compilador `ISCC.exe` no host. Todos os artefatos de código, staging, scripts de automação e especificações de configuração estão íntegros, aprovados e prontos).*

---

## 3. Guia Operacional de Desbloqueio e Compilação

Para compilar o instalador Windows em um ambiente de desenvolvimento ou esteira de CI/CD, execute as instruções abaixo:

### 3.1 Instalação do Inno Setup 6
Obtenha o Inno Setup oficial através de um dos seguintes métodos:
1. **Download Oficial (Recomendado):**
   - Acesse: [https://jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php)
   - Baixe e instale o instalador estável mais recente (`innosetup-6.x.x.exe`).
2. **Via Gerenciador de Pacotes do Windows (`winget`):**
   ```cmd
   winget install --id JRSoftware.InnoSetup -e
   ```
3. **Via Chocolatey:**
   ```cmd
   choco install innosetup
   ```

### 3.2 Execução do Build do Instalador
Após a instalação da ferramenta, abra o PowerShell no diretório do projeto e execute:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1 -Version 1.0.0-rc.2
```

O script irá:
1. Detectar automaticamente o compilador `ISCC.exe` no PATH ou nos diretórios de programas padrão.
2. Validar a integridade dos 7 arquivos mandatórios em `dist\Lodino-v1.0.0-rc.2-Portable-win-x64`.
3. Invocar o compilador Inno Setup passando os parâmetros `/DMyAppVersion=1.0.0-rc.2` e `/DSourceDir=..\dist\Lodino-v1.0.0-rc.2-Portable-win-x64`.
4. Gerar o instalador final: `dist\Lodino-v1.0.0-rc.2-Setup-win-x64.exe`.
5. Calcular e exibir o checksum SHA-256 do instalador gerado.

---

## 4. Matriz de Riscos e Próximos Passos

| Risco Identificado | Severidade | Mitigação Implementada / Recomendada |
|---|:---:|---|
| **Ausência de Assinatura Authenticode** | Média | O executável do instalador gerado sem certificado de assinatura de código disparará o aviso preventivo do Windows SmartScreen. O [README.txt](file:///c:/Projetos/lodino/src/distribution/docs/README.txt) e [KNOWN_LIMITATIONS.txt](file:///c:/Projetos/lodino/src/distribution/docs/KNOWN_LIMITATIONS.txt) documentam claramente essa característica ao usuário, sem solicitar desativação de proteções. |
| **Instalação Silenciosa não Autorizada** | Alta | A política de segurança de rejeitar o download automático e silencioso de ferramentas de compilação foi estritamente cumprida, preservando a governança do repositório. |
| **Conflito de Permissões em Contas Restritas** | Baixa | A diretiva `PrivilegesRequired=lowest` assegura que a instalação transcorra sem necessidade de direitos de administrador ou UAC. |

**Próxima Ação Recomendada:**  
Disponibilizar o compilador Inno Setup no ambiente de build ou proceder com o merge da branch `feature/release-packaging` para a `main`, mantendo o pacote portátil `v1.0.0-rc.2` como entrega homologada e o script do instalador pronto para a esteira oficial de distribuição.
