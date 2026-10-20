# Lodino — Especificação de Arquitetura Visual, UI/UX e Interface Nativa Windows (Win32)

**Autor:** Agente 4 — Especialista em Experiência Visual e Recursos Gráficos (Fase 10)  
**Data:** 19 de Outubro de 2026  
**Status:** Aprovado para Implementação da Fase 10  
**Alvo:** `Lodino.App.exe` (Aplicação Nativa Windows 10/11 Pura — Zero Electron, Zero WPF, Zero WinForms)

---

## 1. Mapeamento Crítico da Interface Legada Electron

A inspeção detalhada do código-fonte legado em `src/renderer/index.html`, `src/renderer/style.css` e `src/renderer/renderer.js` revelou a estrutura visual, semântica de controles e comportamentos de interação que definem o produto. Para assegurar a transição sem quebras de expectativas ou perdas silenciosas de recursos, a tabela a seguir consolida o mapeamento funcional:

### 1.1 Inventário de Telas, Seções e Controles Legados

| Seção / Tela | Elementos HTML / Seletores | Comportamento e Interações | Paridade Crítica no Win32 Nativo |
| :--- | :--- | :--- | :--- |
| **Topbar (Cabeçalho)** | `.topbar`, `.wordmark`, `.local-status`, `#language`, `#btn-theme`, `#btn-configuracoes` | - Exibe a marca com glifo circular terracota (`#e7502c`).<br>- Badge visual de segurança: `● 100% local`.<br>- Seletor dinâmico de idioma (PT-BR / EN).<br>- Alternador de tema Claro/Escuro (`btn-theme`).<br>- Botão de configurações globais (`⚙`). | Barra de ferramentas / Custom Title Bar estilizada com tema escuro nativo DWM, ícone procedural da marca, badge de status local e botões de cabeçalho. |
| **Workflow Rail (Trilho)** | `.workflow-rail`, `.rail-step` (01, 02, 03) | Indicador vertical de progresso dos passos do usuário (01 Adicionar $\to$ 02 Preparar $\to$ 03 Exportar). | Preservado visualmente no painel esquerdo ou na barra de status superior como guia de workflow. |
| **Drop Zone (Entrada)** | `#drop-zone`, `#file-input`, `#folder-input` | - Clique abre diálogo de arquivos (`IFileOpenDialog`).<br>- Drag-and-drop com highlight visual (`.dragover`).<br>- Varredura recursiva de diretórios ao soltar pastas inteiras. | Tratamento nativo de `WM_DROPFILES` e `IDropTarget` (OLE Drag and Drop) com suporte a arquivos e pastas recursivas. |
| **Card de Detalhes** | `#file-details`, `#file-name`, `#file-meta`, `#btn-pasta`, `#btn-trocar` | - Exibe o nome do arquivo truncado com ellipsis.<br>- Exibe tamanho formatado (B, KB, MB, GB) e contagem da fila (`+N na fila`).<br>- Botões rápidos para alternar pasta ou trocar arquivo. | Card de arquivo em foco com renderização tipográfica limpa e ações de troca rápida. |
| **Painel de Pré-visualização** | `#preview-panel`, `#preview-content` | Renderização dinâmica de imagens, vídeos (controles e mudo), áudios e PDFs. | Renderização de preview via decodificador de imagem nativo (D2D/GDI) para bitmaps/fotos; ícones representativos elegantes para outros formatos. |
| **Fila de Conversão (Queue)** | `#queue-panel`, `#queue-items`, `.queue-item` | - Visível para $\ge 2$ arquivos.<br>- Reordenação manual via arrastar e soltar (`⠿`).<br>- Indicador de barra de progresso por item individual.<br>- Badges de status: `waiting`, `processing`, `done`, `error`, `cancelled`.<br>- Ação de retentativa unitária de erro (`↻`).<br>- Ação de remoção unitária da fila (`×`). | Controle nativo tipo lista virtual com double-buffering ou ListView customizada com suporte a seleção múltipla, remoção, retry e progresso por item. |
| **Filtro e Busca de Formato** | `#format-search`, `#category-filter`, `#formato-saida` | - Busca textual em tempo real no dropdown.<br>- Filtro por categoria (Imagens, Mídia, Documentos, Compactados, E-books, Dados).<br>- Dropdown preenchido apenas com saídas suportadas para a entrada atual. | Caixa de busca com auto-complete + ComboBox com categorias ou painel de botões/chips rápidos de formatos suportados. |
| **Dica Inteligente** | `#smart-tip` | Faixa contextual com borda terracota que guia o usuário conforme o tipo de arquivo detectado (Imagem, Mídia, PDF, Genérico). | Label dinâmico informativo no painel de opções guiando os ajustes disponíveis. |
| **Opções de Saída** | `#output-name`, `#preset`, `#btn-salvar-perfil`, `#quality`, `#btn-destino` | - Nome customizado do resultado com variáveis.<br>- Perfis pré-definidos (Equilibrado, Menor, Alta qualidade, WhatsApp, etc.).<br>- Criação/edição/remoção de perfis salvos em arquivo de configuração local.<br>- Slider de qualidade (20% a 100%) com leitura textual síncrona.<br>- Seleção de pasta de destino com fallback para Área de Trabalho. | Caixas de texto, Trackbar nativa com `TB_SETPOS` + rótulo síncrono, ComboBox de perfis e botão de navegação nativo de pastas (`FOS_PICKFOLDERS`). |
| **Ajustes Específicos (Drawer)** | `#advanced-options`, `#width`, `#height`, `#crop-image`, `#crop-grid`, `#batch-sizes`, `#watermark`, `#btn-watermark-image`, `#start-time`, `#duration`, `#audio-only`, `#no-audio`, `#remove-metadata` | - Exibição condicional dependendo do tipo (Imagem vs Mídia).<br>- Dimensões (largura/altura).<br>- Grade 3x3 de quadrantes de corte.<br>- Múltiplos tamanhos em lote (`800x600, 1920x1080`).<br>- Marca-d'água textual ou sobreposição de imagem.<br>- Cortes de início/duração de vídeo e remoção de áudio/metadados. | Controles dinâmicos agrupados por GroupBox; seletor de quadrante 3x3 custom-draw; campos numéricos com validação de limites. |
| **Ferramentas de PDF** | `#pdf-tools`, `#btn-juntar-pdf`, `#btn-separar-pdf`, etc. | Botões para operações atômicas: Juntar, Separar, Extrair Páginas (range `1, 3-5`), Girar 90°, Senha, Compactar e Imagens. | Barra de ferramentas específica exibida quando a entrada for PDF. |
| **Barra de Progresso e Ação** | `#progress-container`, `#progress-bar`, `#progress-text`, `#btn-converter`, `#btn-cancelar` | - Botão de destaque "Converter arquivo" (desativado se inválido).<br>- Barra de progresso contínua 0% a 100% com mensagem de arquivo atual.<br>- Botão de cancelamento cooperativo atômico. | Botão de destaque com acento visual Fluent (`Ctrl+Enter`), barra de progresso nativa suave (`PBM_SETPOS`), cancelamento cooperativo via flag atômica. |
| **Painel de Resultados** | `#result-panel`, `#btn-abrir`, `#btn-copiar-caminho`, `#btn-arrastar-resultado`, `#comparison-panel` | - Ícone e mensagem de sucesso/erro.<br>- Abertura direta do arquivo no Explorer (`/select,`).<br>- Cópia do caminho absoluto para a área de transferência.<br>- Arrastar o arquivo convertido diretamente para outro app (Drag-out).<br>- Comparação de tamanho e redução percentual antes/depois. | Botões de pós-conversão: abrir no Explorer via `ShellExecuteW`, copiar para Clipboard (`OpenClipboard`), Drag-out nativo via `DoDragDrop` (`IDataObject`). |
| **Atividade Recente e Histórico** | `#history-list`, `#history-items`, `#btn-ver-historico`, `#btn-limpar-historico`, `#history-dialog` | - Lista das últimas conversões da sessão e histórico persistente das últimas 50.<br>- Diálogo com repetição de conversão (`repetirConversao`) com as mesmas opções.<br>- Limpeza de histórico e exportação para CSV/JSON. | Persistência local em JSON/SQLite em `%APPDATA%\Lodino\history.json`, janela modal de histórico com re-conversão rápida e exportação. |
| **Configurações Gerais** | `#settings-dialog`, `#settings-theme`, `#settings-language`, `#btn-destino-padrao`, `#rules-list` | - Preferência de tema e idioma.<br>- Pasta padrão de saída.<br>- Regras automáticas de pastas monitoradas.<br>- Modo de privacidade (apagar ao fechar). | Diálogo modal com persistência de preferências em `settings.json`. |

