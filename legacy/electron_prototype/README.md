# ARQUIVO HISTÓRICO: PROTÓTIPO ELECTRON (OMNIFREE / MEU-CONVERSOR)

> **ADVERTÊNCIA NORMATIVA E DE GOVERNANÇA:**
>
> Os componentes, arquivos e códigos contidos neste diretório pertencem exclusivamente ao histórico da prova de conceito inicial (protótipo descartado). Eles **NÃO** fazem parte do produto **Lodino Native Suite**, **NÃO** são utilizados pelo processo de compilação ou execução atual, e **NÃO** estão autorizados para distribuição ou empacotamento. A presença deste material no repositório tem finalidade estritamente histórica e documental, e não implica que seu conteúdo seja juridicamente livre, original ou aprovado para uso comercial.

---

## 1. Finalidade Original do Protótipo

Nas fases embrionárias do projeto, foi construída uma prova de conceito rápida utilizando o ecossistema JavaScript/Node.js sob o framework Electron, referenciada internamente como "OmniFree" ou "meu-conversor".

O objetivo do protótipo era validar fluxos de tela e a interação do usuário com tarefas de conversão de múltiplos formatos. Para realizar essas conversões, o protótipo dependia extensivamente de runtimes externos, wrappers em Node.js e executáveis de terceiros (como FFmpeg para áudio/vídeo, Sharp para processamento de imagem via libvips, pdf-lib para manipulação de documentos e Pandoc).

Após avaliação arquitetural, de consumo de memória, de pegada de instalação e de segurança/propriedade intelectual, a abordagem baseada em Electron foi completamente descontinuada. O projeto adotou como diretriz soberana o desenvolvimento de motores nativos próprios em C# (.NET Framework 4.0 BCL + P/Invoke Win32 direto), eliminando integralmente a dependência de Node.js, Electron, Chromium e codecs de terceiros.

---

## 2. Inventário dos Arquivos Arquivados (24 arquivos)

A estrutura original do protótipo foi preservada dentro deste diretório histórico para permitir rastreabilidade exata:

```text
legacy/electron_prototype/
├── package.json                          # Manifesto de dependências e scripts NPM legados
├── package-lock.json                     # Árvore de dependências resolvidas (189 KB)
├── scripts/
│   └── create-icon.js                    # Script auxiliar legado de conversão de ícones (png-to-ico)
├── src/
│   ├── main/                             # Processo principal do Electron (Node.js)
│   │   ├── conversion-catalog.js         # Catálogo de formatos suportados pelo protótipo
│   │   ├── conversion-service.js         # Orquestrador de conversões legadas
│   │   ├── data-converter.js             # Conversor tabular/JSON em JavaScript
│   │   ├── error-messages.js             # Mensagens de erro da interface legada
│   │   ├── main.js                       # Ponto de entrada Electron (ciclo de vida BrowserWindow)
│   │   ├── media-service.js              # Integração de mídia via fluent-ffmpeg
│   │   ├── output-paths.js               # Resolução de caminhos de saída
│   │   ├── pdf-service.js                # Manipulação de PDFs via pdf-lib
│   │   ├── preferences-service.js        # Armazenamento de preferências em JSON local
│   │   ├── recognition-service.js        # Detecção de tipo de arquivo
│   │   ├── system-tools.js               # Verificação de ferramentas do sistema
│   │   └── update-service.js             # Mecanismo de auto-update via electron-updater
│   ├── preload/
│   │   └── preload.js                    # Script de pré-carregamento com contextBridge IPC
│   └── renderer/                         # Interface Web do protótipo
│       ├── index.html                    # Interface gráfica em HTML5
│       ├── renderer.js                   # Lógica client-side da interface Web (70 KB)
│       └── style.css                     # Folhas de estilo da interface antiga
└── test/                                 # Testes unitários do protótipo antigo
    ├── catalog.test.js                   # Testes do catálogo de formatos
    ├── data-converter.test.js            # Testes do conversor de dados JS
    ├── engines.test.js                   # Testes de orquestração de motores legados
    ├── output-paths.test.js              # Testes de resolução de caminhos
    └── reliability.test.js               # Testes de limites e tolerância a falhas
```

