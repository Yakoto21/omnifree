# Lodino — Roadmap de Migração e Reconstrução

Roteiro progressivo de desenvolvimento para substituição dos motores externos por tecnologia própria e eliminação do Electron.

---

## Fases do Projeto

```text
[Fase 0: Auditoria e Baseline] (CONCLUÍDA)
       │
       ▼
[Fase 1: Contratos, I/O e Núcleo Base]
       │
       ▼
[Fase 2: Motor Piloto 1 — Dados Estruturados (CSV, JSON, INI)]
       │
       ▼
[Fase 3A: Motor Gráfico Base (BMP e Manipulação de Pixels)]
       │
       ▼
[Fase 3B: Motor de Arquivos Compactados (ZIP Seguro)]
       │
       ▼
[Fase 3C: Motor Estrutural de PDF (Merge, Split, Rotação)]
       │
       ▼
[Fase 4: Reconstrução da Interface Desktop Nativa Windows]
       │
       ▼
[Fase 5: Expansão de Formatos e Motores Avançados]
       │
       ▼
[Fase 6: Confiabilidade, Fuzzing e Conclusão]
```

---

## Detalhamento das Fases

### Fase 0 — Auditoria e Baseline (Concluída)
- Inspeção e congelamento do baseline da `main`.
- Criação da branch de desenvolvimento isolada `feature/proprietary-core`.
- Publicação dos documentos de auditoria em `docs/audit/`, `docs/architecture/` e `docs/roadmap/`.

### Fase 1 — Contratos, I/O e Núcleo Base
- Definição formal das interfaces de motor (`ICoreEngine`, `FormatInspector`, `JobController`).
- Criação dos módulos de segurança de I/O:
  - Leitor com teto máximo de alocação de memória (proteção contra DoS).
  - Identificador de tipos de arquivo por *Magic Bytes* (sem depender exclusivamente da extensão).
  - Sistema de escrita atômica com sufixo numérico anti-sobrescrita.
  - Cancelamento cooperativo com encerramento de workers e limpeza garantida de temporários.

### Fase 2 — Motor Piloto 1: Dados Estruturados (CSV, JSON, INI)
- **Escopo:**
  - Parser e escritor de CSV em estrita conformidade com a RFC 4180 (aspas duplas, campos multilinha, CRLF/LF, Unicode).
  - Parser e serializador de JSON (ECMA-404) com controle de profundidade e escapes.
  - Parser e escritor de INI com suporte formal a seções e comentários definidos.
- **Critérios de Aceitação:**
  - Zero dependências de pacotes npm (`fast-xml-parser`, `@iarna/toml`, `yaml`).
  - Suíte completa de testes unitários com fixtures determinísticas e entradas corrompidas.
  - Revisão de conformidade independente.

### Fase 3A — Motor Gráfico Base: BMP e Matriz de Pixels
- **Escopo:**
  - Decodificador e codificador próprio para BMP não comprimido (24-bit e 32-bit com canal alfa).
  - Manipulador de canvas em memória para operações matriciais:
    - Redimensionamento por interpolação bilinear ou vizinho mais próximo.
    - Recorte por coordenadas e grade de 9 quadrantes.
    - Aplicação de marca-d'água textual direta em pixels (renderização tipográfica raster básica).
    - Aplicação de marca-d'água por sobreposição de imagem com transparência.
- **Critérios de Aceitação:**
  - Elimina a dependência de `sharp` e `libvips` para o formato piloto de imagem.
  - Testes visuais e de conformidade comparando histogramas e dimensões.

### Fase 3B — Motor de Compactação e Arquivos: ZIP Seguro
- **Escopo:**
  - Leitor e extrator de arquivos ZIP (especificação PKWARE APPNOTE).
  - Implementação própria de descompressão Deflate (RFC 1951).
  - Proteção nativa contra Zip Slip / Path Traversal e bombas de descompressão (taxa de expansão máxima e limite total de bytes).
- **Critérios de Aceitação:**
  - Elimina a dependência de `7z.exe` para descompactação e criação de arquivos ZIP.

### Fase 3C — Motor Estrutural de PDF
- **Escopo:**
  - Parser de baixo nível de objetos PDF (ISO 32000-1): tabela de referências cruzadas (`xref`), catálogo de páginas e dicionários de objetos.
  - Operações de concatenação (Merge), separação (Split) e rotação de páginas em 90°.
- **Critérios de Aceitação:**
  - Elimina a dependência de `pdf-lib` para as operações estruturais fundamentais.

### Fase 4 — Reconstrução da Interface Desktop Nativa Windows
- Construção da janela nativa Windows em C# / XAML ou Win32 nativo.
- Preservação da identidade visual do OmniFree/Lodino:
  - Layout de 3 colunas (Fluxo à esquerda, Área central com drop zone e fila, Painel de atividade à direita).
  - Suporte a tema escuro/claro e internacionalização (PT-BR e EN).
  - Fila com arrastar e soltar, indicadores de progresso reais e histórico.
- **Resultado:** Eliminação de 100% dos pacotes Electron do produto final.

### Fase 5 — Expansão de Formatos e Motores Avançados
- Codificadores para imagens PNG e formatos adicionais.
- Manipuladores de áudio/vídeo e reconhecimento óptico progressivos.

### Fase 6 — Confiabilidade, Fuzzing e Conclusão
- Fuzzing de robustez em todas as interfaces públicas.
- Auditoria final de procedência e publicação do relatório de aceitação global.