### 1.2 Interações Críticas que Não Podem Ser Perdidas (Anti-Regressão)

1. **Atalhos de Teclado Universais:**
   - `Ctrl + O`: Dispara imediatamente o diálogo de seleção de arquivo.
   - `Ctrl + Enter`: Dispara a conversão da fila quando habilitada.
   - `Esc`: Cancela o processo ativo ou fecha caixas modais.
2. **Drag-and-Drop Bidirecional:**
   - *Inbound (Drop-in):* Soltar 1 ou mais arquivos/pastas em qualquer área da janela os adiciona à fila de trabalho.
   - *Outbound (Drag-out):* O botão ou thumbnail de resultado permite clicar e arrastar o arquivo final diretamente para a Área de Trabalho, pasta do Explorer, navegador web ou mensagem de chat.
3. **Cancelamento Cooperativo Seguro:**
   - O clique em "Cancelar" deve parar imediatamente o processamento do arquivo atual, descartar os bytes em buffer, remover qualquer arquivo temporário parcialmente gerado e retornar o estado da interface para "Pronto".
4. **Retentativa Unitária (Retry Only Files):**
   - Em caso de falha em um arquivo específico de um lote de 20 arquivos, o usuário não pode ser forçado a reiniciar o lote inteiro. A interface deve fornecer um botão de retry individual ao lado do item com erro.

