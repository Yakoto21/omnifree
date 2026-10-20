# Lodino — Opções Arquiteturais e Reconstrução Nativa sem Electron

Planejamento arquitetural para a substituição definitiva do Electron e do Node.js por tecnologia própria nativa para Windows.

---

## 1. Avaliação de Tecnologias para a Reconstrução Nativa

| Critério | Opção A: C++20 Nativo com Win32/DirectX | Opção B: C# / .NET Nativo (WPF/WinForms/NativeAOT) | Opção C: Núcleo em C++ / C com Shell Nativo |
| :--- | :--- | :--- | :--- |
| **Desempenho de Processamento** | Máximo absoluto. Controle de ponteiros, alocação de buffers contíguos e vetorização SIMD (AVX/SSE). | Muito alto. O runtime .NET possui JIT moderno e suporte a `Span<T>` e `Memory<T>`. | Máximo no núcleo; flexibilidade total na interface. |
| **Independência de Runtimes** | **100% nativo.** Compila diretamente para código de máquina x64 sem máquina virtual. | Alto. O Windows já inclui o .NET Framework e suporte a NativeAOT sem runtime separado. | **100% nativo.** O núcleo pode ser compilado como biblioteca estática ou dinâmica C-ABI. |
| **Acesso a Compiladores no Host Atual** | Requer instalação do MSVC (`cl.exe`) ou LLVM/Clang. | **Disponível de fábrica.** O compilador `csc.exe` está presente em `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`. | O núcleo com algoritmos puros pode ser prototipado e compilado com as ferramentas nativas disponíveis. |
| **Segurança de Memória** | Exige gerenciamento rigoroso de ponteiros e bounds-checking (`std::span`, RAII). | Alta segurança automática de memória com coleta de lixo nas estruturas de UI. | Camada de segurança estrita na fronteira C-ABI. |
| **Construção da Interface Desktop** | Janelas Win32 clássicas ou C++/WinRT. Código de UI mais verboso. | XAML/WPF nativo do Windows ou WinForms. Preservação fiel do layout de 3 colunas e animações. | Interface desacoplada consumindo a API estável do núcleo. |

---

## 2. Decisão Arquitetural Recomendada: Arquitetura em Camadas com Núcleo Desacoplado

Adotaremos uma **arquitetura em camadas com núcleo isolado de alta performance**, onde:

1. **O Núcleo (`src/core/`):**
   - Projetado em código de produto próprio com especificação limpa de memória, streams e buffers.
   - Não depende de janelas, threads de UI ou bibliotecas de interface.
   - Todos os algoritmos (parsers, codificadores, transformações) operam sobre abstrações de `Reader`, `Writer` e `Span/Buffer`.
   - Pode ser validado diretamente via suíte de testes unitários determinísticos.

2. **A Interface Desktop (`src/desktop/`):**
   - Conectada ao núcleo exclusivamente por contratos de interface pública (`inspect`, `convert`, `cancel`, `progress`).
   - Implementada com tecnologia nativa do Windows, eliminando os 150 MB do Chromium e Node.js do pacote final.

---

## 3. Estrutura de Diretórios Proposta

```text
src/
  ├── desktop/              # Aplicação nativa Windows (Janela, controles, diálogo, temas)
  │   ├── views/            # Telas: Principal (3 colunas), Modais de Fila, Configurações
  │   ├── viewmodels/       # Estado da fila, progresso, histórico e perfis
  │   └── platform/         # Integração com Explorer, Drag-and-Drop e notificações
  │
  ├── core/                 # Núcleo de processamento 100% próprio
  │   ├── api/              # Interfaces públicas estáveis (ICoreEngine, IJobController)
  │   ├── io/               # Leitura e escrita controladas, limites de buffer e arquivos temporários
  │   ├── registry/         # Registro dinâmico de formatos e resolução de rotas
  │   ├── formats/          # Motores específicos de formato
  │   │   ├── data/         # CSV (RFC 4180), JSON (ECMA-404), INI
  │   │   ├── image/        # Raster BMP, manipulador de matriz de pixels
  │   │   ├── archive/      # Leitor e empacotador ZIP seguro
  │   │   └── pdf/          # Leitor estrutural de PDF, xref e streams
  │   ├── transforms/       # Algoritmos de resize, crop, marca-d'água e filtros
  │   ├── jobs/             # Fila, workers de conversão, progresso e cancelamento atômico
  │   └── safety/           # Validação contra Zip Slip, limites de expansão e integridade
  │
  └── tests/                # Infraestrutura de testes própria
      ├── fixtures/         # Arquivos de teste determinísticos e licenciados
      ├── unit/             # Testes de unidade de cada parser e serializador
      ├── integration/      # Testes ponta a ponta de conversão
      ├── security/         # Fuzzing de entradas malformadas e testes de DoS
      └── conformance/      # Validação contra especificações oficiais
```

---

## 4. Contratos Fundamentais da API do Núcleo

```typescript
// Contratos conceituais equivalentes implementados em código nativo

export interface FormatInspection {
  formatId: string;
  mimeType: string;
  isSupported: boolean;
  fileSizeBytes: number;
  dimensions?: { width: number; height: number };
  pageCount?: number;
  metadata?: Record<string, string>;
}

export interface ConvertRequest {
  jobId: string;
  inputPath: string;
  targetFormat: string;
  outputDirectory: string;
  outputFileName?: string;
  settings: Record<string, any>;
}

export interface ConvertResult {
  jobId: string;
  status: 'completed' | 'cancelled' | 'failed';
  outputPath?: string;
  errorMessage?: string;
  bytesProcessed: number;
  durationMs: number;
}

export interface ICoreEngine {
  readonly id: string;
  readonly supportedInputs: string[];
  readonly supportedOutputs: string[];
  
  inspect(stream: StreamInput): Promise<FormatInspection>;
  validate(stream: StreamInput, targetFormat: string): Promise<boolean>;
  convert(request: ConvertRequest, onProgress: (pct: number) => void, cancelToken: CancelToken): Promise<ConvertResult>;
}
```

---

## 5. Estratégia de Transição Segura

Para garantir que a aplicação continue funcionando enquanto os motores próprios são construídos:
- Os novos motores são implementados e testados inicialmente com cobertura e fixtures independentes.
- A substituição ocorre motor a motor, mantendo a paridade de opções e mensagens da interface.
- Nenhuma funcionalidade legada é removida antes que seu equivalente próprio passe por aprovação independente.
