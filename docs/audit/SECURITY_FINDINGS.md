# Lodino — Avaliação e Vulnerabilidades de Segurança

Auditoria de segurança em tempo de execução, IPC, manipulação de arquivos e fronteiras de execução.

---

## 1. Vulnerabilidades e Riscos Identificados no Código Legado

### A. Risco Crítico: Zip Slip / Path Traversal em Extração de Arquivos
- **Localização:** `src/main/conversion-service.js` (linha 13)
- **Código:**
  ```javascript
  async function archive(input, output) {
    const folder = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-'));
    try {
      await run('7z', ['x', input, `-o${folder}`, '-y']);
      await run('7z', ['a', `-t${extensionOf(output)}`, output, path.join(folder, '*')]);
    } finally {
      await fs.rm(folder, { recursive: true, force: true });
    }
  }
  ```
- **Vulnerabilidade:** O parâmetro `-y` e a ausência de sanitização dos caminhos internos de arquivos `.zip`, `.tar` ou `.7z` permitem que arquivos especialmente construídos contendo sequências de diretório relativo (`../../`) escapem da pasta temporária e sobrescrevam arquivos arbitrários no sistema com as permissões do usuário.
- **Correção no Motor Próprio:** O leitor de arquivos compactados próprio deve validar cada entrada individual do cabeçalho, normalizar o caminho de destino e rejeitar qualquer arquivo cujo caminho resolvido não esteja estritamente contido no diretório raiz de extração.

---

### B. Injeção de SVG no Mecanismo de Marca-d'água
- **Localização:** `src/main/conversion-service.js` (linha 21)
- **Código:**
  ```javascript
  overlays.push({
    input: Buffer.from(
      `<svg width="800" height="80"><text x="20" y="55" font-size="42" fill="white" fill-opacity="0.7">${String(settings.watermark).replace(/[<&>]/g, '')}</text></svg>`
    ),
    gravity: 'southeast'
  });
  ```
- **Vulnerabilidade:** A sanitização regex remove apenas `<` e `&`. O restante da string pode conter caracteres de controle, strings gigantescas que causem exaustão de memória na rasterização do SVG ou falhas no parser XML interno da biblioteca C++.
- **Correção no Motor Próprio:** A inserção de texto e marca-d'água deve ser renderizada por algoritmo raster próprio em matriz de pixels, sem montagem de marcação intermediária e com limites estritos de caracteres e dimensões.

---

### C. Negação de Serviço por Exaustão de Memória (OOM DoS)
- **Localização:** `src/main/data-converter.js` (linha 41)
- **Código:**
  ```javascript
  async function convertData(input, output, source, target) {
    const raw = await fs.readFile(input, 'utf8');
    await fs.writeFile(output, writeTarget(target, parseSource(source, raw)), 'utf8');
  }
  ```
- **Vulnerabilidade:** O arquivo inteiro é lido em memória em uma única string UTF-8 e convertido para objetos JS. Arquivos com dezenas ou centenas de megabytes podem ultrapassar o limite do heap do V8 e travar o processo principal de forma irrecuperável.
- **Correção no Motor Próprio:** Leitura com streaming, buffers de tamanho fixo delimitados e teto máximo de tamanho de arquivo aceito (ex.: limite padrão de 100 MB para dados estruturados, expansível apenas por configuração explícita).

---

### D. Sequestro de Execução via PATH
- **Localização:** `src/main/system-tools.js` (linhas 19-25)
- **Código:**
  ```javascript
  function configureOptionalToolPath() { ... }
  function run(command, args) {
    const child = spawn(command, args, { windowsHide: true });
    ...
  }
  ```
- **Vulnerabilidade:** O `spawn` invoca comandos pelo nome (`soffice`, `7z`, `qpdf`) resolvendo através da variável de ambiente `PATH`. No Windows, se houver um arquivo malicioso com o nome `7z.exe` no diretório atual de trabalho, ele poderá ter precedência de execução sobre a ferramenta legítima.
- **Correção no Motor Próprio:** Eliminação total de chamadas a binários externos. Toda a computação deve ocorrer no processo da aplicação ou em workers internos dedicados.

---

## 2. Pontos Positivos Observados no Shell

- **Isolamento de Contexto:** `contextIsolation: true` e `nodeIntegration: false` estão devidamente ativados em `src/main/main.js` (linha 29).
- **Proteção contra Sobrescrita Acidental:** O módulo `src/main/output-paths.js` verifica a existência prévia do arquivo de saída e adiciona sufixos incrementais (`(1)`, `(2)`, até 1000 tentativas) antes de escrever o resultado.
- **Sandbox em Visualização de Páginas:** A rotina `webToPdf` instancia uma janela oculta com `sandbox: true` para renderizar a página antes de imprimir para PDF.