---

## 2. Design da Interface Nativa Windows (Win32 Pura)

### 2.1 Filosofia Arquitetural: Alta Fidelidade sem Runtimes Pesados

Para eliminar 100% dos pacotes Electron, Chromium e Node.js sem recorrer a runtimes pesados ou frameworks descontinuados, a aplicação desktop do Lodino é estruturada sobre a **API Win32 Nativa com Aceleração Gráfica e Estilo Fluent Moderno**:

```text
┌────────────────────────────────────────────────────────────────────────┐
│                   Lodino.App.exe (Processo Win32 Nativo)               │
├────────────────────────────────────────────────────────────────────────┤
│  Camada de Apresentação (UI Thread - Mensageria Windows):               │
│   • Win32 Message Loop (GetMessage / DispatchMessage)                  │
│   • Subclassing & DWM Dark Mode (DwmSetWindowAttribute)                │
│   • Direct2D / GDI+ Double-Buffered Custom Rendering                   │
│   • Controles Padronizados com Estilização UxTheme (Explorer Theme)    │
│   • OLE Drag and Drop (IDropTarget / IDropSource / IDataObject)        │
├────────────────────────────────────────────────────────────────────────┤
│  Camada de Coordenação e Estado (ViewModel / UI Bridge):               │
│   • Fila em Memória, Seleção Ativa, Opções de Saída, Histórico         │
│   • Dispatch assíncrono via PostMessage(WM_APP_*)                      │
├────────────────────────────────────────────────────────────────────────┤
│  Camada de Execução (Worker Thread Pool):                              │
│   • Invocação direta da API de Lodino.Core.dll (C-ABI / .NET C#)       │
│   • Cancelamento cooperativo com CancellationToken / AtomicFlag        │
│   • Emissão de progresso 0..100% thread-safe                           │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Geometria e Layout da Janela Principal

- **Resolução Inicial Padrão:** 1160 $\times$ 740 pixels (centralizada na tela).
- **Tamanho Mínimo Permitido:** 920 $\times$ 600 pixels.
- **Suporte a HiDPI:** `DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2` completo, escalando todas as margens, fontes e dimensões de controle proporcionalmente ao DPI de cada monitor via `WM_DPICHANGED`.

```text
+----------------------------------------------------------------------------------------------------+
| [o] LODINO  |  100% LOCAL · PRIVACIDADE TOTAL                  [+ Arquivos] [+ Pasta] | [◐] [⚙] [-][□][X]|
+----------------------------------------------------------------------------------------------------+
|  FLUXO DE TRABALHO & FILA DE ARQUIVOS (62%)         |  OPÇÕES DE CONVERSÃO & AJUSTES (38%)         |
|                                                     |                                              |
| +-------------------------------------------------+ | FORMATO DE SAÍDA                             |
| |                                                 | | [🔍 Buscar formato...      ] [Categoria: v ] |
| |        ARRASTE ARQUIVOS OU PASTAS AQUI          | | +------------------------------------------+ |
| |          ou clique para selecionar              | | | [ PNG ] [ JPG ] [ WEBP ] [ BMP ] [ CSV ] | |
| |                                                 | | +------------------------------------------+ |
| |       BMP · PNG · JPG · WEBP · CSV · JSON       | | Formato Selecionado: BMP (Bitmap 24/32-bit)  |
| +-------------------------------------------------+ |                                              |
|                                                     | PERFIL & QUALIDADE                           |
| FILA DE CONVERSÃO (3 arquivos)                      | Perfil: [ Equilibrado                      v]|
| +-------------------------------------------------+ | Qualidade: [========o========] 80%           |
| | [Img] foto_familia.png       4.2 MB   [✓ Concl] | |                                              |
| | [Img] banner_site.bmp        8.1 MB   [ 45%   ] | | DIMENSÕES & CORTE (IMAGEM)                  |
| | [Doc] dados_2026.csv         1.2 MB   [Aguard.] | | Largura: [ 1920 ] px   Altura: [ 1080 ] px   |
| +-------------------------------------------------+ | [x] Manter proporção     [ ] Recortar preench|
|                                                     | Grade de Recorte:                            |
| DETALHES DO ARQUIVO SELECIONADO                     | [ NW ][ N  ][ NE ]   Marca-d'água:           |
| banner_site.bmp · 8.1 MB · 2048x1536 px             | [ W  ][ C* ][ E  ]   Texto: [ Confidencial ] |
| Destino individual: Automático                      | [ SW ][ S  ][ SE ]   [x] Remover Metadados   |
|                                                     |                                              |
|                                                     | DESTINO DA EXPORTAÇÃO                        |
|                                                     | Pasta: [ C:\Users\...\Desktop\Convertidos v] |
|                                                     | [ Procurar... ]  Nome: [ Automático        ] |
+----------------------------------------------------------------------------------------------------+
| STATUS: Convertendo arquivo 2 de 3: banner_site.bmp                                                |
| [========================>........................] 45%   [ Cancelar ] [ Histórico ] [ CONVERTER ] |
+----------------------------------------------------------------------------------------------------+
```

### 2.3 Especificação Detalhada das 4 Zonas de Interface

#### Zona 1: Barra de Navegação e Ferramentas Superior (Top Navigation Toolbar)
- **Altura Fixa:** 56px (em 96 DPI).
- **Conteúdo e Alinhamento:**
  - *Extremo Esquerdo:* Ícone circular da marca Lodino (procedural, diâmetro 28px, fundo `#E7502C`, símbolo 'L' branco) + Texto "LODINO" em Segoe UI Bold (13pt, `#FFFFFF` em dark / `#18202B` em light) + Badge retangular estilizado com bordas arredondadas e texto "● 100% LOCAL" (`#2A7655` / `#4ECB71`).
  - *Extremo Direito:*
    - Botão estilizado `+ Adicionar Arquivos` (ícone `\uE710`).
    - Botão estilizado `+ Pasta` (ícone `\uE838`).
    - Separador visual vertical (1px largura, cor de borda sutil).
    - Botão de alternância de tema `◐` / `☼` (alterna dinamicamente as paletas e notifica janelas filhas).
    - Botão de idioma `PT-BR` / `EN`.
    - Botão de configurações `⚙` (`\uE713`).

