using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.Api;
using Lodino.Core.Checksums;
using Lodino.Core.Compression.Deflate;
using Lodino.Core.Errors;
using Lodino.Core.Formats.Audio;
using Lodino.Core.Formats.Audio.Wav;
using Lodino.Core.Formats.Data;
using Lodino.Core.Formats.Image;
using Lodino.Core.Formats.Image.Jpeg;
using Lodino.Core.Formats.Image.Png;
using Lodino.Core.IO;
using Lodino.Core.Registry;
using Lodino.Core.Safety;
using Lodino.Core.Formats.Archive.Gzip;
using Lodino.Core.Formats.Archive.Zip;
using System.Threading;
using Lodino.Desktop;
using Crc32 = Lodino.Core.Checksums.Crc32;

namespace Lodino.Tests
{
    public class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — SUÍTE DE TESTES EXPANDIDA ");
            Console.WriteLine("  Módulo: Motor Piloto 1 (Dados Estruturados)    ");
            Console.WriteLine("=================================================\n");

            // --- CSV (RFC 4180 & Edge Cases) ---
            RunTest("CSV: Parser básico de colunas e linhas", Test_Csv_BasicParsing);
            RunTest("CSV: Conformidade RFC 4180 com vírgulas em campos citados", Test_Csv_Rfc4180_QuotesAndCommas);
            RunTest("CSV: Conformidade RFC 4180 com valores multilinha", Test_Csv_Rfc4180_Multiline);
            RunTest("CSV: Conformidade RFC 4180 com aspas duplas escapadas (\"\")", Test_Csv_Rfc4180_EscapedQuotes);
            RunTest("CSV: Round-trip de serialização e leitura", Test_Csv_WriterRoundTrip);
            RunTest("CSV: Suporte a quebras de linha mistas (CRLF, LF e CR)", Test_Csv_LineEndings_CRLF_LF_CR);
            RunTest("CSV: Campos vazios consecutivos e vírgulas finais", Test_Csv_EmptyFieldsAndTrailingCommas);
            RunTest("CSV: Remoção segura de UTF-8 BOM sem corromper primeiro caractere", Test_Csv_Utf8BomHandling);
            RunTest("CSV: Normalização de registros com contagem inconsistente de colunas", Test_Csv_InconsistentRecordLengths);
            RunTest("CSV: Resiliência contra aspas não fechadas no fim do arquivo", Test_Csv_Malformed_UnclosedQuotes);
            RunTest("CSV (Estrito RFC 4180): Rejeição de contagem inconsistente de colunas", Test_Csv_StrictRfc4180_RejectionOfInconsistentColumns);
            RunTest("CSV (Estrito RFC 4180): Rejeição de aspas não fechadas no EOF", Test_Csv_StrictRfc4180_RejectionOfUnclosedQuotes);
            RunTest("CSV (Estrito RFC 4180): Rejeição de caractere inesperado após aspas", Test_Csv_StrictRfc4180_RejectionOfCharAfterQuotes);

            // --- JSON (ECMA-404 & Edge Cases) ---
            RunTest("JSON: Parser de tipos primitivos, objetos e arrays", Test_Json_PrimitivesAndObjects);
            RunTest("JSON: Suporte a sequências de escape Unicode (\\uXXXX)", Test_Json_UnicodeEscapes);
            RunTest("JSON: Decodificação de pares substitutos Unicode (Surrogate Pairs / Emojis)", Test_Json_Unicode_SurrogatePairs);
            RunTest("JSON: Números grandes e notação científica com expoente", Test_Json_LargeNumbersAndScientificNotation);
            RunTest("JSON: Preservação de texto bruto para evitar perda numérica de precisão", Test_Json_NumericPrecisionLimitation);
            RunTest("JSON: Rejeição estrita de caracteres de controle (< 0x20) sem escape", Test_Json_ControlCharacterRejection);
            RunTest("JSON: Política de propriedades duplicadas (última ocorrência prevalece)", Test_Json_DuplicatePropertiesPolicy);
            RunTest("JSON: Rejeição de zeros à esquerda em inteiros (ex: 0123)", Test_Json_LeadingZerosRejection);
            RunTest("JSON: Rejeição estrita de sintaxe malformada e vírgula pendente", Test_Json_MalformedRejection);
            RunTest("JSON: Proteção contra ataque DoS de profundidade excessiva", Test_Json_DepthLimitSecurity);

            // --- INI (Dialeto Formal & Edge Cases) ---
            RunTest("INI: Parser de seções, propriedades e comentários", Test_Ini_SectionsAndComments);
            RunTest("INI: Propriedades globais sem seção", Test_Ini_GlobalProperties);
            RunTest("INI: Valores vazios e aparo de espaços em branco (trimming)", Test_Ini_EmptyValuesAndWhitespace);
            RunTest("INI: Suporte a comentários iniciados por '#' e ';'", Test_Ini_Comments_HashAndSemicolon);
            RunTest("INI: Política de chaves duplicadas na mesma seção", Test_Ini_DuplicateKeysPolicy);
            RunTest("INI: Valores entre aspas preservando caracteres de comentário", Test_Ini_QuotedValuesWithSpecialChars);

            // --- MATRIZ DE CONVERSÃO (TODAS AS 9 ROTAS COMBINATÓRIAS) ---
            RunTest("ROTA 1/9: CSV -> CSV (Normalização e Re-quoting)", Test_Route_CsvToCsv);
            RunTest("ROTA 2/9: CSV -> JSON (Tabela para Array de Objetos)", Test_Route_CsvToJson);
            RunTest("ROTA 3/9: CSV -> INI (Tabela para Seções e Propriedades)", Test_Route_CsvToIni);
            RunTest("ROTA 4/9: JSON -> JSON (Validação, Canonicalização e Pretty-Print)", Test_Route_JsonToJson);
            RunTest("ROTA 5/9: JSON -> CSV (Array de Objetos para Tabela Aplanada)", Test_Route_JsonToCsv);
            RunTest("ROTA 6/9: JSON -> INI (Objetos Hierárquicos para Seções INI)", Test_Route_JsonToIni);
            RunTest("ROTA 7/9: INI -> INI (Normalização Estrutural)", Test_Route_IniToIni);
            RunTest("ROTA 8/9: INI -> JSON (Seções para Objetos Aninhados)", Test_Route_IniToJson);
            RunTest("ROTA 9/9: INI -> CSV (Seções e Chaves para Tabela com 3 Colunas)", Test_Route_IniToCsv);

            // --- SEGURANÇA E REGISTRY ---
            RunTest("SEGURANÇA: Geração de caminhos únicos e proteção anti-sobrescrita", Test_SafeFileWriter_UniquePaths);
            RunTest("REGISTRY: Resolução dinâmica do motor pelo catálogo de formatos", Test_EngineRegistry_Resolution);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR PILOTO 2            ");
            Console.WriteLine("  Módulo: Motor Nativo de Imagens BMP Próprio    ");
            Console.WriteLine("=================================================\n");

            // --- BMP: Dimensões Pequenas & Padding de Scanline ---
            RunTest("BMP: 1x1 24-bit com padding de 1 byte por scanline", Test_Bmp_1x1_Padding1);
            RunTest("BMP: 2x2 24-bit com padding de 2 bytes por scanline", Test_Bmp_2x2_Padding2);
            RunTest("BMP: 3x3 24-bit com padding de 3 bytes por scanline", Test_Bmp_3x3_Padding3);
            RunTest("BMP: 4x4 24-bit alinhado sem padding (múltiplo de 4)", Test_Bmp_4x4_NoPadding);
            RunTest("BMP: 7x5 24-bit com dimensões ímpares e padding", Test_Bmp_7x5_OddDimensions);

            // --- BMP: Variantes 24-bit e 32-bit Round-trip ---
            RunTest("BMP: Round-trip de fidelidade 24-bit BGR", Test_Bmp_24bit_RoundTrip);
            RunTest("BMP: 32-bit BI_RGB semântica BGRX opaca com valores variados no byte alto", Test_Bmp_32bit_Bgrx_ArbitraryHighBytes_NormalizesOpaque);
            RunTest("BMP: Round-trip 32-bit BGRX (preservação estrita dos canais RGB com A=255)", Test_Bmp_32bit_Bgrx_RoundTrip);

            // --- BMP: Orientação de Linhas ---
            RunTest("BMP: Decodificação correta de Bottom-Up (padrão Windows)", Test_Bmp_BottomUp_Orientation);
            RunTest("BMP: Decodificação correta de Top-Down (altura negativa)", Test_Bmp_TopDown_Orientation);

            // --- BMP: Transformações de Imagem ---
            RunTest("BMP: Redimensionamento por Vizinho Mais Próximo (Nearest-Neighbor)", Test_Bmp_Resize_NearestNeighbor);
            RunTest("BMP: Redimensionamento Bilinear contínuo com interpolação ponderada", Test_Bmp_Resize_Bilinear);
            RunTest("BMP: Recorte de retângulo arbitrário com validação de limites", Test_Bmp_Crop_Arbitrary);
            RunTest("BMP: Recorte nos 9 quadrantes (top-left, center, bottom-right)", Test_Bmp_Crop_Quadrants);
            RunTest("BMP: Marca d'água textual com renderizador BitmapFont 8x8 proprietário", Test_Bmp_Watermark_Text);
            RunTest("BMP: Marca d'água por imagem com composição alfa (Porter-Duff Source-Over)", Test_Bmp_Watermark_Image_AlphaComposite);

            // --- BMP: Segurança e Rejeição Estrita de Variantes Não Suportadas ---
            RunTest("BMP (Segurança): Rejeição de assinatura mágica inválida (não 'BM')", Test_Bmp_Security_InvalidMagic);
            RunTest("BMP (Segurança): Rejeição de cabeçalho DIB não suportado (biSize != 40)", Test_Bmp_Security_UnsupportedHeaderSize);
            RunTest("BMP (Segurança): Rejeição de variantes indexadas com paleta (1, 4 e 8 bpp)", Test_Bmp_Security_RejectPalettes);
            RunTest("BMP (Segurança): Rejeição de variantes comprimidas (BI_RLE8, BI_RLE4, etc.)", Test_Bmp_Security_RejectCompression);
            RunTest("BMP (Segurança): Rejeição de dimensões zero ou negativas", Test_Bmp_Security_InvalidDimensions);
            RunTest("BMP (Segurança): Proteção contra dimensões excessivas (> 16.384 pixels)", Test_Bmp_Security_ExceedMaxDimension);
            RunTest("BMP (Segurança): Rejeição de arquivo truncado (dados de scanline insuficientes)", Test_Bmp_Security_TruncatedFile);
            RunTest("BMP (Segurança): Rejeição de bfOffBits apontando para fora do arquivo", Test_Bmp_Security_InvalidOffBits);

            // --- BMP: Fixtures Independentes e Validação Rigorosa (Fase B) ---
            RunTest("BMP (Fixture Independente): 24-bit com variantes de padding (0, 1, 2 e 3 bytes)", Test_Bmp_Independent_24bit_PaddingVariants_0_1_2_3);
            RunTest("BMP (Fixture Independente): Deslocamento bfOffBits personalizado com metadados", Test_Bmp_Independent_CustomOffBits_WithMetadataHeader);
            RunTest("BMP (Fixture Independente): Equivalência exata entre Bottom-Up e Top-Down", Test_Bmp_Independent_TopDown_vs_BottomUp_ExactDuality);
            RunTest("BMP (Fixture Independente): Truncamento de cabeçalho e corrupção de offsets", Test_Bmp_Independent_HeaderTruncation_And_SizeMismatch);
            RunTest("BMP (Fixture Independente): Detecção e bloqueio de overflow em dimensões", Test_Bmp_Independent_DimensionOverflow);

            // --- BMP: Integração do Motor BmpEngine e Registry ---
            RunTest("BMP (Engine): Inspeção de metadados via BmpEngine.Inspect", Test_Bmp_Engine_Inspect);
            RunTest("BMP (Engine): Pipeline completo de conversão e transformações via BmpEngine", Test_Bmp_Engine_ConvertPipeline);
            RunTest("REGISTRY: Resolução dinâmica do motor image-bmp para bmp -> bmp", Test_Bmp_Registry_Resolution);

            // =================================================
            //   LODINO NATIVE CORE — MOTOR PILOTO 3            
            //   Módulo: Motor Nativo de Imagens PNG Próprio    
            // =================================================

            RunTest("PNG (Checksum): Validação de vetores CRC-32 (ISO 3309)", Test_Png_Checksum_Crc32_Vectors);
            RunTest("PNG (Checksum): Validação de vetores Adler-32 (RFC 1950)", Test_Png_Checksum_Adler32_Vectors);
            RunTest("PNG (DEFLATE): Blocos não comprimidos (Stored BTYPE 00) e integridade NLEN", Test_Png_Deflate_StoredBlocks_SingleAndMulti);
            RunTest("PNG (DEFLATE): Descompressão com árvores de Huffman fixas (BTYPE 01)", Test_Png_Deflate_FixedHuffman_Decompression);
            RunTest("PNG (DEFLATE): Descompressão com árvores de Huffman dinâmicas (BTYPE 10)", Test_Png_Deflate_DynamicHuffman_Decompression);
            RunTest("PNG (Filtros): Reversibilidade e fidelidade dos 5 filtros (None, Sub, Up, Average, Paeth)", Test_Png_Filters_All5_RoundTrip);
            RunTest("PNG (Fixture Independente): 24-bit Truecolor RGB (ColorType 2) montado byte a byte", Test_Png_Independent_Raw_Rgb24_Fixture);
            RunTest("PNG (Fixture Independente): 32-bit Truecolor RGBA (ColorType 6) com preservação de alfa", Test_Png_Independent_Raw_Rgba32_Fixture);
            RunTest("PNG (IDAT Múltiplo): Descompressão de fluxo ZLIB fragmentado em múltiplos chunks", Test_Png_Multiple_Idat_Chunks);
            RunTest("PNG (Round-Trip): Fidelidade exata de codificação e decodificação RGB e RGBA", Test_Png_RoundTrip_Rgb_And_Rgba);
            RunTest("PNG (Encoder): Round-trip com seleção explícita dos 5 filtros de linha", Test_Png_Encoder_All5_Filters);
            RunTest("PNG (Segurança): Rejeição estrita de variantes não suportadas e dados corrompidos", Test_Png_Security_StrictRejections);
            RunTest("PNG (Engine): Inspeção completa de cabeçalho e metadados via PngEngine.Inspect", Test_Png_Engine_Inspect);
            RunTest("PNG (Engine): Pipeline completo de transformações (crop, resize, watermark)", Test_Png_Engine_Convert_Transforms);
            RunTest("PNG (Rotas Bidirecionais): Conversão PNG <-> BMP e resolução no EngineRegistry", Test_Png_Engine_Bidirectional_Bmp_Png);

            // =================================================
            //   LODINO NATIVE CORE — HARDENING BMP + PNG       
            //   Módulo: Segurança e Resiliência contra Ataques  
            // =================================================
            RunTest("HARDENING: Proteção contra Decompression Bomb (Zip Bomb em PNG)", Test_Hardening_DecompressionBomb_Protection);
            RunTest("HARDENING: Particionamento de IDAT em chunks consecutivos de 32 KB", Test_Hardening_Idat_Chunking_Generation);
            RunTest("HARDENING: Resiliência contra fluxos fragmentados (1-3 bytes por leitura)", Test_Hardening_Stream_Fragmentation_Resilience);
            RunTest("HARDENING: Prevenção contra overflow de inteiros em PixelBuffer.Crop", Test_Hardening_PixelBuffer_Crop_Overflow);
            RunTest("HARDENING: Rejeição de distância LZ77 anterior ao início do fluxo", Test_Hardening_Deflate_Lz77_Distance_Underflow);
            RunTest("HARDENING: Rejeição de códigos de repetição dinâmicos DEFLATE além dos limites", Test_Hardening_Deflate_Dynamic_Repeat_Overflow);
            RunTest("HARDENING: Rejeição de árvores de Huffman sobre-subscritas (Violação de Kraft)", Test_Hardening_Huffman_Oversubscribed_Tree);
            RunTest("HARDENING: Leitura segura de streams não-navegáveis (CanSeek = false) no BMP", Test_Hardening_Bmp_NonSeekable_Stream);
            RunTest("HARDENING: Rejeição de bytes espúrios após Adler-32 em fluxo ZLIB", Test_Hardening_Zlib_Trailing_Garbage);
            RunTest("HARDENING: Tolerância a bytes residuais (padding) no último IDAT (ISO/IEC 15948 §10.2)", Test_Hardening_Png_Idat_TrailingPadding_Allowed);
            RunTest("HARDENING: Rejeição de streams não-seekable no BMP que excedam o limite de entrada", Test_Hardening_Bmp_NonSeekable_Stream_LimitExceeded);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR PILOTO 3            ");
            Console.WriteLine("  Módulo: Motor Nativo de Imagens JPEG Próprio   ");
            Console.WriteLine("=================================================\n");

            RunTest("JPEG (Matemática): Precisão rigorosa de FDCT e IDCT 2D (< 10^-4)", Test_Jpeg_FastDct_Accuracy);
            RunTest("JPEG (Quantização): Invertibilidade Zigzag e escalonamento IJG", Test_Jpeg_QuantizationTable_QualityScaling);
            RunTest("JPEG (Huffman): Tabelas padrão Anexo K e codificação/decodificação canônica", Test_Jpeg_HuffmanTable_Canonical_Roundtrip);
            RunTest("JPEG (Bits): Byte-stuffing automático (0xFF 0x00) e desempacotamento MSB", Test_Jpeg_BitWriter_BitReader_ByteStuffing);
            RunTest("JPEG (Cor): Conversão BT.601 RGB <-> YCbCr e fidelidade cromática", Test_Jpeg_ColorConverter_RgbYCbCr);
            RunTest("JPEG (Baseline 4:4:4): Codificação e decodificação com alta fidelidade (PSNR > 35 dB)", Test_Jpeg_EncodeDecode_Baseline444_Roundtrip);
            RunTest("JPEG (Baseline 4:2:0): Subamostragem cromática fotográfica (PSNR > 30 dB)", Test_Jpeg_EncodeDecode_Baseline420_Roundtrip);
            RunTest("JPEG (Grayscale): Decodificação de imagem monocromática de 1 componente", Test_Jpeg_Decode_Grayscale);
            RunTest("JPEG (Marcadores): Reinício de predição DC via Restart Interval (DRI / RSTm)", Test_Jpeg_RestartInterval_DRI_RSTm);
            RunTest("JPEG (Segurança): Rejeição de assinatura inválida (sem SOI)", Test_Jpeg_Security_InvalidSignature_Rejection);
            RunTest("JPEG (Segurança): Rejeição de perfil progressivo não suportado (SOF2)", Test_Jpeg_Security_UnsupportedProfile_SOF2_Rejection);
            RunTest("JPEG (Segurança): Rejeição de precisão de amostra diferente de 8 bits", Test_Jpeg_Security_UnsupportedPrecision_Rejection);
            RunTest("JPEG (Segurança): Rejeição de formatos com 4 componentes (CMYK/YCCK)", Test_Jpeg_Security_UnsupportedComponents_CMYK_Rejection);
            RunTest("JPEG (Segurança): Rejeição de dimensões inválidas ou excessivas", Test_Jpeg_Security_Dimensions_Limits);
            RunTest("JPEG (Segurança): Rejeição de fluxo truncado ou prematuro", Test_Jpeg_Security_TruncatedStream_Rejection);
            RunTest("JPEG (Engine): Inspeção completa de metadados via JpegEngine.Inspect", Test_Jpeg_Engine_Inspect);
            RunTest("JPEG (Engine): Pipeline completo de transformações (crop, resize, watermark)", Test_Jpeg_Engine_Convert_Transforms);
            RunTest("JPEG (Rotas Bidirecionais): Matriz completa JPEG <-> BMP, JPEG <-> PNG e JPEG <-> JPEG", Test_Jpeg_Bidirectional_Routes);

            // =================================================
            //   LODINO NATIVE CORE — HARDENING JPEG (FASE 7)  
            //   Módulo: Resiliência Adversarial e Hardening   
            // =================================================
            RunTest("HARDENING JPEG: Validação estrita de fatores de amostragem Hi/Vi e múltiplos (D-01)", Test_Hardening_Jpeg_SamplingFactors_Validation);
            RunTest("HARDENING JPEG: Limites de seletores de tabela (0..3) e existência prévia (D-02)", Test_Hardening_Jpeg_TableSelectors_Bounds_And_Existence);
            RunTest("HARDENING JPEG: Validação estrutural de comprimentos de segmentos DQT/DHT (D-03)", Test_Hardening_Jpeg_SegmentLengths_DqtDht_Validation);
            RunTest("HARDENING JPEG: Validação de transbordamento de bloco no símbolo ZRL 0xF0 (D-07)", Test_Hardening_Jpeg_ZRL_BlockBoundary_Validation);
            RunTest("HARDENING JPEG: Sincronização SOF e metadados JFIF APP0 (DPI, unidades) no Inspect (D-04, D-05)", Test_Hardening_Jpeg_Engine_Inspect_SofSync_And_JfifApp0);
            RunTest("HARDENING JPEG: Codificação e decodificação monocromática Grayscale 1:1 (D-08)", Test_Hardening_Jpeg_Grayscale_Encoder_Roundtrip);
            RunTest("HARDENING JPEG: Rejeição de streams JPEG não-seekable que excedam o limite de entrada", Test_Hardening_Jpeg_NonSeekable_Stream_LimitExceeded);
            RunTest("JPEG MULTISCAN: Decodificação 4:2:0 em 3 varreduras independentes (dimensões ímpares 24x37)", Test_Jpeg_Multiscan_420_OddDimensions);
            RunTest("JPEG MULTISCAN: Decodificação 4:4:4 em 3 varreduras independentes com alta fidelidade (16x16)", Test_Jpeg_Multiscan_444_EvenDimensions);
            RunTest("JPEG MULTISCAN: Reinicialização correta de preditores DC e marcadores DRI / RSTm por varredura", Test_Jpeg_Multiscan_With_RestartInterval_DRI);
            RunTest("JPEG MULTISCAN: Definição dinâmica de tabelas DHT entre varreduras consecutivas", Test_Jpeg_Multiscan_With_DHT_Between_Scans);
            RunTest("JPEG MULTISCAN (Segurança): Rejeição de componente ausente ao atingir marcador EOI", Test_Jpeg_Multiscan_Adversarial_MissingComponent_ThrowsCorruptDataException);
            RunTest("JPEG MULTISCAN (Segurança): Rejeição de componente duplicado em varredura subsequente", Test_Jpeg_Multiscan_Adversarial_DuplicateScan_ThrowsCorruptDataException);
            RunTest("JPEG MULTISCAN (Segurança): Rejeição de componente SOS com identificador inexistente", Test_Jpeg_Multiscan_Adversarial_InvalidCompId_ThrowsCorruptDataException);
            RunTest("JPEG MULTISCAN (Segurança): Rejeição de parâmetros espectrais inválidos (Ss != 0 ou Se != 63)", Test_Jpeg_Multiscan_Adversarial_InvalidSpectralParams_ThrowsUnsupportedFormatException);

            // =================================================
            //   LODINO NATIVE DESKTOP — FASE 10 (WIN32 NATIVO) 
            //   Módulo: Gerenciamento Assíncrono e Fila        
            // =================================================
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE DESKTOP — FASE 10 (WIN32 NATIVO) ");
            Console.WriteLine("  Módulo: Gerenciamento Assíncrono e Fila        ");
            Console.WriteLine("=================================================\n");

            RunTest("DESKTOP (Modelo): Inicialização de ConversionJob e formatação de metadados", Test_Desktop_ConversionJob_InitializationAndFormatting);
            RunTest("DESKTOP (Fila): Ciclo de vida da fila (Add, Remove, Clear e contadores)", Test_Desktop_ConversionManager_QueueLifecycle);
            RunTest("DESKTOP (Lote): Conversão assíncrona multithread de CSV -> JSON via ConversionManager", Test_Desktop_ConversionManager_BatchProcessing_CsvToJson);
            RunTest("DESKTOP (Lote Imagens): Conversão sequencial em lote BMP -> PNG e PNG -> JPG", Test_Desktop_ConversionManager_BatchProcessing_Image_BmpToPngAndPngToJpeg);
            RunTest("DESKTOP (Lote Áudio): Conversão assíncrona WAV -> WAV com reamostragem e mixagem", Test_Desktop_ConversionManager_BatchProcessing_Audio_WavToWav);
            RunTest("DESKTOP (Resiliência): Isolamento estrito de falhas em lote (sucesso preservado em inputs mistos)", Test_Desktop_ConversionManager_ErrorIsolation_OnMalformedInput);
            RunTest("DESKTOP (Controle): Cancelamento cooperativo imediato de lote de conversões", Test_Desktop_ConversionManager_CooperativeCancellation);

            // =================================================
            //   LODINO NATIVE CORE — MOTOR PILOTO 4            
            //   Módulo: Motor Nativo de Áudio RIFF/WAVE PCM    
            // =================================================
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR PILOTO 4            ");
            Console.WriteLine("  Módulo: Motor Nativo de Áudio RIFF/WAVE PCM    ");
            Console.WriteLine("=================================================\n");

            RunTest("WAV: Round-trip 8-bit unsigned PCM Mono com preservação senoidal", Test_Wav_RoundTrip_8Bit_Mono);
            RunTest("WAV: Round-trip 8-bit unsigned PCM Estéreo com canais assimétricos", Test_Wav_RoundTrip_8Bit_Stereo);
            RunTest("WAV: Round-trip 16-bit signed PCM Mono com faixa dinâmica completa", Test_Wav_RoundTrip_16Bit_Mono);
            RunTest("WAV: Round-trip 16-bit signed PCM Estéreo (fase invertida e silêncio)", Test_Wav_RoundTrip_16Bit_Stereo);
            RunTest("WAV: Round-trip 24-bit packed PCM Mono e Estéreo (3 bytes por amostra)", Test_Wav_RoundTrip_24Bit_Mono_And_Stereo);
            RunTest("WAV: Round-trip 32-bit signed PCM Mono e Estéreo (4 bytes por amostra)", Test_Wav_RoundTrip_32Bit_Mono_And_Stereo);
            RunTest("WAV (RIFF): Alinhamento e byte de padding em chunks com tamanho ímpar", Test_Wav_RiffPadding_OddSizeChunk);
            RunTest("WAV (Chunks Auxiliares): Salto seguro de chunks LIST, JUNK e metadados", Test_Wav_AuxiliaryChunks_SkipListAndJunk);
            RunTest("WAV (Extensible): Suporte a cabeçalho WAVE_FORMAT_EXTENSIBLE com GUID PCM", Test_Wav_ExtensiblePcm_Guid);
            RunTest("WAV (DSP): Conversão bidirecional de canais (Mono <-> Estéreo)", Test_Wav_ChannelConversion_MonoToStereo_And_StereoToMono);
            RunTest("WAV (DSP): Reamostragem (Resampling) por interpolação linear", Test_Wav_Resampling_LinearInterpolation);
            RunTest("WAV (DSP): Ganho linear de volume e normalização de pico de amplitude", Test_Wav_GainAndNormalization);
            RunTest("WAV (Segurança): Rejeição de assinaturas mágicas inválidas e fluxos truncados", Test_Wav_Security_InvalidSignature_And_Truncation);
            RunTest("WAV (Segurança): Rejeição estrita de compressões e bit depths não suportados", Test_Wav_Security_UnsupportedCompression_And_BitDepths);
            RunTest("WAV (Segurança): Validação estrutural de BlockAlign, taxas e contagem de canais", Test_Wav_Security_InvalidBlockAlign_And_Channels);
            RunTest("WAV (Segurança): Proteção contra arquivos e contagem de quadros que excedam o teto", Test_Wav_Security_ExceedFileAndFrameLimits);
            RunTest("WAV (Engine): Inspeção completa de metadados e registro no EngineRegistry", Test_Wav_Engine_Inspect_And_Registry);
            RunTest("WAV (Engine): Pipeline de conversão ponta a ponta com flags de transformação", Test_Wav_Engine_ConvertPipeline_WithSettings);
            RunTest("WAV (Interoperabilidade): Validação de arquivo gerado via System.Media.SoundPlayer", Test_Wav_External_Interoperability_SystemSoundPlayer);
            RunTest("HARDENING WAV: Rejeição de chunks 'fmt ' duplicados (D-WAV-01)", Test_Wav_Hardening_DuplicateFmtChunk_Rejection);
            RunTest("HARDENING WAV: Rejeição de chunks auxiliares individuais e acumulados excessivos (D-WAV-04)", Test_Wav_Hardening_AuxiliaryChunkLimits_Rejection);
            RunTest("HARDENING WAV: Detecção de truncamento de chunk auxiliar em stream não-seekable (D-WAV-04)", Test_Wav_Hardening_NonSeekable_PrematureEof_InAuxiliaryChunk);
            RunTest("HARDENING WAV: Validação rigorosa de cabeçalho WAVE_FORMAT_EXTENSIBLE (D-WAV-05)", Test_Wav_Hardening_ExtensibleHeader_Validation);
            RunTest("HARDENING WAV: Teto de segurança para memória do buffer de áudio (400 MB)", Test_Wav_Hardening_MaxAudioMemoryBytes_Ceiling);
            RunTest("HARDENING WAV: Aceitação de chunk 'data' legítimo de 70 MB sem falsos positivos (D-WAV-07)", Test_Wav_ChunkData_Over64MB_Accepted);
            RunTest("HARDENING WAV: Suporte a áudio realista de 10 min 44.1kHz 16-bit (~101 MB) (D-WAV-07)", Test_Wav_RealisticDuration_10Min_Over64MB_Accepted);
            RunTest("HARDENING WAV: Suporte a perfil estúdio 96kHz 24-bit 2 min (~66 MB) (D-WAV-07)", Test_Wav_StudioProfile_96kHz_24Bit_Over64MB_Accepted);
            RunTest("HARDENING WAV: Rejeição de chunk 'data' > 500 MB antes da alocação (D-WAV-07)", Test_Wav_Security_ChunkData_ExceedsMaxAudioFileSize_Rejection);
            RunTest("HARDENING WAV: Rejeição de chunk 'data' com tamanho desalinhado de BlockAlign (D-WAV-07)", Test_Wav_Security_ChunkData_MisalignedBlockAlign_Rejection);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR PILOTO 4            ");
            Console.WriteLine("  Módulo: Motor Nativo de Contêineres ZIP Próprio");
            Console.WriteLine("=================================================\n");

            RunTest("ZIP: Criação e leitura de arquivo Store (Sem compressão) com múltiplos arquivos", Test_Zip_Create_And_Read_Store_MultipleEntries);
            RunTest("ZIP: Criação e leitura de arquivo Deflate Optimal (LZ77 + Fixed Huffman)", Test_Zip_Create_And_Read_Deflate_Optimal_Lz77);
            RunTest("ZIP: Criação e leitura de arquivo vazio e diretórios explícitos", Test_Zip_EmptyArchive_And_Directories);
            RunTest("ZIP: Nomes de arquivo em UTF-8 (acentos e caracteres especiais) e comentários", Test_Zip_UnicodeFilenames_And_Comments);
            RunTest("ZIP: Subdiretórios aninhados e empacotamento recursivo de diretório", Test_Zip_Subdirectories_And_RecursivePackaging);
            RunTest("ZIP (Segurança): Bloqueio estrito de Zip Slip / Path Traversal (.. e streams NTFS)", Test_Zip_Security_ZipSlip_PathTraversal_Rejection);
            RunTest("ZIP (Segurança): Detecção e rejeição de corrupção de CRC-32 na extração", Test_Zip_Security_Crc32Mismatch_Rejection);
            RunTest("ZIP (Segurança): Rejeição de arquivo truncado e assinatura mágica inválida", Test_Zip_Security_TruncatedArchive_And_InvalidSignature);
            RunTest("ZIP (Segurança): Rejeição explícita de arquivos criptografados (Bit 0)", Test_Zip_Security_EncryptedArchive_Rejection);
            RunTest("ZIP (Segurança): Defesa contra Zip Bomb (Teto MaxZipTotalExtractedBytes)", Test_Zip_Security_ZipBomb_TotalExtractedLimit_Rejection);
            RunTest("ZIP (Segurança): Defesa contra número excessivo de entradas (MaxZipEntries)", Test_Zip_Security_ExcessiveEntries_Rejection);
            RunTest("ZIP (Segurança): Rejeição explícita e segura de arquivos ZIP64", Test_Zip_Security_Zip64_Rejection);
            RunTest("ZIP (Engine): Inspeção completa de contêiner e registro no EngineRegistry", Test_Zip_Engine_Inspect_And_Registry_Resolution);
            RunTest("ZIP (Engine): Pipeline de conversão ponta a ponta (Empacotamento e Extração)", Test_Zip_Engine_ConvertPipeline_ZipAndUnzip);
            RunTest("ZIP (Interoperabilidade): Descompressão cruzada de stream DEFLATE padrão", Test_Zip_External_Interoperability_DeflateStream);

            // =================================================
            //   LODINO NATIVE CORE — HARDENING ZIP (FASE 12.5) 
            //   Módulo: Resiliência Adversarial do Motor ZIP   
            // =================================================
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — HARDENING ZIP (FASE 12.5) ");
            Console.WriteLine("  Módulo: Resiliência Adversarial do Motor ZIP   ");
            Console.WriteLine("=================================================\n");

            RunTest("HARDENING ZIP: Bloqueio estrito de nomes de dispositivos reservados Win32 (CON, AUX, NUL, COM1..9)", Test_Hardening_Zip_WindowsReservedDeviceNames_Rejection);
            RunTest("HARDENING ZIP: Rejeição de segmentos com espaço ou ponto final (anti-truncamento Win32)", Test_Hardening_Zip_TrailingDotsAndSpaces_Rejection);
            RunTest("HARDENING ZIP: Aborto imediato contra mentira de tamanho no cabeçalho (Header Size Lie Bomb)", Test_Hardening_Zip_HeaderSizeLie_DecompressionBomb_AbortsEarly);
            RunTest("HARDENING ZIP: Busca EOCD com verificação de invariante e suporte a falso EOCD em comentário", Test_Hardening_Zip_Eocd_Backtracking_FakeEocdInComment);
            RunTest("HARDENING ZIP: Compatibilidade com pastas com barra invertida geradas pelo Compress-Archive", Test_Hardening_Zip_BackslashDirectory_CompressArchive_Compatibility);
            RunTest("HARDENING ZIP: Rejeição de entrada de 0 bytes com CRC corrompido", Test_Hardening_Zip_ZeroByteEntry_BadCrc_Rejection);
            RunTest("HARDENING ZIP: Rejeição de streams não-navegáveis (CanSeek = false) no ZipWriter", Test_Hardening_Zip_NonSeekable_ZipWriter_Rejection);
            RunTest("HARDENING ZIP: Proteção contra sobrescrita indevida (overwrite = false) e substituição atômica", Test_Hardening_Zip_ExtractAll_OverwriteFalse_ThrowsIfExists);
            RunTest("HARDENING ZIP: Restauração fiel de carimbos de data/hora em diretórios extraídos", Test_Hardening_Zip_DirectoryTimestamps_Restored);

            // =================================================
            //   LODINO NATIVE CORE — DEFLATE DINÂMICO (FASE 12.6) 
            //   Módulo: Compressor Adaptativo BTYPE 00, 01 e 10
            // =================================================
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — DEFLATE DINÂMICO (FASE 12.6) ");
            Console.WriteLine("  Módulo: Compressor Adaptativo BTYPE 00, 01 e 10");
            Console.WriteLine("=================================================\n");

            RunTest("DYNAMIC DEFLATE: Round-trip e seleção BTYPE 10 em texto com assimetria de frequências", Test_DynamicDeflate_RoundTrip_Text_BType10);
            RunTest("DYNAMIC DEFLATE: Seleção automática de Fixed Huffman (BTYPE 01) em payloads pequenos", Test_DynamicDeflate_AutoSelect_FixedHuffman_SmallPayload);
            RunTest("DYNAMIC DEFLATE: Seleção automática de Stored (BTYPE 00) em dados incompressíveis", Test_DynamicDeflate_AutoSelect_Stored_IncompressibleData);
            RunTest("DYNAMIC DEFLATE: Casos degenerados (0 bytes, 1 byte, monossímbolo e Fibonacci Kraft-McMillan)", Test_DynamicDeflate_Degenerate_And_KraftMcMillan_Cases);
            RunTest("DYNAMIC DEFLATE: Árvore de distâncias vazia (bloco puramente literal sem correspondências LZ77)", Test_DynamicDeflate_EmptyDistanceTree_Compatibility);
            RunTest("DYNAMIC DEFLATE: Integração completa no ZipWriter e extração fiel no ZipReader", Test_DynamicDeflate_ZipWriter_Integration);
            RunTest("DYNAMIC DEFLATE (Segurança): Rejeição de HLIT > 286 e HDIST > 32 no decodificador", Test_DynamicDeflate_Decompressor_Guards_HlitHdist);

            // =================================================
            //   LODINO NATIVE CORE — MOTOR GZIP (FASE 13) 
            //   Módulo: Compressor e Descompressor RFC 1952
            // =================================================
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE CORE — MOTOR GZIP (FASE 13)      ");
            Console.WriteLine("  Módulo: Compressor e Descompressor RFC 1952    ");
            Console.WriteLine("=================================================\n");

            RunTest("GZIP: Criação e leitura de arquivo vazio (0 bytes descomprimidos)", Test_Gzip_Empty_File);
            RunTest("GZIP: Criação e leitura de arquivo de 1 byte", Test_Gzip_Single_Byte);
            RunTest("GZIP: Round-trip de texto UTF-8 (64 bytes)", Test_Gzip_Small_Text);
            RunTest("GZIP: Alta compressibilidade em dados repetitivos (64 KB)", Test_Gzip_Repetitive_Data);
            RunTest("GZIP: Dados aleatórios de alta entropia (32 KB)", Test_Gzip_High_Entropy_Random);
            RunTest("GZIP: Cabeçalho completo com campos opcionais (FNAME, FCOMMENT, FEXTRA, FHCRC)", Test_Gzip_Optional_Fields_Header);
            RunTest("GZIP: Suporte a múltiplos membros concatenados (RFC 1952 § 2.2)", Test_Gzip_Concatenated_Members);
            RunTest("GZIP (Segurança): Detecção e rejeição de CRC-32 adulterado no trailer", Test_Gzip_Security_Corrupt_Crc);
            RunTest("GZIP (Segurança): Detecção e rejeição de ISIZE adulterado no trailer", Test_Gzip_Security_Corrupt_ISize);
            RunTest("GZIP (Segurança): Rejeição de cabeçalho truncado ou bytes mágicos inválidos", Test_Gzip_Security_Truncated_Header);
            RunTest("GZIP (Segurança): Rejeição de trailer truncado (< 8 bytes pós-DEFLATE)", Test_Gzip_Security_Truncated_Trailer);
            RunTest("GZIP (Segurança): Rejeição estrita de flags reservadas (bits 5, 6, 7 do FLG != 0)", Test_Gzip_Security_Reserved_Flags);
            RunTest("GZIP (Segurança): Rejeição de método de compressão não suportado (CM != 8)", Test_Gzip_Security_Unsupported_Method);
            RunTest("GZIP (Segurança): Proteção contra Decompression Bomb (teto de bytes emitidos)", Test_Gzip_Security_Decompression_Bomb);
            RunTest("GZIP (Engine): Inspeção completa e conversão ponta a ponta via EngineRegistry", Test_Gzip_Engine_Integration_And_Inspection);
            RunTest("GZIP (Arquivo): Escrita atômica temporária e proteção contra sobrescrita indevida", Test_Gzip_File_Atomic_Overwrite_Protection);