---

## 3. Dependências Anteriormente Declaradas

O manifesto `package.json` declarava formalmente as seguintes dependências externas:

### Dependências de Runtime (`dependencies`):
* `@iarna/toml` (^2.2.5) — Parser TOML em JS.
* `@zxing/library` (^0.21.3) — Leitor de código de barras/QR Code.
* `electron-updater` (^6.8.9) — Atualizador automático do Electron.
* `fast-xml-parser` (^5.11.1) — Parser XML para JavaScript.
* `ffmpeg-static` (^5.3.0) — Binários pré-compilados do FFmpeg.
* `fluent-ffmpeg` (^2.1.3) — Wrapper de comandos para o FFmpeg.
* `node-pandoc` (^0.3.0) — Wrapper de execução para o conversor Pandoc.
* `pdf-lib` (^1.17.1) — Manipulação e criação de documentos PDF em puro JS.
* `sharp` (^0.35.4) — Wrapper de alta performance para libvips C/C++.
* `yaml` (^2.9.1) — Parser e serializador YAML em JS.

### Dependências de Desenvolvimento (`devDependencies`):
* `electron` (^29.0.0) — Framework de desktop baseado em Chromium e Node.js.
* `electron-builder` (^24.13.3) — Empacotador de executáveis e instaladores Electron.
* `png-to-ico` (^3.0.2) — Conversor de imagens PNG para ícone ICO.

---

## 4. Situação de Licenças e Dúvidas Remanescentes

1. **Licenças Múltiplas e Transitivas:** O ecossistema Node.js / NPM trazia centenas de pacotes transitivos no `package-lock.json`, com combinações de licenças MIT, ISC, BSD-2-Clause, BSD-3-Clause, Apache 2.0 e componentes com restrições LGPL/GPL (como os binários embutidos de FFmpeg).
2. **Incompatibilidade com Soberania:** A complexidade da cadeia de suprimentos de pacotes NPM, aliada ao risco de vulnerabilidades de segurança e custos de conformidade de licenciamento para distribuição comercial, reforçou a decisão estratégica de descartar integralmente o protótipo.
3. **Status Atual:** Estes arquivos permanecem arquivados apenas como registro de versões e procedência para consulta técnica interna. Nenhum componente daqui foi auditado para conformidade com a distribuição do produto comercial.

---

## 5. Separação Estrita com o Lodino Nativo Atual

O projeto atual (**Lodino Native Suite**) adota uma demarcação arquitetural estrita:

| Aspecto | Protótipo Antigo (Arquivado Aqui) | Lodino Native Suite (Atual) |
|---|---|---|
| **Linguagem Principal** | JavaScript (Node.js 20) | C# (.NET Framework 4.0 / BCL nativa) |
| **Interface com Usuário** | HTML / CSS / DOM / Chromium | Janela Win32 Nativa pura via P/Invoke e GDI |
| **Motores de Conversão** | Wrappers para FFmpeg, Sharp, pdf-lib | Motores proprietários em C# (DEFLATE, GZIP, ZIP, BMP, PNG, JPEG, WAV, CSV, JSON, INI) |
| **Pegada do Pacote Portátil** | ~150 MB a 250 MB (Chromium + Node) | **137 KB** (0 DLLs externas, 3 binários nativos) |
| **Pipeline de CI/CD** | `release.yml` legado com `npm test` e `electron-builder` | `release.yml` nativo com `build-core.ps1` e `Lodino.Tests.exe` (233 testes) |
| **Instalador** | Electron-builder NSIS script interno | `installer/lodino_setup.nsi` estrito em modo usuário |

---

## 6. Conclusão da Preservação

A migração destes 24 arquivos para este diretório isola de forma limpa a história de desenvolvimento do Lodino, garantindo que o código em produção permaneça exclusivamente soberano, rastreável e auditável.