#### Zona 2: Painel Principal de Seleção e Fila (Main Content Area - 62% da largura)
- **Estado Vazio (Nenhum arquivo carregado):**
  - Drop Zone ampla ocupando a área central com contorno retangular pontilhado (2px stroke com `PS_DASH`), cantos arredondados (raio de 8px).
  - Ícone vetorial centralizado de nuvem/download estilizado (48x48px).
  - Título em Segoe UI Semi-Bold 14pt: "Arraste um arquivo ou pasta aqui".
  - Subtítulo em Segoe UI 10pt: "ou clique em qualquer ponto desta área para procurar".
  - Rodapé da drop zone com lista de extensões compatíveis em tom cinza secundário: "BMP · PNG · JPG · WEBP · CSV · JSON · PDF · E MAIS".
- **Estado com Arquivos (1 ou mais arquivos):**
  - Mini Drop Zone compacta superior (altura: 60px) permitindo anexar mais itens a qualquer momento.
  - Fila de Conversão em visual de Lista Moderna (`ListView` com estilo Fluent/Explorer, seleção de linha completa e espaçamento generoso de 36px por linha):
    - Coluna 1: Glifo/ícone do formato de entrada (16x16px).
    - Coluna 2: Nome do arquivo (com corte suave `DT_END_ELLIPSIS`).
    - Coluna 3: Tamanho formatado e dimensões/linhas detectadas.
    - Coluna 4: Formato de destino selecionado (ex: `.BMP`).
    - Coluna 5: Estado visual (Badge colorido: *Aguardando* em cinza, *Convertendo X%* com mini barra de progresso embutida em laranja, *Concluído* em verde com `✓`, *Falha* em vermelho com `!`).
    - Coluna 6: Botões de ação rápida por item (Reexecutar `↻` em falhas, Excluir `×` da fila).
  - Card de Arquivo Ativo (abaixo da lista): Detalhes completos do item atualmente selecionado, caminho completo, dimensões, canais de cor ou contagem de registros.

