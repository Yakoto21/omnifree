# Lodino Native Suite

**Conversor e Processador Local e Soberano de Arquivos para Windows**
*Versão Atual: 1.0.0-rc.3 | Arquitetura: Windows x64 Nativo (C# / .NET Framework 4.0 / Win32 GDI)*

---

## 1. Identidade e Objetivo

O **Lodino Native Suite** é uma suíte de software para Windows focada na conversão, compressão, inspeção e processamento em lote de dados estruturados, imagens raster, áudio linear e contêineres de arquivos.

O produto foi concebido sob a diretriz fundamental de **soberania de código e preservação de recursos**:
* **100% Local e Privado:** Não requer conexão com a internet, não realiza requisições remotas e não coleta telemetria de qualquer natureza.
* **Zero Runtimes Pesados:** Livre de Electron, Node.js, Chromium, Python ou frameworks interpretados.
* **Zero Dependências Externas no Runtime:** Não utiliza DLLs de terceiros nem wrappers sobre ferramentas externas (como FFmpeg ou ImageMagick). Todos os motores de transformação operam com algoritmos proprietários implementados em C#.
* **Leve e Eficiente:** Executável ultracompacto (pacote portátil de ~137 KB) projetado para operação fluida mesmo em hardware modesto (ex: Surface Pro 1 com 4 GB de RAM).

---

## 2. Arquitetura da Solução

O projeto adota uma arquitetura em camadas puramente nativa para o sistema operacional Windows:

* **`Lodino.Core.dll` (Núcleo Gerenciado Soberano):** Biblioteca que encapsula os motores de decodificação, codificação, streaming, cálculo de checksums (CRC-32) e políticas de segurança (anti-bomb, anti-traversal). Depende estritamente da Biblioteca de Classes Base (BCL) do .NET Framework 4.0 (`mscorlib`, `System`, `System.Core`).
* **`Lodino.exe` (Interface de Linha de Comando - CLI):** Utilitário de console para automação, scripting e processamento direto de arquivos.
* **`Lodino.App.exe` (Interface Gráfica - GUI Win32 Pura):** Aplicativo gráfico construído diretamente sobre as APIs nativas do Windows (`user32.dll`, `gdi32.dll`, `shell32.dll`, `comctl32.dll`, `dwmapi.dll`) via P/Invoke direto, sem utilizar Windows Forms, WPF ou bibliotecas de UI de terceiros.
* **`Lodino.Tests.exe` (Harness de Testes Automatizados):** Suíte exaustiva de testes unitários e de integração (233 testes) com verificação determinística e tolerância zero a regressões.

---

## 3. Motores de Processamento Próprios

Todos os motores foram implementados com base direta nas especificações públicas e normativas da indústria:

1. **Dados Estruturados (`StructuredDataEngine`):**
   * *CSV (RFC 4180):* Parser e serializador com suporte a campos entre aspas, vírgulas embutidas, quebras multilinha, escape padrão (`""`), tratamento de BOM e normalização de colunas.
   * *JSON (RFC 8259 / ECMA-404):* Parser recursivo e formatador com suporte a tipos primitivos, objetos, matrizes, escapes Unicode (`\uXXXX`), pares substitutos (emojis) e proteção contra profundidade excessiva.
   * *INI:* Parser de seções, propriedades `chave=valor` e comentários (`#`, `;`).
2. **Imagens Bitmap (`BmpEngine`):**
   * Padrão Windows DIB (Device-Independent Bitmap).
   * Leitura e escrita de imagens de 24 bits (BGR) e 32 bits (BGRX opaco).
   * Suporte a orientações Bottom-Up e Top-Down, cálculo de padding por scanline, redimensionamento (Nearest-Neighbor e Bilinear), recorte em quadrantes e aplicação de marca d'água.
3. **Imagens PNG (`PngEngine`):**
   * Especificação ISO/IEC 15948 / W3C Recommendation.
   * Leitura e geração de chunks `IHDR`, `IDAT` e `IEND`.
   * Suporte a Truecolor RGB (24-bit) e RGBA (32-bit), aplicação e reversão dos 5 filtros de scanline (None, Sub, Up, Average, Paeth) e compressão ZLIB própria.
4. **Imagens JPEG (`JpegEngine`):**
   * Especificação ISO/IEC 10918-1 / ITU-T T.81 (Baseline Sequential DCT, SOF0).
   * Codificação e decodificação de 8 bits com tabelas Huffman canônicas, quantização e subamostragens YCbCr 4:4:4, 4:2:2, 4:2:0 e escala de cinza.
5. **Áudio WAV (`WavEngine`):**
   * Especificação IBM/Microsoft RIFF/WAVE.
   * Áudio PCM linear inteiro de 8, 16, 24 e 32 bits, configurações mono e estéreo.
   * Aritmética de saturação Q31 contra clipping, reamostragem, mixagem e suporte a cabeçalhos `WAVE_FORMAT_EXTENSIBLE`.
6. **Contêineres ZIP (`ZipEngine`):**
   * Especificação PKWARE APPNOTE 6.3.9.
   * Leitura e criação de arquivos ZIP com suporte a blocos Stored (BTYPE 00), Fixed Huffman (BTYPE 01) e Dynamic Huffman (BTYPE 10).
   * Blindagem total contra ataques de travessia de caminho (anti-Zip Slip) e rejeição estrita de nomes de dispositivos reservados no Win32 (`CON`, `PRN`, `AUX`, `NUL`, etc.).
7. **Contêineres GZIP (`GzipEngine`):**
   * Especificação RFC 1952.
   * Descompressão e compressão por streaming O(1) de fluxo com verificação estrita de trailer CRC-32 e ISIZE.
   * Suporte a múltiplos membros concatenados (RFC 1952 § 2.2) e sanitização de metadados FNAME/FCOMMENT contra injeção de caracteres de controle C0/ANSI (CWE-150).

---

## 4. Formatos Efetivamente Suportados

| Domínio | Formatos de Entrada | Formatos de Saída | Algoritmo / Especificação |
|---|---|---|---|
| **Dados** | CSV, JSON, INI | CSV, JSON, INI | RFC 4180, RFC 8259, INI padrão |
| **Imagens** | BMP, PNG, JPEG | BMP, PNG, JPEG | DIB, ISO/IEC 15948, ITU-T T.81 |
| **Áudio** | WAV (PCM) | WAV (PCM) | RIFF/WAVE PCM 8/16/24/32-bit |
| **Contêineres**| ZIP (.zip), GZIP (.gz) | ZIP (.zip), GZIP (.gz) | PKZIP 6.3.9, RFC 1951, RFC 1952 |

---

## 5. Requisitos de Execução

* **Sistema Operacional:** Windows 7 SP1, Windows 8.1, Windows 10 ou Windows 11 (arquitetura x64).
* **Runtime:** Microsoft .NET Framework 4.0 ou superior (já incorporado nativamente no Windows 8, 10 e 11).
* **Privilégios:** Nível de usuário padrão (`RequestExecutionLevel user`). Não requer elevação administrativa (UAC).
* **Conectividade:** Nenhuma dependência de rede ou internet.

---

## 6. Como Compilar no Windows

O projeto não requer Visual Studio pesado nem ferramentas externas de compilação; utiliza o compilador oficial C# já integrado ao .NET Framework do Windows:

```powershell
# Compilação padrão (modo Debug)
powershell -ExecutionPolicy Bypass -File scripts\build-core.ps1

# Compilação otimizada para produção (modo Release)
powershell -ExecutionPolicy Bypass -File scripts\build-core.ps1 -Release
```

Os assemblies compilados são gerados no diretório `build/`:
* `build\Lodino.Core.dll`
* `build\Lodino.exe`
* `build\Lodino.App.exe`
* `build\Lodino.Tests.exe`

---

## 7. Como Executar os Testes Unitários

A integridade do software é validada por uma suíte rigorosa de **233 testes unitários**:

```powershell
# Compilação e execução imediata de todos os testes
powershell -ExecutionPolicy Bypass -File scripts\build-core.ps1 -RunTests

# Ou execução direta do harness já compilado
.\build\Lodino.Tests.exe
```

---

## 8. Como Utilizar a Linha de Comando (CLI)

```powershell
# Ajuda e listagem de motores
.\build\Lodino.exe help
.\build\Lodino.exe list-engines

# Conversão de dados
.\build\Lodino.exe convert dados.csv json
.\build\Lodino.exe convert dados.json csv
.\build\Lodino.exe convert configuracao.csv ini

# Conversão de imagens
.\build\Lodino.exe convert imagem.bmp png
.\build\Lodino.exe convert foto.png jpg
.\build\Lodino.exe convert foto.jpg bmp

# Manipulação de contêineres ZIP
.\build\Lodino.exe zip .\MinhaPasta backup.zip --compression=optimal
.\build\Lodino.exe list-zip backup.zip
.\build\Lodino.exe unzip backup.zip .\Destino

# Manipulação de contêineres GZIP
.\build\Lodino.exe gzip documento.txt documento.txt.gz --best
.\build\Lodino.exe inspect-gzip documento.txt.gz
.\build\Lodino.exe gunzip documento.txt.gz
```

---

## 9. Como Iniciar a Interface Gráfica (GUI)

Execute o binário da interface Win32:
```powershell
.\build\Lodino.App.exe
```

* **Arrastar e Soltar:** Arraste arquivos diretamente para a grade de conversão.
* **Detecção Inteligente:** O modo `(Auto / Sugerido por Arquivo)` aplica a conversão nativa recomendada para cada tipo de entrada.
* **Processamento Assíncrono:** A fila executa em segundo plano com barra de progresso, isolamento estrito de erros e cancelamento cooperativo instantâneo.

---

## 10. Como Gerar o Pacote Portátil e o Instalador

Para gerar os artefatos oficiais de distribuição para a versão homologada:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1 -Version 1.0.0-rc.3
```

### Artefatos Gerados em `dist/`:
1. **Pacote Portátil Zero-Install:** `dist\Lodino-v1.0.0-rc.3-Portable-win-x64.zip` (contendo os 3 binários nativos e 4 documentos essenciais).
2. **Checksum Criptográfico:** `dist\Lodino-v1.0.0-rc.3-Portable-win-x64.zip.sha256`.
3. **Manifesto de Integridade:** `dist\Lodino-v1.0.0-rc.3-Portable-win-x64-manifest.txt`.
4. **Instalador NSIS:** `dist\Lodino-v1.0.0-rc.3-Setup-win-x64.exe` (requer NSIS 3.10 ou 3.13 no sistema).

---

## 11. Limitações Conhecidas

Em conformidade com a transparência técnica documentada em [`src/distribution/docs/KNOWN_LIMITATIONS.txt`](src/distribution/docs/KNOWN_LIMITATIONS.txt):
* **BMP:** Não suporta paletas indexadas (1, 4, 8 bpp) nem compressão legada RLE4/RLE8.
* **PNG:** Imagens paletizadas ou em escala de cinza com alfa não são suportadas nesta versão.
* **JPEG:** Progressive JPEG (SOF2), JPEG Lossless, codificação aritmética e espaços de cor CMYK/YCCK não são suportados.
* **WAV:** Não converte de ou para codecs com perdas (MP3, AAC, OGG). Áudio comprimido (ADPCM, GSM) e ponto flutuante IEEE não são suportados.
* **ZIP:** Arquivos ZIP64 (> 4 GB ou > 65.535 entradas) e criptografia por senha não são suportados.
* **GZIP:** Comprime arquivos individuais; para diretórios inteiros, utilize o formato ZIP.

---

## 12. Política de Originalidade, Dependências e Licenciamento

* **Código de Produção:** O núcleo do Lodino Native Suite foi desenvolvido especificamente para o projeto, utilizando algoritmos próprios sem cópia de repositórios externos.
* **Dependências de Runtime:** Zero dependências de terceiros. Apenas a BCL padrão do .NET Framework 4.0 e chamadas P/Invoke da API Win32 do Windows são consumidas.
* **Licenciamento do Software:** O Lodino Native Suite é distribuído sob os termos da **Licença MIT** (consulte [`src/distribution/docs/LICENSE.txt`](src/distribution/docs/LICENSE.txt)).
* **Atribuições Técnicas:** As referências normativas e os avisos sobre a licença permissiva zlib/libpng do instalador NSIS encontram-se em [`src/distribution/docs/THIRD_PARTY_NOTICES.txt`](src/distribution/docs/THIRD_PARTY_NOTICES.txt).

---

## 13. Preservação do Histórico do Protótipo

Os materiais residuais da prova de conceito original baseada em Electron ("OmniFree" / "meu-conversor"), desenvolvida nas fases iniciais do projeto, foram formalmente isolados e mantidos no diretório:
* [`legacy/`](legacy/)
  * `legacy/electron_prototype/`: arquivos JavaScript, interface HTML/CSS, manifestos `package.json` e testes antigos em Node.js. Consulte [`legacy/electron_prototype/README.md`](legacy/electron_prototype/README.md) para a análise de governança e histórico de descontinuação.
  * `legacy/engines/`: especificações de ferramentas externas cogitadas no antigo protótipo.
  * `legacy/THIRD_PARTY_NOTICES.md`: avisos antigos referentes ao protótipo anterior.