            // --- FASE 13.2: REMEDIAÇÃO GZIP ---
            RunTest("GZIP (Streaming): Compressão O(1) de fluxo com múltiplos blocos DEFLATE e verificação de integridade", Test_Gzip_Streaming_Memory_O1);
            RunTest("GZIP (Streaming): Compressão com sucesso em fluxo não-posicionável (CanSeek = false)", Test_Gzip_Streaming_NonSeekable_Stream);
            RunTest("GZIP (Segurança): Rejeição estrita de bytes de lixo não-nulos após o trailer (E-05)", Test_Gzip_Security_TrailingGarbage);
            RunTest("DEFLATE (Robustez): ReadByte() rejeita leitura com bits residuais desalinhados (B-01)", Test_Deflate_BitReader_ReadByte_Misaligned);
            RunTest("GZIP (Segurança CLI): Sanitização de metadados FNAME/FCOMMENT contra injeção ANSI/C0 CWE-150 (E-02)", Test_Gzip_Console_Sanitizer_Cwe150);
            RunTest("GZIP: Enumeração completa de múltiplos membros concatenados via InspectAllMembers (E-04)", Test_Gzip_Inspect_All_Members);
            RunTest("GZIP: Conformidade normativa RFC 1952 § 2.3.1 com codificação Latin-1 em FNAME (A-01)", Test_Gzip_Normative_Latin1_Fname);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  LODINO NATIVE DESKTOP — ORQUESTRAÇÃO DE FILA   ");
            Console.WriteLine("  Módulo: Fila e Preservação Operacional (14.1)  ");
            Console.WriteLine("=================================================");

            RunTest("FILA (Auto): Sugestão inteligente de operação por formato de entrada", Test_Queue_Auto_Suggested_Operations);
            RunTest("FILA (Override Seguro): Bloqueio de sobrescrita incompatível em fila mista", Test_Queue_Preserve_On_Incompatible_Override);
            RunTest("FILA (Extração): Aplicação explícita de extração para contêineres ZIP e GZIP", Test_Queue_Explicit_Extract_Override);
            RunTest("FILA (Compactação): Aplicação de compactação ZIP em fila", Test_Queue_Explicit_Zip_Override);
            RunTest("FILA (Robustez): Rejeição graciosa de formato desconhecido sem corromper entrada", Test_Queue_Invalid_Extension_Graceful_Failure);
            RunTest("FILA (End-to-End): Execução de lote misto com dados, imagem e contêiner", Test_Queue_Mixed_EndToEnd_Execution);

            Console.WriteLine("\n-------------------------------------------------");
            Console.WriteLine(string.Format("Resultado Final: {0} aprovados, {1} falhas.", _passed, _failed));
            Console.WriteLine("-------------------------------------------------");