#### Zona 3: Painel Lateral de Configurações (Right Inspector - 38% da largura)
- **Largura Mínima:** 320px. Painel com fundo levemente diferenciado para criar profundidade visual.
- **Seção 1: Seleção de Formato Alvo:**
  - Caixa de texto de busca rápida (`EDIT`) com texto fantasma (Cue Banner `EM_SETCUEBANNER`: "Pesquisar formato...").
  - Grade de botões/chips rápidos para os formatos mais frequentes (ex.: `PNG`, `JPG`, `WEBP`, `BMP`, `CSV`, `JSON`), com destaque visual no formato selecionado no momento.
  - ComboBox suspensa com todos os formatos compatíveis categorizados para a entrada ativa.
- **Seção 2: Perfil & Qualidade:**
  - ComboBox de Perfis: "Equilibrado", "Arquivo menor", "Maior qualidade", "WhatsApp", "Instagram", "Impressão".
  - Slider nativo (`TRACKBAR_CLASS` / `msctls_trackbar32`): Range 20 a 100, tick marks nos limites e valor numérico atualizado em tempo real em label adjacente ("80%").
- **Seção 3: Ajustes Específicos por Tipo (Dinâmicos):**
  - *Quando o arquivo for Imagem:*
    - Caixas de entrada numérica para Largura e Altura com checkbox "Manter proporção".
    - Checkbox "Recortar para preencher".
    - Grade visual 3x3 para seleção de quadrante de recorte (NW, N, NE, W, Center, E, SW, S, SE). Cada célula é um botão clicável com estado visual selecionado.
    - Caixa de texto para Marca-d'água textual ("Texto da marca").
    - Botão para Marca-d'água por imagem sobreposta.
    - Checkbox "Remover metadados (EXIF/IPTC)".
  - *Quando o arquivo for Dados Estruturados (CSV/JSON/INI):*
    - Seletor de delimitador CSV (Vírgula, Ponto e vírgula, Tabulação).
    - Checkbox "Formatação identada (Pretty-print)".
