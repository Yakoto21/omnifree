; ==============================================================================
; LODINO NATIVE SUITE - NSIS COMPILER SPECIFICATION (Nullsoft Scriptable Install System)
; Target Version: 1.0.0-rc.2 (compatible with NSIS 3.10 / 3.13)
; Target Architecture: Windows x64 (Non-elevated, User-mode, Clean Lifecycle)
; Pure Native Architecture: Zero Electron, Zero Node.js, Zero External Codecs
; License: zlib/libpng license (Zero commercial runtime fees, Zero recurring costs)
; ==============================================================================

; ------------------------------------------------------------------------------
; 1. Definicoes Principais de Produto e Metadados
; ------------------------------------------------------------------------------
!ifndef PRODUCT_VERSION
!define PRODUCT_VERSION "1.0.0-rc.2"
!endif

!ifndef PRODUCT_NAME
!define PRODUCT_NAME "Lodino Native Suite"
!endif

!ifndef PRODUCT_PUBLISHER
!define PRODUCT_PUBLISHER "Lodino Project"
!endif

!ifndef PRODUCT_WEB_SITE
!define PRODUCT_WEB_SITE "https://github.com/lodino/lodino"
!endif

!ifndef PRODUCT_EXE
!define PRODUCT_EXE "Lodino.App.exe"
!endif

!ifndef PRODUCT_CLI
!define PRODUCT_CLI "Lodino.exe"
!endif

!ifndef PRODUCT_UNINST_KEY
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Lodino"
!endif

!ifndef PRODUCT_DIR_REGKEY
!define PRODUCT_DIR_REGKEY "Software\Lodino"
!endif

!ifndef SOURCE_DIR
!define SOURCE_DIR "..\dist\Lodino-v1.0.0-rc.2-Portable-win-x64"
!endif

; ------------------------------------------------------------------------------
; 2. Politica de Compressao e Execucao Segura
; ------------------------------------------------------------------------------
; Compressao nativa zlib por padrao, dispensando modulos extras
SetCompressor /FINAL zlib

; Execucao estrita em espaco de usuario (dispensa elevacao administrativa UAC)
RequestExecutionLevel user

; Configuracao do executavel de saida e diretorio de instalacao
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "..\dist\Lodino-v${PRODUCT_VERSION}-Setup-win-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\Lodino"
InstallDirRegKey HKCU "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

; ------------------------------------------------------------------------------
; 3. Interface Moderna (MUI2)
; ------------------------------------------------------------------------------
!include "MUI2.nsh"
!include "LogicLib.nsh"

; Icone oficial do Lodino
!define MUI_ICON "..\src\desktop\Resources\lodino.ico"
!define MUI_UNICON "..\src\desktop\Resources\lodino.ico"

; Paginas do Instalador
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SOURCE_DIR}\LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

; Pagina de Conclusao com opcao de executar a GUI
!define MUI_FINISHPAGE_RUN "$INSTDIR\${PRODUCT_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Executar o ${PRODUCT_NAME} agora"
!insertmacro MUI_PAGE_FINISH

; Paginas do Desinstalador
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Idioma
!insertmacro MUI_LANGUAGE "Portuguese"

; ------------------------------------------------------------------------------
; 4. Secoes de Instalacao
; ------------------------------------------------------------------------------

