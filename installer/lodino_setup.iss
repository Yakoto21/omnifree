; ==============================================================================
; LODINO NATIVE SUITE - INNO SETUP COMPILER SPECIFICATION
; Release: 1.0.0-rc.2
; Target Architecture: Windows x64 (Non-elevated, User-mode, Clean Lifecycle)
; Pure Native Architecture: Zero Electron, Zero Node.js, Zero External Codecs
; ==============================================================================

#ifndef MyAppVersion
#define MyAppVersion "1.0.0-rc.2"
#endif

#ifndef MyAppName
#define MyAppName "Lodino Native Suite"
#endif

#ifndef MyAppPublisher
#define MyAppPublisher "Lodino Native Suite Team"
#endif

#ifndef MyAppURL
#define MyAppURL "https://github.com/lodino/lodino"
#endif

#ifndef MyAppExeName
#define MyAppExeName "Lodino.App.exe"
#endif

#ifndef MyAppCliName
#define MyAppCliName "Lodino.exe"
#endif

#ifndef SourceDir
#define SourceDir "..\dist\Lodino-v1.0.0-rc.2-Portable-win-x64"
#endif

#ifndef SetupIconPath
#define SetupIconPath "..\src\desktop\Resources\lodino.ico"
#endif

[Setup]
; Identificador de Aplicativo único e imutável
AppId={{8B9645E0-0CF5-45C6-89FB-1498C0BBEF07}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Diretório de Instalação no escopo do Usuário (sem necessidade de elevação administrativa)
DefaultDirName={localappdata}\Programs\Lodino
DefaultGroupName=Lodino Native Suite
DisableProgramGroupPage=yes

; Privilégios Mínimos: Permite instalação limpa e segura em contas padrão sem UAC
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline

; Configurações de Saída do Compilador
OutputDir=..\dist
OutputBaseFilename=Lodino-v{#MyAppVersion}-Setup-win-x64
SetupIconFile={#SetupIconPath}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compressão LZMA2 de Máxima Densidade
Compression=lzma2/max
SolidCompression=yes

; Experiência Visual Moderna
WizardStyle=modern
DisableWelcomePage=no

; Restrição de Arquitetura x64
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Controle de Processos durante Instalação
CloseApplications=yes
RestartApplications=no

; Requisitos Mínimos do Sistema Operacional (Windows 7 SP1 ou posterior)
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Binários de Produção Nativos x64 (100% C# / .NET 4.0 - Zero Electron, Zero Node.js)
Source: "{#SourceDir}\Lodino.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Lodino.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Lodino.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

; Documentação Oficial e Notificações de Licenciamento
Source: "{#SourceDir}\README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\KNOWN_LIMITATIONS.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\THIRD_PARTY_NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Atalho no Menu Iniciar para a Aplicação Gráfica
Name: "{group}\Lodino Native Suite"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

; Atalho no Menu Iniciar para o Prompt de Comando da CLI
Name: "{group}\Lodino CLI"; Filename: "{cmd}"; Parameters: "/K cd /d ""{app}"" && echo Lodino CLI Ready. Try: Lodino.exe help"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppCliName}"

; Atalho de Desinstalação no Menu Iniciar
Name: "{group}\{cm:UninstallProgram,Lodino Native Suite}"; Filename: "{uninstallexe}"

; Atalho Opcional na Área de Trabalho (Desmarcado por padrão)
Name: "{autodesktop}\Lodino Native Suite"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; Iniciar Aplicação ao Concluir o Instalador
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; ==============================================================================
; POLÍTICA DE SEGURANÇA E CICLO DE VIDA LIMPO:
; - Zero serviços do Windows instalados ou registrados.
; - Zero drivers em modo kernel ou modo usuário.
; - Zero tarefas agendadas no Agendador de Tarefas do Windows.
; - Zero entradas de inicialização automática (Run / RunOnce) no Registro.
; - Zero telemetria, telemetria em segundo plano, ou conexões ocultas à Internet.
; - Zero modificação forçada nas variáveis de ambiente globais ou do sistema.
; - Zero associação forçada de extensões de arquivo (.zip, .gz, .png, .bmp, etc.).
; - Preservação estrita de arquivos do usuário: o desinstalador remove somente os
;   arquivos registrados na instalação (unins000.dat). Pastas de trabalho, arquivos
;   convertidos e saídas geradas pelo usuário NUNCA são excluídos na desinstalação.
; ==============================================================================