- **Seção 4: Pasta de Destino e Nomenclatura:**
  - Campo de caminho com botão "Procurar..." que abre o diálogo nativo `IFileOpenDialog` configurado com a flag `FOS_PICKFOLDERS`.
  - Caixa de texto para sufixo/prefixo de saída (ex: `_convertido`).

#### Zona 4: Barra de Status Inferior e Ação Global (Bottom Action Bar)
- **Altura Fixa:** 64px.
- **Lado Esquerdo:**
  - Rótulo de status dinâmico em Segoe UI Semi-Bold 10pt ("3 arquivos prontos para conversão", "Convertendo arquivo 2 de 3: foto.png...", "Conversão finalizada com sucesso").
- **Centro:**
  - Barra de progresso nativa (`PROGRESS_CLASS` / `msctls_progress32`) com visual contínuo e preenchimento suave.
- **Lado Direito (Ações):**
  - Botão secundário "Cancelar" (visível e habilitado somente durante o processamento ativo).
  - Botão secundário "Histórico" (abre a janela modal de histórico de conversões).
  - Botão primário de destaque Fluent "CONVERTER TODOS" (largura: 160px, altura: 38px, cantos arredondados de 4px, fundo terracota `#E7502C`, texto branco bold 11pt, atalho `Ctrl+Enter`).
  - Ao finalizar a conversão, o botão primário transforma-se em "Abrir Pasta" e surge o botão "Arrastar Resultado".

---

## 3. Paleta de Cores, Tipografia e Recursos Gráficos

### 3.1 Paleta de Cores (Suporte a Dark Mode e Light Mode)

A paleta foi desenhada para harmonizar com o design system do Windows 11/10 (Mica/Fluent) e preservar a identidade do Lodino:

| Função do Elemento | Modo Escuro (Padrão) | Modo Claro | Uso e Aplicação |
| :--- | :--- | :--- | :--- |
| **Fundo da Janela (`WindowBg`)** | `#1C1F22` | `#F4F2EB` | Superfície principal da janela da aplicação. |
| **Fundo de Cards / Painéis (`CardBg`)** | `#24282D` | `#FFFFFF` | Superfície da drop zone, cartões de fila e inspector. |
| **Fundo de Entradas / Inputs (`InputBg`)** | `#181A1D` | `#FFFFFF` | Caixas de texto, combos e listas. |
| **Bordas Sutis (`BorderSubtle`)** | `#383C40` | `#D3CFCA` | Separadores, molduras de cards e drop zone. |
| **Bordas em Foco (`BorderFocused`)** | `#E7502C` | `#E7502C` | Borda de controles com foco ou dragover. |
| **Acento Primário (`AccentPrimary`)** | `#E7502C` | `#E7502C` | Cor da marca Lodino; botão "Converter", destaques. |
| **Acento Hover (`AccentHover`)** | `#F06342` | `#D14320` | Estado ao passar o cursor sobre o botão de ação primária. |
| **Texto Primário (`TextPrimary`)** | `#F0F2EB` | `#18202B` | Títulos, rótulos principais, nomes de arquivos. |
| **Texto Secundário (`TextSecondary`)** | `#989C96` | `#6E736B` | Dicas, tamanhos de arquivos, caminhos, legendas. |
| **Sucesso (`StatusSuccess`)** | `#4ECB71` | `#2A7655` | Badge de 100% local, conversão concluída, checkmarks. |
| **Alerta (`StatusWarning`)** | `#F4A261` | `#D97706` | Avisos de conversão parcial, arquivos ignorados. |
| **Erro (`StatusError`)** | `#FF6B6B` | `#C53030` | Falhas de conversão, dados corrompidos. |