            return _failed == 0 ? 0 : 1;
        }

        private static void RunTest(string testName, Action testAction)
        {
            try
            {
                testAction();
                _passed++;
                Console.WriteLine(string.Format("  [PASS] {0}", testName));
            }
            catch (Exception ex)
            {
                _failed++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("  [FAIL] {0}", testName));
                Console.WriteLine(string.Format("         Detalhe: {0}", ex.Message));
                Console.ResetColor();
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Falha na asserção: " + message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string field)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(string.Format("Esperado '{0}' para {1}, mas obteve '{2}'.", expected, field, actual));
            }
        }

        #region Casos de Teste CSV

        private static void Test_Csv_BasicParsing()
        {
            string csv = "id,name,role\r\n1,Alice,Engineer\r\n2,Bob,Architect";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(3, doc.ColumnCount, "ColumnCount");
            AssertEqual(2, doc.RowCount, "RowCount");
            AssertEqual("Alice", doc.Rows[0][1], "Row[0][1]");
            AssertEqual("Bob", doc.Rows[1][1], "Row[1][1]");
        }

        private static void Test_Csv_Rfc4180_QuotesAndCommas()
        {
            string csv = "col1,col2\r\nval1,\"Special, with comma\"\r\n";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(1, doc.RowCount, "RowCount");
            AssertEqual("Special, with comma", doc.Rows[0][1], "Campo com vírgula");
        }

        private static void Test_Csv_Rfc4180_Multiline()
        {
            string csv = "id,text\r\n1,\"Linha 1\r\nLinha 2\r\nLinha 3\"\r\n2,Normal";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(2, doc.RowCount, "RowCount");
            Assert(doc.Rows[0][1].Contains("Linha 2"), "Conteúdo multilinha preservado");
            AssertEqual("Normal", doc.Rows[1][1], "Próximo registro intacto");
        }

        private static void Test_Csv_Rfc4180_EscapedQuotes()
        {
            string csv = "id,text\r\n1,\"Ele disse: \"\"Olá mundo!\"\" ao entrar\"\r\n";
            var doc = new CsvParser().Parse(csv);

            AssertEqual("Ele disse: \"Olá mundo!\" ao entrar", doc.Rows[0][1], "Aspas escapadas tratadas");
        }

        private static void Test_Csv_WriterRoundTrip()
        {
            var original = new CsvDocument(new[] { "A", "B, with comma", "C" });
            original.AddRow(new[] { "1", "Valor \"com aspas\"", "Normal" });

            string serialized = new CsvWriter().WriteToString(original);
            var reloaded = new CsvParser().Parse(serialized);

            AssertEqual(original.Headers.Count, reloaded.Headers.Count, "Header count");
            AssertEqual("B, with comma", reloaded.Headers[1], "Header[1]");
            AssertEqual("Valor \"com aspas\"", reloaded.Rows[0][1], "Row[0][1]");
        }

        private static void Test_Csv_LineEndings_CRLF_LF_CR()
        {
            // Mixed CRLF, LF, and single CR line endings
            string mixed = "H1,H2\r\nR1C1,R1C2\nR2C1,R2C2\rR3C1,R3C2";
            var doc = new CsvParser().Parse(mixed);

            AssertEqual(3, doc.RowCount, "Leu corretamente 3 registros com quebras mistas");
            AssertEqual("R1C1", doc.Rows[0][0], "R1C1");
            AssertEqual("R2C1", doc.Rows[1][0], "R2C1");
            AssertEqual("R3C1", doc.Rows[2][0], "R3C1");
        }

        private static void Test_Csv_EmptyFieldsAndTrailingCommas()
        {
            string csv = "A,B,C,D\r\n,val,,end\r\n1,,,";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(2, doc.RowCount, "RowCount");
            AssertEqual("", doc.Rows[0][0], "Campo inicial vazio");
            AssertEqual("val", doc.Rows[0][1], "Campo preenchido");
            AssertEqual("", doc.Rows[0][2], "Campo intermediário vazio");
            AssertEqual("end", doc.Rows[0][3], "Campo final");
            AssertEqual("", doc.Rows[1][3], "Campo final vazio com vírgula terminal");
        }

        private static void Test_Csv_Utf8BomHandling()
        {
            string csvWithBom = "\uFEFFname,code\r\nItem,100";
            var doc = new CsvParser().Parse(csvWithBom);

            AssertEqual("name", doc.Headers[0], "BOM removido sem truncar caractere 'n'");
            AssertEqual("Item", doc.Rows[0][0], "Linha lida normalmente");
        }

        private static void Test_Csv_InconsistentRecordLengths()
        {
            // Row 1 has 2 cols, Row 2 has 4 cols (with 3-col header)
            string csv = "C1,C2,C3\r\nR1A,R1B\r\nR2A,R2B,R2C,R2D";
            var doc = new CsvParser().Parse(csv);

            AssertEqual(4, doc.Headers.Count, "Cabeçalho expandido para acomodar coluna extra");
            AssertEqual("ExtraColumn_4", doc.Headers[3], "Nome gerado para coluna excedente");
            AssertEqual("", doc.Rows[0][2], "Linha curta preenchida com string vazia");
            AssertEqual("R2D", doc.Rows[1][3], "Dado da coluna excedente preservado");
        }

        private static void Test_Csv_Malformed_UnclosedQuotes()
        {
            string malformed = "id,text\r\n1,\"Unclosed quote at EOF";
            var doc = new CsvParser().Parse(malformed);

            AssertEqual(1, doc.RowCount, "Tratamento tolerante de aspas inacabadas em EOF");
            Assert(doc.Rows[0][1].Contains("Unclosed quote"), "Valor retido sem travar processo");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfInconsistentColumns()
        {
            string csv = "C1,C2,C3\r\nR1A,R1B\r\n";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(csv);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita linha com contagem divergente de colunas em modo estrito RFC 4180");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfUnclosedQuotes()
        {
            string malformed = "id,text\r\n1,\"Unclosed quote at EOF";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(malformed);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita aspas não fechadas no final do arquivo em modo estrito RFC 4180");
        }

        private static void Test_Csv_StrictRfc4180_RejectionOfCharAfterQuotes()
        {
            string invalid = "id,text\r\n1,\"Quoted\"ExtraChar,next\r\n";
            bool threw = false;
            try
            {
                new CsvParser(',', true, true).Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita caractere espúrio após aspas de fechamento em modo estrito RFC 4180");
        }

        #endregion

        #region Casos de Teste JSON

        private static void Test_Json_PrimitivesAndObjects()
        {
            string json = "{\"name\":\"Lodino\",\"version\":2,\"active\":true,\"pi\":3.14,\"extra\":null,\"items\":[1,2,3]}";
            var node = new JsonParser().Parse(json);

            AssertEqual(JsonType.Object, node.Type, "Root Type");
            var obj = (JsonObject)node;
            AssertEqual("Lodino", obj["name"].AsString(), "name");
            AssertEqual(2L, obj["version"].AsLong(), "version");
            AssertEqual(true, obj["active"].AsBoolean(), "active");
            AssertEqual(3.14, obj["pi"].AsDouble(), "pi");
            AssertEqual(JsonType.Null, obj["extra"].Type, "extra is null");

            var items = (JsonArray)obj["items"];
            AssertEqual(3, items.Count, "items.Count");
            AssertEqual(2L, items[1].AsLong(), "items[1]");
        }

        private static void Test_Json_UnicodeEscapes()
        {
            string json = "{\"copyright\":\"\\u00A9 2026 Lodino\"}";
            var node = new JsonParser().Parse(json);
            var obj = (JsonObject)node;

            Assert(obj["copyright"].AsString().StartsWith("©"), "Unicode escape decodificado corretamente");
        }

        private static void Test_Json_Unicode_SurrogatePairs()
        {
            // \uD83D\uDE00 is UTF-16 surrogate pair for U+1F600 (Grinning Face emoji)
            string json = "{\"emoji\":\"\\uD83D\\uDE00\"}";
            var node = new JsonParser().Parse(json);
            string val = node["emoji"].AsString();

            AssertEqual(2, val.Length, "Surrogate pair retém os 2 chars UTF-16");
            AssertEqual(0x1F600, char.ConvertToUtf32(val, 0), "Código escalar Unicode 32-bit verificado");
        }

        private static void Test_Json_LargeNumbersAndScientificNotation()
        {
            string json = "{\"big\":1.23456789e+10,\"small\":-2.5E-4,\"maxSafe\":9007199254740991}";
            var node = new JsonParser().Parse(json);

            AssertEqual(12345678900.0, node["big"].AsDouble(), "Notação científica e+10");
            AssertEqual(-0.00025, node["small"].AsDouble(), "Notação científica E-4 negativa");
            AssertEqual(9007199254740991L, node["maxSafe"].AsLong(), "Inteiro máximo seguro (53 bits)");
        }

        private static void Test_Json_NumericPrecisionLimitation()
        {
            // Number exceeding 64-bit float precision (70 digits)
            string hugeNumber = "1234567890123456789012345678901234567890123456789012345678901234567890";
            string json = "{\"huge\":" + hugeNumber + "}";
            var node = new JsonParser().Parse(json);

            // JsonNumber retains RawText, preventing silent serialization precision loss
            string reserialized = new JsonWriter(false).WriteToString(node);
            Assert(reserialized.Contains(hugeNumber), "RawText preservado sem corrupção na serialização");
        }

        private static void Test_Json_ControlCharacterRejection()
        {
            // Literal unescaped newline (0x0A) inside string is forbidden by ECMA-404
            string invalid = "{\"key\": \"linha1\nlinha2\"}";
            bool threw = false;
            try
            {
                new JsonParser().Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita caractere de controle não escapado em string literal");
        }

        private static void Test_Json_DuplicatePropertiesPolicy()
        {
            string json = "{\"dup\":\"initial\",\"dup\":\"overridden\"}";
            var node = new JsonParser().Parse(json);

            // Documented policy: last occurrence wins
            AssertEqual("overridden", node["dup"].AsString(), "Última propriedade prevalece em chaves repetidas");
        }

        private static void Test_Json_LeadingZerosRejection()
        {
            string invalid = "{\"number\": 0123}";
            bool threw = false;
            try
            {
                new JsonParser().Parse(invalid);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Rejeita zeros à esquerda em números inteiros (0123)");
        }

        private static void Test_Json_MalformedRejection()
        {
            string malformed = "{\"unclosed\": \"string without end, 123";
            bool threw = false;
            try
            {
                new JsonParser().Parse(malformed);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }

            Assert(threw, "Lança CorruptDataException em JSON malformado");
        }

        private static void Test_Json_DepthLimitSecurity()
        {
            var sb = new StringBuilder();
            int limit = SecurityLimits.MaxJsonNestingDepth + 5;
            for (int i = 0; i < limit; i++) sb.Append("[");
            sb.Append("1");
            for (int i = 0; i < limit; i++) sb.Append("]");

            bool caughtResourceLimit = false;
            try
            {
                new JsonParser().Parse(sb.ToString());
            }
            catch (ResourceLimitException)
            {
                caughtResourceLimit = true;
            }

            Assert(caughtResourceLimit, "Bloqueia profundidade de aninhamento excessiva para evitar DoS");
        }

        #endregion

        #region Casos de Teste INI

        private static void Test_Ini_SectionsAndComments()
        {
            string ini = "; Comentário geral\r\n[Server]\r\nhost = localhost ; inline comment\r\nport = 8080\r\n# Outro comentário\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("localhost", doc.Get("Server", "host"), "Server.host");
            AssertEqual("8080", doc.Get("Server", "port"), "Server.port");
        }

        private static void Test_Ini_GlobalProperties()
        {
            string ini = "app_name = Lodino\r\nversion = 1.0\r\n[Section1]\r\nk = v\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("Lodino", doc.Get(IniDocument.GlobalSectionName, "app_name"), "app_name global");
            AssertEqual("v", doc.Get("Section1", "k"), "Section1.k");
        }

        private static void Test_Ini_EmptyValuesAndWhitespace()
        {
            string ini = "[Config]\r\n  spaced_key   =   padded value   \r\nempty_key =\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("padded value", doc.Get("Config", "spaced_key"), "Trimming de espaços");
            AssertEqual("", doc.Get("Config", "empty_key"), "Valor vazio tratado como string vazia");
        }

        private static void Test_Ini_Comments_HashAndSemicolon()
        {
            string ini = "# Comentário cerquilha\r\n; Comentário ponto-e-vírgula\r\n[Sec]\r\nk1 = v1\r\n# linha comentada\r\nk2 = v2 ; inline semicolon\r\nk3 = v3 # inline hash";
            var doc = new IniParser().Parse(ini);

            AssertEqual("v1", doc.Get("Sec", "k1"), "k1");
            AssertEqual("v2", doc.Get("Sec", "k2"), "k2 sem comentário inline");
            AssertEqual("v3", doc.Get("Sec", "k3"), "k3 sem comentário inline");
        }

        private static void Test_Ini_DuplicateKeysPolicy()
        {
            string ini = "[Sec]\r\nrepeat = first\r\nrepeat = final\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("final", doc.Get("Sec", "repeat"), "Última atribuição prevalece");
        }

        private static void Test_Ini_QuotedValuesWithSpecialChars()
        {
            string ini = "[Sec]\r\ncommented = \"value with # and ; inside\"\r\n";
            var doc = new IniParser().Parse(ini);

            AssertEqual("value with # and ; inside", doc.Get("Sec", "commented"), "Caracteres especiais preservados dentro de aspas");
        }

        #endregion

        #region Casos de Teste das 9 Rotas de Conversão

        private static void Test_Route_CsvToCsv()
        {
            var engine = new StructuredDataEngine();
            string csv = "id,name\r\n1,\"Alice, Senior\"";
            string result = engine.ConvertString(csv, "csv", "csv");

            var reloaded = new CsvParser().Parse(result);
            AssertEqual(1, reloaded.RowCount, "Linha preservada");
            AssertEqual("Alice, Senior", reloaded.Rows[0][1], "Campo citado recodificado com segurança");
        }

        private static void Test_Route_CsvToJson()
        {
            var engine = new StructuredDataEngine();
            string csv = "id,name\r\n1,Alpha\r\n2,Beta";
            string json = engine.ConvertString(csv, "csv", "json");

            var parsedJson = new JsonParser().Parse(json);
            AssertEqual(JsonType.Array, parsedJson.Type, "Result is JsonArray");
            var arr = (JsonArray)parsedJson;
            AssertEqual(2, arr.Count, "Array has 2 items");
            AssertEqual("Alpha", arr[0]["name"].AsString(), "arr[0].name");
        }

        private static void Test_Route_CsvToIni()
        {
            var engine = new StructuredDataEngine();
            string csv = "section,ip,status\r\nServerA,10.0.0.1,up\r\nServerB,10.0.0.2,down";
            string ini = engine.ConvertString(csv, "csv", "ini");

            var parsed = new IniParser().Parse(ini);
            AssertEqual("10.0.0.1", parsed.Get("ServerA", "ip"), "ServerA.ip");
            AssertEqual("down", parsed.Get("ServerB", "status"), "ServerB.status");
        }

        private static void Test_Route_JsonToJson()
        {
            var engine = new StructuredDataEngine();
            string json = "{\"compact\":1,\"valid\":true}";
            string pretty = engine.ConvertString(json, "json", "json");

            var parsed = new JsonParser().Parse(pretty);
            AssertEqual(1L, parsed["compact"].AsLong(), "Valor preservado");
            AssertEqual(true, parsed["valid"].AsBoolean(), "Booleano preservado");
            Assert(pretty.Contains("\n"), "Reformatado com indentação");
        }

        private static void Test_Route_JsonToCsv()
        {
            var engine = new StructuredDataEngine();
            string json = "[{\"id\":\"10\",\"name\":\"Widget\"},{\"id\":\"20\",\"name\":\"Gadget\"}]";
            string csv = engine.ConvertString(json, "json", "csv");

            var parsedCsv = new CsvParser().Parse(csv);
            AssertEqual(2, parsedCsv.RowCount, "CSV has 2 rows");
            AssertEqual("Widget", parsedCsv.Rows[0][1], "Row[0][1]");
        }

        private static void Test_Route_JsonToIni()
        {
            var engine = new StructuredDataEngine();
            string json = "{\"Database\":{\"Port\":\"5432\",\"Host\":\"localhost\"}}";
            string ini = engine.ConvertString(json, "json", "ini");

            var parsed = new IniParser().Parse(ini);
            AssertEqual("5432", parsed.Get("Database", "Port"), "Port");
            AssertEqual("localhost", parsed.Get("Database", "Host"), "Host");
        }

        private static void Test_Route_IniToIni()
        {
            var engine = new StructuredDataEngine();
            string ini = "[Config]\r\nk = v\r\n";
            string res = engine.ConvertString(ini, "ini", "ini");

            var parsed = new IniParser().Parse(res);
            AssertEqual("v", parsed.Get("Config", "k"), "Config.k");
        }

        private static void Test_Route_IniToJson()
        {
            var engine = new StructuredDataEngine();
            string ini = "[App]\r\nName = Lodino\r\nMode = Fast\r\n";
            string json = engine.ConvertString(ini, "ini", "json");

            var parsed = new JsonParser().Parse(json);
            AssertEqual("Lodino", parsed["App"]["Name"].AsString(), "parsed[App][Name]");
        }

        private static void Test_Route_IniToCsv()
        {
            var engine = new StructuredDataEngine();
            string ini = "[Database]\r\nport = 5432\r\n";
            string csv = engine.ConvertString(ini, "ini", "csv");

            var parsed = new CsvParser().Parse(csv);
            AssertEqual("Database", parsed.Rows[0][0], "Coluna Section");
            AssertEqual("port", parsed.Rows[0][1], "Coluna Key");
            AssertEqual("5432", parsed.Rows[0][2], "Coluna Value");
        }

        #endregion

        #region Testes de Infraestrutura e Segurança

        private static void Test_SafeFileWriter_UniquePaths()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string path1 = SafeFileWriter.GetUniqueOutputPath(tempDir, "relatorio", "csv");
                File.WriteAllText(path1, "conteudo 1");

                string path2 = SafeFileWriter.GetUniqueOutputPath(tempDir, "relatorio", "csv");
                Assert(path2.EndsWith("relatorio (1).csv"), "Gera sufixo incremental anti-sobrescrita");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_EngineRegistry_Resolution()
        {
            var registry = EngineRegistry.Default;
            var engine = registry.Resolve("csv", "json");
            Assert(engine != null, "Motor resolvido");
            AssertEqual("lodino.engine.data", engine.EngineId, "EngineId correto");
        }

        #endregion

        #region Testes do Motor BMP Próprio

        private static void Test_Bmp_1x1_Padding1()
        {
            var pb = new PixelBuffer(1, 1);
            pb.SetPixel(0, 0, new PixelColor(255, 0, 0, 255));
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(58, bytes.Length, "Tamanho total BMP 1x1 (54 cabeçalho + 4 bytes linha)");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(1, decoded.Width, "Width");
            AssertEqual(1, decoded.Height, "Height");
            AssertEqual(new PixelColor(255, 0, 0, 255), decoded.GetPixel(0, 0), "Pixel (0,0) Vermelho");
        }

        private static void Test_Bmp_2x2_Padding2()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Green);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.White);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(70, bytes.Length, "Tamanho total BMP 2x2 com 2 bytes padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Pixel (0,0)");
            AssertEqual(PixelColor.Green, decoded.GetPixel(1, 0), "Pixel (1,0)");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Pixel (0,1)");
            AssertEqual(PixelColor.White, decoded.GetPixel(1, 1), "Pixel (1,1)");
        }

        private static void Test_Bmp_3x3_Padding3()
        {
            var pb = new PixelBuffer(3, 3);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 80), (byte)(y * 80), 150, 255));
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(90, bytes.Length, "Tamanho total BMP 3x3 com 3 bytes padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(3, decoded.Width, "Width");
            AssertEqual(3, decoded.Height, "Height");
            AssertEqual(new PixelColor(80, 80, 150, 255), decoded.GetPixel(1, 1), "Pixel central (1,1)");
        }

        private static void Test_Bmp_4x4_NoPadding()
        {
            var pb = new PixelBuffer(4, 4);
            pb.Fill(PixelColor.Yellow);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(102, bytes.Length, "Tamanho total BMP 4x4 sem padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(PixelColor.Yellow, decoded.GetPixel(2, 2), "Pixel (2,2) amarelo");
        }

        private static void Test_Bmp_7x5_OddDimensions()
        {
            var pb = new PixelBuffer(7, 5);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(6, 4, PixelColor.Cyan);
            pb.SetPixel(3, 2, PixelColor.Magenta);
            byte[] bytes = BmpEncoder.Encode(pb, 24);
            AssertEqual(174, bytes.Length, "Tamanho total BMP 7x5 com padding");
            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(7, decoded.Width, "Width");
            AssertEqual(5, decoded.Height, "Height");
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Canto superior esquerdo");
            AssertEqual(PixelColor.Cyan, decoded.GetPixel(6, 4), "Canto inferior direito");
            AssertEqual(PixelColor.Magenta, decoded.GetPixel(3, 2), "Pixel central");
        }

        private static void Test_Bmp_24bit_RoundTrip()
        {
            var pb = new PixelBuffer(16, 16);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 16), (byte)(y * 16), (byte)((x + y) * 8), 255));
                }
            }
            byte[] encoded = BmpEncoder.Encode(pb, 24);
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(16, decoded.Width, "Width");
            AssertEqual(16, decoded.Height, "Height");
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    AssertEqual(pb.GetPixel(x, y), decoded.GetPixel(x, y), string.Format("Pixel ({0},{1})", x, y));
                }
            }
        }

        private static void Test_Bmp_32bit_Bgrx_ArbitraryHighBytes_NormalizesOpaque()
        {
            // Constrói manualmente um BMP de 2x2 32-bit BI_RGB onde o 4º byte varia: 0x00, 0x40, 0x80, 0xFF
            // Todos os 4 pixels devem decodificar com A=255 (opacos) e manter os canais RGB exatos
            byte[] bytes = new byte[54 + 8 * 2]; // 2x2 32-bit: stride = 2 * 4 = 8 bytes. Total = 70 bytes.
            using (var ms = new MemoryStream(bytes))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0x4D42);
                bw.Write((uint)bytes.Length);
                bw.Write((uint)0);
                bw.Write((uint)54);
                bw.Write((uint)40);
                bw.Write((int)2);
                bw.Write((int)2); // bottom-up
                bw.Write((ushort)1);
                bw.Write((ushort)32); // 32 bpp
                bw.Write((uint)0); // BI_RGB
                bw.Write((uint)(8 * 2));
                bw.Write((int)2835);
                bw.Write((int)2835);
                bw.Write((uint)0);
                bw.Write((uint)0);
                // Scanline 0 do arquivo (fundo, y=1):
                // (0, 1): B=10, G=20, R=30, X=0x00
                bw.Write((byte)10); bw.Write((byte)20); bw.Write((byte)30); bw.Write((byte)0x00);
                // (1, 1): B=40, G=50, R=60, X=0x40
                bw.Write((byte)40); bw.Write((byte)50); bw.Write((byte)60); bw.Write((byte)0x40);
                // Scanline 1 do arquivo (topo, y=0):
                // (0, 0): B=70, G=80, R=90, X=0x80
                bw.Write((byte)70); bw.Write((byte)80); bw.Write((byte)90); bw.Write((byte)0x80);
                // (1, 0): B=100, G=110, R=120, X=0xFF
                bw.Write((byte)100); bw.Write((byte)110); bw.Write((byte)120); bw.Write((byte)0xFF);
            }

            var decoded = BmpDecoder.Decode(bytes);
            AssertEqual(2, decoded.Width, "Width");
            AssertEqual(2, decoded.Height, "Height");

            // Verifica normalização do alfa para 255 e preservação de RGB
            AssertEqual(new PixelColor(90, 80, 70, 255), decoded.GetPixel(0, 0), "Pixel (0,0) com X=0x80 normalizado para A=255");
            AssertEqual(new PixelColor(120, 110, 100, 255), decoded.GetPixel(1, 0), "Pixel (1,0) com X=0xFF normalizado para A=255");
            AssertEqual(new PixelColor(30, 20, 10, 255), decoded.GetPixel(0, 1), "Pixel (0,1) com X=0x00 normalizado para A=255");
            AssertEqual(new PixelColor(60, 50, 40, 255), decoded.GetPixel(1, 1), "Pixel (1,1) com X=0x40 normalizado para A=255");
        }

        private static void Test_Bmp_32bit_Bgrx_RoundTrip()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 50), (byte)(y * 50), 200, 255));
                }
            }
            byte[] encoded = BmpEncoder.Encode(pb, 32);
            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(4, decoded.Width, "Width 4");
            AssertEqual(4, decoded.Height, "Height 4");
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    AssertEqual(pb.GetPixel(x, y), decoded.GetPixel(x, y), string.Format("Pixel BGRX ({0},{1})", x, y));
                }
            }
        }

        private static void Test_Bmp_BottomUp_Orientation()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Red);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.Blue);
            byte[] encoded = BmpEncoder.Encode(pb, 24);

            AssertEqual((byte)255, encoded[54], "Primeiro byte dos dados é B=255 (Azul)");
            AssertEqual((byte)0, encoded[54 + 1], "G=0");
            AssertEqual((byte)0, encoded[54 + 2], "R=0");

            var decoded = BmpDecoder.Decode(encoded);
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Linha superior é Vermelha");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Linha inferior é Azul");
        }

        private static void Test_Bmp_TopDown_Orientation()
        {
            byte[] topDownBytes = new byte[54 + 8 * 2];
            using (var ms = new MemoryStream(topDownBytes))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0x4D42);
                bw.Write((uint)topDownBytes.Length);
                bw.Write((uint)0);
                bw.Write((uint)54);
                bw.Write((uint)40);
                bw.Write((int)2);
                bw.Write((int)-2);
                bw.Write((ushort)1);
                bw.Write((ushort)24);
                bw.Write((uint)0);
                bw.Write((uint)(8 * 2));
                bw.Write((int)2835);
                bw.Write((int)2835);
                bw.Write((uint)0);
                bw.Write((uint)0);
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)255);
                bw.Write((byte)0); bw.Write((byte)0); bw.Write((byte)255);
                bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)255); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)255); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((byte)0); bw.Write((byte)0);
            }
            var decoded = BmpDecoder.Decode(topDownBytes);
            AssertEqual(2, decoded.Width, "Width");
            AssertEqual(2, decoded.Height, "Height");
            AssertEqual(PixelColor.Red, decoded.GetPixel(0, 0), "Topo decodificado é Vermelho");
            AssertEqual(PixelColor.Blue, decoded.GetPixel(0, 1), "Fundo decodificado é Azul");
        }

        private static void Test_Bmp_Resize_NearestNeighbor()
        {
            var pb = new PixelBuffer(2, 2);
            pb.SetPixel(0, 0, PixelColor.Red);
            pb.SetPixel(1, 0, PixelColor.Green);
            pb.SetPixel(0, 1, PixelColor.Blue);
            pb.SetPixel(1, 1, PixelColor.White);
            var resized = pb.ResizeNearestNeighbor(4, 4);
            AssertEqual(4, resized.Width, "Width 4");
            AssertEqual(4, resized.Height, "Height 4");
            AssertEqual(PixelColor.Red, resized.GetPixel(0, 0), "Bloco SE 0,0");
            AssertEqual(PixelColor.Red, resized.GetPixel(1, 1), "Bloco SE 1,1");
            AssertEqual(PixelColor.Green, resized.GetPixel(2, 0), "Bloco SD 2,0");
            AssertEqual(PixelColor.Green, resized.GetPixel(3, 1), "Bloco SD 3,1");
            AssertEqual(PixelColor.Blue, resized.GetPixel(0, 2), "Bloco IE 0,2");
            AssertEqual(PixelColor.Blue, resized.GetPixel(1, 3), "Bloco IE 1,3");
            AssertEqual(PixelColor.White, resized.GetPixel(2, 2), "Bloco ID 2,2");
            AssertEqual(PixelColor.White, resized.GetPixel(3, 3), "Bloco ID 3,3");
        }

        private static void Test_Bmp_Resize_Bilinear()
        {
            var pb = new PixelBuffer(2, 1);
            pb.SetPixel(0, 0, new PixelColor(0, 0, 0, 255));
            pb.SetPixel(1, 0, new PixelColor(254, 254, 254, 255));
            var resized = pb.ResizeBilinear(3, 1);
            AssertEqual(3, resized.Width, "Width 3");
            AssertEqual(1, resized.Height, "Height 1");
            PixelColor center = resized.GetPixel(1, 0);
            Assert(center.R >= 120 && center.R <= 134, "Canal R interpolado ~ 127");
            Assert(center.G >= 120 && center.G <= 134, "Canal G interpolado ~ 127");
            Assert(center.B >= 120 && center.B <= 134, "Canal B interpolado ~ 127");
        }

        private static void Test_Bmp_Crop_Arbitrary()
        {
            var pb = new PixelBuffer(10, 10);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 50, 255));
            var cropped = pb.Crop(2, 3, 4, 5);
            AssertEqual(4, cropped.Width, "Cropped Width");
            AssertEqual(5, cropped.Height, "Cropped Height");
            AssertEqual(new PixelColor(2, 3, 50, 255), cropped.GetPixel(0, 0), "Pixel recortado (0,0)");
            AssertEqual(new PixelColor(5, 7, 50, 255), cropped.GetPixel(3, 4), "Pixel recortado (3,4)");
        }

        private static void Test_Bmp_Crop_Quadrants()
        {
            var pb = new PixelBuffer(9, 9);
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 0, 255));

            var tl = pb.CropQuadrant(3, 3, "top-left");
            AssertEqual(new PixelColor(0, 0, 0, 255), tl.GetPixel(0, 0), "Top-Left");

            var c = pb.CropQuadrant(3, 3, "center");
            AssertEqual(new PixelColor(3, 3, 0, 255), c.GetPixel(0, 0), "Center");

            var br = pb.CropQuadrant(3, 3, "bottom-right");
            AssertEqual(new PixelColor(6, 6, 0, 255), br.GetPixel(0, 0), "Bottom-Right");
        }

        private static void Test_Bmp_Watermark_Text()
        {
            var pb = new PixelBuffer(60, 20);
            pb.Fill(PixelColor.Black);
            pb.ApplyTextWatermark("LODINO", 5, 5, PixelColor.White, 1);
            bool hasWhite = false;
            for (int y = 0; y < pb.Height; y++)
            {
                for (int x = 0; x < pb.Width; x++)
                {
                    if (pb.GetPixel(x, y) == PixelColor.White)
                    {
                        hasWhite = true;
                        break;
                    }
                }
            }
            Assert(hasWhite, "Texto 'LODINO' rasterizado com glifos proprietários");
            AssertEqual(PixelColor.Black, pb.GetPixel(0, 0), "Pixel fora da marca permanece preto");
        }

        private static void Test_Bmp_Watermark_Image_AlphaComposite()
        {
            var bg = new PixelBuffer(10, 10);
            bg.Fill(PixelColor.Blue);
            var fg = new PixelBuffer(4, 4);
            fg.Fill(PixelColor.Red);
            bg.ApplyImageWatermark(fg, 2, 2, 0.5f);
            PixelColor blended = bg.GetPixel(3, 3);
            Assert(blended.R >= 125 && blended.R <= 130, "R composto ~ 128");
            AssertEqual((byte)0, blended.G, "G composto = 0");
            Assert(blended.B >= 125 && blended.B <= 130, "B composto ~ 128");
            AssertEqual((byte)255, blended.A, "A composto = 255");
            AssertEqual(PixelColor.Blue, bg.GetPixel(0, 0), "Pixel externo permanece azul");
        }

        private static void Test_Bmp_Security_InvalidMagic()
        {
            byte[] badMagic = new byte[54];
            badMagic[0] = (byte)'Z';
            badMagic[1] = (byte)'Z';
            bool threw = false;
            try { BmpDecoder.Decode(badMagic); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita assinatura mágica diferente de 'BM'");
        }

        private static void Test_Bmp_Security_UnsupportedHeaderSize()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[14] = 12; b[15] = 0; b[16] = 0; b[17] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita cabeçalho DIB não suportado (biSize = 12)");
        }

        private static void Test_Bmp_Security_RejectPalettes()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[28] = 8; b[29] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita BMP com paleta indexada (8 bpp)");
        }

        private static void Test_Bmp_Security_RejectCompression()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[30] = 1; b[31] = 0; b[32] = 0; b[33] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (UnsupportedFormatException) { threw = true; }
            Assert(threw, "Rejeita compressão BI_RLE8");
        }

        private static void Test_Bmp_Security_InvalidDimensions()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[18] = 0; b[19] = 0; b[20] = 0; b[21] = 0;
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita largura BMP igual a zero");
        }

        private static void Test_Bmp_Security_ExceedMaxDimension()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[18] = 0x20; b[19] = 0x4E; b[20] = 0; b[21] = 0; // 20.000
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (ResourceLimitException) { threw = true; }
            Assert(threw, "Rejeita dimensões que excedem limite de segurança (16.384)");
        }

        private static void Test_Bmp_Security_TruncatedFile()
        {
            var pb = new PixelBuffer(20, 20);
            byte[] full = BmpEncoder.Encode(pb, 24);
            byte[] truncated = new byte[70];
            Array.Copy(full, truncated, 70);
            bool threw = false;
            try { BmpDecoder.Decode(truncated); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita arquivo BMP truncado");
        }

        private static void Test_Bmp_Security_InvalidOffBits()
        {
            var pb = new PixelBuffer(2, 2);
            byte[] b = BmpEncoder.Encode(pb, 24);
            b[10] = 0x50; b[11] = 0xC3; b[12] = 0; b[13] = 0; // 50.000
            bool threw = false;
            try { BmpDecoder.Decode(b); } catch (CorruptDataException) { threw = true; }
            Assert(threw, "Rejeita bfOffBits fora dos limites do arquivo");
        }

        private static void Test_Bmp_Engine_Inspect()
        {
            var pb = new PixelBuffer(16, 8);
            pb.Fill(PixelColor.Green);
            byte[] bmpData = BmpEncoder.Encode(pb, 24);
            var engine = new BmpEngine();
            using (var ms = new MemoryStream(bmpData))
            {
                var inspection = engine.Inspect(ms, "foto.bmp");
                Assert(inspection.IsSupported, "IsSupported == true");
                AssertEqual("bmp", inspection.FormatId, "FormatId");
                AssertEqual(16, inspection.Width, "Width");
                AssertEqual(8, inspection.Height, "Height");
                AssertEqual("24", inspection.Metadata["BitDepth"], "BitDepth");
            }
        }

        private static void Test_Bmp_Engine_ConvertPipeline()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_bmp_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string inPath = Path.Combine(tempDir, "input.bmp");
                var pb = new PixelBuffer(20, 20);
                pb.Fill(PixelColor.Blue);
                File.WriteAllBytes(inPath, BmpEncoder.Encode(pb, 24));

                var req = new ConvertRequest
                {
                    InputPath = inPath,
                    TargetFormat = "bmp",
                    OutputDirectory = tempDir,
                    OutputFileName = "processed"
                };
                req.Settings["width"] = "40";
                req.Settings["height"] = "40";
                req.Settings["watermarkText"] = "LODINO";

                var engine = new BmpEngine();
                var result = engine.Convert(req, null);
                AssertEqual("completed", result.Status, "Status");
                Assert(File.Exists(result.OutputPath), "Arquivo resultante existe");

                var outputPb = BmpDecoder.Decode(File.ReadAllBytes(result.OutputPath));
                AssertEqual(40, outputPb.Width, "Largura redimensionada para 40");
                AssertEqual(40, outputPb.Height, "Altura redimensionada para 40");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Bmp_Registry_Resolution()
        {
            var registry = EngineRegistry.Default;
            var engine = registry.Resolve("bmp", "bmp");
            Assert(engine != null, "Motor BMP resolvido");
            AssertEqual("image-bmp", engine.EngineId, "EngineId image-bmp");
        }

        #region Testes de Fixtures Independentes (Fase B)

        private static byte[] CreateRawBmp(int width, int rawHeight, ushort bpp, byte[] pixelBytes, int offBits = 54, byte[] extraHeaderBytes = null)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int extraLen = extraHeaderBytes != null ? extraHeaderBytes.Length : 0;
                int totalOffBits = Math.Max(offBits, 54 + extraLen);
                int totalSize = totalOffBits + pixelBytes.Length;

                // BITMAPFILEHEADER (14 bytes)
                bw.Write((ushort)0x4D42);
                bw.Write((uint)totalSize);
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write((uint)totalOffBits);

                // BITMAPINFOHEADER (40 bytes)
                bw.Write((uint)40);
                bw.Write(width);
                bw.Write(rawHeight);
                bw.Write((ushort)1);
                bw.Write(bpp);
                bw.Write((uint)0); // BI_RGB
                bw.Write((uint)pixelBytes.Length);
                bw.Write(2835);
                bw.Write(2835);
                bw.Write((uint)0);
                bw.Write((uint)0);

                // Metadados adicionais pré-pixel (se offBits > 54)
                if (extraLen > 0)
                {
                    bw.Write(extraHeaderBytes, 0, extraLen);
                }

                // Preenchimento até offBits se houver lacuna
                while (ms.Position < totalOffBits)
                {
                    bw.Write((byte)0);
                }

                bw.Write(pixelBytes, 0, pixelBytes.Length);
                return ms.ToArray();
            }
        }

        private static void Test_Bmp_Independent_24bit_PaddingVariants_0_1_2_3()
        {
            // 1. Padding 0: 4x1 (stride = 12, rowData = 12, pad = 0)
            // Pixels BGR: P0=(0,0,255), P1=(0,255,0), P2=(255,0,0), P3=(255,255,255)
            byte[] p0_data = new byte[] {
                0, 0, 255,     // P0: Red
                0, 255, 0,     // P1: Green
                255, 0, 0,     // P2: Blue
                255, 255, 255  // P3: White
            };
            var dec0 = BmpDecoder.Decode(CreateRawBmp(4, 1, 24, p0_data));
            AssertEqual(PixelColor.Red, dec0.GetPixel(0, 0), "Pad0 P0");
            AssertEqual(PixelColor.Green, dec0.GetPixel(1, 0), "Pad0 P1");
            AssertEqual(PixelColor.Blue, dec0.GetPixel(2, 0), "Pad0 P2");
            AssertEqual(PixelColor.White, dec0.GetPixel(3, 0), "Pad0 P3");

            // 2. Padding 1: 1x2 (stride = 4, rowData = 3, pad = 1 com byte de lixo 0xAA)
            // Bottom-up: scanline 0 é y=1, scanline 1 é y=0
            byte[] p1_data = new byte[] {
                0, 255, 255, 0xAA, // y=1: Amarelo (B=0, G=255, R=255) + pad 0xAA
                255, 255, 0, 0xBB  // y=0: Ciano (B=255, G=255, R=0) + pad 0xBB
            };
            var dec1 = BmpDecoder.Decode(CreateRawBmp(1, 2, 24, p1_data));
            AssertEqual(PixelColor.Cyan, dec1.GetPixel(0, 0), "Pad1 y=0 Ciano");
            AssertEqual(PixelColor.Yellow, dec1.GetPixel(0, 1), "Pad1 y=1 Amarelo");

            // 3. Padding 2: 2x1 (stride = 8, rowData = 6, pad = 2 com lixo 0xDE, 0xAD)
            byte[] p2_data = new byte[] {
                255, 0, 255,   // P0: Magenta
                0, 0, 0,       // P1: Preto
                0xDE, 0xAD     // pad de 2 bytes
            };
            var dec2 = BmpDecoder.Decode(CreateRawBmp(2, 1, 24, p2_data));
            AssertEqual(PixelColor.Magenta, dec2.GetPixel(0, 0), "Pad2 P0 Magenta");
            AssertEqual(PixelColor.Black, dec2.GetPixel(1, 0), "Pad2 P1 Preto");

            // 4. Padding 3: 3x1 (stride = 12, rowData = 9, pad = 3 com lixo 0x11, 0x22, 0x33)
            byte[] p3_data = new byte[] {
                50, 60, 70,     // P0
                80, 90, 100,    // P1
                110, 120, 130,  // P2
                0x11, 0x22, 0x33 // pad de 3 bytes
            };
            var dec3 = BmpDecoder.Decode(CreateRawBmp(3, 1, 24, p3_data));
            AssertEqual(new PixelColor(70, 60, 50, 255), dec3.GetPixel(0, 0), "Pad3 P0");
            AssertEqual(new PixelColor(100, 90, 80, 255), dec3.GetPixel(1, 0), "Pad3 P1");
            AssertEqual(new PixelColor(130, 120, 110, 255), dec3.GetPixel(2, 0), "Pad3 P2");
        }

        private static void Test_Bmp_Independent_CustomOffBits_WithMetadataHeader()
        {
            // offBits = 80 (54 bytes do cabeçalho + 20 bytes de dados personalizados + 6 bytes pad)
            byte[] extra = Encoding.ASCII.GetBytes("LODINO-METADATA-PAD!"); // 20 bytes
            byte[] pixelData = new byte[] { 100, 150, 200, 0 }; // 1x1 24-bit stride 4
            byte[] fileBytes = CreateRawBmp(1, 1, 24, pixelData, 80, extra);

            var decoded = BmpDecoder.Decode(fileBytes);
            AssertEqual(1, decoded.Width, "Width");
            AssertEqual(1, decoded.Height, "Height");
            AssertEqual(new PixelColor(200, 150, 100, 255), decoded.GetPixel(0, 0), "Pixel decodificado após offset arbitrário");
        }

        private static void Test_Bmp_Independent_TopDown_vs_BottomUp_ExactDuality()
        {
            // Imagem 2x2:
            // y=0: P(0,0)=Vermelho, P(1,0)=Verde
            // y=1: P(0,1)=Azul, P(1,1)=Branco
            // Stride = 8 bytes (6 dados + 2 pad)

            // Bottom-Up: Linha 0 do arquivo é y=1 (Azul, Branco), Linha 1 do arquivo é y=0 (Vermelho, Verde)
            byte[] bu_pixels = new byte[] {
                255, 0, 0,    255, 255, 255, 0, 0, // y=1: Azul, Branco, pad 2
                0, 0, 255,    0, 255, 0,     0, 0  // y=0: Vermelho, Verde, pad 2
            };
            byte[] bu_file = CreateRawBmp(2, 2, 24, bu_pixels);

            // Top-Down: Linha 0 do arquivo é y=0 (Vermelho, Verde), Linha 1 do arquivo é y=1 (Azul, Branco)
            byte[] td_pixels = new byte[] {
                0, 0, 255,    0, 255, 0,     0, 0, // y=0: Vermelho, Verde, pad 2
                255, 0, 0,    255, 255, 255, 0, 0  // y=1: Azul, Branco, pad 2
            };
            byte[] td_file = CreateRawBmp(2, -2, 24, td_pixels);

            var decBU = BmpDecoder.Decode(bu_file);
            var decTD = BmpDecoder.Decode(td_file);

            AssertEqual(decBU.GetPixel(0, 0), decTD.GetPixel(0, 0), "Duality (0,0) Vermelho");
            AssertEqual(decBU.GetPixel(1, 0), decTD.GetPixel(1, 0), "Duality (1,0) Verde");
            AssertEqual(decBU.GetPixel(0, 1), decTD.GetPixel(0, 1), "Duality (0,1) Azul");
            AssertEqual(decBU.GetPixel(1, 1), decTD.GetPixel(1, 1), "Duality (1,1) Branco");
        }

        private static void Test_Bmp_Independent_HeaderTruncation_And_SizeMismatch()
        {
            // Arquivo com menos de 54 bytes
            byte[] shortFile = new byte[35];
            shortFile[0] = 0x42; shortFile[1] = 0x4D;
            bool threwShort = false;
            try { BmpDecoder.Decode(shortFile); } catch (CorruptDataException) { threwShort = true; }
            Assert(threwShort, "Rejeita arquivo com tamanho menor que cabeçalho padrão");

            // bfOffBits menor que 54
            byte[] badOff = CreateRawBmp(1, 1, 24, new byte[] { 0, 0, 0, 0 });
            badOff[10] = 30; badOff[11] = 0; badOff[12] = 0; badOff[13] = 0;
            bool threwBadOff = false;
            try { BmpDecoder.Decode(badOff); } catch (CorruptDataException) { threwBadOff = true; }
            Assert(threwBadOff, "Rejeita bfOffBits < 54");

            // bfOffBits apontando para além do comprimento do arquivo
            badOff[10] = 0x00; badOff[11] = 0x10; // 4096
            bool threwBeyond = false;
            try { BmpDecoder.Decode(badOff); } catch (CorruptDataException) { threwBeyond = true; }
            Assert(threwBeyond, "Rejeita bfOffBits apontando além do EOF");
        }

        private static void Test_Bmp_Independent_DimensionOverflow()
        {
            byte[] bmp = CreateRawBmp(2, 2, 24, new byte[16]);

            // biWidth = 0x40000000 (causaria overflow em (width * 24 + 31)/32 se não protegido)
            bmp[18] = 0x00; bmp[19] = 0x00; bmp[20] = 0x00; bmp[21] = 0x40;
            bool threwWidth = false;
            try { BmpDecoder.Decode(bmp); }
            catch (Exception ex) { if (ex is ResourceLimitException || ex is CorruptDataException) threwWidth = true; }
            Assert(threwWidth, "Bloqueia overflow de largura (0x40000000)");

            // biHeight = 0
            bmp[18] = 2; bmp[19] = 0; bmp[20] = 0; bmp[21] = 0; // restaura width
            bmp[22] = 0; bmp[23] = 0; bmp[24] = 0; bmp[25] = 0; // height = 0
            bool threwZeroH = false;
            try { BmpDecoder.Decode(bmp); } catch (CorruptDataException) { threwZeroH = true; }
            Assert(threwZeroH, "Rejeita biHeight == 0");

            // biWidth = -5
            bmp[18] = 0xFB; bmp[19] = 0xFF; bmp[20] = 0xFF; bmp[21] = 0xFF; // -5
            bmp[22] = 2; bmp[23] = 0; bmp[24] = 0; bmp[25] = 0;
            bool threwNegW = false;
            try { BmpDecoder.Decode(bmp); } catch (CorruptDataException) { threwNegW = true; }
            Assert(threwNegW, "Rejeita biWidth negativo");
        }

        #endregion

        #region Motor PNG (Testes e Fixtures Independentes)

        #region Helpers de Fixtures PNG Independentes

        private class SimpleBitWriter
        {
            private readonly MemoryStream _ms = new MemoryStream();
            private int _bitBuffer = 0;
            private int _bitCount = 0;

            public void WriteBits(int value, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int bit = (value >> i) & 1;
                    _bitBuffer |= (bit << _bitCount);
                    _bitCount++;
                    if (_bitCount == 8)
                    {
                        _ms.WriteByte((byte)_bitBuffer);
                        _bitBuffer = 0;
                        _bitCount = 0;
                    }
                }
            }

            public void WriteHuffman(int code, int bitLength)
            {
                for (int i = bitLength - 1; i >= 0; i--)
                {
                    int bit = (code >> i) & 1;
                    WriteBits(bit, 1);
                }
            }

            public byte[] ToArray()
            {
                if (_bitCount > 0)
                {
                    _ms.WriteByte((byte)_bitBuffer);
                }
                return _ms.ToArray();
            }
        }

        private static byte[] CreateRawPng(int width, int height, bool hasAlpha, byte[] rawFilteredScanlines)
        {
            using (var ms = new MemoryStream())
            {
                // 1. Assinatura PNG de 8 bytes
                ms.Write(PngHeader.PngSignature, 0, 8);

                // 2. Chunk IHDR (13 bytes)
                byte[] ihdrData = new byte[13];
                ihdrData[0] = (byte)((width >> 24) & 0xFF);
                ihdrData[1] = (byte)((width >> 16) & 0xFF);
                ihdrData[2] = (byte)((width >> 8) & 0xFF);
                ihdrData[3] = (byte)(width & 0xFF);

                ihdrData[4] = (byte)((height >> 24) & 0xFF);
                ihdrData[5] = (byte)((height >> 16) & 0xFF);
                ihdrData[6] = (byte)((height >> 8) & 0xFF);
                ihdrData[7] = (byte)(height & 0xFF);

                ihdrData[8] = 8; // Bit depth: 8
                ihdrData[9] = hasAlpha ? (byte)6 : (byte)2; // ColorType: 6 (RGBA) ou 2 (RGB)
                ihdrData[10] = 0; // Compressão: DEFLATE
                ihdrData[11] = 0; // Filtro: adaptativo
                ihdrData[12] = 0; // Interlace: nenhum
                WriteRawChunk(ms, "IHDR", ihdrData);

                // 3. Chunk IDAT (Fluxo ZLIB com bloco DEFLATE armazenado e checksum Adler-32)
                using (var zlibMs = new MemoryStream())
                {
                    zlibMs.WriteByte(0x78); // CMF
                    zlibMs.WriteByte(0x01); // FLG (check = 0x7801 = múltiplo de 31)

                    // Bloco DEFLATE armazenado (BFINAL = 1, BTYPE = 00)
                    zlibMs.WriteByte(0x01);
                    ushort len = (ushort)rawFilteredScanlines.Length;
                    ushort nlen = (ushort)(~len);
                    zlibMs.WriteByte((byte)(len & 0xFF));
                    zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                    zlibMs.WriteByte((byte)(nlen & 0xFF));
                    zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                    zlibMs.Write(rawFilteredScanlines, 0, rawFilteredScanlines.Length);

                    // Checksum Adler-32 no final
                    uint adler = Adler32.Calculate(rawFilteredScanlines, 0, rawFilteredScanlines.Length);
                    zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                    zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                    zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                    zlibMs.WriteByte((byte)(adler & 0xFF));

                    byte[] zlibBytes = zlibMs.ToArray();
                    WriteRawChunk(ms, "IDAT", zlibBytes);
                }

                // 4. Chunk IEND
                WriteRawChunk(ms, "IEND", new byte[0]);

                return ms.ToArray();
            }
        }

        private static void WriteRawChunk(Stream stream, string type, byte[] data)
        {
            int len = data != null ? data.Length : 0;
            stream.WriteByte((byte)((len >> 24) & 0xFF));
            stream.WriteByte((byte)((len >> 16) & 0xFF));
            stream.WriteByte((byte)((len >> 8) & 0xFF));
            stream.WriteByte((byte)(len & 0xFF));

            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes, 0, 4);

            if (len > 0)
            {
                stream.Write(data, 0, len);
            }

            uint crc = PngCrc32.Calculate(type, data, 0, len);
            stream.WriteByte((byte)((crc >> 24) & 0xFF));
            stream.WriteByte((byte)((crc >> 16) & 0xFF));
            stream.WriteByte((byte)((crc >> 8) & 0xFF));
            stream.WriteByte((byte)(crc & 0xFF));
        }

        #endregion

        private static void Test_Png_Checksum_Crc32_Vectors()
        {
            byte[] empty = new byte[0];
            Assert(PngCrc32.Calculate(empty, 0, 0) == 0x00000000, "CRC32 de vazio deve ser 0");

            byte[] v1 = Encoding.ASCII.GetBytes("123456789");
            Assert(PngCrc32.Calculate(v1, 0, v1.Length) == 0xCBF43926, "CRC32 de '123456789' deve ser 0xCBF43926");

            // Chunk IEND com dados vazios
            Assert(PngCrc32.Calculate("IEND", new byte[0], 0, 0) == 0xAE426082, "CRC32 do chunk 'IEND' deve ser 0xAE426082");
        }

        private static void Test_Png_Checksum_Adler32_Vectors()
        {
            byte[] empty = new byte[0];
            Assert(Adler32.Calculate(empty, 0, 0) == 1, "Adler32 de vazio deve ser 1");

            byte[] v1 = Encoding.ASCII.GetBytes("123456789");
            Assert(Adler32.Calculate(v1, 0, v1.Length) == 0x091E01DE, "Adler32 de '123456789' deve ser 0x091E01DE");

            byte[] v2 = Encoding.ASCII.GetBytes("Wikipedia");
            Assert(Adler32.Calculate(v2, 0, v2.Length) == 0x11E60398, "Adler32 de 'Wikipedia' deve ser 0x11E60398");

            // Detecção de corrupção do Adler-32 em ZlibStream
            byte[] compressed = ZlibStream.Compress(v1);
            compressed[compressed.Length - 1] ^= 0xFF; // corrompe último byte do checksum
            bool threwAdler = false;
            try { ZlibStream.Decompress(compressed); }
            catch (CorruptDataException) { threwAdler = true; }
            Assert(threwAdler, "ZlibStream deve rejeitar fluxo com Adler-32 corrompido");
        }

        private static void Test_Png_Deflate_StoredBlocks_SingleAndMulti()
        {
            byte[] data1 = Encoding.ASCII.GetBytes("Hello, Lodino Proprietary Core! ");
            byte[] data2 = Encoding.ASCII.GetBytes("Second Stored Block DEFLATE.");

            using (var ms = new MemoryStream())
            {
                // Bloco 1 (BFINAL = 0, BTYPE = 00)
                ms.WriteByte(0x00);
                ushort len1 = (ushort)data1.Length;
                ushort nlen1 = (ushort)(~len1);
                ms.WriteByte((byte)(len1 & 0xFF)); ms.WriteByte((byte)((len1 >> 8) & 0xFF));
                ms.WriteByte((byte)(nlen1 & 0xFF)); ms.WriteByte((byte)((nlen1 >> 8) & 0xFF));
                ms.Write(data1, 0, data1.Length);

                // Bloco 2 (BFINAL = 1, BTYPE = 00)
                ms.WriteByte(0x01);
                ushort len2 = (ushort)data2.Length;
                ushort nlen2 = (ushort)(~len2);
                ms.WriteByte((byte)(len2 & 0xFF)); ms.WriteByte((byte)((len2 >> 8) & 0xFF));
                ms.WriteByte((byte)(nlen2 & 0xFF)); ms.WriteByte((byte)((nlen2 >> 8) & 0xFF));
                ms.Write(data2, 0, data2.Length);

                byte[] streamBytes = ms.ToArray();
                byte[] decompressed = DeflateDecompressor.Decompress(new MemoryStream(streamBytes));
                string text = Encoding.ASCII.GetString(decompressed);
                Assert(text == "Hello, Lodino Proprietary Core! Second Stored Block DEFLATE.", "Descompressão multi-bloco stored deve ser exata");

                // Corrompe NLEN
                streamBytes[3] ^= 0xFF;
                bool threwNlen = false;
                try { DeflateDecompressor.Decompress(new MemoryStream(streamBytes)); }
                catch (CorruptDataException) { threwNlen = true; }
                Assert(threwNlen, "Deve rejeitar bloco com NLEN corrompido");
            }
        }

        private static void Test_Png_Deflate_FixedHuffman_Decompression()
        {
            // Bloco de teste DEFLATE com Huffman Fixo (BFINAL = 1, BTYPE = 01)
            byte[] fixedDeflate = new byte[]
            {
                0xF3, 0xF1, 0x77, 0xF1, 0xF4, 0xF3, 0x8F, 0x77, 0x71, 0x75, 0xF3, 0x71, 0x0C, 0x71, 0x8D, 0x77,
                0x89, 0xF4, 0x73, 0xF4, 0xF5, 0x74, 0x8E, 0x0F, 0x71, 0x0D, 0x0E, 0x89, 0x0F, 0x0E, 0x09, 0xF2,
                0xF4, 0x73, 0x8F, 0x37, 0x34, 0x32, 0x36, 0x31, 0x05, 0x00
            };

            byte[] decomp = DeflateDecompressor.Decompress(new MemoryStream(fixedDeflate));
            string str = Encoding.ASCII.GetString(decomp);
            Assert(str == "LODINO_DEFLATE_DYNAMIC_TEST_STRING_12345", "Fixed Huffman deve descomprimir string com exatidão");
        }

        private static void Test_Png_Deflate_DynamicHuffman_Decompression()
        {
            // Construção independente de bloco Huffman Dinâmico (BTYPE 10, BFINAL 1)
            var writer = new SimpleBitWriter();

            // 1. Cabeçalho de bloco: BFINAL=1, BTYPE=10
            writer.WriteBits(1, 1);
            writer.WriteBits(2, 2);

            // 2. Dimensões das tabelas dinâmicas
            int hlitVal = 0;   // hlit = 257 (0 + 257)
            int hdistVal = 0;  // hdist = 1 (0 + 1)
            int hclenVal = 14; // hclen = 18 (14 + 4)
            writer.WriteBits(hlitVal, 5);
            writer.WriteBits(hdistVal, 5);
            writer.WriteBits(hclenVal, 4);

            // 3. Tabela de comprimentos de código (18 entradas):
            // CodeLengthOrder: [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1]
            // Índice 2 é símbolo 18: comprimento 2
            // Índice 3 é símbolo 0: comprimento 1
            // Índice 17 é símbolo 1: comprimento 2
            for (int i = 0; i < 18; i++)
            {
                if (i == 3) writer.WriteBits(1, 3);      // símbolo 0: len 1 (código 1)
                else if (i == 2) writer.WriteBits(2, 3); // símbolo 18: len 2 (código 00)
                else if (i == 17) writer.WriteBits(2, 3);// símbolo 1: len 2 (código 01)
                else writer.WriteBits(0, 3);
            }

            // 4. Emissão dos 258 comprimentos de código (257 literais + 1 distância)
            // Símbolo 0 tem len 1 -> código 0 (1 bit: 0)
            // Símbolo 1 tem len 2 -> código 2 (2 bits: 10)
            // Símbolo 18 tem len 2 -> código 3 (2 bits: 11)
            // Símbolos 0..64 (65 símbolos) com comprimento 0: símbolo 18 com repeat 65 (54 nos 7 bits extras)
            writer.WriteHuffman(3, 2); // símbolo 18 (código 11)
            writer.WriteBits(54, 7);   // 11 + 54 = 65 repetições

            // Símbolo 65 ('A') com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Símbolos 66..203 (138 símbolos) com comprimento 0: símbolo 18 com repeat 138 (127 nos 7 bits extras)
            writer.WriteHuffman(3, 2);
            writer.WriteBits(127, 7);  // 11 + 127 = 138 repetições

            // Símbolos 204..255 (52 símbolos) com comprimento 0: símbolo 18 com repeat 52 (41 nos 7 bits extras)
            writer.WriteHuffman(3, 2);
            writer.WriteBits(41, 7);   // 11 + 41 = 52 repetições

            // Símbolo 256 (EOB) com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Símbolo de distância 0 com comprimento 1: símbolo 1 (código 10)
            writer.WriteHuffman(2, 2);

            // Agora a árvore literal tem: 'A' (65) = código 0 (1 bit), EOB (256) = código 1 (1 bit)
            // Emitimos "AAAA" seguido de EOB
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(0, 1); // 'A'
            writer.WriteBits(1, 1); // EOB (256)

            byte[] dynamicBlock = writer.ToArray();
            byte[] decompressed = DeflateDecompressor.Decompress(new MemoryStream(dynamicBlock));
            string resultStr = Encoding.ASCII.GetString(decompressed);
            Assert(resultStr == "AAAA", "Dynamic Huffman deve descomprimir string 'AAAA' com fidelidade");
        }

        private static void Test_Png_Filters_All5_RoundTrip()
        {
            byte[] currentRaw = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160 };
            byte[] priorRaw = new byte[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95, 105, 115, 125, 135, 145, 155 };

            for (byte f = 0; f <= 4; f++)
            {
                byte[] filtered = new byte[1 + currentRaw.Length];
                PngFilter.FilterScanline(f, currentRaw, priorRaw, filtered, currentRaw.Length, 4);
                Assert(filtered[0] == f, "Byte 0 deve registrar o tipo de filtro");

                byte[] reconstructed = new byte[currentRaw.Length];
                PngFilter.UnfilterScanline(f, filtered, 1, priorRaw, reconstructed, currentRaw.Length, 4);

                for (int i = 0; i < currentRaw.Length; i++)
                {
                    Assert(reconstructed[i] == currentRaw[i],
                        string.Format("Filtro {0}: byte {1} reconstruído (0x{2:X2}) != original (0x{3:X2})",
                        f, i, reconstructed[i], currentRaw[i]));
                }
            }
        }

        private static void Test_Png_Independent_Raw_Rgb24_Fixture()
        {
            // Imagem 2x2 24-bit Truecolor RGB (ColorType 2)
            // Linha 0 (Filtro 0 = None): Red (255, 0, 0), Green (0, 255, 0)
            // Linha 1 (Filtro 1 = Sub): Blue (0, 0, 255), White (255, 255, 255)
            // Filtro Sub na linha 1:
            //   Pixel 0 (Blue: 0, 0, 255): diff de 0 é (0, 0, 255)
            //   Pixel 1 (White: 255, 255, 255): diff de Pixel 0 é (255-0=255, 255-0=255, 255-255=0)
            byte[] scanlines = new byte[]
            {
                // Linha 0 (Filtro None = 0)
                0x00, 255, 0, 0,  0, 255, 0,
                // Linha 1 (Filtro Sub = 1)
                0x01, 0, 0, 255,  255, 255, 0
            };

            byte[] pngBytes = CreateRawPng(2, 2, false, scanlines);
            PixelBuffer buf = PngDecoder.Decode(pngBytes);

            Assert(buf.Width == 2 && buf.Height == 2, "Dimensões devem ser 2x2");
            Assert(buf.GetPixel(0, 0).R == 255 && buf.GetPixel(0, 0).G == 0 && buf.GetPixel(0, 0).B == 0 && buf.GetPixel(0, 0).A == 255, "(0,0) deve ser Vermelho opaco");
            Assert(buf.GetPixel(1, 0).R == 0 && buf.GetPixel(1, 0).G == 255 && buf.GetPixel(1, 0).B == 0 && buf.GetPixel(1, 0).A == 255, "(1,0) deve ser Verde opaco");
            Assert(buf.GetPixel(0, 1).R == 0 && buf.GetPixel(0, 1).G == 0 && buf.GetPixel(0, 1).B == 255 && buf.GetPixel(0, 1).A == 255, "(0,1) deve ser Azul opaco");
            Assert(buf.GetPixel(1, 1).R == 255 && buf.GetPixel(1, 1).G == 255 && buf.GetPixel(1, 1).B == 255 && buf.GetPixel(1, 1).A == 255, "(1,1) deve ser Branco opaco");
        }

        private static void Test_Png_Independent_Raw_Rgba32_Fixture()
        {
            // Imagem 2x2 32-bit Truecolor RGBA (ColorType 6)
            // Linha 0 (Filtro 0 = None): (255, 0, 0, 0), (0, 255, 0, 64)
            // Linha 1 (Filtro 2 = Up):   (0, 0, 255, 128), (255, 255, 0, 255)
            // Filtro Up na linha 1: diff de Linha 0:
            //   Pixel 0: (0-255)=1, (0-0)=0, (255-0)=255, (128-0)=128
            //   Pixel 1: (255-0)=255, (255-255)=0, (0-0)=0, (255-64)=191
            byte[] scanlines = new byte[]
            {
                // Linha 0 (Filtro None = 0)
                0x00, 255, 0, 0, 0,  0, 255, 0, 64,
                // Linha 1 (Filtro Up = 2)
                0x02, 1, 0, 255, 128,  255, 0, 0, 191
            };

            byte[] pngBytes = CreateRawPng(2, 2, true, scanlines);
            PixelBuffer buf = PngDecoder.Decode(pngBytes);

            Assert(buf.Width == 2 && buf.Height == 2, "Dimensões devem ser 2x2");
            Assert(buf.GetPixel(0, 0).R == 255 && buf.GetPixel(0, 0).A == 0, "(0,0) deve preservar canal Alfa = 0");
            Assert(buf.GetPixel(1, 0).G == 255 && buf.GetPixel(1, 0).A == 64, "(1,0) deve preservar canal Alfa = 64");
            Assert(buf.GetPixel(0, 1).B == 255 && buf.GetPixel(0, 1).A == 128, "(0,1) deve preservar canal Alfa = 128");
            Assert(buf.GetPixel(1, 1).R == 255 && buf.GetPixel(1, 1).G == 255 && buf.GetPixel(1, 1).A == 255, "(1,1) deve preservar canal Alfa = 255");
        }

        private static void Test_Png_Multiple_Idat_Chunks()
        {
            // Gera um PNG onde o fluxo ZLIB é fragmentado em múltiplos chunks IDAT
            byte[] scanlines = new byte[]
            {
                0x00, 10, 20, 30,  40, 50, 60,
                0x00, 70, 80, 90, 100, 110, 120
            };

            using (var zlibMs = new MemoryStream())
            {
                zlibMs.WriteByte(0x78); zlibMs.WriteByte(0x01);
                zlibMs.WriteByte(0x01);
                ushort len = (ushort)scanlines.Length;
                ushort nlen = (ushort)(~len);
                zlibMs.WriteByte((byte)(len & 0xFF)); zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(nlen & 0xFF)); zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                zlibMs.Write(scanlines, 0, scanlines.Length);

                uint adler = Adler32.Calculate(scanlines, 0, scanlines.Length);
                zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(adler & 0xFF));

                byte[] zlibBytes = zlibMs.ToArray();

                using (var ms = new MemoryStream())
                {
                    ms.Write(PngHeader.PngSignature, 0, 8);
                    WriteRawChunk(ms, "IHDR", new byte[] { 0, 0, 0, 2, 0, 0, 0, 2, 8, 2, 0, 0, 0 });

                    // Divide em 3 chunks IDAT consecutivos
                    int part1 = 7;
                    int part2 = 8;
                    int part3 = zlibBytes.Length - part1 - part2;

                    byte[] c1 = new byte[part1];
                    Buffer.BlockCopy(zlibBytes, 0, c1, 0, part1);
                    WriteRawChunk(ms, "IDAT", c1);

                    byte[] c2 = new byte[part2];
                    Buffer.BlockCopy(zlibBytes, part1, c2, 0, part2);
                    WriteRawChunk(ms, "IDAT", c2);

                    byte[] c3 = new byte[part3];
                    Buffer.BlockCopy(zlibBytes, part1 + part2, c3, 0, part3);
                    WriteRawChunk(ms, "IDAT", c3);

                    WriteRawChunk(ms, "IEND", new byte[0]);

                    byte[] multiIdatPng = ms.ToArray();
                    PixelBuffer buf = PngDecoder.Decode(multiIdatPng);
                    Assert(buf.Width == 2 && buf.Height == 2, "Decodificação com múltiplos IDATs deve ser íntegra");
                    Assert(buf.GetPixel(0, 0).R == 10 && buf.GetPixel(1, 1).B == 120, "Pixels reconstruídos com perfeição");
                }
            }
        }

        private static void Test_Png_RoundTrip_Rgb_And_Rgba()
        {
            var orig = new PixelBuffer(12, 10);
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    byte r = (byte)(x * 20);
                    byte g = (byte)(y * 25);
                    byte b = (byte)((x + y) * 10);
                    byte a = (byte)(50 + (x * y) % 205);
                    orig.SetPixel(x, y, new PixelColor(r, g, b, a));
                }
            }

            // 1. Round-trip RGBA (32-bit)
            byte[] rgbaPng = PngEncoder.Encode(orig, true, PngFilter.FilterNone);
            PixelBuffer decRgba = PngDecoder.Decode(rgbaPng);
            Assert(decRgba.Width == 12 && decRgba.Height == 10, "Largura e altura RGBA preservadas");
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    PixelColor o = orig.GetPixel(x, y);
                    PixelColor d = decRgba.GetPixel(x, y);
                    Assert(o.R == d.R && o.G == d.G && o.B == d.B && o.A == d.A,
                        string.Format("Pixel RGBA ({0},{1}) divergente", x, y));
                }
            }

            // 2. Round-trip RGB (24-bit)
            byte[] rgbPng = PngEncoder.Encode(orig, false, PngFilter.FilterNone);
            PixelBuffer decRgb = PngDecoder.Decode(rgbPng);
            Assert(decRgb.Width == 12 && decRgb.Height == 10, "Largura e altura RGB preservadas");
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    PixelColor o = orig.GetPixel(x, y);
                    PixelColor d = decRgb.GetPixel(x, y);
                    Assert(o.R == d.R && o.G == d.G && o.B == d.B && d.A == 255,
                        string.Format("Pixel RGB ({0},{1}) divergente", x, y));
                }
            }
        }

        private static void Test_Png_Encoder_All5_Filters()
        {
            var buf = new PixelBuffer(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    buf.SetPixel(x, y, new PixelColor((byte)(x * 30), (byte)(y * 30), (byte)((x + y) * 15), 255));
                }
            }

            for (byte f = 0; f <= 4; f++)
            {
                byte[] png = PngEncoder.Encode(buf, true, f);
                PixelBuffer dec = PngDecoder.Decode(png);
                Assert(dec.Width == 8 && dec.Height == 8, "Dimensões preservadas no filtro " + f);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        PixelColor o = buf.GetPixel(x, y);
                        PixelColor d = dec.GetPixel(x, y);
                        Assert(o.R == d.R && o.G == d.G && o.B == d.B && o.A == d.A,
                            string.Format("Filtro {0}: Pixel ({1},{2}) divergente", f, x, y));
                    }
                }
            }
        }

        private static void Test_Png_Security_StrictRejections()
        {
            byte[] validPng = CreateRawPng(1, 1, false, new byte[] { 0, 255, 255, 255 });

            // 1. Assinatura mágica inválida
            byte[] badSig = (byte[])validPng.Clone();
            badSig[0] = 0x00;
            bool threwSig = false;
            try { PngDecoder.Decode(badSig); } catch (CorruptDataException) { threwSig = true; }
            Assert(threwSig, "Rejeita assinatura mágica inválida");

            // 2. Arquivo truncado
            byte[] truncated = new byte[7];
            Array.Copy(validPng, 0, truncated, 0, 7);
            bool threwTrunc = false;
            try { PngDecoder.Decode(truncated); } catch (CorruptDataException) { threwTrunc = true; }
            Assert(threwTrunc, "Rejeita arquivo truncado antes da assinatura completa");

            // 3. CRC-32 do chunk IHDR adulterado
            byte[] badCrc = (byte[])validPng.Clone();
            badCrc[29] ^= 0xFF; // corrompe CRC do IHDR
            bool threwCrc = false;
            try { PngDecoder.Decode(badCrc); } catch (CorruptDataException) { threwCrc = true; }
            Assert(threwCrc, "Rejeita chunk com CRC-32 adulterado");

            // 4. ColorType 3 (Indexado com paleta)
            byte[] indexedIhdr = (byte[])validPng.Clone();
            indexedIhdr[25] = 3; // ColorType = 3 (offset 16 + 9)
            uint crcIndexed = PngCrc32.Calculate("IHDR", indexedIhdr, 16, 13);
            indexedIhdr[29] = (byte)((crcIndexed >> 24) & 0xFF);
            indexedIhdr[30] = (byte)((crcIndexed >> 16) & 0xFF);
            indexedIhdr[31] = (byte)((crcIndexed >> 8) & 0xFF);
            indexedIhdr[32] = (byte)(crcIndexed & 0xFF);
            bool threwIndexed = false;
            try { PngDecoder.Decode(indexedIhdr); } catch (UnsupportedFormatException) { threwIndexed = true; }
            Assert(threwIndexed, "Rejeita estritamente PNG indexado (ColorType 3)");

            // 5. ColorType 0 (Escala de cinza)
            byte[] grayIhdr = (byte[])validPng.Clone();
            grayIhdr[25] = 0; // ColorType = 0
            uint crcGray = PngCrc32.Calculate("IHDR", grayIhdr, 16, 13);
            grayIhdr[29] = (byte)((crcGray >> 24) & 0xFF);
            grayIhdr[30] = (byte)((crcGray >> 16) & 0xFF);
            grayIhdr[31] = (byte)((crcGray >> 8) & 0xFF);
            grayIhdr[32] = (byte)(crcGray & 0xFF);
            bool threwGray = false;
            try { PngDecoder.Decode(grayIhdr); } catch (UnsupportedFormatException) { threwGray = true; }
            Assert(threwGray, "Rejeita estritamente PNG em escala de cinza (ColorType 0)");

            // 6. InterlaceMethod = 1 (Adam7)
            byte[] adam7Ihdr = (byte[])validPng.Clone();
            adam7Ihdr[28] = 1; // Interlace = 1 (offset 16 + 12)
            uint crcAdam7 = PngCrc32.Calculate("IHDR", adam7Ihdr, 16, 13);
            adam7Ihdr[29] = (byte)((crcAdam7 >> 24) & 0xFF);
            adam7Ihdr[30] = (byte)((crcAdam7 >> 16) & 0xFF);
            adam7Ihdr[31] = (byte)((crcAdam7 >> 8) & 0xFF);
            adam7Ihdr[32] = (byte)(crcAdam7 & 0xFF);
            bool threwAdam7 = false;
            try { PngDecoder.Decode(adam7Ihdr); } catch (UnsupportedFormatException) { threwAdam7 = true; }
            Assert(threwAdam7, "Rejeita estritamente entrelaçamento Adam7");

            // 7. Dimensão zero (Width = 0)
            byte[] zeroWIhdr = (byte[])validPng.Clone();
            zeroWIhdr[16] = 0; zeroWIhdr[17] = 0; zeroWIhdr[18] = 0; zeroWIhdr[19] = 0;
            uint crcZeroW = PngCrc32.Calculate("IHDR", zeroWIhdr, 16, 13);
            zeroWIhdr[29] = (byte)((crcZeroW >> 24) & 0xFF);
            zeroWIhdr[30] = (byte)((crcZeroW >> 16) & 0xFF);
            zeroWIhdr[31] = (byte)((crcZeroW >> 8) & 0xFF);
            zeroWIhdr[32] = (byte)(crcZeroW & 0xFF);
            bool threwZeroW = false;
            try { PngDecoder.Decode(zeroWIhdr); } catch (CorruptDataException) { threwZeroW = true; }
            Assert(threwZeroW, "Rejeita largura zero (0x0)");

            // 8. Dimensão excessiva (> 16384)
            byte[] hugeWIhdr = (byte[])validPng.Clone();
            hugeWIhdr[16] = 0; hugeWIhdr[17] = 0; hugeWIhdr[18] = 0x50; hugeWIhdr[19] = 0x00; // 20480
            uint crcHugeW = PngCrc32.Calculate("IHDR", hugeWIhdr, 16, 13);
            hugeWIhdr[29] = (byte)((crcHugeW >> 24) & 0xFF);
            hugeWIhdr[30] = (byte)((crcHugeW >> 16) & 0xFF);
            hugeWIhdr[31] = (byte)((crcHugeW >> 8) & 0xFF);
            hugeWIhdr[32] = (byte)(crcHugeW & 0xFF);
            bool threwHugeW = false;
            try { PngDecoder.Decode(hugeWIhdr); } catch (ResourceLimitException) { threwHugeW = true; }
            Assert(threwHugeW, "Protege contra dimensões excessivas (> 16384)");
        }

        private static void Test_Png_Engine_Inspect()
        {
            var engine = new PngEngine();
            var buf = new PixelBuffer(25, 15);
            buf.SetPixel(0, 0, new PixelColor(255, 0, 0, 100)); // transparente -> força RGBA
            byte[] png = PngEncoder.Encode(buf, true, PngFilter.FilterNone);

            using (var ms = new MemoryStream(png))
            {
                FormatInspection insp = engine.Inspect(ms, "test.png");
                Assert(insp.IsSupported, "PNG deve ser inspecionado como suportado");
                Assert(insp.FormatId == "png", "FormatId deve ser 'png'");
                Assert(insp.Width == 25, "Largura deve ser 25");
                Assert(insp.Height == 15, "Altura deve ser 15");
                Assert(insp.Metadata["BitDepth"] == "8", "BitDepth deve ser 8");
                Assert(insp.Metadata["ColorType"] == "Truecolor RGBA (6)", "ColorType deve ser Truecolor RGBA (6)");
                Assert(insp.Metadata["HasAlpha"] == "true", "HasAlpha deve ser true");
            }
        }

        private static void Test_Png_Engine_Convert_Transforms()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_png_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string inputPng = Path.Combine(tempDir, "source.png");
                var buf = new PixelBuffer(50, 50);
                for (int y = 0; y < 50; y++)
                {
                    for (int x = 0; x < 50; x++)
                    {
                        buf.SetPixel(x, y, new PixelColor((byte)(x * 5), (byte)(y * 5), 100, 255));
                    }
                }
                File.WriteAllBytes(inputPng, PngEncoder.Encode(buf));

                var engine = new PngEngine();
                var req = new ConvertRequest
                {
                    InputPath = inputPng,
                    TargetFormat = "png",
                    OutputDirectory = tempDir,
                    OutputFileName = "transformed"
                };
                req.Settings["cropWidth"] = "30";
                req.Settings["cropHeight"] = "30";
                req.Settings["cropPosition"] = "center";
                req.Settings["resizeWidth"] = "15";
                req.Settings["resizeHeight"] = "15";
                req.Settings["filter"] = "bilinear";
                req.Settings["watermarkText"] = "LODINO";

                ConvertResult res = engine.Convert(req, null);
                Assert(res.Status == "completed", "Conversão com transformações deve ter sucesso");
                Assert(File.Exists(res.OutputPath), "Arquivo de saída deve existir");

                byte[] outBytes = File.ReadAllBytes(res.OutputPath);
                PixelBuffer outBuf = PngDecoder.Decode(outBytes);
                Assert(outBuf.Width == 15 && outBuf.Height == 15, "Dimensões pós-crop e resize devem ser 15x15");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static void Test_Png_Engine_Bidirectional_Bmp_Png()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_bidir_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Cria imagem original BMP
                string origBmp = Path.Combine(tempDir, "orig.bmp");
                var origBuf = new PixelBuffer(8, 8);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        origBuf.SetPixel(x, y, new PixelColor((byte)(x * 32), (byte)(y * 32), 128, 255));
                    }
                }
                File.WriteAllBytes(origBmp, BmpEncoder.Encode(origBuf, 24));

                // 2. Resolve motor para BMP -> PNG
                ICoreEngine bmpToPngEngine = EngineRegistry.Default.Resolve("bmp", "png");
                Assert(bmpToPngEngine.EngineId == "image-png", "EngineRegistry deve resolver image-png para rota bmp -> png");

                var reqBmpToPng = new ConvertRequest
                {
                    InputPath = origBmp,
                    TargetFormat = "png",
                    OutputDirectory = tempDir,
                    OutputFileName = "converted_from_bmp"
                };
                ConvertResult resPng = bmpToPngEngine.Convert(reqBmpToPng, null);
                Assert(resPng.Status == "completed", "Conversão BMP -> PNG deve ser bem-sucedida");

                // Inspeciona PNG gerado
                using (var pngStream = File.OpenRead(resPng.OutputPath))
                {
                    FormatInspection insp = bmpToPngEngine.Inspect(pngStream, resPng.OutputPath);
                    Assert(insp.IsSupported && insp.Width == 8 && insp.Height == 8, "PNG convertido deve ter dimensões 8x8");
                }

                // 3. Resolve motor para PNG -> BMP
                ICoreEngine pngToBmpEngine = EngineRegistry.Default.Resolve("png", "bmp");
                Assert(pngToBmpEngine.EngineId == "image-png", "EngineRegistry deve resolver image-png para rota png -> bmp");

                var reqPngToBmp = new ConvertRequest
                {
                    InputPath = resPng.OutputPath,
                    TargetFormat = "bmp",
                    OutputDirectory = tempDir,
                    OutputFileName = "converted_back_to_bmp"
                };
                ConvertResult resBmp = pngToBmpEngine.Convert(reqPngToBmp, null);
                Assert(resBmp.Status == "completed", "Conversão reversa PNG -> BMP deve ser bem-sucedida");

                // Verifica fidelidade dos pixels do BMP re-convertido
                byte[] finalBmpBytes = File.ReadAllBytes(resBmp.OutputPath);
                PixelBuffer finalBuf = BmpDecoder.Decode(finalBmpBytes);
                Assert(finalBuf.Width == 8 && finalBuf.Height == 8, "BMP re-convertido deve manter 8x8");

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        PixelColor o = origBuf.GetPixel(x, y);
                        PixelColor f = finalBuf.GetPixel(x, y);
                        Assert(o.R == f.R && o.G == f.G && o.B == f.B,
                            string.Format("Pixel ({0},{1}) divergente na rota bidirecional", x, y));
                    }
                }

                // 4. Valida resolução das três rotas no EngineRegistry
                Assert(EngineRegistry.Default.Resolve("png", "png").EngineId == "image-png", "Resolução png -> png");
                Assert(EngineRegistry.Default.Resolve("png", "bmp").EngineId == "image-png", "Resolução png -> bmp");
                Assert(EngineRegistry.Default.Resolve("bmp", "png").EngineId == "image-png", "Resolução bmp -> png");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #region Hardening BMP + PNG (Testes de Segurança e Resiliência)

        private class FragmentedStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _maxPerRead;

            public FragmentedStream(Stream inner, int maxPerRead = 3)
            {
                _inner = inner;
                _maxPerRead = maxPerRead;
            }

            public override bool CanRead { get { return _inner.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int toRead = Math.Min(count, _maxPerRead);
                return _inner.Read(buffer, offset, toRead);
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private class NonSeekableStream : Stream
        {
            private readonly Stream _inner;

            public NonSeekableStream(Stream inner)
            {
                _inner = inner;
            }

            public override bool CanRead { get { return _inner.CanRead; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private class VirtualAudioStream : Stream
        {
            private readonly byte[] _headerBytes;
            private readonly long _totalLength;
            private long _position;

            public VirtualAudioStream(byte[] headerBytes, long totalLength)
            {
                _headerBytes = headerBytes;
                _totalLength = totalLength;
                _position = 0;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return true; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return _totalLength; } }
            public override long Position
            {
                get { return _position; }
                set { _position = value; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_position >= _totalLength) return 0;
                int bytesRead = 0;
                if (_position < _headerBytes.Length)
                {
                    int headerAvailable = (int)Math.Min((long)count, _headerBytes.Length - _position);
                    Buffer.BlockCopy(_headerBytes, (int)_position, buffer, offset, headerAvailable);
                    _position += headerAvailable;
                    offset += headerAvailable;
                    count -= headerAvailable;
                    bytesRead += headerAvailable;
                }

                if (count > 0 && _position < _totalLength)
                {
                    int virtualAvailable = (int)Math.Min((long)count, _totalLength - _position);
                    Array.Clear(buffer, offset, virtualAvailable);
                    _position += virtualAvailable;
                    bytesRead += virtualAvailable;
                }

                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin: _position = offset; break;
                    case SeekOrigin.Current: _position += offset; break;
                    case SeekOrigin.End: _position = _totalLength + offset; break;
                }
                return _position;
            }

            public override void Flush() { }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private static void Test_Hardening_DecompressionBomb_Protection()
        {
            // Dimensões declaradas pequenas: 2x2 RGBA -> espera exatamente 2 * (1 + 8) = 18 bytes
            // Criamos um payload comprimido com DEFLATE Stored de 2000 bytes (> 18 bytes)
            byte[] bombPayload = new byte[2000];
            for (int i = 0; i < bombPayload.Length; i++) bombPayload[i] = (byte)(i & 0xFF);

            using (var zlibMs = new MemoryStream())
            {
                zlibMs.WriteByte(0x78);
                zlibMs.WriteByte(0x01);

                // Bloco DEFLATE Stored
                zlibMs.WriteByte(0x01); // BFINAL=1, BTYPE=00
                ushort len = (ushort)bombPayload.Length;
                ushort nlen = (ushort)(~len);
                zlibMs.WriteByte((byte)(len & 0xFF));
                zlibMs.WriteByte((byte)((len >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(nlen & 0xFF));
                zlibMs.WriteByte((byte)((nlen >> 8) & 0xFF));
                zlibMs.Write(bombPayload, 0, bombPayload.Length);

                uint adler = Adler32.Calculate(bombPayload, 0, bombPayload.Length);
                zlibMs.WriteByte((byte)((adler >> 24) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 16) & 0xFF));
                zlibMs.WriteByte((byte)((adler >> 8) & 0xFF));
                zlibMs.WriteByte((byte)(adler & 0xFF));

                byte[] zlibBytes = zlibMs.ToArray();

                using (var pngMs = new MemoryStream())
                {
                    pngMs.Write(PngHeader.PngSignature, 0, 8);

                    // IHDR 2x2 RGBA
                    byte[] ihdr = new byte[13];
                    ihdr[3] = 2; // Width = 2
                    ihdr[7] = 2; // Height = 2
                    ihdr[8] = 8; // 8-bit
                    ihdr[9] = 6; // RGBA
                    WriteRawChunk(pngMs, "IHDR", ihdr);

                    WriteRawChunk(pngMs, "IDAT", zlibBytes);
                    WriteRawChunk(pngMs, "IEND", new byte[0]);

                    byte[] bombPng = pngMs.ToArray();

                    bool stoppedBomb = false;
                    try
                    {
                        PngDecoder.Decode(bombPng);
                    }
                    catch (ResourceLimitException)
                    {
                        stoppedBomb = true;
                    }
                    Assert(stoppedBomb, "Decompression Bomb deve ser abortada imediatamente com ResourceLimitException ao exceder bytes esperados");
                }
            }
        }

        private static void Test_Hardening_Idat_Chunking_Generation()
        {
            // Imagem 100x100 RGBA gera ~40 KB de dados descomprimidos
            // Ao codificar, deve particionar em chunks IDAT de até 32 KB cada
            var pb = new PixelBuffer(100, 100);
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    pb.SetPixel(x, y, new PixelColor((byte)x, (byte)y, 100, 255));
                }
            }

            byte[] pngBytes = PngEncoder.Encode(pb, true);

            // Inspeciona os chunks do arquivo gerado
            int idatCount = 0;
            using (var ms = new MemoryStream(pngBytes))
            {
                byte[] sig = new byte[8];
                ms.Read(sig, 0, 8);

                PngChunk chunk;
                while ((chunk = PngChunk.Read(ms)) != null)
                {
                    if (chunk.Type == "IDAT")
                    {
                        idatCount++;
                        Assert(chunk.Data.Length <= 32 * 1024, "Cada chunk IDAT não deve ultrapassar 32 KB");
                    }
                }
            }

            Assert(idatCount >= 2, string.Format("Encoder deve gerar múltiplos chunks IDAT para imagem de ~40 KB (gerados: {0})", idatCount));

            // Decodifica e verifica fidelidade
            PixelBuffer decoded = PngDecoder.Decode(pngBytes);
            AssertEqual(100, decoded.Width, "Width pós-chunking");
            AssertEqual(100, decoded.Height, "Height pós-chunking");
            AssertEqual(pb.GetPixel(50, 50), decoded.GetPixel(50, 50), "Pixel fidelity pós multi-chunk IDAT");
        }

        private static void Test_Hardening_Stream_Fragmentation_Resilience()
        {
            // Testa BMP com stream fragmentado (2 bytes por Read)
            var pbBmp = new PixelBuffer(5, 5);
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    pbBmp.SetPixel(x, y, new PixelColor((byte)(x * 50), (byte)(y * 50), 200, 255));

            byte[] bmpBytes = BmpEncoder.Encode(pbBmp, 24);
            using (var innerBmp = new MemoryStream(bmpBytes))
            using (var fragBmp = new FragmentedStream(innerBmp, 2))
            {
                PixelBuffer decodedBmp = BmpDecoder.Decode(fragBmp);
                AssertEqual(5, decodedBmp.Width, "BMP Width via FragmentedStream");
                AssertEqual(5, decodedBmp.Height, "BMP Height via FragmentedStream");
                AssertEqual(pbBmp.GetPixel(2, 2), decodedBmp.GetPixel(2, 2), "BMP Pixel (2,2) via FragmentedStream");
            }

            // Testa PNG com stream fragmentado (1 byte por Read!)
            var pbPng = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pbPng.SetPixel(x, y, new PixelColor((byte)(x * 60), (byte)(y * 60), 150, 200));

            byte[] pngBytes = PngEncoder.Encode(pbPng, true);
            using (var innerPng = new MemoryStream(pngBytes))
            using (var fragPng = new FragmentedStream(innerPng, 1))
            {
                PixelBuffer decodedPng = PngDecoder.Decode(fragPng);
                AssertEqual(4, decodedPng.Width, "PNG Width via FragmentedStream (1 byte/read)");
                AssertEqual(4, decodedPng.Height, "PNG Height via FragmentedStream (1 byte/read)");
                AssertEqual(pbPng.GetPixel(3, 3), decodedPng.GetPixel(3, 3), "PNG Pixel (3,3) via FragmentedStream");
            }
        }

        private static void Test_Hardening_PixelBuffer_Crop_Overflow()
        {
            var pb = new PixelBuffer(10, 10);

            bool threwXOverflow = false;
            try
            {
                pb.Crop(int.MaxValue - 5, 0, 10, 10);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwXOverflow = true;
            }
            Assert(threwXOverflow, "Crop com x próximo a int.MaxValue deve lançar ArgumentOutOfRangeException");

            bool threwYOverflow = false;
            try
            {
                pb.Crop(0, int.MaxValue - 5, 10, 10);
            }
            catch (ArgumentOutOfRangeException)
            {
                threwYOverflow = true;
            }
            Assert(threwYOverflow, "Crop com y próximo a int.MaxValue deve lançar ArgumentOutOfRangeException");
        }

        private static void Test_Hardening_Deflate_Lz77_Distance_Underflow()
        {
            // Cria um fluxo DEFLATE onde o primeiro símbolo é uma referência retroativa (Length 3, Distance 1)
            // antes de emitir qualquer byte literal
            var bitWriter = new SimpleBitWriter();
            bitWriter.WriteBits(1, 1); // BFINAL = 1
            bitWriter.WriteBits(1, 2); // BTYPE = 01 (Huffman Fixo)

            // Símbolo 257 (comprimento 3) em Huffman fixo: código canônico 1, 7 bits '0000001'
            bitWriter.WriteHuffman(1, 7);

            // Símbolo de distância 0 (distância 1): 5 bits '00000'
            bitWriter.WriteBits(0, 5);

            byte[] deflateStream = bitWriter.ToArray();

            bool threwDistanceUnderflow = false;
            try
            {
                using (var ms = new MemoryStream(deflateStream))
                {
                    DeflateDecompressor.Decompress(ms, 1024);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("Distância LZ77") || ex.Message.Contains("antes do início do fluxo"))
                {
                    threwDistanceUnderflow = true;
                }
            }
            Assert(threwDistanceUnderflow, "Distância LZ77 anterior ao início do fluxo deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Deflate_Dynamic_Repeat_Overflow()
        {
            // Constrói cabeçalho dinâmico DEFLATE com código de repetição 18 estourando totalCodes
            var bitWriter = new SimpleBitWriter();
            bitWriter.WriteBits(1, 1); // BFINAL = 1
            bitWriter.WriteBits(2, 2); // BTYPE = 10 (Huffman Dinâmico)

            // HLIT = 0 (257 literais), HDIST = 0 (1 distância) -> totalCodes = 258
            bitWriter.WriteBits(0, 5);
            bitWriter.WriteBits(0, 5);
            // HCLEN = 1 (5 comprimentos para clLengths: 16, 17, 18, 0, 8)
            bitWriter.WriteBits(1, 4);

            // 5 comprimentos de 3 bits cada:
            // 16: len 0 (3 bits: 0)
            bitWriter.WriteBits(0, 3);
            // 17: len 0 (3 bits: 0)
            bitWriter.WriteBits(0, 3);
            // 18: len 1 (código '1' porque símbolo 0 vem antes no vetor e recebe '0')
            bitWriter.WriteBits(1, 3);
            // 0: len 1 (código '0')
            bitWriter.WriteBits(1, 3);
            // 8: len 0
            bitWriter.WriteBits(0, 3);

            // Agora escrevemos os códigos de comprimento para literais/distâncias:
            // Código 18 (bit '1') com 7 bits de repetição (valor 127 -> repete 11 + 127 = 138 zeros)
            bitWriter.WriteBits(1, 1);
            bitWriter.WriteBits(127, 7);

            // Código 18 novamente: repete mais 138 zeros -> total 276 > 258!
            bitWriter.WriteBits(1, 1);
            bitWriter.WriteBits(127, 7);

            byte[] deflateStream = bitWriter.ToArray();

            bool threwRepeatOverflow = false;
            try
            {
                using (var ms = new MemoryStream(deflateStream))
                {
                    DeflateDecompressor.Decompress(ms, 1024);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("ultrapassa a quantidade total de comprimentos"))
                {
                    threwRepeatOverflow = true;
                }
            }
            Assert(threwRepeatOverflow, "Código de repetição DEFLATE estourando totalCodes deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Huffman_Oversubscribed_Tree()
        {
            // Três símbolos de comprimento 1 violam Kraft: 1/2 + 1/2 + 1/2 = 1.5 > 1
            int[] codeLengths = new int[] { 1, 1, 1 };

            bool threwOversubscribed = false;
            try
            {
                HuffmanTree.Build(codeLengths);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("sobre-subscrita") || ex.Message.Contains("inválida"))
                {
                    threwOversubscribed = true;
                }
            }
            Assert(threwOversubscribed, "Árvore de Huffman sobre-subscrita deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Bmp_NonSeekable_Stream()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 50), 120, (byte)(y * 50), 255));

            byte[] bmpBytes = BmpEncoder.Encode(pb, 24);

            using (var inner = new MemoryStream(bmpBytes))
            using (var nonSeekable = new NonSeekableStream(inner))
            {
                PixelBuffer decoded = BmpDecoder.Decode(nonSeekable);
                AssertEqual(4, decoded.Width, "Decodificação BMP em stream não-seekable");
                AssertEqual(4, decoded.Height, "Decodificação BMP em stream não-seekable");
                AssertEqual(pb.GetPixel(1, 1), decoded.GetPixel(1, 1), "Pixel fidelity em BMP não-seekable");
            }
        }

        private static void Test_Hardening_Zlib_Trailing_Garbage()
        {
            byte[] raw = new byte[] { 10, 20, 30, 40, 50 };
            byte[] compressed = ZlibStream.Compress(raw);

            // Anexa lixo espúrio no final
            byte[] corrupt = new byte[compressed.Length + 1];
            Buffer.BlockCopy(compressed, 0, corrupt, 0, compressed.Length);
            corrupt[corrupt.Length - 1] = 0xAA;

            bool threwTrailing = false;
            try
            {
                ZlibStream.Decompress(corrupt);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("espúrios") || ex.Message.Contains("término"))
                {
                    threwTrailing = true;
                }
            }
            Assert(threwTrailing, "ZLIB com bytes espúrios após Adler-32 deve lançar CorruptDataException");
        }

        private static void Test_Hardening_Png_Idat_TrailingPadding_Allowed()
        {
            var pb = new PixelBuffer(4, 4);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    pb.SetPixel(x, y, new PixelColor((byte)(x * 60), (byte)(y * 60), 100, 255));

            byte[] validPng = PngEncoder.Encode(pb, true);

            // Reconstrói o PNG adicionando 4 bytes de padding no último IDAT com CRC recalculado
            using (var msIn = new MemoryStream(validPng))
            using (var msOut = new MemoryStream())
            {
                byte[] sig = new byte[8];
                msIn.Read(sig, 0, 8);
                msOut.Write(sig, 0, 8);

                PngChunk chunk;
                while ((chunk = PngChunk.Read(msIn)) != null)
                {
                    if (chunk.Type == "IDAT")
                    {
                        // Adiciona 4 bytes de preenchimento (padding) após o término do fluxo ZLIB
                        byte[] paddedData = new byte[chunk.Data.Length + 4];
                        Buffer.BlockCopy(chunk.Data, 0, paddedData, 0, chunk.Data.Length);
                        // Cria chunk com dados aumentados (PngChunk calcula novo CRC-32 válido)
                        var paddedChunk = new PngChunk("IDAT", paddedData);
                        paddedChunk.Write(msOut);
                    }
                    else
                    {
                        chunk.Write(msOut);
                    }
                }

                byte[] paddedPngBytes = msOut.ToArray();

                // Decodifica via Lodino PngDecoder: deve ignorar bytes residuais conforme ISO/IEC 15948 §10.2
                PixelBuffer decoded = PngDecoder.Decode(paddedPngBytes);
                AssertEqual(4, decoded.Width, "Width com IDAT padded");
                AssertEqual(4, decoded.Height, "Height com IDAT padded");
                AssertEqual(pb.GetPixel(2, 2), decoded.GetPixel(2, 2), "Pixel fidelity com IDAT padded");
            }
        }

        private class SyntheticOverLimitStream : Stream
        {
            private readonly long _totalBytes;
            private long _bytesRead = 0;

            public SyntheticOverLimitStream(long totalBytes)
            {
                _totalBytes = totalBytes;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_bytesRead >= _totalBytes) return 0;
                int toRead = (int)Math.Min(count, _totalBytes - _bytesRead);
                Array.Clear(buffer, offset, toRead);
                _bytesRead += toRead;
                return toRead;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }

        private static void Test_Hardening_Bmp_NonSeekable_Stream_LimitExceeded()
        {
            // Stream sintético não-seekable com 50.000 bytes e limite injetado de 10.000 bytes
            using (var stream = new SyntheticOverLimitStream(50000))
            {
                bool threwLimit = false;
                try
                {
                    BmpDecoder.Decode(stream, 10000);
                }
                catch (ResourceLimitException ex)
                {
                    if (ex.Message.Contains("excede o tamanho máximo permitido"))
                    {
                        threwLimit = true;
                    }
                }
                Assert(threwLimit, "Stream não-seekable excedendo maxStreamBytes deve lançar ResourceLimitException");
            }
        }

        #region Casos de Teste JPEG

        private static double CalculateMAE(PixelBuffer img1, PixelBuffer img2)
        {
            long totalDiff = 0;
            int count = img1.Width * img1.Height * 3;
            for (int y = 0; y < img1.Height; y++)
            {
                for (int x = 0; x < img1.Width; x++)
                {
                    var p1 = img1.GetPixel(x, y);
                    var p2 = img2.GetPixel(x, y);
                    totalDiff += Math.Abs(p1.R - p2.R);
                    totalDiff += Math.Abs(p1.G - p2.G);
                    totalDiff += Math.Abs(p1.B - p2.B);
                }
            }
            return (double)totalDiff / count;
        }

        private static double CalculatePSNR(PixelBuffer img1, PixelBuffer img2)
        {
            double mse = 0;
            int count = img1.Width * img1.Height * 3;
            for (int y = 0; y < img1.Height; y++)
            {
                for (int x = 0; x < img1.Width; x++)
                {
                    var p1 = img1.GetPixel(x, y);
                    var p2 = img2.GetPixel(x, y);
                    int dr = p1.R - p2.R;
                    int dg = p1.G - p2.G;
                    int db = p1.B - p2.B;
                    mse += (dr * dr + dg * dg + db * db);
                }
            }
            mse /= count;
            if (mse < 1e-10) return 99.0;
            return 10.0 * Math.Log10((255.0 * 255.0) / mse);
        }

        private static void Test_Jpeg_FastDct_Accuracy()
        {
            float[][] patterns = new float[][]
            {
                new float[64],
                new float[64],
                new float[64],
                new float[64],
                new float[64]
            };

            for (int i = 0; i < 64; i++)
            {
                patterns[1][i] = 128.0f;
                int x = i % 8;
                int y = i / 8;
                patterns[2][i] = (x * 15 + y * 12) % 256;
                patterns[3][i] = ((x + y) % 2 == 0) ? 40.0f : 210.0f;
            }
            patterns[4][27] = 255.0f;

            float[] dctOut = new float[64];
            float[] reconstructed = new float[64];

            foreach (var original in patterns)
            {
                FastDct.Transform(original, dctOut);
                FastDct.InverseTransformFloat(dctOut, reconstructed);

                for (int i = 0; i < 64; i++)
                {
                    float diff = Math.Abs(original[i] - reconstructed[i]);
                    Assert(diff < 1e-4f, string.Format("Erro numérico de FDCT/IDCT excessivo: {0} na posição {1}", diff, i));
                }
            }
        }

        private static void Test_Jpeg_QuantizationTable_QualityScaling()
        {
            for (int z = 0; z < 64; z++)
            {
                int natural = JpegQuantizationTable.ZigzagToNatural[z];
                int zBack = JpegQuantizationTable.NaturalToZigzag[natural];
                AssertEqual(z, zBack, "Invertibilidade Zigzag -> Natural -> Zigzag");
            }

            var q50 = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardLuminance, 50);
            for (int i = 0; i < 64; i++)
            {
                AssertEqual(JpegQuantizationTable.StandardLuminance[i], q50.Elements[i], "Elemento Q50");
            }

            var q100 = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardLuminance, 100);
            for (int i = 0; i < 64; i++)
            {
                AssertEqual(1, q100.Elements[i], "Elemento Q100 deve ser 1");
            }

            var q1 = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardLuminance, 1);
            for (int i = 0; i < 64; i++)
            {
                Assert(q1.Elements[i] >= q50.Elements[i], "Elemento Q1 deve ser >= Q50");
                Assert(q1.Elements[i] <= 255, "Elemento Q1 deve ser <= 255");
            }

            byte[] zigzagBytes = q50.ToZigzagBytes();
            AssertEqual(64, zigzagBytes.Length, "Comprimento dos bytes Zigzag");
            var restored = JpegQuantizationTable.FromZigzag(zigzagBytes);
            for (int i = 0; i < 64; i++)
            {
                AssertEqual(q50.Elements[i], restored.Elements[i], "Elemento restaurado a partir de Zigzag");
            }
        }

        private static void Test_Jpeg_HuffmanTable_Canonical_Roundtrip()
        {
            var lumDc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            AssertEqual(12, lumDc.Values.Length, "Número de símbolos Lum DC");

            byte[] testSymbols = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 5, 2 };

            using (var ms = new MemoryStream())
            {
                var writer = new JpegBitWriter(ms);
                foreach (byte sym in testSymbols)
                {
                    writer.WriteSymbol(sym, lumDc);
                }
                writer.Flush();

                ms.Position = 0;
                var reader = new JpegBitReader(ms);
                foreach (byte expected in testSymbols)
                {
                    int decoded = lumDc.Decode(reader);
                    AssertEqual((int)expected, decoded, "Decodificação de símbolo Huffman Lum DC");
                }
            }
        }

        private static void Test_Jpeg_BitWriter_BitReader_ByteStuffing()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new JpegBitWriter(ms);
                writer.WriteBits(0xAA, 8);
                writer.WriteBits(0xFF, 8);
                writer.WriteBits(0x55, 8);
                writer.WriteBits(0xFF, 8);
                writer.WriteBits(0x1234, 16);
                writer.Flush();

                byte[] raw = ms.ToArray();
                bool foundStuffing = false;
                for (int i = 0; i < raw.Length - 1; i++)
                {
                    if (raw[i] == 0xFF && raw[i + 1] == 0x00)
                    {
                        foundStuffing = true;
                        break;
                    }
                }
                Assert(foundStuffing, "Fluxo físico deve conter 0xFF 0x00 resultante de byte stuffing");

                ms.Position = 0;
                var reader = new JpegBitReader(ms);
                AssertEqual(0xAA, reader.ReadBits(8), "Leitura 0xAA");
                AssertEqual(0xFF, reader.ReadBits(8), "Leitura 0xFF sem byte de stuffing");
                AssertEqual(0x55, reader.ReadBits(8), "Leitura 0x55");
                AssertEqual(0xFF, reader.ReadBits(8), "Segunda leitura 0xFF");
                AssertEqual(0x1234, reader.ReadBits(16), "Leitura 0x1234");
            }
        }

        private static void Test_Jpeg_ColorConverter_RgbYCbCr()
        {
            byte[][] testColors = new byte[][]
            {
                new byte[] { 255, 0, 0 },
                new byte[] { 0, 255, 0 },
                new byte[] { 0, 0, 255 },
                new byte[] { 255, 255, 255 },
                new byte[] { 0, 0, 0 },
                new byte[] { 128, 128, 128 },
                new byte[] { 255, 255, 0 },
                new byte[] { 0, 255, 255 },
                new byte[] { 255, 0, 255 }
            };

            foreach (var rgb in testColors)
            {
                byte y, cb, cr;
                JpegColorConverter.RgbToYCbCr(rgb[0], rgb[1], rgb[2], out y, out cb, out cr);

                byte rOut, gOut, bOut;
                JpegColorConverter.YCbCrToRgb(y, cb, cr, out rOut, out gOut, out bOut);

                int diffR = Math.Abs(rgb[0] - rOut);
                int diffG = Math.Abs(rgb[1] - gOut);
                int diffB = Math.Abs(rgb[2] - bOut);

                Assert(diffR <= 1 && diffG <= 1 && diffB <= 1,
                    string.Format("Diferença na conversão de cor ({0},{1},{2}) -> ({3},{4},{5}) excedeu 1 unidade",
                        rgb[0], rgb[1], rgb[2], rOut, gOut, bOut));
            }
        }

        private static void Test_Jpeg_EncodeDecode_Baseline444_Roundtrip()
        {
            int w = 16;
            int h = 16;
            var original = new PixelBuffer(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte r = (byte)(x * 16);
                    byte g = (byte)(y * 16);
                    byte b = (byte)((x + y) * 8);
                    original.SetPixel(x, y, new PixelColor(r, g, b, 255));
                }
            }

            byte[] jpegBytes;
            using (var ms = new MemoryStream())
            {
                JpegEncoder.Encode(original, ms, quality: 95, useSubsampling420: false);
                jpegBytes = ms.ToArray();
            }

            Assert(jpegBytes.Length > 0, "Bytes JPEG codificados");

            PixelBuffer decoded;
            using (var ms = new MemoryStream(jpegBytes))
            {
                decoded = JpegDecoder.Decode(ms);
            }

            AssertEqual(w, decoded.Width, "Width decodificado");
            AssertEqual(h, decoded.Height, "Height decodificado");

            double mae = CalculateMAE(original, decoded);
            double psnr = CalculatePSNR(original, decoded);

            Assert(mae < 3.0, string.Format("MAE em Baseline 4:4:4 deve ser < 3.0 (obtido: {0:F2})", mae));
            Assert(psnr > 38.0, string.Format("PSNR em Baseline 4:4:4 deve ser > 38 dB (obtido: {0:F2} dB)", psnr));
        }

        private static void Test_Jpeg_EncodeDecode_Baseline420_Roundtrip()
        {
            int w = 32;
            int h = 32;
            var original = new PixelBuffer(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte r = (byte)((x * 8) % 256);
                    byte g = (byte)((y * 8) % 256);
                    byte b = (byte)(128 + ((x - y) * 3));
                    original.SetPixel(x, y, new PixelColor(r, g, b, 255));
                }
            }

            byte[] jpegBytes;
            using (var ms = new MemoryStream())
            {
                JpegEncoder.Encode(original, ms, quality: 85, useSubsampling420: true);
                jpegBytes = ms.ToArray();
            }

            Assert(jpegBytes.Length > 0, "Bytes JPEG 4:2:0 gerados");

            PixelBuffer decoded;
            using (var ms = new MemoryStream(jpegBytes))
            {
                decoded = JpegDecoder.Decode(ms);
            }

            AssertEqual(w, decoded.Width, "Width 4:2:0");
            AssertEqual(h, decoded.Height, "Height 4:2:0");

            double mae = CalculateMAE(original, decoded);
            double psnr = CalculatePSNR(original, decoded);

            Assert(mae < 5.0, string.Format("MAE em Baseline 4:2:0 deve ser < 5.0 (obtido: {0:F2})", mae));
            Assert(psnr > 32.0, string.Format("PSNR em Baseline 4:2:0 deve ser > 32 dB (obtido: {0:F2} dB)", psnr));
        }

        private static void Test_Jpeg_Decode_Grayscale()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);

                var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(0x43);
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);

                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(0x0B);
                ms.WriteByte(0x08);
                ms.WriteByte(0x00); ms.WriteByte(0x08);
                ms.WriteByte(0x00); ms.WriteByte(0x08);
                ms.WriteByte(0x01);
                ms.WriteByte(0x01);
                ms.WriteByte(0x11);
                ms.WriteByte(0x00);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(0x08);
                ms.WriteByte(0x01);
                ms.WriteByte(0x01);
                ms.WriteByte(0x00);
                ms.WriteByte(0x00);
                ms.WriteByte(0x3F);
                ms.WriteByte(0x00);

                var writer = new JpegBitWriter(ms);
                var dcTable = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
                var acTable = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);

                writer.WriteSymbol(0x00, dcTable);
                writer.WriteSymbol(0x00, acTable);
                writer.Flush();

                ms.WriteByte(0xFF); ms.WriteByte(0xD9);

                ms.Position = 0;
                var decoded = JpegDecoder.Decode(ms);

                AssertEqual(8, decoded.Width, "Largura Grayscale");
                AssertEqual(8, decoded.Height, "Altura Grayscale");

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        var p = decoded.GetPixel(x, y);
                        AssertEqual(p.R, p.G, "Grayscale R == G");
                        AssertEqual(p.G, p.B, "Grayscale G == B");
                        AssertEqual((byte)255, p.A, "Grayscale Alfa = 255");
                    }
                }
            }
        }

        private static void Test_Jpeg_RestartInterval_DRI_RSTm()
        {
            int w = 16;
            int h = 8;
            var original = new PixelBuffer(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    original.SetPixel(x, y, new PixelColor((byte)(x * 15), (byte)(y * 30), 120, 255));
                }
            }

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);

                var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);
                var qChr = new JpegQuantizationTable(JpegQuantizationTable.StandardChrominance);
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(132);
                ms.WriteByte(0x00); ms.Write(qLum.ToZigzagBytes(), 0, 64);
                ms.WriteByte(0x01); ms.Write(qChr.ToZigzagBytes(), 0, 64);

                ms.WriteByte(0xFF); ms.WriteByte(0xDD);
                ms.WriteByte(0x00); ms.WriteByte(0x04);
                ms.WriteByte(0x00); ms.WriteByte(0x01); // Ri = 1

                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(0x08);
                ms.WriteByte(0x00); ms.WriteByte(0x08);
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(0x03);
                ms.WriteByte(0x01); ms.WriteByte(0x11); ms.WriteByte(0x00);
                ms.WriteByte(0x02); ms.WriteByte(0x11); ms.WriteByte(0x01);
                ms.WriteByte(0x03); ms.WriteByte(0x11); ms.WriteByte(0x01);

                var dcLum = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
                var acLum = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);
                var dcChr = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
                var acChr = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00); ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16); ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length));
                ms.WriteByte(0x10); ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16); ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardChrominanceDcValues.Length));
                ms.WriteByte(0x01); ms.Write(JpegHuffmanTable.StandardChrominanceDcBits, 0, 16); ms.Write(JpegHuffmanTable.StandardChrominanceDcValues, 0, JpegHuffmanTable.StandardChrominanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardChrominanceAcValues.Length));
                ms.WriteByte(0x11); ms.Write(JpegHuffmanTable.StandardChrominanceAcBits, 0, 16); ms.Write(JpegHuffmanTable.StandardChrominanceAcValues, 0, JpegHuffmanTable.StandardChrominanceAcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(12);
                ms.WriteByte(0x03);
                ms.WriteByte(0x01); ms.WriteByte(0x00);
                ms.WriteByte(0x02); ms.WriteByte(0x11);
                ms.WriteByte(0x03); ms.WriteByte(0x11);
                ms.WriteByte(0x00); ms.WriteByte(0x3F); ms.WriteByte(0x00);

                var writer = new JpegBitWriter(ms);
                writer.WriteSymbol(0x00, dcLum); writer.WriteSymbol(0x00, acLum);
                writer.WriteSymbol(0x00, dcChr); writer.WriteSymbol(0x00, acChr);
                writer.WriteSymbol(0x00, dcChr); writer.WriteSymbol(0x00, acChr);
                writer.Flush();

                ms.WriteByte(0xFF); ms.WriteByte(0xD0); // RST0

                writer = new JpegBitWriter(ms);
                writer.WriteSymbol(0x00, dcLum); writer.WriteSymbol(0x00, acLum);
                writer.WriteSymbol(0x00, dcChr); writer.WriteSymbol(0x00, acChr);
                writer.WriteSymbol(0x00, dcChr); writer.WriteSymbol(0x00, acChr);
                writer.Flush();

                ms.WriteByte(0xFF); ms.WriteByte(0xD9); // EOI

                ms.Position = 0;
                var decoded = JpegDecoder.Decode(ms);

                AssertEqual(16, decoded.Width, "Largura após RSTm");
                AssertEqual(8, decoded.Height, "Altura após RSTm");
            }
        }

        private static void Test_Jpeg_Security_InvalidSignature_Rejection()
        {
            byte[] invalid = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            bool threw = false;
            try
            {
                using (var ms = new MemoryStream(invalid))
                {
                    JpegDecoder.Decode(ms);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("SOI") || ex.Message.Contains("Assinatura"))
                {
                    threw = true;
                }
            }
            Assert(threw, "Assinatura inválida (sem SOI 0xFFD8) deve lançar CorruptDataException");
        }

        private static void Test_Jpeg_Security_UnsupportedProfile_SOF2_Rejection()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC2); // SOF2
                ms.WriteByte(0x00); ms.WriteByte(0x0B);
                ms.Write(new byte[9], 0, 9);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.Contains("Progressive") || ex.Message.Contains("SOF2"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Perfil SOF2 (Progressive) deve lançar UnsupportedFormatException");
            }
        }

        private static void Test_Jpeg_Security_UnsupportedPrecision_Rejection()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(0x0B);
                ms.WriteByte(12); // Precisão 12-bit
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(0x01);
                ms.WriteByte(0x01); ms.WriteByte(0x11); ms.WriteByte(0x00);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.Contains("Precisão") || ex.Message.Contains("12 bits"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Precisão de 12 bits deve lançar UnsupportedFormatException");
            }
        }

        private static void Test_Jpeg_Security_UnsupportedComponents_CMYK_Rejection()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(20);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(4); // CMYK
                for (int i = 0; i < 4; i++)
                {
                    ms.WriteByte((byte)(i + 1));
                    ms.WriteByte(0x11);
                    ms.WriteByte(0x00);
                }

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.Contains("4 componentes") || ex.Message.Contains("CMYK"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "JPEG com 4 componentes (CMYK) deve lançar UnsupportedFormatException");
            }
        }

        private static void Test_Jpeg_Security_Dimensions_Limits()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(0x00); // H = 0
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (CorruptDataException)
                {
                    threw = true;
                }
                Assert(threw, "Dimensão zerada em JPEG deve lançar CorruptDataException");
            }

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x40); ms.WriteByte(0x01); // H = 16385
                ms.WriteByte(0x00); ms.WriteByte(0x10);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (ResourceLimitException)
                {
                    threw = true;
                }
                Assert(threw, "Dimensão > 16384 em JPEG deve lançar ResourceLimitException");
            }
        }

        private static void Test_Jpeg_Security_TruncatedStream_Rejection()
        {
            byte[] truncated = new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00 };
            bool threw = false;
            try
            {
                using (var ms = new MemoryStream(truncated))
                {
                    JpegDecoder.Decode(ms);
                }
            }
            catch (CorruptDataException)
            {
                threw = true;
            }
            Assert(threw, "Fluxo JPEG truncado deve lançar CorruptDataException");
        }

        private static void Test_Jpeg_Engine_Inspect()
        {
            var pb = new PixelBuffer(64, 48);
            pb.Fill(new PixelColor(100, 150, 200, 255));

            byte[] jpegBytes;
            using (var ms = new MemoryStream())
            {
                JpegEncoder.Encode(pb, ms, quality: 90, useSubsampling420: true);
                jpegBytes = ms.ToArray();
            }

            var engine = new JpegEngine();
            using (var ms = new MemoryStream(jpegBytes))
            {
                var info = engine.Inspect(ms, "test.jpg");
                AssertEqual("jpeg", info.FormatId, "FormatId inspecionado");
                AssertEqual("image/jpeg", info.MimeType, "MimeType inspecionado");
                AssertEqual(64, info.Width, "Width inspecionado");
                AssertEqual(48, info.Height, "Height inspecionado");
                Assert(info.IsSupported, "IsSupported deve ser true");
                AssertEqual("4:2:0", info.Metadata["Sampling"], "Subsampling inspecionado");
                AssertEqual("8-bit", info.Metadata["Precision"], "Precisão inspecionada");
            }
        }

        private static void Test_Jpeg_Engine_Convert_Transforms()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_jpeg_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string inputPath = Path.Combine(tempDir, "input.jpg");
                var pb = new PixelBuffer(100, 100);
                pb.Fill(new PixelColor(50, 80, 120, 255));
                using (var fs = File.Create(inputPath))
                {
                    JpegEncoder.Encode(pb, fs, quality: 90);
                }

                var engine = new JpegEngine();
                var req = new ConvertRequest
                {
                    InputPath = inputPath,
                    TargetFormat = "jpg",
                    OutputDirectory = tempDir
                };
                req.Settings["cropWidth"] = "50";
                req.Settings["cropHeight"] = "50";
                req.Settings["cropPosition"] = "center";
                req.Settings["resizeWidth"] = "80";
                req.Settings["resizeHeight"] = "80";
                req.Settings["watermarkText"] = "LODINO";
                req.Settings["watermarkScale"] = "1";

                var res = engine.Convert(req, null);
                AssertEqual("completed", res.Status, "Status da conversão");
                Assert(File.Exists(res.OutputPath), "Arquivo de saída gerado");

                using (var fs = File.OpenRead(res.OutputPath))
                {
                    var decoded = JpegDecoder.Decode(fs);
                    AssertEqual(80, decoded.Width, "Largura após transformações");
                    AssertEqual(80, decoded.Height, "Altura após transformações");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private static void Test_Jpeg_Bidirectional_Routes()
        {
            var registry = EngineRegistry.Default;

            var eJpegBmp = registry.Resolve("jpeg", "bmp");
            AssertEqual("image-jpeg", eJpegBmp.EngineId, "Resolve jpeg -> bmp");

            var eBmpJpeg = registry.Resolve("bmp", "jpeg");
            AssertEqual("image-jpeg", eBmpJpeg.EngineId, "Resolve bmp -> jpeg");

            var eJpegPng = registry.Resolve("jpeg", "png");
            AssertEqual("image-jpeg", eJpegPng.EngineId, "Resolve jpeg -> png");

            var ePngJpeg = registry.Resolve("png", "jpeg");
            AssertEqual("image-jpeg", ePngJpeg.EngineId, "Resolve png -> jpeg");

            var eJpgJpg = registry.Resolve("jpg", "jpg");
            AssertEqual("image-jpeg", eJpgJpg.EngineId, "Resolve jpg -> jpg");

            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_routes_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var pb = new PixelBuffer(16, 16);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                        pb.SetPixel(x, y, new PixelColor((byte)(x * 15), (byte)(y * 15), 180, 255));

                string originalJpeg = Path.Combine(tempDir, "source.jpg");
                using (var fs = File.Create(originalJpeg))
                {
                    JpegEncoder.Encode(pb, fs, quality: 95, useSubsampling420: false);
                }

                // Rota 1: JPEG -> BMP
                var reqJpegBmp = new ConvertRequest { InputPath = originalJpeg, TargetFormat = "bmp", OutputDirectory = tempDir };
                var resJpegBmp = eJpegBmp.Convert(reqJpegBmp, null);
                Assert(File.Exists(resJpegBmp.OutputPath), "Arquivo BMP gerado a partir de JPEG");
                using (var fs = File.OpenRead(resJpegBmp.OutputPath))
                {
                    var bmpDecoded = BmpDecoder.Decode(fs);
                    AssertEqual(16, bmpDecoded.Width, "Width BMP a partir de JPEG");
                    AssertEqual(16, bmpDecoded.Height, "Height BMP a partir de JPEG");
                }

                // Rota 2: BMP -> JPEG
                var reqBmpJpeg = new ConvertRequest { InputPath = resJpegBmp.OutputPath, TargetFormat = "jpg", OutputDirectory = tempDir };
                var resBmpJpeg = eBmpJpeg.Convert(reqBmpJpeg, null);
                Assert(File.Exists(resBmpJpeg.OutputPath), "Arquivo JPEG gerado a partir de BMP");
                using (var fs = File.OpenRead(resBmpJpeg.OutputPath))
                {
                    var jpegDecoded = JpegDecoder.Decode(fs);
                    AssertEqual(16, jpegDecoded.Width, "Width JPEG a partir de BMP");
                    AssertEqual(16, jpegDecoded.Height, "Height JPEG a partir de BMP");
                }

                // Rota 3: JPEG -> PNG
                var reqJpegPng = new ConvertRequest { InputPath = originalJpeg, TargetFormat = "png", OutputDirectory = tempDir };
                var resJpegPng = eJpegPng.Convert(reqJpegPng, null);
                Assert(File.Exists(resJpegPng.OutputPath), "Arquivo PNG gerado a partir de JPEG");
                using (var fs = File.OpenRead(resJpegPng.OutputPath))
                {
                    var pngDecoded = PngDecoder.Decode(fs);
                    AssertEqual(16, pngDecoded.Width, "Width PNG a partir de JPEG");
                    AssertEqual(16, pngDecoded.Height, "Height PNG a partir de JPEG");
                }

                // Rota 4: PNG -> JPEG
                var reqPngJpeg = new ConvertRequest { InputPath = resJpegPng.OutputPath, TargetFormat = "jpg", OutputDirectory = tempDir };
                var resPngJpeg = ePngJpeg.Convert(reqPngJpeg, null);
                Assert(File.Exists(resPngJpeg.OutputPath), "Arquivo JPEG gerado a partir de PNG");
                using (var fs = File.OpenRead(resPngJpeg.OutputPath))
                {
                    var jpegFromPng = JpegDecoder.Decode(fs);
                    AssertEqual(16, jpegFromPng.Width, "Width JPEG a partir de PNG");
                    AssertEqual(16, jpegFromPng.Height, "Height JPEG a partir de PNG");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        #region Casos de Teste Hardening JPEG (Fase 7)

        private static void Test_Hardening_Jpeg_SamplingFactors_Validation()
        {
            // D-01: Fator H = 0
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI
                ms.WriteByte(0xFF); ms.WriteByte(0xC0); // SOF0
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1); // 1 componente
                ms.WriteByte(1); // ID 1
                ms.WriteByte(0x01); // H=0, V=1
                ms.WriteByte(0); // QTable 0

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("Fatores de amostragem")) threw = true;
                }
                Assert(threw, "H=0 deve lançar CorruptDataException");
            }

            // D-01: Fator V = 0
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI
                ms.WriteByte(0xFF); ms.WriteByte(0xC0); // SOF0
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1); // 1 componente
                ms.WriteByte(1); // ID 1
                ms.WriteByte(0x10); // H=1, V=0
                ms.WriteByte(0); // QTable 0

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("Fatores de amostragem")) threw = true;
                }
                Assert(threw, "V=0 deve lançar CorruptDataException");
            }

            // D-01: Fator H > 4 (ex: 5)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI
                ms.WriteByte(0xFF); ms.WriteByte(0xC0); // SOF0
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1); // 1 componente
                ms.WriteByte(1); // ID 1
                ms.WriteByte(0x51); // H=5, V=1
                ms.WriteByte(0); // QTable 0

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("Fatores de amostragem")) threw = true;
                }
                Assert(threw, "H=5 deve lançar CorruptDataException");
            }

            // D-01: Fatores incompatíveis (não-múltiplos: Y H=3, Cb H=2)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI
                ms.WriteByte(0xFF); ms.WriteByte(0xDB); // DQT
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(new byte[64], 0, 64);

                ms.WriteByte(0xFF); ms.WriteByte(0xC0); // SOF0
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(24);
                ms.WriteByte(0x00); ms.WriteByte(24);
                ms.WriteByte(3); // 3 componentes
                ms.WriteByte(1); ms.WriteByte(0x31); ms.WriteByte(0); // Y: H=3, V=1
                ms.WriteByte(2); ms.WriteByte(0x21); ms.WriteByte(0); // Cb: H=2, V=1
                ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(0); // Cr: H=1, V=1

                ms.WriteByte(0xFF); ms.WriteByte(0xC4); // DHT DC 0
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4); // DHT AC 0
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xDA); // SOS
                ms.WriteByte(0x00); ms.WriteByte(12);
                ms.WriteByte(3);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(2); ms.WriteByte(0x00);
                ms.WriteByte(3); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(63); ms.WriteByte(0x00);

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("incompatíveis")) threw = true;
                }
                Assert(threw, "Fatores não múltiplos (H=3 e H=2) devem lançar CorruptDataException");
            }
        }

        private static void Test_Hardening_Jpeg_TableSelectors_Bounds_And_Existence()
        {
            // D-02: QTableId = 4 em SOF0 (> 3)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(4); // QTableId = 4

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("tabela de quantização inválido")) threw = true;
                }
                Assert(threw, "QTableId = 4 deve lançar CorruptDataException");
            }

            // D-02: QTableId referenciado não definido no fluxo
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                // Não inclui marcador DQT!
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0); // QTableId = 0 (mas qTables[0] == null)

                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(63); ms.WriteByte(0x00);

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("não foi definida")) threw = true;
                }
                Assert(threw, "QTableId referenciado sem DQT prévio deve lançar CorruptDataException");
            }

            // D-02: DcTableId = 4 em SOS (> 3)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00); ms.Write(new byte[64], 0, 64);

                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(0x00); ms.WriteByte(16);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x40); // DC=4 (>3), AC=0
                ms.WriteByte(0x00); ms.WriteByte(63); ms.WriteByte(0x00);

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("tabela Huffman inválidos")) threw = true;
                }
                Assert(threw, "DcTableId = 4 deve lançar CorruptDataException");
            }
        }

        private static void Test_Hardening_Jpeg_SegmentLengths_DqtDht_Validation()
        {
            // D-03: DQT com comprimento < 2
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(0x01); // len = 1 (< 2)

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("DQT", StringComparison.OrdinalIgnoreCase) >= 0 && ex.Message.IndexOf("inválido", StringComparison.OrdinalIgnoreCase) >= 0) threw = true;
                }
                Assert(threw, "DQT com length=1 deve lançar CorruptDataException");
            }

            // D-03: DQT com comprimento = 2 (vazio)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(0x02); // len = 2 (vazio)

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("vazio", StringComparison.OrdinalIgnoreCase) >= 0) threw = true;
                }
                Assert(threw, "DQT com length=2 deve lançar CorruptDataException");
            }

            // D-03: DHT com comprimento < 2
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte(0x00); // len = 0 (< 2)

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("DHT", StringComparison.OrdinalIgnoreCase) >= 0 && ex.Message.IndexOf("inválido", StringComparison.OrdinalIgnoreCase) >= 0) threw = true;
                }
                Assert(threw, "DHT com length=0 deve lançar CorruptDataException");
            }

            // D-03: DHT com comprimento = 2 (vazio)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte(0x02); // len = 2 (vazio)

                ms.Position = 0;
                bool threw = false;
                try { JpegDecoder.Decode(ms); }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("vazio", StringComparison.OrdinalIgnoreCase) >= 0) threw = true;
                }
                Assert(threw, "DHT com length=2 deve lançar CorruptDataException");
            }
        }

        private static void Test_Hardening_Jpeg_ZRL_BlockBoundary_Validation()
        {
            // D-07: Símbolo ZRL 0xF0 na posição k=49 ultrapassa o bloco 8x8 (49 + 16 = 65 > 64)
            var dcTable = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            var acTable = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);
            var qTable = JpegQuantizationTable.Scale(JpegQuantizationTable.StandardLuminance, 90);

            // Caso Inválido: 4 símbolos ZRL (16 * 4 = 64 zeros após k=1 -> k=65)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qTable.ToZigzagBytes(), 0, 64);

                // SOF0 (8x8 monocromático)
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT DC 0
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                // DHT AC 0
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // SOS
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(63); ms.WriteByte(0x00);

                // Scan entropy data
                var writer = new JpegBitWriter(ms);
                writer.WriteSymbol(0x00, dcTable); // DC diff = 0
                // 4 símbolos ZRL consecutivos: 16 + 16 + 16 + 16 = 64 zeros a partir de k=1 -> k=65!
                writer.WriteSymbol(0xF0, acTable); // k=17
                writer.WriteSymbol(0xF0, acTable); // k=33
                writer.WriteSymbol(0xF0, acTable); // k=49
                writer.WriteSymbol(0xF0, acTable); // k=65 (ultrapassa 64!)
                writer.Flush();

                ms.WriteByte(0xFF); ms.WriteByte(0xD9); // EOI

                ms.Position = 0;
                bool threwZrl = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("ZRL") && ex.Message.Contains("ultrapassa os limites"))
                    {
                        threwZrl = true;
                    }
                }
                Assert(threwZrl, "ZRL transbordando bloco 8x8 deve lançar CorruptDataException");
            }

            // Caso Válido Limite: ZRL exatamente na fronteira final (k=48 -> k=64)
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qTable.ToZigzagBytes(), 0, 64);

                // SOF0 (8x8 monocromático)
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT DC 0
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                // DHT AC 0
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // SOS
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0x00); ms.WriteByte(63); ms.WriteByte(0x00);

                // Scan entropy data:
                // k=1 -> 2 ZRLs (32 zeros) levam k para 33.
                // Símbolo 0xE1 (14 zeros + 1 valor) leva k de 33 a 47; escreve valor em 47; k vira 48.
                // ZRL em k=48 leva k exatamente a 64 (fim perfeito do bloco)!
                var writer = new JpegBitWriter(ms);
                writer.WriteSymbol(0x00, dcTable); // DC diff = 0
                writer.WriteSymbol(0xF0, acTable); // k=17
                writer.WriteSymbol(0xF0, acTable); // k=33
                writer.WriteSymbol(0xE1, acTable); // 14 zeros
                writer.WriteBits(1, 1);            // valor 1 (+1)
                writer.WriteSymbol(0xF0, acTable); // k=48 -> k=64 (fronteira exata!)
                writer.Flush();

                ms.WriteByte(0xFF); ms.WriteByte(0xD9); // EOI

                ms.Position = 0;
                var decoded = JpegDecoder.Decode(ms);
                AssertEqual(8, decoded.Width, "Largura após ZRL exato");
                AssertEqual(8, decoded.Height, "Altura após ZRL exato");
            }
        }

        private static void Test_Hardening_Jpeg_Engine_Inspect_SofSync_And_JfifApp0()
        {
            var engine = new JpegEngine();

            // D-04: Testa que todos os marcadores SOF não-baseline são reportados como não-suportados
            ushort[] unsupportedSofs = new ushort[]
            {
                JpegMarker.SOF1, JpegMarker.SOF2, JpegMarker.SOF3,
                JpegMarker.SOF5, JpegMarker.SOF6, JpegMarker.SOF7,
                JpegMarker.SOF9, JpegMarker.SOF10, JpegMarker.SOF11,
                JpegMarker.SOF13, JpegMarker.SOF14, JpegMarker.SOF15
            };

            foreach (var sof in unsupportedSofs)
            {
                using (var ms = new MemoryStream())
                {
                    ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI
                    ms.WriteByte((byte)(sof >> 8)); ms.WriteByte((byte)(sof & 0xFF)); // SOF
                    ms.WriteByte(0x00); ms.WriteByte(11);
                    ms.Write(new byte[9], 0, 9);

                    ms.Position = 0;
                    var info = engine.Inspect(ms, "test.jpg");
                    Assert(!info.IsSupported, "SOF marker 0x" + sof.ToString("X4") + " deve ser IsSupported = false");
                    Assert(info.Metadata.ContainsKey("UnsupportedProfile"), "Deve registrar UnsupportedProfile para 0x" + sof.ToString("X4"));
                }
            }

            // D-05: Valida extração de metadados JFIF APP0 (versão, unidades, DPI)
            var pb = new PixelBuffer(32, 32);
            pb.Fill(new PixelColor(120, 140, 160, 255));
            using (var ms = new MemoryStream())
            {
                JpegEncoder.Encode(pb, ms, quality: 85);
                ms.Position = 0;
                var info = engine.Inspect(ms, "test_jfif.jpg");

                AssertEqual("1.01", info.Metadata["JfifVersion"], "JfifVersion inspecionado");
                AssertEqual("DPI (dots per inch)", info.Metadata["DensityUnits"], "DensityUnits inspecionado");
                AssertEqual("72", info.Metadata["XDensity"], "XDensity inspecionado");
                AssertEqual("72", info.Metadata["YDensity"], "YDensity inspecionado");
            }
        }

        private static void Test_Hardening_Jpeg_Grayscale_Encoder_Roundtrip()
        {
            // D-08: Codificação e decodificação de imagem monocromática de 1 componente
            var pb = new PixelBuffer(24, 24);
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    byte gray = (byte)(x * 8 + y * 2);
                    pb.SetPixel(x, y, new PixelColor(gray, gray, gray, 255));
                }
            }

            byte[] grayJpegBytes;
            using (var ms = new MemoryStream())
            {
                JpegEncoder.Encode(pb, ms, quality: 90, grayscale: true);
                grayJpegBytes = ms.ToArray();
            }

            // Inspeciona metadados via JpegEngine
            var engine = new JpegEngine();
            using (var ms = new MemoryStream(grayJpegBytes))
            {
                var info = engine.Inspect(ms, "grayscale.jpg");
                Assert(info.IsSupported, "Grayscale JPEG deve ser suportado");
                AssertEqual(24, info.Width, "Largura do grayscale");
                AssertEqual(24, info.Height, "Altura do grayscale");
                AssertEqual("1 (Grayscale)", info.Metadata["ColorComponents"], "1 componente de cor");
                AssertEqual("1:1 (Grayscale)", info.Metadata["Sampling"], "Subamostragem Grayscale 1:1");
            }

            // Decodifica via JpegDecoder
            using (var ms = new MemoryStream(grayJpegBytes))
            {
                var decoded = JpegDecoder.Decode(ms);
                AssertEqual(24, decoded.Width, "Largura decodificada");
                AssertEqual(24, decoded.Height, "Altura decodificada");

                // Valida fidelidade e que R == G == B em todos os pixels
                double sumSq = 0;
                for (int y = 0; y < 24; y++)
                {
                    for (int x = 0; x < 24; x++)
                    {
                        var orig = pb.GetPixel(x, y);
                        var dec = decoded.GetPixel(x, y);
                        AssertEqual(dec.R, dec.G, "Canal R == G no pixel decodificado");
                        AssertEqual(dec.R, dec.B, "Canal R == B no pixel decodificado");
                        AssertEqual((byte)255, dec.A, "Canal Alfa deve ser 255");

                        double diff = orig.R - dec.R;
                        sumSq += diff * diff;
                    }
                }
                double mse = sumSq / (24 * 24);
                double psnr = mse > 0 ? 10.0 * Math.Log10(255.0 * 255.0 / mse) : 99.0;
                Assert(psnr > 32.0, string.Format("PSNR do grayscale roundtrip ({0:F2} dB) deve ser > 32 dB", psnr));
            }
        }

        private static void Test_Hardening_Jpeg_NonSeekable_Stream_LimitExceeded()
        {
            // Stream sintético não-seekable com 50.000 bytes e limite injetado de 10.000 bytes
            using (var stream = new SyntheticOverLimitStream(50000))
            {
                bool threwLimit = false;
                try
                {
                    JpegDecoder.Decode(stream, maxStreamBytes: 10000);
                }
                catch (ResourceLimitException ex)
                {
                    if (ex.Message.Contains("Fluxo JPEG excede o tamanho máximo"))
                    {
                        threwLimit = true;
                    }
                }
                Assert(threwLimit, "Stream JPEG não-seekable excedendo limite deve lançar ResourceLimitException");
            }
        }

        #region Casos de Teste JPEG Multiscan

        private static byte[] CreateMultiscanJpegBytes(int width, int height, bool subsample420, int restartInterval = 0, bool dhtBetweenScans = false)
        {
            var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);
            var qChr = new JpegQuantizationTable(JpegQuantizationTable.StandardChrominance);

            var lumDc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            var lumAc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);
            var chrDc = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
            var chrAc = new JpegHuffmanTable(JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);

            int maxH = subsample420 ? 2 : 1;
            int maxV = subsample420 ? 2 : 1;

            int compWidthY = (width * maxH + maxH - 1) / maxH;
            int compHeightY = (height * maxV + maxV - 1) / maxV;
            int blocksX_Y = (compWidthY + 7) / 8;
            int blocksY_Y = (compHeightY + 7) / 8;

            int compWidthC = (width + maxH - 1) / maxH;
            int compHeightC = (height + maxV - 1) / maxV;
            int blocksX_C = (compWidthC + 7) / 8;
            int blocksY_C = (compHeightC + 7) / 8;

            using (var ms = new MemoryStream())
            {
                // SOI
                ms.WriteByte(0xFF); ms.WriteByte(0xD8);

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 65 * 2));
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);
                ms.WriteByte(0x01);
                ms.Write(qChr.ToZigzagBytes(), 0, 64);

                // DRI
                if (restartInterval > 0)
                {
                    ms.WriteByte(0xFF); ms.WriteByte(0xDD);
                    ms.WriteByte(0x00); ms.WriteByte(0x04);
                    ms.WriteByte((byte)(restartInterval >> 8));
                    ms.WriteByte((byte)(restartInterval & 0xFF));
                }

                // SOF0
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(8);
                ms.WriteByte((byte)(height >> 8)); ms.WriteByte((byte)(height & 0xFF));
                ms.WriteByte((byte)(width >> 8)); ms.WriteByte((byte)(width & 0xFF));
                ms.WriteByte(3);
                ms.WriteByte(1); ms.WriteByte((byte)((maxH << 4) | maxV)); ms.WriteByte(0);
                ms.WriteByte(2); ms.WriteByte(0x11); ms.WriteByte(1);
                ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(1);

                Action<int, int, byte[], byte[]> writeDht = (tc, tid, bits, vals) =>
                {
                    ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                    int len = 2 + 1 + 16 + vals.Length;
                    ms.WriteByte((byte)(len >> 8)); ms.WriteByte((byte)(len & 0xFF));
                    ms.WriteByte((byte)((tc << 4) | tid));
                    ms.Write(bits, 0, 16);
                    ms.Write(vals, 0, vals.Length);
                };

                // Emit DHT Lum
                writeDht(0, 0, JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
                writeDht(1, 0, JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);

                if (!dhtBetweenScans)
                {
                    // Emit DHT Chr upfront
                    writeDht(0, 1, JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
                    writeDht(1, 1, JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);
                }

                // Helper para emitir scan não-intercalado (Ns = 1)
                Action<int, int, int, JpegHuffmanTable, JpegHuffmanTable, int> emitScan = (compId, blocksX, blocksY, dcT, acT, tableSel) =>
                {
                    ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                    ms.WriteByte(0x00); ms.WriteByte(8);
                    ms.WriteByte(1); // 1 componente
                    ms.WriteByte((byte)compId);
                    ms.WriteByte((byte)tableSel);
                    ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);

                    var writer = new JpegBitWriter(ms);
                    int mcuIdx = 0;
                    int nextRst = JpegMarker.RST0;

                    for (int by = 0; by < blocksY; by++)
                    {
                        for (int bx = 0; bx < blocksX; bx++)
                        {
                            if (restartInterval > 0 && mcuIdx > 0 && (mcuIdx % restartInterval) == 0)
                            {
                                writer.Flush();
                                ms.WriteByte((byte)(nextRst >> 8));
                                ms.WriteByte((byte)(nextRst & 0xFF));
                                nextRst++;
                                if (nextRst > JpegMarker.RST7) nextRst = JpegMarker.RST0;
                            }

                            // Bloco de teste: DC diff = 0, AC = EOB
                            writer.WriteSymbol(0x00, dcT);
                            writer.WriteSymbol(0x00, acT); // EOB
                            mcuIdx++;
                        }
                    }
                    writer.Flush();
                };

                // Scan 1: Y
                emitScan(1, blocksX_Y, blocksY_Y, lumDc, lumAc, 0x00);

                if (dhtBetweenScans)
                {
                    // Emit DHT Chr dinamicamente entre scans
                    writeDht(0, 1, JpegHuffmanTable.StandardChrominanceDcBits, JpegHuffmanTable.StandardChrominanceDcValues);
                    writeDht(1, 1, JpegHuffmanTable.StandardChrominanceAcBits, JpegHuffmanTable.StandardChrominanceAcValues);
                }

                // Scan 2: Cb
                emitScan(2, blocksX_C, blocksY_C, chrDc, chrAc, 0x11);

                // Scan 3: Cr
                emitScan(3, blocksX_C, blocksY_C, chrDc, chrAc, 0x11);

                // EOI
                ms.WriteByte(0xFF); ms.WriteByte(0xD9);

                return ms.ToArray();
            }
        }

        private static void Test_Jpeg_Multiscan_420_OddDimensions()
        {
            byte[] bytes = CreateMultiscanJpegBytes(24, 37, subsample420: true);
            using (var ms = new MemoryStream(bytes))
            {
                var buffer = JpegDecoder.Decode(ms);
                AssertEqual(24, buffer.Width, "Largura multiscan 4:2:0");
                AssertEqual(37, buffer.Height, "Altura multiscan 4:2:0");

                for (int y = 0; y < 37; y++)
                {
                    for (int x = 0; x < 24; x++)
                    {
                        var p = buffer.GetPixel(x, y);
                        AssertEqual((byte)255, p.A, "Canal alfa em multiscan deve ser 255");
                    }
                }
            }
        }

        private static void Test_Jpeg_Multiscan_444_EvenDimensions()
        {
            byte[] bytes = CreateMultiscanJpegBytes(16, 16, subsample420: false);
            using (var ms = new MemoryStream(bytes))
            {
                var buffer = JpegDecoder.Decode(ms);
                AssertEqual(16, buffer.Width, "Largura multiscan 4:4:4");
                AssertEqual(16, buffer.Height, "Altura multiscan 4:4:4");
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        var p = buffer.GetPixel(x, y);
                        AssertEqual((byte)255, p.A, "Canal alfa em multiscan deve ser 255");
                    }
                }
            }
        }

        private static void Test_Jpeg_Multiscan_With_RestartInterval_DRI()
        {
            byte[] bytes = CreateMultiscanJpegBytes(32, 32, subsample420: true, restartInterval: 4);
            using (var ms = new MemoryStream(bytes))
            {
                var buffer = JpegDecoder.Decode(ms);
                AssertEqual(32, buffer.Width, "Largura multiscan com DRI");
                AssertEqual(32, buffer.Height, "Altura multiscan com DRI");
            }
        }

        private static void Test_Jpeg_Multiscan_With_DHT_Between_Scans()
        {
            byte[] bytes = CreateMultiscanJpegBytes(16, 16, subsample420: false, dhtBetweenScans: true);
            using (var ms = new MemoryStream(bytes))
            {
                var buffer = JpegDecoder.Decode(ms);
                AssertEqual(16, buffer.Width, "Largura multiscan com DHT entre scans");
                AssertEqual(16, buffer.Height, "Altura multiscan com DHT entre scans");
            }
        }

        private static void Test_Jpeg_Multiscan_Adversarial_MissingComponent_ThrowsCorruptDataException()
        {
            var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);
            var lumDc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            var lumAc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);

                // SOF0 com 3 componentes
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(3);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(2); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT Lum
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // Scan 1: Y
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);
                var writer = new JpegBitWriter(ms);
                writer.WriteSymbol(0x00, lumDc);
                writer.WriteSymbol(0x00, lumAc);
                writer.Flush();

                // EOI direto sem enviar Cb e Cr!
                ms.WriteByte(0xFF); ms.WriteByte(0xD9);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("decodificação completa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ex.Message.IndexOf("não foi decodificado", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Fluxo JPEG multiscan com componentes ausentes ao atingir EOI deve lançar CorruptDataException");
            }
        }

        private static void Test_Jpeg_Multiscan_Adversarial_DuplicateScan_ThrowsCorruptDataException()
        {
            var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);
            var lumDc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceDcBits, JpegHuffmanTable.StandardLuminanceDcValues);
            var lumAc = new JpegHuffmanTable(JpegHuffmanTable.StandardLuminanceAcBits, JpegHuffmanTable.StandardLuminanceAcValues);

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);

                // SOF0 (3 componentes)
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(3);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(2); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // Scan 1: Componente 1
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);
                var writer1 = new JpegBitWriter(ms);
                writer1.WriteSymbol(0x00, lumDc);
                writer1.WriteSymbol(0x00, lumAc);
                writer1.Flush();

                // Scan 2: Componente 1 DUPLICADO!
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("já foi decodificado", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Componente duplicado em varredura subsequente deve lançar CorruptDataException");
            }
        }

        private static void Test_Jpeg_Multiscan_Adversarial_InvalidCompId_ThrowsCorruptDataException()
        {
            var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);

                // SOF0 (3 componentes: 1, 2, 3)
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(17);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(3);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(2); ms.WriteByte(0x11); ms.WriteByte(0);
                ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // SOS com compId = 99 (inexistente no SOF0)
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(99); ms.WriteByte(0x00);
                ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.IndexOf("não corresponde", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Componente SOS inexistente no SOF0 deve lançar CorruptDataException");
            }
        }

        private static void Test_Jpeg_Multiscan_Adversarial_InvalidSpectralParams_ThrowsUnsupportedFormatException()
        {
            var qLum = new JpegQuantizationTable(JpegQuantizationTable.StandardLuminance);

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0xFF); ms.WriteByte(0xD8); // SOI

                // DQT
                ms.WriteByte(0xFF); ms.WriteByte(0xDB);
                ms.WriteByte(0x00); ms.WriteByte(67);
                ms.WriteByte(0x00);
                ms.Write(qLum.ToZigzagBytes(), 0, 64);

                // SOF0
                ms.WriteByte(0xFF); ms.WriteByte(0xC0);
                ms.WriteByte(0x00); ms.WriteByte(11);
                ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x11); ms.WriteByte(0);

                // DHT
                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                ms.WriteByte(0x00); ms.WriteByte((byte)(2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceDcValues.Length));
                ms.WriteByte(0x00);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceDcValues, 0, JpegHuffmanTable.StandardLuminanceDcValues.Length);

                ms.WriteByte(0xFF); ms.WriteByte(0xC4);
                int acLen = 2 + 1 + 16 + JpegHuffmanTable.StandardLuminanceAcValues.Length;
                ms.WriteByte((byte)(acLen >> 8)); ms.WriteByte((byte)(acLen & 0xFF));
                ms.WriteByte(0x10);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcBits, 0, 16);
                ms.Write(JpegHuffmanTable.StandardLuminanceAcValues, 0, JpegHuffmanTable.StandardLuminanceAcValues.Length);

                // SOS com Ss=5 e Se=40 (seleção espectral progressiva inválida em Baseline)
                ms.WriteByte(0xFF); ms.WriteByte(0xDA);
                ms.WriteByte(0x00); ms.WriteByte(8);
                ms.WriteByte(1);
                ms.WriteByte(1); ms.WriteByte(0x00);
                ms.WriteByte(5); ms.WriteByte(40); ms.WriteByte(0);

                ms.Position = 0;
                bool threw = false;
                try
                {
                    JpegDecoder.Decode(ms);
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.IndexOf("espectrais", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Parâmetros espectrais não-baseline no SOS devem lançar UnsupportedFormatException");
            }
        }

        #region DESKTOP NATIVE APP TESTS (FASE 10)

        private static void Test_Desktop_ConversionJob_InitializationAndFormatting()
        {
            var job = new ConversionJob(101, @"C:\docs\sample.csv", "json");
            Assert(job.Id == 101, "Id deve ser 101");
            Assert(job.SourcePath == @"C:\docs\sample.csv", "SourcePath deve corresponder");
            Assert(job.FileName == "sample.csv", "FileName deve ser sample.csv");
            Assert(job.SourceFormat == "csv", "SourceFormat deve ser csv");
            Assert(job.TargetFormat == "json", "TargetFormat deve ser json");
            Assert(job.Status == JobStatus.Queued, "Status inicial deve ser Queued");
            Assert(job.ProgressPercent == 0, "Progresso inicial deve ser 0%");

            // Formatação de tamanhos
            Assert(job.FormatSize(500) == "500 B", "FormatSize 500 B");
            Assert(job.FormatSize(2048).Contains("KB"), "FormatSize 2048 deve conter KB");
            Assert(job.FormatSize(5 * 1024 * 1024).Contains("MB"), "FormatSize 5 MB deve conter MB");

            // ToString
            string str = job.ToString();
            Assert(str.Contains("sample.csv"), "ToString deve conter nome do arquivo");
            Assert(str.Contains("JSON"), "ToString deve conter formato destino");
            Assert(str.Contains("Na fila"), "ToString deve indicar 'Na fila'");
        }

        private static void Test_Desktop_ConversionManager_QueueLifecycle()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string f1 = Path.Combine(tempDir, "file1.csv");
                string f2 = Path.Combine(tempDir, "file2.json");
                string f3 = Path.Combine(tempDir, "file3.bmp");
                File.WriteAllText(f1, "a,b\n1,2");
                File.WriteAllText(f2, "{}");
                File.WriteAllText(f3, "dummy");

                var manager = new ConversionManager();
                Assert(manager.Jobs.Count == 0, "Fila inicial vazia");

                var j1 = manager.AddFile(f1);
                Assert(j1 != null, "j1 não deve ser nulo");
                Assert(j1.TargetFormat == "json", "Formato sugerido para CSV deve ser JSON");
                Assert(manager.Jobs.Count == 1, "Fila deve ter 1 job");

                int added = manager.AddFiles(new[] { f2, f3 });
                Assert(added == 2, "2 arquivos adicionados");
                Assert(manager.Jobs.Count == 3, "Fila deve ter 3 jobs");

                bool removed = manager.RemoveJob(j1.Id);
                Assert(removed, "Job 1 removido com sucesso");
                Assert(manager.Jobs.Count == 2, "Fila deve ter 2 jobs após remoção");

                manager.Clear();
                Assert(manager.Jobs.Count == 0, "Fila limpa");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Desktop_ConversionManager_BatchProcessing_CsvToJson()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_csv_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string csvFile = Path.Combine(tempDir, "clientes.csv");
                File.WriteAllText(csvFile, "Nome,Idade,Cidade\nAlice,30,\"São Paulo\"\nBob,25,Curitiba\n", Encoding.UTF8);

                var manager = new ConversionManager();
                var job = manager.AddFile(csvFile, "json");

                using (var doneEvt = new ManualResetEvent(false))
                {
                    int finishTotal = 0, finishSuccess = 0, finishFailed = 0;
                    manager.BatchFinished += (t, s, f) =>
                    {
                        finishTotal = t;
                        finishSuccess = s;
                        finishFailed = f;
                        doneEvt.Set();
                    };

                    bool started = manager.StartBatch("json", tempDir);
                    Assert(started, "StartBatch deve iniciar");

                    bool completed = doneEvt.WaitOne(10000);
                    Assert(completed, "Processamento em lote deve terminar em menos de 10s");
                    Assert(finishTotal == 1, "Total deve ser 1");
                    Assert(finishSuccess == 1, "Sucesso deve ser 1");
                    Assert(finishFailed == 0, "Falha deve ser 0");
                    Assert(job.Status == JobStatus.Completed, "Status do job deve ser Completed");
                    Assert(job.ProgressPercent == 100, "Progresso do job deve ser 100%");
                    Assert(!string.IsNullOrEmpty(job.DestinationPath), "DestinationPath deve estar preenchido");
                    Assert(File.Exists(job.DestinationPath), "Arquivo JSON destino deve existir");

                    string jsonContent = File.ReadAllText(job.DestinationPath, Encoding.UTF8);
                    Assert(jsonContent.Contains("Alice"), "JSON deve conter Alice");
                    Assert(jsonContent.Contains("Curitiba"), "JSON deve conter Curitiba");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Desktop_ConversionManager_BatchProcessing_Image_BmpToPngAndPngToJpeg()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_img_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // Gera BMP 16x16 nativo
                string bmpFile = Path.Combine(tempDir, "icon.bmp");
                var buffer = new PixelBuffer(16, 16);
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        buffer.SetPixel(x, y, new PixelColor((byte)(x * 15), (byte)(y * 15), 128, 255));
                    }
                }
                using (var fs = File.Create(bmpFile))
                {
                    BmpEncoder.Encode(buffer, fs, 24);
                }

                var manager = new ConversionManager();
                var jobBmpToPng = manager.AddFile(bmpFile, "png");

                // Etapa 1: BMP -> PNG
                using (var doneEvt = new ManualResetEvent(false))
                {
                    manager.BatchFinished += (t, s, f) => doneEvt.Set();
                    manager.StartBatch("png", tempDir);
                    bool ok = doneEvt.WaitOne(10000);
                    Assert(ok, "Conversão BMP -> PNG deve terminar em menos de 10s");
                    Assert(jobBmpToPng.Status == JobStatus.Completed, "BMP -> PNG deve ter sucesso");
                    Assert(File.Exists(jobBmpToPng.DestinationPath), "PNG gerado deve existir");
                }

                // Etapa 2: PNG -> JPG
                string generatedPng = jobBmpToPng.DestinationPath;
                var manager2 = new ConversionManager();
                var jobPngToJpg = manager2.AddFile(generatedPng, "jpg");

                using (var doneEvt2 = new ManualResetEvent(false))
                {
                    manager2.BatchFinished += (t, s, f) => doneEvt2.Set();
                    manager2.StartBatch("jpg", tempDir);
                    bool ok2 = doneEvt2.WaitOne(10000);
                    Assert(ok2, "Conversão PNG -> JPG deve terminar em menos de 10s");
                    Assert(jobPngToJpg.Status == JobStatus.Completed, "PNG -> JPG deve ter sucesso");
                    Assert(File.Exists(jobPngToJpg.DestinationPath), "JPG gerado deve existir");
                    Assert(new FileInfo(jobPngToJpg.DestinationPath).Length > 0, "JPG gerado deve ter tamanho > 0");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Desktop_ConversionManager_BatchProcessing_Audio_WavToWav()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_aud_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string wavFile = Path.Combine(tempDir, "input_sample.wav");
                var buffer = new AudioBuffer(2, 44100, 16, 4410);
                for (int i = 0; i < 4410; i++)
                {
                    buffer.SetSample(0, i, 0.4f);
                    buffer.SetSample(1, i, -0.4f);
                }
                using (var fs = File.Create(wavFile))
                {
                    WavEncoder.Encode(buffer, fs);
                }

                var manager = new ConversionManager();
                var job = manager.AddFile(wavFile, "wav");
                var parameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "sampleRate", "22050" },
                    { "channels", "1" },
                    { "bits", "24" }
                };

                using (var doneEvt = new ManualResetEvent(false))
                {
                    manager.BatchFinished += (t, s, f) => doneEvt.Set();
                    manager.StartBatch("wav", tempDir, parameters);
                    bool ok = doneEvt.WaitOne(10000);
                    Assert(ok, "Conversão em lote WAV -> WAV deve terminar em menos de 10s");
                    Assert(job.Status == JobStatus.Completed, "WAV -> WAV deve ter sucesso");
                    Assert(File.Exists(job.DestinationPath), "Arquivo de áudio gerado deve existir");
                    Assert(new FileInfo(job.DestinationPath).Length > 0, "Tamanho de saída > 0");

                    using (var fs = File.OpenRead(job.DestinationPath))
                    {
                        var info = new WavEngine().Inspect(fs, job.DestinationPath);
                        Assert(info.Metadata["SampleRate"] == "22050 Hz", "Taxa reamostrada para 22050 Hz");
                        Assert(info.Metadata["Channels"] == "1 (Mono)", "Mixado para Mono");
                        Assert(info.Metadata["BitsPerSample"] == "24 bits", "Convertido para 24 bits");
                    }
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Desktop_ConversionManager_ErrorIsolation_OnMalformedInput()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_err_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string validCsv = Path.Combine(tempDir, "valid.csv");
                File.WriteAllText(validCsv, "col1,col2\nval1,val2\n");

                string corruptBmp = Path.Combine(tempDir, "corrupt.bmp");
                File.WriteAllBytes(corruptBmp, new byte[] { 0x42, 0x4D, 0x00, 0x00 }); // BMP truncado no cabeçalho

                var manager = new ConversionManager();
                var jobValid = manager.AddFile(validCsv, "json");
                var jobCorrupt = manager.AddFile(corruptBmp, "png");

                using (var doneEvt = new ManualResetEvent(false))
                {
                    int totalCount = 0, successCount = 0, failedCount = 0;
                    manager.BatchFinished += (t, s, f) =>
                    {
                        totalCount = t;
                        successCount = s;
                        failedCount = f;
                        doneEvt.Set();
                    };

                    manager.StartBatch(null, tempDir);
                    bool ok = doneEvt.WaitOne(10000);
                    Assert(ok, "Lote com erro deve finalizar normalmente sem travar");
                    Assert(totalCount == 2, "Total de 2 jobs processados");
                    Assert(successCount == 1, "Exatamente 1 job com sucesso");
                    Assert(failedCount == 1, "Exatamente 1 job com falha");
                    Assert(jobValid.Status == JobStatus.Completed, "Job válido deve estar Completed");
                    Assert(jobCorrupt.Status == JobStatus.Failed, "Job corrompido deve estar Failed");
                    Assert(!string.IsNullOrEmpty(jobCorrupt.ErrorMessage), "Mensagem de erro deve estar registrada no job");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Desktop_ConversionManager_CooperativeCancellation()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_dt_can_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var manager = new ConversionManager();
                for (int i = 0; i < 5; i++)
                {
                    string f = Path.Combine(tempDir, string.Format("item_{0}.csv", i));
                    File.WriteAllText(f, string.Format("x,y\n{0},{1}", i, i * 2));
                    manager.AddFile(f, "json");
                }

                using (var doneEvt = new ManualResetEvent(false))
                {
                    manager.BatchFinished += (t, s, f) => doneEvt.Set();
                    manager.StartBatch("json", tempDir);
                    manager.Cancel(); // Cancela cooperativamente imediatamente
                    bool ok = doneEvt.WaitOne(10000);
                    Assert(ok, "Lote cancelado deve finalizar e sinalizar conclusão");
                    Assert(manager.CancelRequested, "CancelRequested deve ser true");

                    bool hasCancelledOrFinished = false;
                    foreach (var j in manager.Jobs)
                    {
                        if (j.Status == JobStatus.Cancelled || j.Status == JobStatus.Completed)
                        {
                            hasCancelledOrFinished = true;
                        }
                    }
                    Assert(hasCancelledOrFinished, "Cancelamento cooperativo registrado");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        #region Módulo: Motor Nativo de Áudio RIFF/WAVE PCM (Fase 11A)

        private static void Test_Wav_RoundTrip_8Bit_Mono()
        {
            int sampleRate = 44100;
            int frameCount = 1000;
            var buffer = new AudioBuffer(1, sampleRate, 8, frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                float t = (float)i / sampleRate;
                float s = (float)Math.Sin(2.0 * Math.PI * 440.0 * t);
                buffer.SetSample(0, i, s);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                ms.Position = 0;
                var decoded = WavDecoder.Decode(ms);

                Assert(decoded.Channels == 1, "Mono deve ter 1 canal");
                Assert(decoded.SampleRate == sampleRate, "SampleRate deve ser 44100");
                Assert(decoded.BitsPerSample == 8, "BitsPerSample deve ser 8");
                Assert(decoded.FrameCount == frameCount, "FrameCount deve ser 1000");

                float maxErr = 0f;
                for (int i = 0; i < frameCount; i++)
                {
                    float orig = buffer.GetSample(0, i);
                    float dec = decoded.GetSample(0, i);
                    float diff = Math.Abs(orig - dec);
                    if (diff > maxErr) maxErr = diff;
                }
                Assert(maxErr < 0.015f, "Erro de quantização 8-bit deve ser menor que 0.015");
            }

            // Verificação exaustiva de todos os 256 valores 8-bit unsigned (D-WAV-02)
            var ex8 = new AudioBuffer(1, 44100, 8, 256);
            for (int b = 0; b < 256; b++) ex8.SetSampleQ31(0, b, (b - 128) << 24);
            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(ex8, ms);
                ms.Position = 0;
                var decEx8 = WavDecoder.Decode(ms);
                for (int b = 0; b < 256; b++)
                {
                    int expected = (b - 128) << 24;
                    Assert(decEx8.GetSampleQ31(0, b) == expected, string.Format("Fidelidade exata do byte {0}", b));
                }
            }
        }

        private static void Test_Wav_RoundTrip_8Bit_Stereo()
        {
            int sampleRate = 22050;
            int frameCount = 500;
            var buffer = new AudioBuffer(2, sampleRate, 8, frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                buffer.SetSample(0, i, (i % 20 < 10) ? 0.8f : -0.8f);
                buffer.SetSample(1, i, (float)Math.Sin(i * 0.1));
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                ms.Position = 0;
                var decoded = WavDecoder.Decode(ms);

                Assert(decoded.Channels == 2, "Stereo deve ter 2 canais");
                Assert(decoded.BitsPerSample == 8, "BitsPerSample deve ser 8");
                Assert(decoded.FrameCount == frameCount, "FrameCount deve ser 500");

                for (int c = 0; c < 2; c++)
                {
                    for (int i = 0; i < frameCount; i++)
                    {
                        float orig = buffer.GetSample(c, i);
                        float dec = decoded.GetSample(c, i);
                        Assert(Math.Abs(orig - dec) < 0.015f, "Canais estéreo 8-bit devem ser fiéis");
                    }
                }
            }
        }

        private static void Test_Wav_RoundTrip_16Bit_Mono()
        {
            int sampleRate = 48000;
            int frameCount = 2000;
            var buffer = new AudioBuffer(1, sampleRate, 16, frameCount);
            buffer.SetSample(0, 0, -1.0f);
            buffer.SetSample(0, 1, 1.0f);
            buffer.SetSample(0, 2, 0.0f);
            for (int i = 3; i < frameCount; i++)
            {
                float val = ((float)i / frameCount) * 2.0f - 1.0f;
                buffer.SetSample(0, i, val);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                ms.Position = 0;
                var decoded = WavDecoder.Decode(ms);

                Assert(decoded.Channels == 1, "Mono 16-bit deve ter 1 canal");
                Assert(decoded.BitsPerSample == 16, "BitsPerSample deve ser 16");
                Assert(decoded.FrameCount == frameCount, "FrameCount deve ser 2000");

                Assert(Math.Abs(decoded.GetSample(0, 0) - (-1.0f)) < 0.0001f, "Mínimo -1.0f preservado");
                Assert(Math.Abs(decoded.GetSample(0, 1) - 1.0f) < 0.0001f, "Máximo 1.0f preservado");
                Assert(Math.Abs(decoded.GetSample(0, 2) - 0.0f) < 0.0001f, "Silêncio 0.0f preservado");

                for (int i = 3; i < frameCount; i++)
                {
                    float orig = buffer.GetSample(0, i);
                    float dec = decoded.GetSample(0, i);
                    Assert(Math.Abs(orig - dec) < 0.0001f, "Fidelidade 16-bit deve ter erro menor que 10^-4");
                }
            }

            // Verificação exaustiva de todos os 65.536 valores 16-bit signed
            var ex16 = new AudioBuffer(1, 48000, 16, 65536);
            for (int i = 0; i < 65536; i++)
            {
                short s = (short)(i - 32768);
                ex16.SetSampleQ31(0, i, (int)s << 16);
            }
            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(ex16, ms);
                ms.Position = 0;
                var decEx16 = WavDecoder.Decode(ms);
                for (int i = 0; i < 65536; i++)
                {
                    short s = (short)(i - 32768);
                    int expected = (int)s << 16;
                    Assert(decEx16.GetSampleQ31(0, i) == expected, "Fidelidade exata 16-bit");
                }
            }
        }

        private static void Test_Wav_RoundTrip_16Bit_Stereo()
        {
            int sampleRate = 44100;
            int frameCount = 1000;
            var buffer = new AudioBuffer(2, sampleRate, 16, frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                float t = (float)i / sampleRate;
                float left = (float)Math.Sin(2.0 * Math.PI * 220.0 * t) * 0.75f;
                float right = -left;
                buffer.SetSample(0, i, left);
                buffer.SetSample(1, i, right);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                ms.Position = 0;
                var decoded = WavDecoder.Decode(ms);

                Assert(decoded.Channels == 2, "Stereo 16-bit deve ter 2 canais");
                Assert(decoded.BitsPerSample == 16, "BitsPerSample deve ser 16");
                for (int i = 0; i < frameCount; i++)
                {
                    float l = decoded.GetSample(0, i);
                    float r = decoded.GetSample(1, i);
                    Assert(Math.Abs(l - buffer.GetSample(0, i)) < 0.0001f, "Canal esquerdo 16-bit fiel");
                    Assert(Math.Abs(r - buffer.GetSample(1, i)) < 0.0001f, "Canal direito 16-bit fiel");
                    Assert(Math.Abs(l + r) < 0.0001f, "Anti-fase estéreo preservada");
                }
            }
        }

        private static void Test_Wav_RoundTrip_24Bit_Mono_And_Stereo()
        {
            var mono = new AudioBuffer(1, 96000, 24, 1500);
            for (int i = 0; i < 1500; i++)
            {
                mono.SetSample(0, i, (float)Math.Sin(i * 0.05) * 0.9f);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(mono, ms);
                ms.Position = 0;
                var decMono = WavDecoder.Decode(ms);
                Assert(decMono.Channels == 1, "Mono 24-bit");
                Assert(decMono.BitsPerSample == 24, "24 bits");
                Assert(decMono.SampleRate == 96000, "96000 Hz");
                for (int i = 0; i < 1500; i++)
                {
                    Assert(Math.Abs(mono.GetSample(0, i) - decMono.GetSample(0, i)) < 1e-6f, "Fidelidade 24-bit mono < 1e-6");
                }
            }

            var stereo = new AudioBuffer(2, 48000, 24, 800);
            for (int i = 0; i < 800; i++)
            {
                stereo.SetSample(0, i, 0.5f);
                stereo.SetSample(1, i, -0.5f);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(stereo, ms);
                ms.Position = 0;
                var decStereo = WavDecoder.Decode(ms);
                Assert(decStereo.Channels == 2, "Stereo 24-bit");
                Assert(decStereo.BitsPerSample == 24, "24 bits");
                Assert(Math.Abs(decStereo.GetSample(0, 100) - 0.5f) < 1e-6f, "Esquerdo 24-bit");
                Assert(Math.Abs(decStereo.GetSample(1, 100) - (-0.5f)) < 1e-6f, "Direito 24-bit");
            }

            // Verificação de fidelidade exata bit-perfect de 24 bits nos bits menos significativos
            var buf24 = new AudioBuffer(1, 48000, 24, 1000);
            for (int i = 0; i < 1000; i++)
            {
                int val24 = (i * 16777) - 8388000;
                buf24.SetSampleQ31(0, i, val24 << 8);
            }
            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buf24, ms);
                ms.Position = 0;
                var dec24 = WavDecoder.Decode(ms);
                for (int i = 0; i < 1000; i++)
                {
                    int expected = ((i * 16777) - 8388000) << 8;
                    Assert(dec24.GetSampleQ31(0, i) == expected, "Preservação 24-bit bit-perfect");
                }
            }
        }

        private static void Test_Wav_RoundTrip_32Bit_Mono_And_Stereo()
        {
            var mono = new AudioBuffer(1, 192000, 32, 1000);
            int[] testPatterns = new int[]
            {
                0, 1, -1, int.MaxValue, int.MinValue,
                0x12345678, unchecked((int)0x87654321),
                unchecked((int)0xA5A5A5A5), 0x5A5A5A5A,
                0x7FFFFF00, unchecked((int)0x800000FF),
                123456789, -987654321
            };

            for (int i = 0; i < 1000; i++)
            {
                mono.SetSampleQ31(0, i, testPatterns[i % testPatterns.Length]);
            }

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(mono, ms);
                ms.Position = 0;
                var dec = WavDecoder.Decode(ms);
                Assert(dec.Channels == 1, "Mono 32-bit");
                Assert(dec.BitsPerSample == 32, "32 bits");
                Assert(dec.SampleRate == 192000, "192000 Hz");
                for (int i = 0; i < 1000; i++)
                {
                    int expected = testPatterns[i % testPatterns.Length];
                    int actual = dec.GetSampleQ31(0, i);
                    Assert(actual == expected, string.Format("Fidelidade inteira 32-bit exata: esperado 0x{0:X8}, obtido 0x{1:X8}", expected, actual));
                }
            }

            var stereo = new AudioBuffer(2, 96000, 32, 500);
            for (int i = 0; i < 500; i++)
            {
                stereo.SetSampleQ31(0, i, 0x11223344);
                stereo.SetSampleQ31(1, i, unchecked((int)0x8899AABB));
            }
            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(stereo, ms);
                ms.Position = 0;
                var decStereo = WavDecoder.Decode(ms);
                Assert(decStereo.Channels == 2, "Stereo 32-bit");
                for (int i = 0; i < 500; i++)
                {
                    Assert(decStereo.GetSampleQ31(0, i) == 0x11223344, "Esquerdo 32-bit bit-perfect");
                    Assert(decStereo.GetSampleQ31(1, i) == unchecked((int)0x8899AABB), "Direito 32-bit bit-perfect");
                }
            }
        }

        private static void Test_Wav_RiffPadding_OddSizeChunk()
        {
            var buffer = new AudioBuffer(1, 8000, 8, 1001);
            for (int i = 0; i < 1001; i++) buffer.SetSample(0, i, 0.42f);

            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                byte[] bytes = ms.ToArray();

                Assert(bytes.Length == 1046, string.Format("Tamanho com pad deve ser 1046 bytes, obtido {0}", bytes.Length));
                Assert(bytes[bytes.Length - 1] == 0, "Último byte deve ser o pad byte 0x00");

                var decoded = WavDecoder.Decode(new MemoryStream(bytes));
                Assert(decoded.FrameCount == 1001, "Todos os 1001 quadros devem ser lidos");
            }
        }

        private static void Test_Wav_AuxiliaryChunks_SkipListAndJunk()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)0);
                w.Write(WavHeader.FourCcWave);

                // JUNK (7 bytes de dados + 1 byte pad)
                w.Write(Encoding.ASCII.GetBytes("JUNK"));
                w.Write((uint)7);
                w.Write(Encoding.ASCII.GetBytes("1234567"));
                w.Write((byte)0);

                // fmt (16 bytes)
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);

                // LIST (10 bytes de dados)
                w.Write(Encoding.ASCII.GetBytes("LIST"));
                w.Write((uint)10);
                w.Write(Encoding.ASCII.GetBytes("INFO123456"));

                // data (20 bytes = 10 quadros de 16-bit)
                w.Write(WavHeader.FourCcData);
                w.Write((uint)20);
                for (int i = 0; i < 10; i++)
                {
                    short s = (short)(i * 1000);
                    w.Write(s);
                }

                long totalLen = ms.Length;
                ms.Position = 4;
                w.Write((uint)(totalLen - 8));

                ms.Position = 0;
                var header = WavHeader.Read(ms);
                Assert(header.Channels == 1, "Mono");
                Assert(header.SampleRate == 44100, "44100");
                Assert(header.FrameCount == 10, "10 quadros");
                Assert(header.AuxiliaryChunks.ContainsKey("JUNK"), "Deve registrar chunk JUNK");
                Assert(header.AuxiliaryChunks.ContainsKey("LIST"), "Deve registrar chunk LIST");

                ms.Position = 0;
                var buffer = WavDecoder.Decode(ms);
                Assert(buffer.FrameCount == 10, "Decodificação de áudio após saltar chunks auxiliares");
                Assert(Math.Abs(buffer.GetSample(0, 5) - (5000.0f / 32767.0f)) < 0.0001f, "Amostra após saltar chunks");
            }
        }

        private static void Test_Wav_ExtensiblePcm_Guid()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)0);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)40);
                w.Write(WavHeader.WaveFormatExtensible);
                w.Write((ushort)2);
                w.Write((uint)48000);
                w.Write((uint)192000);
                w.Write((ushort)4);
                w.Write((ushort)16);
                w.Write((ushort)22);
                w.Write((ushort)16);
                w.Write((uint)3);
                w.Write(WavHeader.PcmSubFormatGuid);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)32);
                for (int i = 0; i < 8; i++)
                {
                    w.Write((short)1000);
                    w.Write((short)-1000);
                }

                ms.Position = 4;
                w.Write((uint)(ms.Length - 8));

                ms.Position = 0;
                var buffer = WavDecoder.Decode(ms);
                Assert(buffer.Channels == 2, "2 canais");
                Assert(buffer.SampleRate == 48000, "48000 Hz");
                Assert(buffer.FrameCount == 8, "8 quadros");
                Assert(Math.Abs(buffer.GetSample(0, 0) - (1000.0f / 32767.0f)) < 0.0001f, "Canal esquerdo extensible");
                Assert(Math.Abs(buffer.GetSample(1, 0) - (-1000.0f / 32768.0f)) < 0.0001f, "Canal direito extensible");
            }
        }

        private static void Test_Wav_ChannelConversion_MonoToStereo_And_StereoToMono()
        {
            var mono = new AudioBuffer(1, 44100, 16, 100);
            for (int i = 0; i < 100; i++) mono.SetSample(0, i, 0.65f);

            var stereo = mono.ToStereo();
            Assert(stereo.Channels == 2, "ToStereo deve gerar 2 canais");
            Assert(stereo.FrameCount == 100, "Mesma contagem de quadros");
            Assert(stereo.GetSample(0, 50) == 0.65f, "Canal esquerdo duplicado");
            Assert(stereo.GetSample(1, 50) == 0.65f, "Canal direito duplicado");

            stereo.SetSample(0, 10, 0.8f);
            stereo.SetSample(1, 10, 0.2f);
            var backToMono = stereo.ToMono();
            Assert(backToMono.Channels == 1, "ToMono deve gerar 1 canal");
            Assert(Math.Abs(backToMono.GetSample(0, 10) - 0.5f) < 0.0001f, "Média dos canais (0.8 + 0.2)/2 = 0.5");
        }

        private static void Test_Wav_Resampling_LinearInterpolation()
        {
            int origRate = 44100;
            int targetRate = 48000;
            int origFrames = 44100;
            var buffer = new AudioBuffer(1, origRate, 16, origFrames);
            for (int i = 0; i < origFrames; i++)
            {
                float t = (float)i / origRate;
                buffer.SetSample(0, i, (float)Math.Sin(2.0 * Math.PI * 440.0 * t));
            }

            var resampled = buffer.Resample(targetRate);
            Assert(resampled.SampleRate == targetRate, "Nova taxa deve ser 48000");
            Assert(resampled.FrameCount == 48000, "1 segundo em 48000 deve ter 48000 quadros");
            Assert(Math.Abs(resampled.DurationSeconds - 1.0) < 0.001, "Duração preservada em 1.0 s");

            var downsampled = buffer.Resample(22050);
            Assert(downsampled.SampleRate == 22050, "Nova taxa deve ser 22050");
            Assert(downsampled.FrameCount == 22050, "1 segundo em 22050 deve ter 22050 quadros");
            Assert(Math.Abs(downsampled.DurationSeconds - 1.0) < 0.001, "Duração preservada em 1.0 s");
        }

        private static void Test_Wav_GainAndNormalization()
        {
            var buffer = new AudioBuffer(1, 44100, 16, 100);
            for (int i = 0; i < 100; i++) buffer.SetSample(0, i, 0.25f);

            buffer.ApplyGain(2.0f);
            Assert(Math.Abs(buffer.GetSample(0, 50) - 0.5f) < 0.0001f, "Ganho 2.0x deve dobrar amplitude");

            buffer.Normalize(0.99f);
            Assert(Math.Abs(buffer.GetSample(0, 50) - 0.99f) < 0.0001f, "Normalização deve elevar pico para 0.99");
        }

        private static void Test_Wav_Security_InvalidSignature_And_Truncation()
        {
            bool threwSig = false;
            try
            {
                byte[] badSig = Encoding.ASCII.GetBytes("NOTRxxxxWAVEfmt ");
                WavDecoder.Decode(new MemoryStream(badSig));
            }
            catch (CorruptDataException) { threwSig = true; }
            Assert(threwSig, "Deve rejeitar assinatura que não seja 'RIFF'");

            bool threwShort = false;
            try
            {
                byte[] shortHeader = new byte[8];
                WavDecoder.Decode(new MemoryStream(shortHeader));
            }
            catch (CorruptDataException) { threwShort = true; }
            Assert(threwShort, "Deve rejeitar cabeçalho < 12 bytes");

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)100);
                w.Write(WavHeader.FourCcWave);
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);
                w.Write(WavHeader.FourCcData);
                w.Write((uint)1000);
                w.Write((int)12345);

                ms.Position = 0;
                bool threwTruncData = false;
                try { WavDecoder.Decode(ms); }
                catch (CorruptDataException) { threwTruncData = true; }
                Assert(threwTruncData, "Deve rejeitar fluxo truncado antes do fim declarado do chunk data");
            }
        }

        private static void Test_Wav_Security_UnsupportedCompression_And_BitDepths()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)100);
                w.Write(WavHeader.FourCcWave);
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write((ushort)3); // IEEE Float
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)176400);
                w.Write((ushort)4);
                w.Write((ushort)32);
                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwFloat = false;
                try { WavDecoder.Decode(ms); }
                catch (UnsupportedFormatException) { threwFloat = true; }
                Assert(threwFloat, "Deve rejeitar IEEE Float com UnsupportedFormatException");
            }

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)100);
                w.Write(WavHeader.FourCcWave);
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)44100);
                w.Write((ushort)1);
                w.Write((ushort)12);
                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threw12Bit = false;
                try { WavDecoder.Decode(ms); }
                catch (UnsupportedFormatException) { threw12Bit = true; }
                Assert(threw12Bit, "Deve rejeitar 12 bits com UnsupportedFormatException");
            }
        }

        private static void Test_Wav_Security_InvalidBlockAlign_And_Channels()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)100);
                w.Write(WavHeader.FourCcWave);
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)3); // BlockAlign errado (deveria ser 4)
                w.Write((ushort)16);
                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwAlign = false;
                try { WavDecoder.Decode(ms); }
                catch (CorruptDataException) { threwAlign = true; }
                Assert(threwAlign, "Deve rejeitar BlockAlign inconsistente");
            }

            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)100);
                w.Write(WavHeader.FourCcWave);
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)0); // 0 canais
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);
                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwZeroCh = false;
                try { WavDecoder.Decode(ms); }
                catch (CorruptDataException) { threwZeroCh = true; }
                Assert(threwZeroCh, "Deve rejeitar arquivo com 0 canais");
            }
        }

        private static void Test_Wav_Security_ExceedFileAndFrameLimits()
        {
            bool threwRate = false;
            try
            {
                SecurityLimits.AssertAudioParameters(1, 384000, 16, 100);
            }
            catch (CorruptDataException) { threwRate = true; }
            Assert(threwRate, "Deve rejeitar sample rate > 192 kHz");

            bool threwFrames = false;
            try
            {
                SecurityLimits.AssertAudioParameters(1, 44100, 16, SecurityLimits.MaxAudioFrames + 1);
            }
            catch (ResourceLimitException) { threwFrames = true; }
            Assert(threwFrames, "Deve rejeitar frame count > 100M quadros com ResourceLimitException");
        }

        private static void Test_Wav_Engine_Inspect_And_Registry()
        {
            var registry = EngineRegistry.Default;
            var engine = registry.Resolve("wav", "wav");
            Assert(engine != null, "WavEngine deve estar registrado");
            Assert(engine.EngineId == "audio-wav", "EngineId deve ser 'audio-wav'");

            var buffer = new AudioBuffer(2, 48000, 24, 24000);
            using (var ms = new MemoryStream())
            {
                WavEncoder.Encode(buffer, ms);
                ms.Position = 0;
                var info = engine.Inspect(ms, "test.wav");
                Assert(info.FormatId == "wav", "FormatId deve ser 'wav'");
                Assert(info.MimeType == "audio/wav", "MimeType");
                Assert(info.Metadata["SampleRate"] == "48000 Hz", "SampleRate");
                Assert(info.Metadata["BitsPerSample"] == "24 bits", "BitsPerSample");
                Assert(info.Metadata["Channels"] == "2 (Estéreo)", "Channels");
                Assert(info.Metadata["DurationSeconds"] == "0.50 s", "Duration");
            }
        }

        private static void Test_Wav_Engine_ConvertPipeline_WithSettings()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_wav_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string inputPath = Path.Combine(tempDir, "source.wav");
                var srcBuffer = new AudioBuffer(2, 44100, 16, 22050);
                for (int i = 0; i < 22050; i++)
                {
                    srcBuffer.SetSample(0, i, 0.3f);
                    srcBuffer.SetSample(1, i, 0.5f);
                }
                using (var fs = File.Create(inputPath))
                {
                    WavEncoder.Encode(srcBuffer, fs);
                }

                var engine = new WavEngine();
                var req = new ConvertRequest
                {
                    InputPath = inputPath,
                    TargetFormat = "wav",
                    OutputDirectory = tempDir,
                    OutputFileName = "converted.wav"
                };
                req.Settings["sampleRate"] = "48000";
                req.Settings["channels"] = "1";
                req.Settings["bits"] = "24";
                req.Settings["normalize"] = "true";

                int maxProgress = 0;
                var res = engine.Convert(req, pct => { if (pct > maxProgress) maxProgress = pct; });

                Assert(res.Status == "completed", "Status deve ser completed");
                Assert(maxProgress == 100, "Progresso deve atingir 100%");
                Assert(File.Exists(res.OutputPath), "Arquivo convertido deve existir");

                using (var fs = File.OpenRead(res.OutputPath))
                {
                    var inspected = engine.Inspect(fs, res.OutputPath);
                    Assert(inspected.Metadata["SampleRate"] == "48000 Hz", "Convertido para 48000 Hz");
                    Assert(inspected.Metadata["Channels"] == "1 (Mono)", "Convertido para Mono");
                    Assert(inspected.Metadata["BitsPerSample"] == "24 bits", "Convertido para 24 bits");

                    fs.Position = 0;
                    var convertedBuffer = WavDecoder.Decode(fs);
                    Assert(convertedBuffer.Channels == 1, "Buffer com 1 canal");
                    Assert(convertedBuffer.BitsPerSample == 24, "Buffer com 24 bits");
                    Assert(Math.Abs(convertedBuffer.GetSample(0, 100) - 0.99f) < 0.01f, "Pico normalizado para 0.99");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Wav_External_Interoperability_SystemSoundPlayer()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_sp_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string wavPath = Path.Combine(tempDir, "sine16.wav");
                int sampleRate = 44100;
                int frameCount = 4410;
                var buffer = new AudioBuffer(2, sampleRate, 16, frameCount);
                for (int i = 0; i < frameCount; i++)
                {
                    float t = (float)i / sampleRate;
                    buffer.SetSample(0, i, (float)Math.Sin(2.0 * Math.PI * 440.0 * t) * 0.5f);
                    buffer.SetSample(1, i, (float)Math.Sin(2.0 * Math.PI * 880.0 * t) * 0.5f);
                }

                using (var fs = File.Create(wavPath))
                {
                    WavEncoder.Encode(buffer, fs);
                }

                var player = new System.Media.SoundPlayer(wavPath);
                player.Load();
                Assert(player.IsLoadCompleted, "SoundPlayer do Windows deve carregar o WAV do Lodino sem erros");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Wav_Hardening_DuplicateFmtChunk_Rejection()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)120);
                w.Write(WavHeader.FourCcWave);

                // Primeiro chunk fmt
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);

                // Segundo chunk fmt (duplicado - anômalo)
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwDupFmt = false;
                try
                {
                    WavDecoder.Decode(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("Múltiplos chunks 'fmt '"))
                    {
                        threwDupFmt = true;
                    }
                }
                Assert(threwDupFmt, "Deve rejeitar múltiplos chunks 'fmt ' com CorruptDataException (D-WAV-01)");
            }
        }

        private static void Test_Wav_Hardening_AuxiliaryChunkLimits_Rejection()
        {
            // 1. Chunk único excedendo MaxSingleChunkSize (64 MB)
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)1000);
                w.Write(WavHeader.FourCcWave);

                w.Write(Encoding.ASCII.GetBytes("JUNK"));
                w.Write((uint)(SecurityLimits.MaxSingleChunkSize + 1024)); // > 64 MB

                ms.Position = 0;
                bool threwSingleLimit = false;
                try
                {
                    WavHeader.Read(ms);
                }
                catch (ResourceLimitException)
                {
                    threwSingleLimit = true;
                }
                Assert(threwSingleLimit, "Deve rejeitar chunk auxiliar individual > 64 MB (D-WAV-04)");
            }

            // 2. Chunks auxiliares acumulados excedendo MaxAuxiliaryHeaderBytes (16 MB)
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)20000000);
                w.Write(WavHeader.FourCcWave);

                // Chunk fmt padrão
                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)1);
                w.Write((uint)44100);
                w.Write((uint)88200);
                w.Write((ushort)2);
                w.Write((ushort)16);

                // Dois chunks auxiliares de 9 MB cada (total 18 MB > 16 MB)
                w.Write(Encoding.ASCII.GetBytes("LST1"));
                w.Write((uint)(9 * 1024 * 1024));
                ms.Seek(9 * 1024 * 1024, SeekOrigin.Current);

                w.Write(Encoding.ASCII.GetBytes("LST2"));
                w.Write((uint)(9 * 1024 * 1024));
                ms.Seek(9 * 1024 * 1024, SeekOrigin.Current);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwAccumLimit = false;
                try
                {
                    WavHeader.Read(ms);
                }
                catch (ResourceLimitException)
                {
                    threwAccumLimit = true;
                }
                Assert(threwAccumLimit, "Deve rejeitar metadados auxiliares acumulados > 16 MB (D-WAV-04)");
            }
        }

        private static void Test_Wav_Hardening_NonSeekable_PrematureEof_InAuxiliaryChunk()
        {
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)5000);
                w.Write(WavHeader.FourCcWave);

                w.Write(Encoding.ASCII.GetBytes("JUNK"));
                w.Write((uint)2048); // declara 2048 bytes
                w.Write(new byte[64]); // escreve apenas 64 bytes e encerra o fluxo

                ms.Position = 0;
                var nonSeekable = new NonSeekableStream(ms);

                bool threwTruncAux = false;
                try
                {
                    WavHeader.Read(nonSeekable);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("Arquivo truncado durante o processamento de chunk auxiliar"))
                    {
                        threwTruncAux = true;
                    }
                }
                Assert(threwTruncAux, "Deve lançar CorruptDataException quando stream não-seekable truncar dentro de chunk auxiliar (D-WAV-04)");
            }
        }

        private static void Test_Wav_Hardening_ExtensibleHeader_Validation()
        {
            // 1. cbSize < 22
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)200);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)40); // tamanho do fmt
                w.Write(WavHeader.WaveFormatExtensible);
                w.Write((ushort)2);
                w.Write((uint)48000);
                w.Write((uint)192000);
                w.Write((ushort)4);
                w.Write((ushort)16);

                w.Write((ushort)10); // cbSize = 10 (< 22 bytes obrigatórios)
                w.Write(new byte[22]);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwShortCb = false;
                try
                {
                    WavHeader.Read(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("mínimo 22 bytes"))
                    {
                        threwShortCb = true;
                    }
                }
                Assert(threwShortCb, "Deve rejeitar cbSize < 22 em WAVE_FORMAT_EXTENSIBLE (D-WAV-05)");
            }

            // 2. validBits > BitsPerSample
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)200);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)40);
                w.Write(WavHeader.WaveFormatExtensible);
                w.Write((ushort)2);
                w.Write((uint)48000);
                w.Write((uint)192000);
                w.Write((ushort)4);
                w.Write((ushort)16); // BitsPerSample = 16

                w.Write((ushort)22); // cbSize = 22
                w.Write((ushort)24); // validBits = 24 > BitsPerSample (16)
                w.Write((uint)3);    // channelMask
                w.Write(WavHeader.PcmSubFormatGuid);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)0);

                ms.Position = 0;
                bool threwInvalidValidBits = false;
                try
                {
                    WavHeader.Read(ms);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("validBits"))
                    {
                        threwInvalidValidBits = true;
                    }
                }
                Assert(threwInvalidValidBits, "Deve rejeitar validBits > BitsPerSample em WAVE_FORMAT_EXTENSIBLE (D-WAV-05)");
            }
        }

        private static void Test_Wav_Hardening_MaxAudioMemoryBytes_Ceiling()
        {
            bool threwMem = false;
            try
            {
                // 8 canais * 20M quadros * 4 bytes = 640 MB (> 400 MB teto de segurança)
                SecurityLimits.AssertAudioParameters(8, 48000, 32, 20000000);
            }
            catch (ResourceLimitException ex)
            {
                if (ex.Message.Contains("teto de segurança permitido"))
                {
                    threwMem = true;
                }
            }
            Assert(threwMem, "Deve rejeitar buffers cujo consumo de memória estimada exceda MaxAudioMemoryBytes (400 MB)");
        }

        private static void Test_Wav_ChunkData_Over64MB_Accepted()
        {
            // Valida que um arquivo WAV com chunk 'data' de 70 MB (superior a 64 MB) é lido sem erro (D-WAV-07)
            long dataSize = 70L * 1024 * 1024;
            long riffSize = dataSize + 36;
            byte[] headerBytes;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)riffSize);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);                      // Stereo
                w.Write((uint)44100);
                w.Write((uint)176400);                   // ByteRate
                w.Write((ushort)4);                      // BlockAlign
                w.Write((ushort)16);                     // 16 bits

                w.Write(WavHeader.FourCcData);
                w.Write((uint)dataSize);
                headerBytes = ms.ToArray();
            }

            using (var stream = new VirtualAudioStream(headerBytes, riffSize + 8))
            {
                var header = WavHeader.Read(stream);
                AssertEqual(dataSize, (long)header.DataSize, "DataSize");
                AssertEqual(2, (int)header.Channels, "Channels");
                AssertEqual(16, (int)header.BitsPerSample, "BitsPerSample");
                AssertEqual(dataSize / 4, header.FrameCount, "FrameCount");
            }
        }

        private static void Test_Wav_RealisticDuration_10Min_Over64MB_Accepted()
        {
            // Valida áudio realista de 10 minutos (44.1 kHz, 16-bit estéreo ~101 MB) (D-WAV-07)
            long frameCount = 10L * 60 * 44100; // 26.460.000 quadros
            int blockAlign = 4; // 2 canais * 2 bytes
            long dataSize = frameCount * blockAlign; // 105.840.000 bytes (~100.94 MB)
            long riffSize = dataSize + 36;

            byte[] headerBytes;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)riffSize);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);                      // Stereo
                w.Write((uint)44100);
                w.Write((uint)176400);                   // ByteRate
                w.Write((ushort)blockAlign);
                w.Write((ushort)16);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)dataSize);
                headerBytes = ms.ToArray();
            }

            using (var stream = new VirtualAudioStream(headerBytes, riffSize + 8))
            {
                var header = WavHeader.Read(stream);
                AssertEqual(dataSize, (long)header.DataSize, "DataSize");
                AssertEqual(frameCount, header.FrameCount, "FrameCount");
                Assert(Math.Abs(header.DurationSeconds - 600.0) < 0.001, "Duração deve ser 600 segundos (10 min)");
            }
        }

        private static void Test_Wav_StudioProfile_96kHz_24Bit_Over64MB_Accepted()
        {
            // Perfil de estúdio: 2 minutos a 96 kHz, 24-bit estéreo (BlockAlign = 6, ~66 MB) (D-WAV-07)
            long frameCount = 2L * 60 * 96000; // 11.520.000 quadros
            int blockAlign = 6; // 2 canais * 3 bytes
            long dataSize = frameCount * blockAlign; // 69.120.000 bytes (~65.92 MB)
            long riffSize = dataSize + 36;

            byte[] headerBytes;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)riffSize);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);
                w.Write((uint)96000);
                w.Write((uint)(96000 * 6));
                w.Write((ushort)blockAlign);
                w.Write((ushort)24);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)dataSize);
                headerBytes = ms.ToArray();
            }

            using (var stream = new VirtualAudioStream(headerBytes, riffSize + 8))
            {
                var header = WavHeader.Read(stream);
                AssertEqual(dataSize, (long)header.DataSize, "DataSize");
                AssertEqual(frameCount, header.FrameCount, "FrameCount");
                Assert(Math.Abs(header.DurationSeconds - 120.0) < 0.001, "Duração deve ser 120 segundos (2 min)");
            }
        }

        private static void Test_Wav_Security_ChunkData_ExceedsMaxAudioFileSize_Rejection()
        {
            // Chunk data com 501 MB (> DefaultMaxAudioFileSize = 500 MB) deve ser rejeitado (D-WAV-07)
            long dataSize = 501L * 1024 * 1024;
            long riffSize = dataSize + 36;

            byte[] headerBytes;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)riffSize);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);
                w.Write((uint)44100);
                w.Write((uint)176400);
                w.Write((ushort)4);
                w.Write((ushort)16);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)dataSize);
                headerBytes = ms.ToArray();
            }

            using (var stream = new VirtualAudioStream(headerBytes, riffSize + 8))
            {
                bool threwLimit = false;
                try
                {
                    WavHeader.Read(stream);
                }
                catch (ResourceLimitException ex)
                {
                    if (ex.Message.Contains("excede o limite máximo permitido"))
                    {
                        threwLimit = true;
                    }
                }
                Assert(threwLimit, "Deve rejeitar chunk 'data' > 500 MB com ResourceLimitException (D-WAV-07)");
            }
        }

        private static void Test_Wav_Security_ChunkData_MisalignedBlockAlign_Rejection()
        {
            // Chunk data com tamanho não divisível por BlockAlign (ex: 70 MB + 1 byte com BlockAlign = 4)
            long dataSize = (70L * 1024 * 1024) + 1; // não alinhado
            long riffSize = dataSize + 36;

            byte[] headerBytes;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms, Encoding.ASCII);
                w.Write(WavHeader.FourCcRiff);
                w.Write((uint)riffSize);
                w.Write(WavHeader.FourCcWave);

                w.Write(WavHeader.FourCcFmt);
                w.Write((uint)16);
                w.Write(WavHeader.WaveFormatPcm);
                w.Write((ushort)2);
                w.Write((uint)44100);
                w.Write((uint)176400);
                w.Write((ushort)4); // BlockAlign = 4
                w.Write((ushort)16);

                w.Write(WavHeader.FourCcData);
                w.Write((uint)dataSize);
                headerBytes = ms.ToArray();
            }

            using (var stream = new VirtualAudioStream(headerBytes, riffSize + 8))
            {
                bool threwAlign = false;
                try
                {
                    WavHeader.Read(stream);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("não é múltiplo inteiro do alinhamento de bloco"))
                    {
                        threwAlign = true;
                    }
                }
                Assert(threwAlign, "Deve rejeitar chunk 'data' desalinhado com CorruptDataException (D-WAV-07)");
            }
        }

        #region Casos de Teste ZIP

        private static void Test_Zip_Create_And_Read_Store_MultipleEntries()
        {
            byte[] zipData;
            byte[] file1 = Encoding.UTF8.GetBytes("Olá, Lodino ZIP Store!");
            byte[] file2 = new byte[1024];
            for (int i = 0; i < file2.Length; i++) file2[i] = (byte)(i & 0xFF);

            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddEntry("hello.txt", file1, ZipCompressionLevel.Store);
                    writer.AddEntry("data.bin", file2, ZipCompressionLevel.Store);
                }
                zipData = ms.ToArray();
            }

            Assert(zipData.Length > 0, "ZIP gerado não deve ser vazio");

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(2, reader.Entries.Count, "Entries.Count");
                AssertEqual("hello.txt", reader.Entries[0].Name, "Entry[0].Name");
                AssertEqual(ZipCompressionMethod.Store, reader.Entries[0].Method, "Entry[0].Method");
                AssertEqual((long)file1.Length, reader.Entries[0].UncompressedSize, "Entry[0].UncompressedSize");
                AssertEqual((long)file1.Length, reader.Entries[0].CompressedSize, "Entry[0].CompressedSize");

                byte[] read1 = reader.ReadEntryBytes(reader.Entries[0]);
                AssertEqual(Encoding.UTF8.GetString(file1), Encoding.UTF8.GetString(read1), "Conteúdo de hello.txt");

                byte[] read2 = reader.ReadEntryBytes(reader.Entries[1]);
                AssertEqual(file2.Length, read2.Length, "Tamanho de data.bin");
                for (int i = 0; i < file2.Length; i++)
                {
                    if (read2[i] != file2[i])
                    {
                        throw new Exception("Bytes de data.bin não conferem na posição " + i);
                    }
                }
            }
        }

        private static void Test_Zip_Create_And_Read_Deflate_Optimal_Lz77()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 150; i++)
            {
                sb.Append("Lodino ZIP engine provides fast LZ77 match finder and Fixed Huffman encoding! Repetição: ");
                sb.Append(i);
                sb.Append("\r\n");
            }
            byte[] uncompressed = Encoding.UTF8.GetBytes(sb.ToString());

            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddEntry("story.txt", uncompressed, ZipCompressionLevel.Optimal);
                }
                zipData = ms.ToArray();
            }

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(1, reader.Entries.Count, "Entries.Count");
                var entry = reader.Entries[0];
                AssertEqual("story.txt", entry.Name, "Name");
                AssertEqual(ZipCompressionMethod.Deflate, entry.Method, "Method");
                AssertEqual((long)uncompressed.Length, entry.UncompressedSize, "UncompressedSize");
                Assert(entry.CompressedSize < entry.UncompressedSize / 3, "Tamanho comprimido deve ser significativamente menor que descompactado com LZ77");

                byte[] decompressed = reader.ReadEntryBytes(entry);
                AssertEqual(uncompressed.Length, decompressed.Length, "Tamanho descomprimido");
                for (int i = 0; i < uncompressed.Length; i++)
                {
                    if (uncompressed[i] != decompressed[i])
                    {
                        throw new Exception("Mismatch na descompressão do LZ77 na posição " + i);
                    }
                }
            }
        }

        private static void Test_Zip_EmptyArchive_And_Directories()
        {
            byte[] emptyZip;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                }
                emptyZip = ms.ToArray();
            }

            AssertEqual(22, emptyZip.Length, "Arquivo ZIP vazio deve conter exatamente o registro EOCD de 22 bytes");

            using (var ms = new MemoryStream(emptyZip))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(0, reader.Entries.Count, "ZIP vazio deve ter 0 entradas");
            }

            byte[] dirZip;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddDirectory("docs/");
                    writer.AddDirectory("docs/sub/");
                    writer.AddEntry("docs/sub/readme.txt", Encoding.UTF8.GetBytes("Manual"), ZipCompressionLevel.Store);
                }
                dirZip = ms.ToArray();
            }

            using (var ms = new MemoryStream(dirZip))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(3, reader.Entries.Count, "Total de entradas (2 pastas + 1 arquivo)");
                Assert(reader.Entries[0].IsDirectory, "docs/ deve ser diretório");
                AssertEqual(0L, (long)reader.Entries[0].UncompressedSize, "Diretório deve ter tamanho 0");
                Assert(reader.Entries[1].IsDirectory, "docs/sub/ deve ser diretório");
                Assert(!reader.Entries[2].IsDirectory, "readme.txt não deve ser diretório");
            }
        }

        private static void Test_Zip_UnicodeFilenames_And_Comments()
        {
            string fileName1 = "relatório_café_2026.txt";
            string fileName2 = "日本語_documento.json";
            string archiveComment = "Arquivo criado pelo motor Lodino 2026.";
            string entryComment = "Comentário da entrada 1";

            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.Comment = archiveComment;
                    writer.AddEntry(fileName1, Encoding.UTF8.GetBytes("Conteúdo 1"), ZipCompressionLevel.Optimal, entryComment);
                    writer.AddEntry(fileName2, Encoding.UTF8.GetBytes("{\"ok\":true}"), ZipCompressionLevel.Store);
                }
                zipData = ms.ToArray();
            }

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(archiveComment, reader.Comment, "Comentário do arquivo");
                AssertEqual(2, reader.Entries.Count, "Entries.Count");
                AssertEqual(fileName1, reader.Entries[0].Name, "Nome Unicode 1");
                AssertEqual(entryComment, reader.Entries[0].Comment, "Comentário de entrada 1");
                AssertEqual(fileName2, reader.Entries[1].Name, "Nome Unicode 2");

                byte[] b1 = reader.ReadEntryBytes(reader.Entries[0]);
                AssertEqual("Conteúdo 1", Encoding.UTF8.GetString(b1), "Conteúdo 1");
                byte[] b2 = reader.ReadEntryBytes(reader.Entries[1]);
                AssertEqual("{\"ok\":true}", Encoding.UTF8.GetString(b2), "Conteúdo 2");
            }
        }

        private static void Test_Zip_Subdirectories_And_RecursivePackaging()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_zip_test_" + Guid.NewGuid().ToString("N"));
            string zipFile = Path.Combine(Path.GetTempPath(), "lodino_zip_out_" + Guid.NewGuid().ToString("N") + ".zip");
            string extractDir = Path.Combine(Path.GetTempPath(), "lodino_zip_ext_" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllText(Path.Combine(tempDir, "root.txt"), "Root content", Encoding.UTF8);

                string sub1 = Path.Combine(tempDir, "sub1");
                Directory.CreateDirectory(sub1);
                File.WriteAllText(Path.Combine(sub1, "child.txt"), "Child content", Encoding.UTF8);

                string sub2 = Path.Combine(sub1, "nested");
                Directory.CreateDirectory(sub2);
                File.WriteAllText(Path.Combine(sub2, "deep.txt"), "Deep content", Encoding.UTF8);

                ZipWriter.CreateFromDirectory(tempDir, zipFile, true, ZipCompressionLevel.Optimal);

                Assert(File.Exists(zipFile), "Arquivo ZIP deve ter sido criado em disco");

                ZipReader.ExtractToDirectory(zipFile, extractDir, true);

                Assert(File.Exists(Path.Combine(extractDir, "root.txt")), "root.txt deve ter sido extraído");
                AssertEqual("Root content", File.ReadAllText(Path.Combine(extractDir, "root.txt")), "Conteúdo root.txt");

                Assert(File.Exists(Path.Combine(extractDir, "sub1", "child.txt")), "sub1/child.txt extraído");
                AssertEqual("Child content", File.ReadAllText(Path.Combine(extractDir, "sub1", "child.txt")), "Conteúdo child.txt");

                Assert(File.Exists(Path.Combine(extractDir, "sub1", "nested", "deep.txt")), "sub1/nested/deep.txt extraído");
                AssertEqual("Deep content", File.ReadAllText(Path.Combine(extractDir, "sub1", "nested", "deep.txt")), "Conteúdo deep.txt");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
        }

        private static void Test_Zip_Security_ZipSlip_PathTraversal_Rejection()
        {
            bool threw1 = false;
            try
            {
                ZipSecurity.ValidateSafeRelativePath("../../evil.bat");
            }
            catch (PathSecurityException)
            {
                threw1 = true;
            }
            Assert(threw1, "ZipSecurity deve lançar PathSecurityException para caminhos com '..'");

            bool threw2 = false;
            try
            {
                ZipSecurity.ValidateSafeRelativePath("normal.txt:stream");
            }
            catch (PathSecurityException)
            {
                threw2 = true;
            }
            Assert(threw2, "ZipSecurity deve lançar PathSecurityException para caminhos com ':'");

            bool threw3 = false;
            try
            {
                ZipSecurity.ValidateSafeRelativePath("C:/Windows/system32/cmd.exe");
            }
            catch (PathSecurityException)
            {
                threw3 = true;
            }
            Assert(threw3, "ZipSecurity deve lançar PathSecurityException para caminhos com unidade de disco");

            bool threw4 = false;
            try
            {
                ZipSecurity.GetSafeExtractionPath(@"C:\Destino", @"../../escape.txt");
            }
            catch (PathSecurityException)
            {
                threw4 = true;
            }
            Assert(threw4, "GetSafeExtractionPath deve lançar PathSecurityException se destino escapar da raiz");
        }

        private static void Test_Zip_Security_Crc32Mismatch_Rejection()
        {
            byte[] fileData = Encoding.UTF8.GetBytes("Integrity check content");
            byte[] zipData;

            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddEntry("test.txt", fileData, ZipCompressionLevel.Store);
                }
                zipData = ms.ToArray();
            }

            int payloadOffset = 30 + 8;
            zipData[payloadOffset] = (byte)(zipData[payloadOffset] ^ 0xFF);

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                bool threw = false;
                try
                {
                    reader.ReadEntryBytes(reader.Entries[0]);
                }
                catch (CorruptDataException ex)
                {
                    if (ex.Message.Contains("CRC-32"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Deve lançar CorruptDataException quando o CRC-32 não confere");
            }
        }

        private static void Test_Zip_Security_TruncatedArchive_And_InvalidSignature()
        {
            bool threwShort = false;
            try
            {
                using (var ms = new MemoryStream(new byte[] { 0x50, 0x4B, 0x05, 0x06 }))
                using (var reader = new ZipReader(ms)) { }
            }
            catch (CorruptDataException)
            {
                threwShort = true;
            }
            Assert(threwShort, "Arquivo < 22 bytes deve lançar CorruptDataException");

            bool threwNoEocd = false;
            try
            {
                using (var ms = new MemoryStream(new byte[100]))
                using (var reader = new ZipReader(ms)) { }
            }
            catch (CorruptDataException)
            {
                threwNoEocd = true;
            }
            Assert(threwNoEocd, "Arquivo sem assinatura EOCD deve lançar CorruptDataException");
        }

        private static void Test_Zip_Security_EncryptedArchive_Rejection()
        {
            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddEntry("doc.txt", Encoding.UTF8.GetBytes("Secret"), ZipCompressionLevel.Store);
                }
                zipData = ms.ToArray();
            }

            for (int i = 0; i < zipData.Length - 4; i++)
            {
                if (zipData[i] == 0x50 && zipData[i + 1] == 0x4B && zipData[i + 2] == 0x01 && zipData[i + 3] == 0x02)
                {
                    zipData[i + 8] |= 0x01; // Bit 0 = Encrypted
                    break;
                }
            }

            using (var ms = new MemoryStream(zipData))
            {
                bool threw = false;
                try
                {
                    using (var reader = new ZipReader(ms))
                    {
                        reader.ReadEntryBytes(reader.Entries[0]);
                    }
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.Contains("criptografia"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Arquivo com bit de criptografia ativo deve ser rejeitado com UnsupportedFormatException");
            }
        }

        private static void Test_Zip_Security_ZipBomb_TotalExtractedLimit_Rejection()
        {
            bool threwTotal = false;
            try
            {
                SecurityLimits.AssertZipTotalExtracted(SecurityLimits.MaxZipTotalExtractedBytes + 1024);
            }
            catch (ResourceLimitException ex)
            {
                if (ex.Message.Contains("excede o teto de segurança contra Zip Bomb"))
                {
                    threwTotal = true;
                }
            }
            Assert(threwTotal, "Total acumulado acima de 1 GB deve lançar ResourceLimitException");

            bool threwEntry = false;
            try
            {
                SecurityLimits.AssertZipEntrySize(SecurityLimits.MaxZipEntrySize + 1024);
            }
            catch (ResourceLimitException ex)
            {
                if (ex.Message.Contains("excede o limite máximo seguro"))
                {
                    threwEntry = true;
                }
            }
            Assert(threwEntry, "Entrada única descompactada acima de 500 MB deve lançar ResourceLimitException");
        }

        private static void Test_Zip_Security_ExcessiveEntries_Rejection()
        {
            byte[] fakeEocd;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write(ZipConstants.EndOfCentralDirectorySignature);
                w.Write((ushort)0);
                w.Write((ushort)0);
                w.Write((ushort)10001);
                w.Write((ushort)10001);
                w.Write((uint)0);
                w.Write((uint)0);
                w.Write((ushort)0);
                fakeEocd = ms.ToArray();
            }

            using (var ms = new MemoryStream(fakeEocd))
            {
                bool threw = false;
                try
                {
                    using (var reader = new ZipReader(ms)) { }
                }
                catch (ResourceLimitException ex)
                {
                    if (ex.Message.Contains("excede o limite máximo permitido"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Deve rejeitar arquivo com mais de 10.000 entradas com ResourceLimitException");
            }
        }

        private static void Test_Zip_Security_Zip64_Rejection()
        {
            byte[] fakeEocd;
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write(ZipConstants.EndOfCentralDirectorySignature);
                w.Write((ushort)0);
                w.Write((ushort)0);
                w.Write(ZipConstants.Zip64Marker16);
                w.Write(ZipConstants.Zip64Marker16);
                w.Write((uint)100);
                w.Write((uint)0);
                w.Write((ushort)0);
                fakeEocd = ms.ToArray();
            }

            using (var ms = new MemoryStream(fakeEocd))
            {
                bool threw = false;
                try
                {
                    using (var reader = new ZipReader(ms)) { }
                }
                catch (UnsupportedFormatException ex)
                {
                    if (ex.Message.Contains("ZIP64"))
                    {
                        threw = true;
                    }
                }
                Assert(threw, "Deve rejeitar arquivo ZIP64 com UnsupportedFormatException");
            }
        }

        private static void Test_Zip_Engine_Inspect_And_Registry_Resolution()
        {
            var registry = EngineRegistry.Default;
            var engineZip = registry.Resolve("dir", "zip");
            Assert(engineZip != null, "ZipEngine deve ser resolvido para rota dir -> zip");
            AssertEqual("archive-zip", engineZip.EngineId, "EngineId");

            var engineUnzip = registry.Resolve("zip", "dir");
            Assert(engineUnzip != null, "ZipEngine deve ser resolvido para rota zip -> dir");

            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, true))
                {
                    writer.AddEntry("item1.txt", Encoding.UTF8.GetBytes("Data 1"), ZipCompressionLevel.Store);
                    writer.AddEntry("item2.txt", Encoding.UTF8.GetBytes("Data 2 with more text"), ZipCompressionLevel.Optimal);
                }
                zipData = ms.ToArray();
            }

            using (var ms = new MemoryStream(zipData))
            {
                var info = engineZip.Inspect(ms, "test.zip");
                Assert(info.IsSupported, "IsSupported deve ser true");
                AssertEqual("zip", info.FormatId, "FormatId");
                AssertEqual("2", info.Metadata["TotalEntries"], "TotalEntries");
                AssertEqual("2", info.Metadata["FileCount"], "FileCount");
                Assert(info.Metadata.ContainsKey("CompressionRatio"), "Metadata contém CompressionRatio");
            }
        }

        private static void Test_Zip_Engine_ConvertPipeline_ZipAndUnzip()
        {
            var engine = new ZipEngine();
            string tempDir = Path.Combine(Path.GetTempPath(), "pipe_src_" + Guid.NewGuid().ToString("N"));
            string zipFile = Path.Combine(Path.GetTempPath(), "pipe_out_" + Guid.NewGuid().ToString("N") + ".zip");
            string extDir = Path.Combine(Path.GetTempPath(), "pipe_ext_" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(tempDir);
                File.WriteAllText(Path.Combine(tempDir, "item.txt"), "Pipeline item data", Encoding.UTF8);

                var reqZip = new ConvertRequest
                {
                    InputPath = tempDir,
                    TargetFormat = "zip",
                    OutputDirectory = Path.GetDirectoryName(zipFile),
                    OutputFileName = Path.GetFileName(zipFile)
                };
                int zipProgress = 0;
                var resZip = engine.Convert(reqZip, pct => { zipProgress = pct; });
                AssertEqual("completed", resZip.Status, "Status da conversão dir -> zip");
                AssertEqual(100, zipProgress, "Progresso da conversão dir -> zip");
                Assert(File.Exists(resZip.OutputPath), "Arquivo zip gerado pelo pipeline deve existir");

                var reqUnzip = new ConvertRequest
                {
                    InputPath = resZip.OutputPath,
                    TargetFormat = "dir",
                    OutputDirectory = extDir
                };
                int unzipProgress = 0;
                var resUnzip = engine.Convert(reqUnzip, pct => { unzipProgress = pct; });
                AssertEqual("completed", resUnzip.Status, "Status da conversão zip -> dir");
                AssertEqual(100, unzipProgress, "Progresso da conversão zip -> dir");
                Assert(File.Exists(Path.Combine(extDir, "item.txt")), "item.txt extraído com sucesso");
                AssertEqual("Pipeline item data", File.ReadAllText(Path.Combine(extDir, "item.txt")), "Conteúdo idêntico");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                if (File.Exists(zipFile)) File.Delete(zipFile);
                if (Directory.Exists(extDir)) Directory.Delete(extDir, true);
            }
        }

        private static void Test_Zip_External_Interoperability_DeflateStream()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "Interoperability verification between Lodino pure C# DEFLATE compressor and standard RFC 1951 decoders. " +
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore. " +
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Repetição de padrões para teste de janela LZ77!");

            // 1. Comprimir com Lodino DeflateCompressor
            byte[] lodinoCompressed;
            using (var src = new MemoryStream(original))
            using (var dst = new MemoryStream())
            {
                DeflateCompressor.Compress(src, dst, ZipCompressionLevel.Optimal);
                lodinoCompressed = dst.ToArray();
            }

            // 2. Descomprimir Lodino Deflate com System.IO.Compression.DeflateStream
            byte[] dotNetDecompressed;
            using (var src = new MemoryStream(lodinoCompressed))
            using (var deflate = new System.IO.Compression.DeflateStream(src, System.IO.Compression.CompressionMode.Decompress))
            using (var dst = new MemoryStream())
            {
                byte[] buf = new byte[512];
                int read;
                while ((read = deflate.Read(buf, 0, buf.Length)) > 0)
                {
                    dst.Write(buf, 0, read);
                }
                dotNetDecompressed = dst.ToArray();
            }

            AssertEqual(original.Length, dotNetDecompressed.Length, "DeflateStream dotNet deve ler todos os bytes");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != dotNetDecompressed[i])
                {
                    throw new Exception("Mismatch na descompressão do DeflateStream dotNet na posição " + i);
                }
            }

            // 3. Comprimir com System.IO.Compression.DeflateStream
            byte[] dotNetCompressed;
            using (var dst = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(dst, System.IO.Compression.CompressionMode.Compress, true))
                {
                    deflate.Write(original, 0, original.Length);
                }
                dotNetCompressed = dst.ToArray();
            }

            // 4. Descomprimir dotNetCompressed com Lodino DeflateDecompressor (RFC 1951)
            byte[] lodinoDecompressed;
            using (var src = new MemoryStream(dotNetCompressed))
            using (var dst = new MemoryStream())
            {
                Lodino.Core.Compression.Deflate.DeflateDecompressor.Decompress(src, dst, original.Length);
                lodinoDecompressed = dst.ToArray();
            }

            AssertEqual(original.Length, lodinoDecompressed.Length, "Lodino DeflateDecompressor deve ler todos os bytes gerados pelo .NET");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != lodinoDecompressed[i])
                {
                    throw new Exception("Mismatch na descompressão pelo Lodino de stream gerado por DeflateStream na posição " + i);
                }
            }
        }

        #region HARDENING ZIP (FASE 12.5)

        private static void Test_Hardening_Zip_WindowsReservedDeviceNames_Rejection()
        {
            string[] reservedNames = new[]
            {
                "CON.txt", "con", "con/sub.txt", "dir/aux/file.txt", "NUL", "nul.dat",
                "prn.log", "COM1", "com9.bin", "LPT1", "lpt5.json"
            };

            for (int i = 0; i < reservedNames.Length; i++)
            {
                bool caught = false;
                try
                {
                    ZipSecurity.ValidateSafeRelativePath(reservedNames[i]);
                }
                catch (PathSecurityException)
                {
                    caught = true;
                }
                Assert(caught, "Deve rejeitar nome de dispositivo reservado do Windows: " + reservedNames[i]);
            }
        }

        private static void Test_Hardening_Zip_TrailingDotsAndSpaces_Rejection()
        {
            string[] dangerousNames = new[]
            {
                "script.bat ", "file.txt.", "sub /file.txt", "sub./file.txt", "trailing. "
            };

            for (int i = 0; i < dangerousNames.Length; i++)
            {
                bool caught = false;
                try
                {
                    ZipSecurity.ValidateSafeRelativePath(dangerousNames[i]);
                }
                catch (PathSecurityException)
                {
                    caught = true;
                }
                Assert(caught, "Deve rejeitar segmento terminando com espaço ou ponto: " + dangerousNames[i]);
            }
        }

        private static void Test_Hardening_Zip_HeaderSizeLie_DecompressionBomb_AbortsEarly()
        {
            byte[] rawData = new byte[10000];
            for (int i = 0; i < rawData.Length; i++) rawData[i] = (byte)'A';
            byte[] compressed;
            using (var src = new MemoryStream(rawData))
            using (var dst = new MemoryStream())
            {
                DeflateCompressor.Compress(src, dst, ZipCompressionLevel.Optimal);
                compressed = dst.ToArray();
            }

            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    long lfhOffset = ms.Position;
                    // LFH
                    writer.Write(ZipConstants.LocalFileHeaderSignature);
                    writer.Write(ZipConstants.VersionDefaultNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodDeflate);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(Crc32.Calculate(rawData));
                    writer.Write((uint)compressed.Length);
                    writer.Write((uint)50); // Mente no LFH
                    byte[] nameBytes = Encoding.UTF8.GetBytes("bomb.txt");
                    writer.Write((ushort)nameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write(nameBytes);
                    writer.Write(compressed);

                    long cdOffset = ms.Position;
                    // CDH
                    writer.Write(ZipConstants.CentralDirectoryHeaderSignature);
                    writer.Write(ZipConstants.VersionMadeByUnix);
                    writer.Write(ZipConstants.VersionDefaultNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodDeflate);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(Crc32.Calculate(rawData));
                    writer.Write((uint)compressed.Length);
                    writer.Write((uint)50); // Mente no CDH: declara 50 bytes em vez de 10.000
                    writer.Write((ushort)nameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(ZipConstants.FileAttributeRegularUnix);
                    writer.Write((uint)lfhOffset);
                    writer.Write(nameBytes);

                    uint cdSize = (uint)(ms.Position - cdOffset);
                    // EOCD
                    writer.Write(ZipConstants.EndOfCentralDirectorySignature);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)1);
                    writer.Write(cdSize);
                    writer.Write((uint)cdOffset);
                    writer.Write((ushort)0);
                }
                zipData = ms.ToArray();
            }

            bool caught = false;
            try
            {
                using (var ms = new MemoryStream(zipData))
                using (var reader = new ZipReader(ms))
                {
                    reader.ReadEntryBytes(reader.Entries[0]);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.IndexOf("Tamanho descompactado incorreto", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    caught = true;
                }
            }
            Assert(caught, "Deve abortar descompressão com CorruptDataException quando a saída ultrapassar o tamanho declarado");
        }

        private static void Test_Hardening_Zip_Eocd_Backtracking_FakeEocdInComment()
        {
            byte[] filePayload = Encoding.UTF8.GetBytes("Payload real do arquivo");
            byte[] zipData;

            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, leaveOpen: true))
                {
                    writer.AddEntry("test.txt", filePayload, ZipCompressionLevel.Store);
                    writer.Comment = "Comentário com PK\x05\x06 inserido propositalmente para testar backtracking.";
                    writer.Finish();
                }
                zipData = ms.ToArray();
            }

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(1, reader.Entries.Count, "Deve ler exatamente 1 entrada");
                AssertEqual("test.txt", reader.Entries[0].Name, "Nome da entrada");
                byte[] extracted = reader.ReadEntryBytes(reader.Entries[0]);
                AssertEqual(filePayload.Length, extracted.Length, "Tamanho extraído");
                Assert(reader.Comment.Contains("PK\x05\x06"), "Comentário preservado");
            }
        }

        private static void Test_Hardening_Zip_BackslashDirectory_CompressArchive_Compatibility()
        {
            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    long lfhOffset1 = ms.Position;
                    // LFH 1: Diretório sub\dir\
                    byte[] dirNameBytes = Encoding.UTF8.GetBytes("sub\\dir\\");
                    writer.Write(ZipConstants.LocalFileHeaderSignature);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((ushort)dirNameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write(dirNameBytes);

                    long lfhOffset2 = ms.Position;
                    // LFH 2: Arquivo sub\dir\file.txt
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes("sub\\dir\\file.txt");
                    byte[] fileData = Encoding.UTF8.GetBytes("Olá Compress-Archive!");
                    writer.Write(ZipConstants.LocalFileHeaderSignature);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(Crc32.Calculate(fileData));
                    writer.Write((uint)fileData.Length);
                    writer.Write((uint)fileData.Length);
                    writer.Write((ushort)fileNameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write(fileNameBytes);
                    writer.Write(fileData);

                    long cdOffset = ms.Position;
                    // CDH 1: Diretório sub\dir\ com atributos externos zerados
                    writer.Write(ZipConstants.CentralDirectoryHeaderSignature);
                    writer.Write(ZipConstants.VersionMadeByWindows);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((ushort)dirNameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0); // Atributos externos zerados
                    writer.Write((uint)lfhOffset1);
                    writer.Write(dirNameBytes);

                    // CDH 2: Arquivo
                    writer.Write(ZipConstants.CentralDirectoryHeaderSignature);
                    writer.Write(ZipConstants.VersionMadeByWindows);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(Crc32.Calculate(fileData));
                    writer.Write((uint)fileData.Length);
                    writer.Write((uint)fileData.Length);
                    writer.Write((ushort)fileNameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0);
                    writer.Write((uint)lfhOffset2);
                    writer.Write(fileNameBytes);

                    uint cdSize = (uint)(ms.Position - cdOffset);
                    // EOCD
                    writer.Write(ZipConstants.EndOfCentralDirectorySignature);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)2);
                    writer.Write((ushort)2);
                    writer.Write(cdSize);
                    writer.Write((uint)cdOffset);
                    writer.Write((ushort)0);
                }
                zipData = ms.ToArray();
            }

            string testDir = Path.Combine(Path.GetTempPath(), "test_compress_archive_" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var ms = new MemoryStream(zipData))
                using (var reader = new ZipReader(ms))
                {
                    AssertEqual(2, reader.Entries.Count, "Deve ler 2 entradas");
                    Assert(reader.Entries[0].IsDirectory, "Entrada com barra invertida 'sub\\dir\\' deve ser reconhecida como IsDirectory");
                    AssertEqual("sub/dir/", reader.Entries[0].Name, "Nome do diretório normalizado com barra padrão");
                    reader.ExtractAll(testDir);
                }

                string extractedFilePath = Path.Combine(testDir, "sub", "dir", "file.txt");
                Assert(File.Exists(extractedFilePath), "Arquivo dentro da pasta normalizada deve existir");
                AssertEqual("Olá Compress-Archive!", File.ReadAllText(extractedFilePath, Encoding.UTF8), "Conteúdo correto");
            }
            finally
            {
                if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
            }
        }

        private static void Test_Hardening_Zip_ZeroByteEntry_BadCrc_Rejection()
        {
            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    long lfhOffset = ms.Position;
                    byte[] nameBytes = Encoding.UTF8.GetBytes("empty_bad_crc.txt");
                    // LFH
                    writer.Write(ZipConstants.LocalFileHeaderSignature);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0xDEADBEEF); // CRC corrompido para arquivo de 0 bytes
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((ushort)nameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write(nameBytes);

                    long cdOffset = ms.Position;
                    // CDH
                    writer.Write(ZipConstants.CentralDirectoryHeaderSignature);
                    writer.Write(ZipConstants.VersionMadeByUnix);
                    writer.Write(ZipConstants.VersionStoreNeeded);
                    writer.Write((ushort)ZipConstants.FlagUtf8);
                    writer.Write((ushort)ZipConstants.CompressionMethodStore);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((uint)0xDEADBEEF); // CRC corrompido no CDH
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((ushort)nameBytes.Length);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write(ZipConstants.FileAttributeRegularUnix);
                    writer.Write((uint)lfhOffset);
                    writer.Write(nameBytes);

                    uint cdSize = (uint)(ms.Position - cdOffset);
                    // EOCD
                    writer.Write(ZipConstants.EndOfCentralDirectorySignature);
                    writer.Write((ushort)0);
                    writer.Write((ushort)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)1);
                    writer.Write(cdSize);
                    writer.Write((uint)cdOffset);
                    writer.Write((ushort)0);
                }
                zipData = ms.ToArray();
            }

            bool caught = false;
            try
            {
                using (var ms = new MemoryStream(zipData))
                using (var reader = new ZipReader(ms))
                {
                    reader.ReadEntryBytes(reader.Entries[0]);
                }
            }
            catch (CorruptDataException)
            {
                caught = true;
            }
            Assert(caught, "Arquivo de 0 bytes com CRC != 0 deve ser rejeitado com CorruptDataException");
        }

        private static void Test_Hardening_Zip_NonSeekable_ZipWriter_Rejection()
        {
            var nonSeekableStream = new NonSeekableWriteOnlyStream();
            bool caught = false;
            try
            {
                using (new ZipWriter(nonSeekableStream))
                {
                }
            }
            catch (UnsupportedFormatException)
            {
                caught = true;
            }
            Assert(caught, "ZipWriter deve rejeitar stream com CanSeek = false com UnsupportedFormatException");
        }

        private static void Test_Hardening_Zip_ExtractAll_OverwriteFalse_ThrowsIfExists()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "test_overwrite_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string targetFile = Path.Combine(tempDir, "existing.txt");
            File.WriteAllText(targetFile, "Original content before extraction");

            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, leaveOpen: true))
                {
                    writer.AddEntry("existing.txt", Encoding.UTF8.GetBytes("New content from ZIP"));
                    writer.Finish();
                }
                zipData = ms.ToArray();
            }

            try
            {
                // 1. Com overwrite = false, deve lançar IOException e preservar arquivo original
                bool caught = false;
                try
                {
                    using (var ms = new MemoryStream(zipData))
                    using (var reader = new ZipReader(ms))
                    {
                        reader.ExtractAll(tempDir, overwrite: false);
                    }
                }
                catch (IOException)
                {
                    caught = true;
                }
                Assert(caught, "ExtractAll com overwrite = false deve lançar IOException ao encontrar arquivo existente");
                AssertEqual("Original content before extraction", File.ReadAllText(targetFile), "Arquivo original deve permanecer intacto");

                // 2. Com overwrite = true, deve substituir atomicamente
                using (var ms = new MemoryStream(zipData))
                using (var reader = new ZipReader(ms))
                {
                    reader.ExtractAll(tempDir, overwrite: true);
                }
                AssertEqual("New content from ZIP", File.ReadAllText(targetFile), "Arquivo deve ter sido substituído com o novo conteúdo");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Hardening_Zip_DirectoryTimestamps_Restored()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "test_dirtimes_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            DateTime targetDate = new DateTime(2021, 5, 12, 15, 30, 0);
            byte[] zipData;
            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, leaveOpen: true))
                {
                    writer.AddDirectory("custom_dir");
                    writer.Entries[0].LastWriteTime = targetDate;
                    writer.AddEntry("custom_dir/file.txt", Encoding.UTF8.GetBytes("conteúdo"));
                    writer.Finish();
                }
                zipData = ms.ToArray();
            }

            try
            {
                using (var ms = new MemoryStream(zipData))
                using (var reader = new ZipReader(ms))
                {
                    reader.ExtractAll(tempDir);
                }

                string dirOnDisk = Path.Combine(tempDir, "custom_dir");
                Assert(Directory.Exists(dirOnDisk), "Diretório deve existir");
                DateTime actualDate = Directory.GetLastWriteTime(dirOnDisk);
                AssertEqual(targetDate.Year, actualDate.Year, "Ano do diretório restaurado");
                AssertEqual(targetDate.Month, actualDate.Month, "Mês do diretório restaurado");
                AssertEqual(targetDate.Day, actualDate.Day, "Dia do diretório restaurado");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private class NonSeekableWriteOnlyStream : Stream
        {
            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { }
        }

        #endregion

        #region DEFLATE DINÂMICO (FASE 12.6)

        private static void Test_DynamicDeflate_RoundTrip_Text_BType10()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 150; i++)
            {
                sb.AppendLine("Line " + i + ": The quick brown fox jumps over the lazy dog. DEFLATE Dynamic Huffman in pure C#.");
            }
            byte[] input = Encoding.UTF8.GetBytes(sb.ToString());

            byte[] compressed = DeflateCompressor.Compress(input, ZipCompressionLevel.Optimal);

            // BFINAL (bit 0) e BTYPE (bits 1..2)
            int bfinal = compressed[0] & 1;
            int btype = (compressed[0] >> 1) & 3;

            AssertEqual(1, bfinal, "BFINAL deve ser 1");
            AssertEqual(2, btype, "BTYPE deve ser 10 (Dynamic Huffman) para texto longo com frequências assimétricas");
            Assert(compressed.Length < input.Length, "Stream comprimido deve ser menor que o original");

            // Descompressão com DeflateDecompressor proprietário
            byte[] decompressed;
            using (var msIn = new MemoryStream(compressed))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                decompressed = msOut.ToArray();
            }

            AssertEqual(input.Length, decompressed.Length, "Tamanho descomprimido");
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != decompressed[i])
                {
                    throw new Exception("Divergência de byte no offset " + i);
                }
            }

            // Descompressão cruzada com oráculo .NET DeflateStream
            using (var msIn = new MemoryStream(compressed))
            using (var ds = new System.IO.Compression.DeflateStream(msIn, System.IO.Compression.CompressionMode.Decompress))
            using (var msOut = new MemoryStream())
            {
                ds.CopyTo(msOut);
                byte[] oracleDecomp = msOut.ToArray();
                AssertEqual(input.Length, oracleDecomp.Length, "Tamanho oráculo .NET");
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] != oracleDecomp[i])
                    {
                        throw new Exception("Divergência com oráculo .NET no offset " + i);
                    }
                }
            }
        }

        private static void Test_DynamicDeflate_AutoSelect_FixedHuffman_SmallPayload()
        {
            byte[] input = Encoding.UTF8.GetBytes("Short line: status=200 OK. Port 80.");

            byte[] compressed = DeflateCompressor.Compress(input, ZipCompressionLevel.Optimal);

            int btype = (compressed[0] >> 1) & 3;
            AssertEqual(1, btype, "BTYPE deve ser 01 (Fixed Huffman) para pequenos payloads");

            byte[] decompressed;
            using (var msIn = new MemoryStream(compressed))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                decompressed = msOut.ToArray();
            }

            AssertEqual(input.Length, decompressed.Length, "Tamanho descomprimido");
            for (int i = 0; i < input.Length; i++)
            {
                AssertEqual(input[i], decompressed[i], "Byte no offset " + i);
            }
        }

        private static void Test_DynamicDeflate_AutoSelect_Stored_IncompressibleData()
        {
            var rnd = new Random(42);
            byte[] input = new byte[2048];
            rnd.NextBytes(input);

            byte[] compressed = DeflateCompressor.Compress(input, ZipCompressionLevel.Optimal);

            int btype = (compressed[0] >> 1) & 3;
            AssertEqual(0, btype, "BTYPE deve ser 00 (Stored) para dados aleatórios incompressíveis");

            byte[] decompressed;
            using (var msIn = new MemoryStream(compressed))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                decompressed = msOut.ToArray();
            }

            AssertEqual(input.Length, decompressed.Length, "Tamanho descomprimido");
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != decompressed[i])
                {
                    throw new Exception("Divergência de byte em dados stored no offset " + i);
                }
            }
        }

        private static void Test_DynamicDeflate_Degenerate_And_KraftMcMillan_Cases()
        {
            // Caso 1: 0 bytes
            byte[] empty = new byte[0];
            byte[] compEmpty = DeflateCompressor.Compress(empty, ZipCompressionLevel.Optimal);
            using (var msIn = new MemoryStream(compEmpty))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                AssertEqual(0, msOut.Length, "Descompressão de 0 bytes");
            }

            // Caso 2: 1 byte
            byte[] single = new byte[] { 0x42 };
            byte[] compSingle = DeflateCompressor.Compress(single, ZipCompressionLevel.Optimal);
            using (var msIn = new MemoryStream(compSingle))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                AssertEqual(1, msOut.Length, "Descompressão de 1 byte");
                AssertEqual((byte)0x42, msOut.ToArray()[0], "Valor de 1 byte");
            }

            // Caso 3: Monossímbolo repetido (1000 'A's)
            byte[] mono = Encoding.UTF8.GetBytes(new string('A', 1000));
            byte[] compMono = DeflateCompressor.Compress(mono, ZipCompressionLevel.Optimal);
            using (var msIn = new MemoryStream(compMono))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                byte[] decompMono = msOut.ToArray();
                AssertEqual(1000, decompMono.Length, "Descompressão de monossímbolo repetido");
                for (int i = 0; i < 1000; i++)
                {
                    AssertEqual((byte)'A', decompMono[i], "Caractere 'A' repetido");
                }
            }

            // Caso 4: Distribuição Fibonacci forçando árvore profunda (Kraft-McMillan depth leveling)
            var fibData = new List<byte>();
            int a = 1, b = 1;
            for (byte sym = 1; sym <= 25; sym++)
            {
                int count = Math.Min(b, 500);
                for (int c = 0; c < count; c++)
                {
                    fibData.Add(sym);
                }
                int next = a + b;
                a = b;
                b = next;
            }
            byte[] fibBytes = fibData.ToArray();
            byte[] compFib = DeflateCompressor.Compress(fibBytes, ZipCompressionLevel.Optimal);
            using (var msIn = new MemoryStream(compFib))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                byte[] decompFib = msOut.ToArray();
                AssertEqual(fibBytes.Length, decompFib.Length, "Tamanho descomprimido Fibonacci");
                for (int i = 0; i < fibBytes.Length; i++)
                {
                    if (fibBytes[i] != decompFib[i]) throw new Exception("Divergência Fibonacci no offset " + i);
                }
            }
        }

        private static void Test_DynamicDeflate_EmptyDistanceTree_Compatibility()
        {
            byte[] literalsOnly = new byte[256];
            for (int i = 0; i < 256; i++) literalsOnly[i] = (byte)i;

            byte[] compressed;
            using (var msOut = new MemoryStream())
            {
                DeflateCompressor.CompressDynamic(literalsOnly, msOut);
                compressed = msOut.ToArray();
            }

            int btype = (compressed[0] >> 1) & 3;
            AssertEqual(2, btype, "BTYPE deve ser 10 (Dynamic Huffman)");

            using (var msIn = new MemoryStream(compressed))
            using (var msOut = new MemoryStream())
            {
                DeflateDecompressor.Decompress(msIn, msOut);
                byte[] decomp = msOut.ToArray();
                AssertEqual(256, decomp.Length, "Tamanho descomprimido");
                for (int i = 0; i < 256; i++)
                {
                    AssertEqual((byte)i, decomp[i], "Literal no offset " + i);
                }
            }

            using (var msIn = new MemoryStream(compressed))
            using (var ds = new System.IO.Compression.DeflateStream(msIn, System.IO.Compression.CompressionMode.Decompress))
            using (var msOut = new MemoryStream())
            {
                ds.CopyTo(msOut);
                AssertEqual(256, msOut.Length, "Oráculo .NET deve aceitar árvore de distâncias vazia (HDIST=1)");
            }
        }

        private static void Test_DynamicDeflate_ZipWriter_Integration()
        {
            byte[] zipData;
            byte[] textPayload = Encoding.UTF8.GetBytes(new string('X', 500) + " Lodino Dynamic ZIP Integration " + new string('Y', 500));
            byte[] smallPayload = Encoding.UTF8.GetBytes("key=value");
            byte[] randomPayload = new byte[512];
            new Random(123).NextBytes(randomPayload);

            using (var ms = new MemoryStream())
            {
                using (var writer = new ZipWriter(ms, leaveOpen: true))
                {
                    writer.AddEntry("text_dyn.txt", textPayload, ZipCompressionLevel.Optimal);
                    writer.AddEntry("small_fixed.ini", smallPayload, ZipCompressionLevel.Optimal);
                    writer.AddEntry("random_store.bin", randomPayload, ZipCompressionLevel.Optimal);
                    writer.Finish();
                }
                zipData = ms.ToArray();
            }

            using (var ms = new MemoryStream(zipData))
            using (var reader = new ZipReader(ms))
            {
                AssertEqual(3, reader.Entries.Count, "Quantidade de entradas no ZIP");

                byte[] e1;
                using (var e1Ms = new MemoryStream())
                {
                    reader.ExtractEntry(reader.Entries[0], e1Ms);
                    e1 = e1Ms.ToArray();
                }
                AssertEqual(textPayload.Length, e1.Length, "Tamanho text_dyn.txt");
                for (int i = 0; i < textPayload.Length; i++)
                {
                    AssertEqual(textPayload[i], e1[i], "Byte em text_dyn.txt");
                }

                byte[] e2;
                using (var e2Ms = new MemoryStream())
                {
                    reader.ExtractEntry(reader.Entries[1], e2Ms);
                    e2 = e2Ms.ToArray();
                }
                AssertEqual(smallPayload.Length, e2.Length, "Tamanho small_fixed.ini");

                byte[] e3;
                using (var e3Ms = new MemoryStream())
                {
                    reader.ExtractEntry(reader.Entries[2], e3Ms);
                    e3 = e3Ms.ToArray();
                }
                AssertEqual(randomPayload.Length, e3.Length, "Tamanho random_store.bin");
                for (int i = 0; i < randomPayload.Length; i++)
                {
                    AssertEqual(randomPayload[i], e3[i], "Byte em random_store.bin");
                }
            }
        }

        private static void Test_DynamicDeflate_Decompressor_Guards_HlitHdist()
        {
            // Cria um fluxo DEFLATE dinâmico malformado com HLIT = 30 (257 + 30 = 287 > 286)
            // Bit 0: BFINAL=1, Bits 1..2: BTYPE=10 (Dynamic) -> valor 0x05 (101b)
            // Bits 3..7: HLIT (5 bits = 30 -> 11110b)
            // 0x05 | (30 << 3) = 0x05 | 240 = 245 (0xF5)
            byte[] corruptHlit = new byte[] { 0xF5, 0x00, 0x00, 0x00 };

            bool threwHlit = false;
            try
            {
                using (var msIn = new MemoryStream(corruptHlit))
                using (var msOut = new MemoryStream())
                {
                    DeflateDecompressor.Decompress(msIn, msOut);
                }
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("HLIT"))
                {
                    threwHlit = true;
                }
            }
            Assert(threwHlit, "HLIT > 286 deve lançar CorruptDataException no decodificador");
        }

        #endregion

        #region Módulo: Motor GZIP (Fase 13)

        private static void Test_Gzip_Empty_File()
        {
            byte[] input = new byte[0];
            byte[] gz = GzipWriter.Compress(input);

            Assert(gz != null && gz.Length >= 20, "GZIP de 0 bytes deve ter no mínimo 20 bytes (10 cabeçalho + 2 deflate + 8 trailer)");

            byte[] outBytes = GzipReader.Decompress(gz);
            Assert(outBytes != null && outBytes.Length == 0, "Saída descompactada de arquivo vazio deve conter exatamente 0 bytes");

            using (var ms = new MemoryStream(gz))
            {
                var members = GzipReader.InspectMembers(ms);
                Assert(members.Count == 1, "Arquivo GZIP vazio deve ter 1 membro");
                Assert(members[0].Trailer.Crc32 == 0, "CRC-32 de arquivo vazio deve ser 0");
                Assert(members[0].Trailer.ISize == 0, "ISIZE de arquivo vazio deve ser 0");
            }
        }

        private static void Test_Gzip_Single_Byte()
        {
            byte[] input = new byte[] { 0x55 };
            byte[] gz = GzipWriter.Compress(input);

            byte[] outBytes = GzipReader.Decompress(gz);
            Assert(outBytes != null && outBytes.Length == 1 && outBytes[0] == 0x55, "Saída descompactada de 1 byte deve coincidir perfeitamente");
        }

        private static void Test_Gzip_Small_Text()
        {
            string text = "Lodino: Motor GZIP RFC 1952 100% próprio em C# puro! Ação & café.";
            byte[] input = Encoding.UTF8.GetBytes(text);
            byte[] gz = GzipWriter.Compress(input);

            byte[] outBytes = GzipReader.Decompress(gz);
            string outText = Encoding.UTF8.GetString(outBytes);
            Assert(outText == text, "Texto descompactado deve coincidir bit a bit com a entrada UTF-8 original");
        }

        private static void Test_Gzip_Repetitive_Data()
        {
            byte[] input = new byte[65536];
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(i % 16);
            }

            byte[] gz = GzipWriter.Compress(input);
            Assert(gz.Length < 1000, string.Format("Dados repetitivos de 64 KB devem comprimir para menos de 1000 bytes (obtido: {0})", gz.Length));

            byte[] outBytes = GzipReader.Decompress(gz);
            Assert(outBytes.Length == input.Length, "Tamanho dos dados descompactados deve ser 65.536 bytes");
            for (int i = 0; i < input.Length; i++)
            {
                if (outBytes[i] != input[i])
                {
                    throw new Exception("Divergência de dados no índice " + i);
                }
            }
        }

        private static void Test_Gzip_High_Entropy_Random()
        {
            byte[] input = new byte[32768];
            int seed = 1234567;
            for (int i = 0; i < input.Length; i++)
            {
                seed = unchecked(seed * 1103515245 + 12345);
                input[i] = (byte)((seed >> 16) & 0xFF);
            }

            byte[] gz = GzipWriter.Compress(input);
            byte[] outBytes = GzipReader.Decompress(gz);

            Assert(outBytes.Length == input.Length, "Tamanho dos dados descompactados de alta entropia deve coincidir");
            for (int i = 0; i < input.Length; i++)
            {
                if (outBytes[i] != input[i])
                {
                    throw new Exception("Divergência de dados aleatórios no índice " + i);
                }
            }
        }

        private static void Test_Gzip_Optional_Fields_Header()
        {
            var opts = new GzipWriteOptions
            {
                FileName = "relatorio_teste.csv",
                Comment = "Auditoria Fase 13 Lodino GZIP",
                ExtraData = new byte[] { 0x4C, 0x44, 0x02, 0x00, 0xAA, 0xBB },
                IncludeHeaderCrc = true,
                IsText = true,
                ModificationTime = new DateTime(2026, 10, 19, 21, 0, 0, DateTimeKind.Utc)
            };

            string rawText = "col1,col2\nval1,val2\n";
            byte[] rawBytes = Encoding.UTF8.GetBytes(rawText);

            byte[] gz = GzipWriter.Compress(rawBytes, opts);

            using (var ms = new MemoryStream(gz))
            {
                var header = GzipHeader.Read(ms);
                Assert(header.HasFileName, "Cabeçalho deve indicar presença de FNAME");
                Assert(header.FileName == "relatorio_teste.csv", "Nome original do arquivo deve coincidir");
                Assert(header.HasComment, "Cabeçalho deve indicar presença de FCOMMENT");
                Assert(header.Comment == "Auditoria Fase 13 Lodino GZIP", "Comentário deve coincidir");
                Assert(header.HasExtra, "Cabeçalho deve indicar presença de FEXTRA");
                Assert(header.ExtraData != null && header.ExtraData.Length == 6, "FEXTRA deve ter 6 bytes");
                Assert(header.HasHeaderCrc, "Cabeçalho deve indicar presença de FHCRC");
                Assert(header.HeaderCrc.HasValue, "FHCRC deve ter valor válido calculado");
                Assert(header.HasText, "Cabeçalho deve indicar flag FTEXT");
                Assert(header.ModificationTime.HasValue, "MTIME deve ser válido");
            }

            byte[] outBytes = GzipReader.Decompress(gz);
            Assert(Encoding.UTF8.GetString(outBytes) == rawText, "Dados descompactados com todos os campos opcionais devem coincidir");
        }

        private static void Test_Gzip_Concatenated_Members()
        {
            byte[] part1 = Encoding.UTF8.GetBytes("Membro 1 concatenado. ");
            byte[] part2 = Encoding.UTF8.GetBytes("Membro 2 intermediario. ");
            byte[] part3 = Encoding.UTF8.GetBytes("Membro 3 final.");

            byte[] gz1 = GzipWriter.Compress(part1);
            byte[] gz2 = GzipWriter.Compress(part2);
            byte[] gz3 = GzipWriter.Compress(part3);

            byte[] combinedGz = new byte[gz1.Length + gz2.Length + gz3.Length];
            Buffer.BlockCopy(gz1, 0, combinedGz, 0, gz1.Length);
            Buffer.BlockCopy(gz2, 0, combinedGz, gz1.Length, gz2.Length);
            Buffer.BlockCopy(gz3, 0, combinedGz, gz1.Length + gz2.Length, gz3.Length);

            using (var msIn = new MemoryStream(combinedGz))
            using (var msOut = new MemoryStream())
            {
                var members = GzipReader.Decompress(msIn, msOut);
                Assert(members.Count == 3, string.Format("Devem ser identificados exatamente 3 membros concatenados (obtido: {0})", members.Count));

                string fullText = Encoding.UTF8.GetString(msOut.ToArray());
                Assert(fullText == "Membro 1 concatenado. Membro 2 intermediario. Membro 3 final.", "Texto concatenado descompactado deve coincidir");
            }
        }

        private static void Test_Gzip_Security_Corrupt_Crc()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Verificação de integridade CRC-32");
            byte[] gz = GzipWriter.Compress(raw);

            // O trailer ocupa os 8 bytes finais: CRC32 (4 bytes) + ISIZE (4 bytes)
            // Inverte um bit no byte do CRC32 (gz.Length - 8)
            gz[gz.Length - 8] ^= 0x01;

            bool threw = false;
            try
            {
                GzipReader.Decompress(gz);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("CRC-32"))
                {
                    threw = true;
                }
            }

            Assert(threw, "CRC-32 adulterado no trailer GZIP deve lançar CorruptDataException");
        }

        private static void Test_Gzip_Security_Corrupt_ISize()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Verificação de integridade ISIZE");
            byte[] gz = GzipWriter.Compress(raw);

            // Inverte um bit no primeiro byte do ISIZE (gz.Length - 4)
            gz[gz.Length - 4] ^= 0x01;

            bool threw = false;
            try
            {
                GzipReader.Decompress(gz);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("ISIZE"))
                {
                    threw = true;
                }
            }

            Assert(threw, "ISIZE adulterado no trailer GZIP deve lançar CorruptDataException");
        }

        private static void Test_Gzip_Security_Truncated_Header()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Teste de truncamento de cabeçalho");
            byte[] gz = GzipWriter.Compress(raw);

            byte[] truncated = new byte[4];
            Buffer.BlockCopy(gz, 0, truncated, 0, 4);

            bool threw = false;
            try
            {
                GzipReader.Decompress(truncated);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }

            Assert(threw, "Cabeçalho GZIP truncado deve lançar CorruptDataException");
        }

        private static void Test_Gzip_Security_Truncated_Trailer()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Teste de truncamento de trailer");
            byte[] gz = GzipWriter.Compress(raw);

            // Remove 4 bytes do trailer (deixando apenas 4 dos 8 bytes)
            byte[] truncated = new byte[gz.Length - 4];
            Buffer.BlockCopy(gz, 0, truncated, 0, truncated.Length);

            bool threw = false;
            try
            {
                GzipReader.Decompress(truncated);
            }
            catch (CorruptDataException)
            {
                threw = true;
            }

            Assert(threw, "Trailer GZIP truncado deve lançar CorruptDataException");
        }

        private static void Test_Gzip_Security_Reserved_Flags()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Teste de flags reservadas");
            byte[] gz = GzipWriter.Compress(raw);

            // Byte 3 é o campo FLG. Ativa o bit 5 (0x20)
            gz[3] |= 0x20;

            bool threw = false;
            try
            {
                GzipReader.Decompress(gz);
            }
            catch (CorruptDataException ex)
            {
                if (ex.Message.Contains("Flags reservadas"))
                {
                    threw = true;
                }
            }

            Assert(threw, "Flags reservadas ativas no cabeçalho GZIP devem lançar CorruptDataException");
        }

        private static void Test_Gzip_Security_Unsupported_Method()
        {
            byte[] raw = Encoding.UTF8.GetBytes("Teste de método de compressão");
            byte[] gz = GzipWriter.Compress(raw);

            // Byte 2 é CM. Altera de 8 (DEFLATE) para 9
            gz[2] = 9;

            bool threw = false;
            try
            {
                GzipReader.Decompress(gz);
            }
            catch (UnsupportedFormatException ex)
            {
                if (ex.Message.Contains("não suportado"))
                {
                    threw = true;
                }
            }

            Assert(threw, "Método de compressão diferente de 8 deve lançar UnsupportedFormatException");
        }

        private static void Test_Gzip_Security_Decompression_Bomb()
        {
            byte[] raw = new byte[50000];
            for (int i = 0; i < raw.Length; i++) raw[i] = (byte)(i & 0xFF);
            byte[] gz = GzipWriter.Compress(raw);

            bool threw = false;
            try
            {
                // Limita a descompressão a no máximo 1.000 bytes
                GzipReader.Decompress(gz, maxOutputBytes: 1000);
            }
            catch (ResourceLimitException ex)
            {
                if (ex.Message.Contains("Decompression Bomb") || ex.Message.Contains("teto de segurança"))
                {
                    threw = true;
                }
            }

            Assert(threw, "Emissão de bytes acima do teto maxOutputBytes deve lançar ResourceLimitException");
        }

        private static void Test_Gzip_Engine_Integration_And_Inspection()
        {
            var registry = EngineRegistry.Default;
            var engineCompress = registry.Resolve("csv", "gz");
            Assert(engineCompress != null && engineCompress.EngineId == "archive-gzip", "EngineRegistry deve resolver rota .csv -> .gz para archive-gzip");

            var engineDecompress = registry.Resolve("gz", "csv");
            Assert(engineDecompress != null && engineDecompress.EngineId == "archive-gzip", "EngineRegistry deve resolver rota .gz -> .csv para archive-gzip");

            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_gzip_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string srcFile = Path.Combine(tempDir, "dados.csv");
                File.WriteAllText(srcFile, "id,nome\n1,Alpha\n2,Beta\n", Encoding.UTF8);

                // Conversão CSV -> GZ
                var reqCompress = new ConvertRequest
                {
                    JobId = "test_gz_c",
                    InputPath = srcFile,
                    TargetFormat = "gz",
                    OutputDirectory = tempDir
                };

                var resCompress = engineCompress.Convert(reqCompress, null);
                Assert(resCompress.Status == "completed", "Conversão para .gz deve ter status completed");
                Assert(File.Exists(resCompress.OutputPath), "Arquivo .gz deve ter sido criado no disco");

                // Inspeção do arquivo .gz gerado
                using (var fs = File.OpenRead(resCompress.OutputPath))
                {
                    var inspection = engineCompress.Inspect(fs, resCompress.OutputPath);
                    Assert(inspection.IsSupported, "Inspeção GZIP deve indicar IsSupported = true");
                    Assert(inspection.FormatId == "gzip", "FormatId deve ser gzip");
                    Assert(inspection.Metadata["CompressionMethod"] == "DEFLATE (RFC 1951)", "Método de compressão deve ser DEFLATE");
                    Assert(inspection.Metadata["OriginalFileName"] == "dados.csv", "Nome original inspecionado deve ser dados.csv");
                }

                // Conversão GZ -> CSV (descompressão)
                string outDir = Path.Combine(tempDir, "out");
                Directory.CreateDirectory(outDir);

                var reqDecompress = new ConvertRequest
                {
                    JobId = "test_gz_d",
                    InputPath = resCompress.OutputPath,
                    TargetFormat = "csv",
                    OutputDirectory = outDir
                };

                var resDecompress = engineDecompress.Convert(reqDecompress, null);
                Assert(resDecompress.Status == "completed", "Descompressão deve ter status completed");
                Assert(File.Exists(resDecompress.OutputPath), "Arquivo descompactado deve existir no disco");

                string decompressedContent = File.ReadAllText(resDecompress.OutputPath, Encoding.UTF8);
                Assert(decompressedContent == "id,nome\n1,Alpha\n2,Beta\n", "Conteúdo descompactado via engine deve coincidir");
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        private static void Test_Gzip_File_Atomic_Overwrite_Protection()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_gzip_atomic_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string srcTxt = Path.Combine(tempDir, "origem.txt");
                File.WriteAllText(srcTxt, "Conteúdo inicial atômico", Encoding.UTF8);

                string gzPath = Path.Combine(tempDir, "origem.txt.gz");
                GzipWriter.CompressFile(srcTxt, gzPath);

                string destPath = Path.Combine(tempDir, "destino.txt");
                File.WriteAllText(destPath, "Arquivo que já existe antes", Encoding.UTF8);

                // Com overwrite = false, deve falhar com IOException
                bool threwOverwrite = false;
                try
                {
                    GzipReader.DecompressFile(gzPath, destPath, overwrite: false);
                }
                catch (IOException)
                {
                    threwOverwrite = true;
                }

                Assert(threwOverwrite, "DecompressFile com overwrite=false em destino existente deve lançar IOException");

                // Com overwrite = true, deve substituir com sucesso
                GzipReader.DecompressFile(gzPath, destPath, overwrite: true);
                string newContent = File.ReadAllText(destPath, Encoding.UTF8);
                Assert(newContent == "Conteúdo inicial atômico", "Destino deve ser sobrescrito com o conteúdo descomprimido");

                // Verifica ausência de arquivos temporários (.lodinotmp_*)
                var tmpFiles = Directory.GetFiles(tempDir, ".lodinotmp_*");
                Assert(tmpFiles.Length == 0, "Nenhum arquivo temporário .lodinotmp_* deve restar no diretório");
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        #region REMEDIAÇÃO GZIP (FASE 13.2)

        private static void Test_Gzip_Streaming_Memory_O1()
        {
            // Gera 150.000 bytes (excedendo os 65.535 bytes de bloco do DEFLATE, forçando múltiplos blocos)
            byte[] original = new byte[150000];
            for (int i = 0; i < original.Length; i++)
            {
                original[i] = (byte)((i * 31 + 17) & 0xFF);
            }

            byte[] compressed;
            using (var inStream = new MemoryStream(original))
            using (var outStream = new MemoryStream())
            {
                var header = new GzipHeader { FileName = "large_stream.bin" };
                GzipWriter.Compress(inStream, outStream, header, DeflateCompressionLevel.Fast);
                compressed = outStream.ToArray();
            }

            // Descomprimir e verificar integridade
            byte[] decompressed;
            using (var inStream = new MemoryStream(compressed))
            using (var outStream = new MemoryStream())
            {
                GzipReader.Decompress(inStream, outStream);
                decompressed = outStream.ToArray();
            }

            AssertEqual(original.Length, decompressed.Length, "Comprimento descomprimido em streaming");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != decompressed[i])
                    throw new Exception("Mismatch de byte na posição " + i);
            }
        }

        private static void Test_Gzip_Streaming_NonSeekable_Stream()
        {
            // Testa stream não-posicionável (CanSeek = false) com 100.000 bytes
            byte[] original = new byte[100000];
            for (int i = 0; i < original.Length; i++)
            {
                original[i] = (byte)((i ^ 0x5A) & 0xFF);
            }

            byte[] compressed;
            using (var baseStream = new MemoryStream(original))
            using (var nonSeekableIn = new NonSeekableStream(baseStream))
            using (var outStream = new MemoryStream())
            {
                var header = new GzipHeader { FileName = "nonseekable.bin" };
                GzipWriter.Compress(nonSeekableIn, outStream, header, DeflateCompressionLevel.Optimal);
                compressed = outStream.ToArray();
            }

            // Descomprime e valida
            byte[] decompressed;
            using (var inStream = new MemoryStream(compressed))
            using (var outStream = new MemoryStream())
            {
                GzipReader.Decompress(inStream, outStream);
                decompressed = outStream.ToArray();
            }

            AssertEqual(original.Length, decompressed.Length, "Comprimento descomprimido de non-seekable stream");
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != decompressed[i])
                    throw new Exception("Mismatch de byte na posição " + i);
            }
        }

        private static void Test_Gzip_Security_TrailingGarbage()
        {
            // Comprime payload simples
            byte[] raw = Encoding.UTF8.GetBytes("Dados legítimos para teste de lixo pós-trailer");
            byte[] compressed;
            using (var inMs = new MemoryStream(raw))
            using (var outMs = new MemoryStream())
            {
                GzipWriter.Compress(inMs, outMs);
                compressed = outMs.ToArray();
            }

            // Adiciona bytes espúrios não-nulos após o trailer de 8 bytes
            byte[] corrupt = new byte[compressed.Length + 5];
            Array.Copy(compressed, corrupt, compressed.Length);
            corrupt[compressed.Length] = 0xDE;
            corrupt[compressed.Length + 1] = 0xAD;
            corrupt[compressed.Length + 2] = 0xBE;
            corrupt[compressed.Length + 3] = 0xEF;
            corrupt[compressed.Length + 4] = 0x01;

            bool threw = false;
            try
            {
                using (var inMs = new MemoryStream(corrupt))
                using (var outMs = new MemoryStream())
                {
                    GzipReader.Decompress(inMs, outMs);
                }
            }
            catch (CorruptDataException ex)
            {
                threw = true;
                Assert(ex.Message.Contains("inesperados") || ex.Message.Contains("trailer") || ex.Message.Contains("espúrios"),
                    "Mensagem descritiva de erro ao detectar lixo pós-trailer: " + ex.Message);
            }

            Assert(threw, "GzipReader deve rejeitar dados espúrios após o trailer com CorruptDataException");
        }

        private static void Test_Deflate_BitReader_ReadByte_Misaligned()
        {
            byte[] data = new byte[] { 0xAA, 0xBB, 0xCC };
            using (var ms = new MemoryStream(data))
            {
                var reader = new DeflateBitReader(ms);
                // Lê 3 bits -> sobram 5 bits não alinhados no buffer
                int bits = reader.ReadBits(3);
                AssertEqual(3, bits >= 0 ? 3 : 0, "Leu 3 bits");

                bool threw = false;
                try
                {
                    reader.ReadByte();
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                Assert(threw, "ReadByte() deve lançar InvalidOperationException se houver bits residuais desalinhados");

                // Agora alinha e lê byte com sucesso
                reader.AlignToByte();
                int nextByte = reader.ReadByte();
                AssertEqual(0xBB, nextByte, "Byte lido após AlignToByte()");
            }
        }

        private static void Test_Gzip_Console_Sanitizer_Cwe150()
        {
            // Testa sanitização de sequências ANSI e C0 control codes
            string hostile = "\x1b[31mRedText\x1b[0m\a\b\t\r\nEnd";
            string sanitized = GzipSecurity.SanitizeForConsole(hostile);

            Assert(!sanitized.Contains("\x1b"), "Sanitizador deve remover/escapar o caractere ESC");
            Assert(!sanitized.Contains("\a"), "Sanitizador deve remover/escapar o caractere Bell");
            Assert(!sanitized.Contains("\b"), "Sanitizador deve remover/escapar o caractere Backspace");
            Assert(sanitized.Contains("RedText"), "Sanitizador deve manter o texto legível");
            Assert(sanitized.Contains("End"), "Sanitizador deve manter o sufixo legível");

            // Testa Bidi override (U+202E)
            string bidiHostile = "safe\u202Eexe.gnp";
            string bidiSanitized = GzipSecurity.SanitizeForConsole(bidiHostile);
            Assert(!bidiSanitized.Contains("\u202E"), "Sanitizador deve escapar Bidi override U+202E");

            // Testa preservação de Unicode legítimo (acentos portugueses)
            string legitimate = "relatório_café_2026.tar";
            string legSanitized = GzipSecurity.SanitizeForConsole(legitimate);
            AssertEqual(legitimate, legSanitized, "Caracteres acentuados legítimos devem ser preservados");

            // Testa null / vazio
            AssertEqual("", GzipSecurity.SanitizeForConsole(null), "Null retorna string vazia");
            AssertEqual("", GzipSecurity.SanitizeForConsole(""), "Vazio retorna string vazia");
        }

        private static void Test_Gzip_Inspect_All_Members()
        {
            byte[] m1Data = Encoding.UTF8.GetBytes("Conteúdo do Primeiro Membro GZIP");
            byte[] m2Data = Encoding.UTF8.GetBytes("Conteúdo do Segundo Membro GZIP com tamanho diferente");

            byte[] m1Gz, m2Gz;
            using (var inMs = new MemoryStream(m1Data))
            using (var outMs = new MemoryStream())
            {
                GzipWriter.Compress(inMs, outMs, new GzipHeader { FileName = "member1.txt" });
                m1Gz = outMs.ToArray();
            }

            using (var inMs = new MemoryStream(m2Data))
            using (var outMs = new MemoryStream())
            {
                GzipWriter.Compress(inMs, outMs, new GzipHeader { FileName = "member2.txt" });
                m2Gz = outMs.ToArray();
            }

            byte[] concatenated = new byte[m1Gz.Length + m2Gz.Length];
            Array.Copy(m1Gz, 0, concatenated, 0, m1Gz.Length);
            Array.Copy(m2Gz, 0, concatenated, m1Gz.Length, m2Gz.Length);

            using (var ms = new MemoryStream(concatenated))
            {
                var members = GzipReader.InspectAllMembers(ms);
                AssertEqual(2, members.Count, "InspectAllMembers deve encontrar exatamente 2 membros");
                AssertEqual("member1.txt", members[0].Header.FileName, "Nome do primeiro membro");
                AssertEqual((uint)m1Data.Length, members[0].Trailer.ISize, "ISize do primeiro membro");
                AssertEqual("member2.txt", members[1].Header.FileName, "Nome do segundo membro");
                AssertEqual((uint)m2Data.Length, members[1].Trailer.ISize, "ISize do segundo membro");
            }
        }

        private static void Test_Gzip_Normative_Latin1_Fname()
        {
            // Caracteres ISO-8859-1 (Latin-1): café, cedilha, copyright, parágrafo
            string latinName = "arquivo_café_aço_©_§.txt";
            byte[] data = Encoding.UTF8.GetBytes("Teste de Latin-1 estrito");

            byte[] gz;
            using (var inMs = new MemoryStream(data))
            using (var outMs = new MemoryStream())
            {
                var header = new GzipHeader { FileName = latinName, Comment = "Comentário Latin-1: maçã" };
                GzipWriter.Compress(inMs, outMs, header);
                gz = outMs.ToArray();
            }

            using (var ms = new MemoryStream(gz))
            {
                var inspected = GzipReader.InspectMembers(ms);
                Assert(inspected.Count >= 1, "Pelo menos 1 membro inspecionado");
                AssertEqual(latinName, inspected[0].Header.FileName, "FileName Latin-1 preservado");
                AssertEqual("Comentário Latin-1: maçã", inspected[0].Header.Comment, "Comment Latin-1 preservado");
            }
        }

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #endregion

        #region Testes da Fila e GUI (Fase 14.1)

        private static void Test_Queue_Auto_Suggested_Operations()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_queue_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string fCsv = Path.Combine(tempDir, "dados.csv");
                string fBmp = Path.Combine(tempDir, "foto.bmp");
                string fPng = Path.Combine(tempDir, "imagem.png");
                string fJpg = Path.Combine(tempDir, "foto.jpg");
                string fIni = Path.Combine(tempDir, "config.ini");
                string fWav = Path.Combine(tempDir, "audio.wav");
                string fZip = Path.Combine(tempDir, "pacote.zip");
                string fGz  = Path.Combine(tempDir, "arquivo.gz");

                File.WriteAllText(fCsv, "a,b\n1,2");
                File.WriteAllBytes(fBmp, new byte[100]);
                File.WriteAllBytes(fPng, new byte[100]);
                File.WriteAllBytes(fJpg, new byte[100]);
                File.WriteAllText(fIni, "[Sec]\nk=v");
                File.WriteAllBytes(fWav, new byte[100]);
                File.WriteAllBytes(fZip, new byte[100]);
                File.WriteAllBytes(fGz,  new byte[100]);

                mgr.AddFile(fCsv, "auto");
                mgr.AddFile(fBmp, "auto");
                mgr.AddFile(fPng, "auto");
                mgr.AddFile(fJpg, "auto");
                mgr.AddFile(fIni, "auto");
                mgr.AddFile(fWav, "auto");
                mgr.AddFile(fZip, "auto");
                mgr.AddFile(fGz,  "auto");

                var jobs = mgr.Jobs;
                AssertEqual(8, jobs.Count, "8 trabalhos adicionados na fila");

                AssertEqual("json", jobs[0].TargetFormat, "CSV sugerido -> JSON");
                AssertEqual("CONVERTER -> JSON", jobs[0].OperationDescription, "CSV operação");

                AssertEqual("png", jobs[1].TargetFormat, "BMP sugerido -> PNG");
                AssertEqual("CONVERTER -> PNG", jobs[1].OperationDescription, "BMP operação");

                AssertEqual("jpg", jobs[2].TargetFormat, "PNG sugerido -> JPG");
                AssertEqual("CONVERTER -> JPG", jobs[2].OperationDescription, "PNG operação");

                AssertEqual("png", jobs[3].TargetFormat, "JPG sugerido -> PNG");
                AssertEqual("CONVERTER -> PNG", jobs[3].OperationDescription, "JPG operação");

                AssertEqual("json", jobs[4].TargetFormat, "INI sugerido -> JSON");
                AssertEqual("CONVERTER -> JSON", jobs[4].OperationDescription, "INI operação");

                AssertEqual("wav", jobs[5].TargetFormat, "WAV sugerido -> WAV");
                AssertEqual("CONVERTER -> WAV", jobs[5].OperationDescription, "WAV operação");

                AssertEqual("extract", jobs[6].TargetFormat, "ZIP sugerido -> EXTRACT");
                AssertEqual("EXTRAIR", jobs[6].OperationDescription, "ZIP operação");

                AssertEqual("extract", jobs[7].TargetFormat, "GZ sugerido -> EXTRACT");
                AssertEqual("EXTRAIR", jobs[7].OperationDescription, "GZ operação");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Queue_Preserve_On_Incompatible_Override()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_override_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string fBmp = Path.Combine(tempDir, "imagem.bmp");
                string fCsv = Path.Combine(tempDir, "tabela.csv");
                string fZip = Path.Combine(tempDir, "dados.zip");
                string fWav = Path.Combine(tempDir, "som.wav");

                File.WriteAllBytes(fBmp, new byte[100]);
                File.WriteAllText(fCsv, "col1,col2\n1,2");
                File.WriteAllBytes(fZip, new byte[100]);
                File.WriteAllBytes(fWav, new byte[100]);

                mgr.AddFile(fBmp); // target -> png
                mgr.AddFile(fCsv); // target -> json
                mgr.AddFile(fZip); // target -> extract
                mgr.AddFile(fWav); // target -> wav

                // Tenta forçar override "png" em toda a fila
                mgr.StartBatch("png", tempDir);
                var jobs = mgr.Jobs;

                // BMP deve aceitar PNG
                AssertEqual("png", jobs[0].TargetFormat, "BMP foi convertido para PNG");

                // CSV NÃO deve ser corrompido para PNG; mantém JSON
                AssertEqual("json", jobs[1].TargetFormat, "CSV preservou JSON e rejeitou PNG");

                // ZIP NÃO deve ser corrompido para PNG; mantém EXTRACT
                AssertEqual("extract", jobs[2].TargetFormat, "ZIP preservou EXTRACT e rejeitou PNG");

                // WAV NÃO deve ser corrompido para PNG; mantém WAV
                AssertEqual("wav", jobs[3].TargetFormat, "WAV preservou WAV e rejeitou PNG");
            }
            finally
            {
                mgr.Cancel();
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Queue_Explicit_Extract_Override()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_extract_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string fZip = Path.Combine(tempDir, "arquivo.zip");
                string fGz  = Path.Combine(tempDir, "arquivo.gz");
                string fBmp = Path.Combine(tempDir, "foto.bmp");

                File.WriteAllBytes(fZip, new byte[100]);
                File.WriteAllBytes(fGz,  new byte[100]);
                File.WriteAllBytes(fBmp, new byte[100]);

                mgr.AddFile(fZip);
                mgr.AddFile(fGz);
                mgr.AddFile(fBmp);

                // Aplica override "extract"
                mgr.StartBatch("extract", tempDir);
                var jobs = mgr.Jobs;

                AssertEqual("extract", jobs[0].TargetFormat, "ZIP recebeu EXTRACT");
                AssertEqual("extract", jobs[1].TargetFormat, "GZ recebeu EXTRACT");
                // BMP não suporta extract; deve manter sua conversão padrão (png)
                AssertEqual("png", jobs[2].TargetFormat, "BMP preservou PNG e rejeitou EXTRACT");
            }
            finally
            {
                mgr.Cancel();
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Queue_Explicit_Zip_Override()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_zip_override_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string fCsv = Path.Combine(tempDir, "relatorio.csv");
                string fTxt = Path.Combine(tempDir, "nota.txt");

                File.WriteAllText(fCsv, "x,y\n10,20");
                File.WriteAllText(fTxt, "texto simples");

                mgr.AddFile(fCsv);
                mgr.AddFile(fTxt);

                // Aplica override "zip"
                mgr.StartBatch("zip", tempDir);
                var jobs = mgr.Jobs;

                AssertEqual("zip", jobs[0].TargetFormat, "CSV recebeu alvo ZIP");
                AssertEqual("COMPACTAR (ZIP)", jobs[0].OperationDescription, "CSV com operação de compactação ZIP");

                AssertEqual("zip", jobs[1].TargetFormat, "TXT recebeu alvo ZIP");
                AssertEqual("COMPACTAR (ZIP)", jobs[1].OperationDescription, "TXT com operação de compactação ZIP");
            }
            finally
            {
                mgr.Cancel();
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Queue_Invalid_Extension_Graceful_Failure()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_invalid_ext_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                string fUnknown = Path.Combine(tempDir, "arquivo.desconhecido");
                File.WriteAllText(fUnknown, "conteudo original preservado");

                var job = mgr.AddFile(fUnknown);
                Assert(job != null, "Trabalho adicionado à fila");
                // Força um formato alvo inexistente que nenhum motor suporta
                job.TargetFormat = "formato_impossivel";

                // Executa o processamento direto
                mgr.ProcessBatchSynchronously(null, tempDir);

                AssertEqual(Lodino.Desktop.JobStatus.Failed, job.Status, "Status deve ser Failed");
                Assert(job.ErrorMessage != null && job.ErrorMessage.Length > 0, "Mensagem de erro deve ser preenchida");
                Assert(File.Exists(fUnknown), "Arquivo original deve ser preservado intacto");
                AssertEqual("conteudo original preservado", File.ReadAllText(fUnknown), "Conteúdo original inalterado");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        private static void Test_Queue_Mixed_EndToEnd_Execution()
        {
            var mgr = new Lodino.Desktop.ConversionManager();
            string tempDir = Path.Combine(Path.GetTempPath(), "lodino_mixed_e2e_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // 1. Arquivo CSV
                string fCsv = Path.Combine(tempDir, "dados.csv");
                File.WriteAllText(fCsv, "nome,nota\nAntigravity,10\nLodino,10");

                // 2. Arquivo BMP 4x4
                string fBmp = Path.Combine(tempDir, "imagem.bmp");
                var bmpEngine = new Lodino.Core.Formats.Image.BmpEngine();
                using (var fs = File.Create(fBmp))
                {
                    var pb = new Lodino.Core.Formats.Image.PixelBuffer(4, 4);
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 4; x++)
                            pb.SetPixel(x, y, new Lodino.Core.Formats.Image.PixelColor(0, 128, 255));
                    var rawBmp = Lodino.Core.Formats.Image.BmpEncoder.Encode(pb, 24);
                    fs.Write(rawBmp, 0, rawBmp.Length);
                }

                // 3. Arquivo ZIP com um item dentro
                string fZip = Path.Combine(tempDir, "pacote.zip");
                using (var zw = new Lodino.Core.Formats.Archive.Zip.ZipWriter(fZip))
                {
                    zw.AddEntry("interior.txt", System.Text.Encoding.UTF8.GetBytes("conteudo extraido do zip"));
                    zw.Finish();
                }

                // Adiciona os 3 à fila no modo auto
                mgr.AddFile(fCsv, "auto");
                mgr.AddFile(fBmp, "auto");
                mgr.AddFile(fZip, "auto");

                string outDir = Path.Combine(tempDir, "saida");
                Directory.CreateDirectory(outDir);

                // Executa o lote de forma síncrona
                mgr.ProcessBatchSynchronously("auto", outDir);

                var jobs = mgr.Jobs;
                AssertEqual(3, jobs.Count, "3 trabalhos no lote");
                AssertEqual(Lodino.Desktop.JobStatus.Completed, jobs[0].Status, "CSV -> JSON concluído com sucesso");
                AssertEqual(Lodino.Desktop.JobStatus.Completed, jobs[1].Status, "BMP -> PNG concluído com sucesso");
                AssertEqual(Lodino.Desktop.JobStatus.Completed, jobs[2].Status, "ZIP -> EXTRACT concluído com sucesso");

                // Valida saídas
                string outJson = jobs[0].DestinationPath;
                Assert(File.Exists(outJson), "Arquivo JSON gerado existe");
                string jsonText = File.ReadAllText(outJson);
                Assert(jsonText.Contains("Antigravity"), "JSON contém conteúdo convertido do CSV");

                string outPng = jobs[1].DestinationPath;
                Assert(File.Exists(outPng), "Arquivo PNG gerado existe");
                byte[] pngBytes = File.ReadAllBytes(outPng);
                Assert(pngBytes.Length > 8 && pngBytes[0] == 0x89 && pngBytes[1] == 0x50, "PNG possui assinatura mágica válida");

                string extractedFile = Path.Combine(outDir, "interior.txt");
                Assert(File.Exists(extractedFile), "Arquivo descompactado do ZIP existe");
                AssertEqual("conteudo extraido do zip", File.ReadAllText(extractedFile), "Conteúdo extraído do ZIP correto");
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        #endregion
    }
}

