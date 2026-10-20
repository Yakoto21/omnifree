# Lodino Native Suite - Instalador Inno Setup

Este diretório contém a especificação formal do instalador do Lodino Native Suite para Windows x64 baseada no Inno Setup 6.

## Arquivos

- `lodino_setup.iss`: Script de compilação Inno Setup 6.
- `..\scripts\build-installer.ps1`: Script PowerShell para automatizar a verificação e compilação do instalador.

## Requisitos de Compilação

- **Compilador:** Inno Setup 6.2 ou superior (`ISCC.exe`).
- **Download Oficial:** https://jrsoftware.org/isdl.php
- **Diretório de Staging:** `dist\Lodino-v1.0.0-rc.2-Portable-win-x64` contendo os 7 arquivos de produção:
  - `Lodino.App.exe`
  - `Lodino.exe`
  - `Lodino.Core.dll`
  - `README.txt`
  - `LICENSE.txt`
  - `KNOWN_LIMITATIONS.txt`
  - `THIRD_PARTY_NOTICES.txt`
- **Ícone:** `src\desktop\Resources\lodino.ico`

## Como Compilar

### Via PowerShell (Recomendado)
```powershell
.\scripts\build-installer.ps1 -Version "1.0.0-rc.2"
```

### Via Linha de Comando Manual
```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=1.0.0-rc.2 /DSourceDir="..\dist\Lodino-v1.0.0-rc.2-Portable-win-x64" installer\lodino_setup.iss
```

## Características Arquiteturais do Instalador

1. **Instalação em Nível de Usuário (`PrivilegesRequired=lowest`):**
   - Diretório de destino: `%LOCALAPPDATA%\Programs\Lodino`.
   - Registro de desinstalação gravado em `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall`.
   - **Zero elevação administrativa:** Não dispara alertas do UAC (User Account Control) e não requer privilégios de Administrador.

2. **Atalhos e Acesso:**
   - Atalho obrigatório no Menu Iniciar: `Programs > Lodino Native Suite > Lodino Native Suite` apontando para `Lodino.App.exe`.
   - Atalho para o CLI no Menu Iniciar: `Lodino CLI` abrindo console configurado na pasta da aplicação.
   - Atalho de desinstalação no Menu Iniciar: `Uninstall Lodino Native Suite`.
   - Atalho opcional na Área de Trabalho: controlado pela task `desktopicon`, **desmarcado por padrão** (`Flags: unchecked`).

3. **Ciclo de Vida Limpo e Higiene de Sistema:**
   - **Zero serviços:** Nenhum serviço do Windows é instalado.
   - **Zero drivers:** Nenhum driver em modo kernel ou usuário.
   - **Zero tarefas agendadas:** Nenhuma tarefa no Agendador do Windows.
   - **Zero inicialização automática:** Nenhuma chave `Run` ou `RunOnce` no Registro.
   - **Zero telemetria:** Nenhuma chamada de rede ou coleta de telemetria.
   - **Zero alteração de PATH:** A variável de ambiente do sistema não é modificada por padrão.
   - **Zero sequestro de extensões:** Nenhuma associação forçada de formatos (.zip, .gz, .png, etc.).
   - **Preservação de arquivos do usuário:** Na desinstalação, apenas os arquivos instalados são removidos. Pastas e arquivos de saída criados pelo usuário nunca são apagados.

4. **Pureza Tecnológica:**
   - Binários 100% nativos desenvolvidos em C# (.NET Framework 4.0).
   - Zero dependências de Node.js, Electron ou frameworks web.
   - Zero codecs externos ou bibliotecas dinâmicas de terceiros.