### 3.2 Tipografia Oficial Windows (DirectWrite / Segoe UI)

Para garantir legibilidade nítida em qualquer resolução e DPI, a aplicação utiliza exclusivamente a tipografia padrão do sistema Windows:

1. **Fonte Principal:** `Segoe UI Variable Text` (Windows 11) com fallback nativo automático para `Segoe UI` (Windows 10/7).
2. **Fonte para Títulos:** `Segoe UI Variable Display` (Windows 11) ou `Segoe UI Semi-Bold`.
3. **Escala de Tamanhos:**
   - Wordmark / Logo: 14pt (Bold, Letter-Spacing +1.5px).
   - Títulos de Seção: 11pt (Semi-Bold).
   - Botões Principais de Ação: 10pt (Semi-Bold).
   - Rótulos de Campos, Listas e Inputs: 9pt / 9.5pt (Regular).
   - Dicas, Badges e Metadados: 8pt (Regular / Semi-Bold).

### 3.3 Recursos Gráficos e Ícones Seguros (Zero Direitos Autorais)

Para eliminar qualquer dependência de pacotes de imagens externos com licenças incertas ou restritivas, a interface utiliza uma **estratégia nativa dupla**:

#### 1. Glifos Nativos do Sistema Windows (Segoe Fluent Icons & Segoe MDL2 Assets)
A Microsoft distribui nativamente com o Windows as fontes de glifos `Segoe Fluent Icons` (Windows 11) e `Segoe MDL2 Assets` (Windows 10). A tabela de caracteres a seguir mapeia os ícones oficiais utilizados:

| Ação / Conceito | Ponto de Código Unicode | Nome do Glifo Oficial Microsoft |
| :--- | :--- | :--- |
| **Adicionar Arquivo** | `\uE710` | Add |
| **Adicionar Pasta** | `\uE838` | FolderOpen / Folder |
| **Imagem** | `\uEB9F` / `\uE790` | Photo2 / Image |
| **Documento** | `\uE8A5` | Document |
| **Dados / Tabela** | `\uE9F9` | Table |
| **Compactado / Zip** | `\uF012` | ZipFolder |
| **Configurações** | `\uE713` | Settings |
| **Tema Escuro / Claro** | `\uE708` / `\uE706` | Night / Brightness |
| **Histórico** | `\uE81C` | History |
| **Converter / Play** | `\uE768` | Play |
| **Cancelar / Fechar** | `\uE711` | Cancel / Dismiss |
| **Sucesso / Checkmark** | `\uE73E` | CheckMark |
| **Erro / Alerta** | `\uE783` | Error / Warning |
| **Tentar Novamente (Retry)** | `\uE72C` | Refresh |
| **Alça de Arrastar (Reordenar)** | `\uE76F` | GripperDotsVertical |
| **Procurar Pasta** | `\uE8DA` | OpenFolderHorizontal |

#### 2. Renderizador Vetorial Procedural de Fallback
Caso o aplicativo seja executado em versões legadas do Windows onde essas fontes de glifos não estejam presentes, o sistema aciona rotinas de desenho vetorial procedural via GDI / Direct2D:
- **Logo Lodino:** `Ellipse` preenchido em `#E7502C` com `Polyline` branco desenhando a inicial estipulada.
- **Checkmark:** `Polyline` de 3 vértices: $(x, y+h/2) \to (x+w/3, y+h) \to (x+w, y)$.
- **X de Cancelamento:** Duas linhas cruzadas com espessura de 2px.
- **Seta de Arraste:** Três linhas formando seta para baixo apontando para uma linha de base horizontal.

---

## 4. Especificação do Primeiro Marco Funcional (Vertical Slice: 'Lodino.App.exe')