; Secao Mandatoria: Binarios do Lodino e Documentos de Distribuicao
Section "!${PRODUCT_NAME} (Obrigatorio)" SEC_CORE
    SectionIn RO
    SetOutPath "$INSTDIR"

    ; Instalacao estrita e exclusiva dos 7 artefatos do staging
    File "${SOURCE_DIR}\Lodino.Core.dll"
    File "${SOURCE_DIR}\Lodino.exe"
    File "${SOURCE_DIR}\Lodino.App.exe"
    File "${SOURCE_DIR}\README.txt"
    File "${SOURCE_DIR}\LICENSE.txt"
    File "${SOURCE_DIR}\KNOWN_LIMITATIONS.txt"
    File "${SOURCE_DIR}\THIRD_PARTY_NOTICES.txt"

    ; Geracao do Desinstalador
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Criacao de Atalhos no Menu Iniciar (Espaco de Usuario: $SMPROGRAMS)
    CreateDirectory "$SMPROGRAMS\Lodino Native Suite"
    CreateShortCut "$SMPROGRAMS\Lodino Native Suite\Lodino Native Suite.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0
    CreateShortCut "$SMPROGRAMS\Lodino Native Suite\Lodino CLI.lnk" "$INSTDIR\${PRODUCT_CLI}" "" "$INSTDIR\${PRODUCT_CLI}" 0
    CreateShortCut "$SMPROGRAMS\Lodino Native Suite\Desinstalar Lodino.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0

    ; Registro de Desinstalacao Limpo em HKCU (Adicionar/Remover Programas)
    WriteRegStr HKCU "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\${PRODUCT_EXE},0"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "HelpLink" "${PRODUCT_WEB_SITE}"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
    WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegDWORD HKCU "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKCU "${PRODUCT_UNINST_KEY}" "NoRepair" 1
SectionEnd

; Secao Opcional: Atalho na Area de Trabalho (Desmarcado por Padrao)
Section /o "Criar atalho na Area de Trabalho" SEC_DESKTOP
    CreateShortCut "$DESKTOP\Lodino Native Suite.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0
SectionEnd

; Descricoes das Secoes para a Pagina de Componentes
LangString DESC_SEC_CORE ${LANG_PORTUGUESE} "Arquivos essenciais do Lodino Native Suite, CLI, biblioteca de motores e documentacao."
LangString DESC_SEC_DESKTOP ${LANG_PORTUGUESE} "Cria um atalho para a interface grafica do Lodino na sua Area de Trabalho."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SEC_CORE} $(DESC_SEC_CORE)
    !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DESKTOP} $(DESC_SEC_DESKTOP)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; ------------------------------------------------------------------------------
; 5. Secao de Desinstalacao (Clean Lifecycle)
; ------------------------------------------------------------------------------
Section "Uninstall"
    ; 1. Remover atalhos do Menu Iniciar e Desktop
    Delete "$SMPROGRAMS\Lodino Native Suite\Lodino Native Suite.lnk"
    Delete "$SMPROGRAMS\Lodino Native Suite\Lodino CLI.lnk"
    Delete "$SMPROGRAMS\Lodino Native Suite\Desinstalar Lodino.lnk"
    RMDir "$SMPROGRAMS\Lodino Native Suite"
    Delete "$DESKTOP\Lodino Native Suite.lnk"

    ; 2. Remover exclusivamente os 7 arquivos instalados e o desinstalador
    Delete "$INSTDIR\Lodino.Core.dll"
    Delete "$INSTDIR\Lodino.exe"
    Delete "$INSTDIR\Lodino.App.exe"
    Delete "$INSTDIR\README.txt"
    Delete "$INSTDIR\LICENSE.txt"
    Delete "$INSTDIR\KNOWN_LIMITATIONS.txt"
    Delete "$INSTDIR\THIRD_PARTY_NOTICES.txt"
    Delete "$INSTDIR\uninstall.exe"

    ; 3. Remover chaves de registro do espaco de usuario
    DeleteRegKey HKCU "${PRODUCT_UNINST_KEY}"
    DeleteRegKey HKCU "${PRODUCT_DIR_REGKEY}"

    ; 4. Preservacao de dados do usuario:
    ; RMDir sem flag /r remove a pasta APENAS se estiver vazia.
    ; Se o usuario tiver salvo dados, imagens ou arquivos de conversao dentro de $INSTDIR,
    ; esses arquivos serao preservados integralmente sem exclusao acidental.
    RMDir "$INSTDIR"

    SetAutoClose true
SectionEnd
