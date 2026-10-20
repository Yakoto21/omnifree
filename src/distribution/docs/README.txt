================================================================================
  LODINO NATIVE SUITE — CONVERSOR LOCAL E SOBERANO
  Versão: 1.0.0-rc.2 (Release Candidate 2) — Edição Portátil para Windows x64
================================================================================

O Lodino é um conjunto de ferramentas nativas para Windows que oferece conversão,
inspeção, compressão e processamento de dados estruturados, imagens, áudio e
contêineres de arquivo com código 100% proprietário, sem dependências de terceiros,
sem Electron e sem codecs externos.

--------------------------------------------------------------------------------
REQUISITOS DE SISTEMA E ARTEFATOS
--------------------------------------------------------------------------------
- Pacote Oficial: Lodino-v1.0.0-rc.2-Portable-win-x64.zip
- Integridade: Lodino-v1.0.0-rc.2-Portable-win-x64.zip.sha256
- Manifesto: Lodino-v1.0.0-rc.2-Portable-win-x64-manifest.txt
- Sistema Operacional: Windows 7 SP1, Windows 8.1, Windows 10 ou Windows 11 (64 bits).
- Runtime: .NET Framework 4.0 ou superior (já vem pré-instalado de fábrica no
  Windows 8, 10 e 11).
- Instalação: Nenhuma. Esta é uma edição portátil autossuficiente. Basta descompactar
  e executar.
- Privilégios: Não exige privilégios de Administrador. Executa em nível de usuário.

--------------------------------------------------------------------------------
COMO UTILIZAR A INTERFACE GRÁFICA (GUI)
--------------------------------------------------------------------------------
1. Execute "Lodino.App.exe".
2. Adicione arquivos clicando no botão "Adicionar Arquivos..." ou simplesmente
   arraste e solte os arquivos desejados dentro da janela principal.
3. No painel direito, escolha a operação desejada:
   - "(Auto / Sugerido por Arquivo)": Opção recomendada! Detecta inteligentemente o
     tipo de cada arquivo e aplica a operação nativa ideal (converte imagens para PNG,
     converte CSV para JSON, e extrai arquivos .zip ou .gz).
   - "Converter para...": Força a conversão de todos os arquivos compatíveis para o
     formato escolhido (ex: PNG, JPG, BMP, JSON, CSV, INI, WAV).
   - "Compactar em ZIP": Agrupa ou compacta arquivos em contêineres .zip.
   - "Compactar em GZIP": Compacta arquivos individuais com algoritmo RFC 1952.
   - "Extrair Contêiner": Descompacta arquivos .zip e .gz com restauração de pastas.
4. Escolha a pasta de destino (ou deixe na mesma pasta dos arquivos originais).
5. Clique em "CONVERTER TUDO".
6. Ao término, clique em "Abrir Destino" para acessar os arquivos resultantes.

--------------------------------------------------------------------------------
COMO UTILIZAR A LINHA DE COMANDO (CLI)
--------------------------------------------------------------------------------
Abra o Prompt de Comando (cmd) ou o PowerShell nesta pasta e execute:

  Lodino.exe help
  Lodino.exe list-engines
  Lodino.exe convert <entrada> <formato_destino> [pasta_destino]
  Lodino.exe inspect <arquivo>
  Lodino.exe zip <pasta_ou_arquivo> [saida.zip] [--compression=optimal|fast|store]
  Lodino.exe unzip <arquivo.zip> [pasta_destino]
  Lodino.exe list-zip <arquivo.zip>
  Lodino.exe gzip <arquivo> [saida.gz] [--best|--fast|--store]
  Lodino.exe gunzip <arquivo.gz> [saida_descompactada]
  Lodino.exe inspect-gzip <arquivo.gz>

Exemplos Práticos:
  Lodino.exe convert dados.csv json
  Lodino.exe convert foto.bmp png
  Lodino.exe convert foto.jpg bmp
  Lodino.exe zip .\MinhaPasta backup.zip --compression=optimal
  Lodino.exe unzip backup.zip .\Extraidos
  Lodino.exe gzip documento.txt documento.txt.gz --best
  Lodino.exe gunzip documento.txt.gz

--------------------------------------------------------------------------------
FORMATOS SUPORTADOS NESTA VERSÃO
--------------------------------------------------------------------------------
1. Dados Estruturados: CSV (RFC 4180), JSON (RFC 8259), INI.
2. Imagens: BMP (24/32 bpp), PNG (Truecolor RGB/RGBA), JPEG (Baseline Sequential).
3. Áudio: WAV (RIFF/WAVE PCM e IEEE Float 8, 16, 24 e 32 bits, mono/estéreo).
4. Arquivamento: ZIP (PKZIP Store e Deflate próprio), GZIP (RFC 1952).

Consulte o arquivo "KNOWN_LIMITATIONS.txt" para detalhes técnicos de cada formato.

--------------------------------------------------------------------------------
SUPORTE E NOTAS DE SEGURANÇA
--------------------------------------------------------------------------------
- O Lodino não se comunica com a internet nem envia telemetria. Todo o processamento
  ocorre estritamente no seu computador.
- Caso o Windows Defender SmartScreen apresente a tela azul "O Windows protegeu o
  seu computador", isto ocorre porque os binários do Release Candidate 2 (RC2) ainda não
  possuem certificado de assinatura comercial de código pago. Clique em "Mais
  informações" e em seguida em "Executar assim mesmo".