O objetivo do Primeiro Marco Funcional é produzir um executável nativo autônomo e plenamente funcional (`build/Lodino.App.exe`) que compila e executa diretamente nas ferramentas do host Windows, consumindo a biblioteca `Lodino.Core.dll` já desenvolvida.

### 4.1 Escopo do Primeiro Marco Funcional

```text
Entrada (Drop Zone / Diálogo)
       │
       ▼
Detecção de Formato (Lodino.Core.dll)
       │
       ▼
Fila de Arquivos com Seleção Unitária / Lote
       │
       ▼
Painel de Opções (Formato Alvo: BMP / CSV / JSON + Dimensões + Qualidade)
       │
       ▼
Thread de Conversão Assíncrona (Worker Background)
       │
       ▼
Progresso Síncrono na Barra de Status (0..100%)
       │
       ▼
Resultado Concluído + Botão 'Abrir na Pasta'
```

### 4.2 Telas e Controles Presentes no Marco 1

1. **Janela Principal Nativa Win32:**
   - Criação da janela com classe registrada, suporte a tema escuro DWM (`DWMWA_USE_IMMERSIVE_DARK_MODE`).
   - Título da Janela: `Lodino — Conversor Local de Arquivos`.
2. **Zona de Seleção e Arrastar e Soltar (Drop Zone):**
   - Habilitação de `DragAcceptFiles(hwnd, TRUE)` capturando `WM_DROPFILES`.
   - Botão "Adicionar Arquivos" disparando `GetOpenFileNameW` ou `IFileOpenDialog`.
3. **Lista de Arquivos (Fila):**
   - Lista nativa de arquivos adicionados exibindo Nome, Tamanho, Formato Detectado e Status.
   - Suporte a limpar lista e remover item individual.
4. **Painel de Opções de Saída:**
   - ComboBox de Formato de Destino (integrada com os motores implementados de Imagem BMP e Dados Estruturados CSV/JSON/INI).
   - Controles de redimensionamento de imagem (Largura e Altura).
   - Botão de seleção de pasta de saída (`IFileOpenDialog` com `FOS_PICKFOLDERS`).
5. **Execução e Barra de Status:**
   - Botão primário "Converter Todos".
   - Execução em background thread (UI responsiva a 60 fps sem congelar).
   - Barra de progresso real atualizada via `PBM_SETPOS`.
   - Mensagem de status ("Arquivo 1 de 2 convertido com sucesso").
   - Botão "Abrir Pasta" invocando `ShellExecuteW(NULL, L"open", outputDir, NULL, NULL, SW_SHOWNORMAL)`.

### 4.3 Critérios de Aceitação do Marco 1

1. **Zero Electron / Zero Node.js:** O executável não depende de nenhum processo Node ou Chromium em execução.
2. **Binário Leve e Portável:** O executável nativo deve ter tamanho inferior a 2 MB (excluindo a DLL do núcleo) e consumir menos de 30 MB de memória RAM em repouso.
3. **Fluidez e Responsividade:** A janela pode ser movida, redimensionada e minimizada durante a conversão sem engasgos ou aviso de "(Não Respondendo)".
4. **Confiabilidade de Conversão:** A conversão de um arquivo de teste de imagem ou dados via UI gera um arquivo de saída binariamente idêntico ao produzido pela CLI `Lodino.exe`.

---

## 5. Roteiro de Implementação da Fase 10

1. **Etapa 1:** Criação da estrutura `src/desktop/` e esqueleto da janela Win32 em C# com P/Invoke Win32 limpo (compilável via `csc.exe` nativo de `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` sem dependências externas).
2. **Etapa 2:** Implementação do subsistema gráfico e de temas (DWM Dark Mode, cores Fluent, glifos Segoe MDL2).
3. **Etapa 3:** Integração da Drop Zone e manipulação de `WM_DROPFILES`.
4. **Etapa 4:** Conexão com `Lodino.Core.dll` e despacho de conversões em background worker thread.
5. **Etapa 5:** Validação do slice vertical e compilação do executável final `build/Lodino.App.exe`.
