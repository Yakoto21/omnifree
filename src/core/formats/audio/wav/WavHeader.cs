using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio.Wav
{
    /// <summary>
    /// Descritor estrutural de cabeçalho e metadados de arquivos RIFF/WAVE PCM.
    /// Valida estrutura de chunks, alinhamento, taxas de amostragem e segurança.
    /// </summary>
    public class WavHeader
    {
        public const uint FourCcRiff = 0x46464952; // "RIFF"
        public const uint FourCcWave = 0x45564157; // "WAVE"
        public const uint FourCcFmt  = 0x20746D66; // "fmt "
        public const uint FourCcData = 0x61746164; // "data"

        public const ushort WaveFormatPcm = 1;
        public const ushort WaveFormatExtensible = 0xFFFE;

        // SubFormat GUID para PCM: 00000001-0000-0010-8000-00AA00389B71
        public static readonly byte[] PcmSubFormatGuid = new byte[]
        {
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00,
            0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71
        };

        public long RiffSize { get; set; }
        public ushort AudioFormat { get; set; }
        public ushort Channels { get; set; }
        public uint SampleRate { get; set; }
        public uint ByteRate { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }
        public long DataOffset { get; set; }
        public long DataSize { get; set; }
        public long FrameCount { get; set; }
        public double DurationSeconds { get; set; }
        public Dictionary<string, string> AuxiliaryChunks { get; private set; }

        public WavHeader()
        {
            AuxiliaryChunks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static WavHeader Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            long streamLength = stream.CanSeek ? stream.Length : -1;
            if (streamLength >= 0 && streamLength < 12)
            {
                throw new CorruptDataException("Arquivo truncado: tamanho menor que o cabeçalho RIFF mínimo (12 bytes).");
            }

            var header = new WavHeader();
            var reader = new BinaryReader(stream, Encoding.ASCII, true);

            // 1. Assinatura RIFF e WAVE
            uint riff = ReadUInt32(reader, "assinatura RIFF");
            if (riff != FourCcRiff)
            {
                throw new CorruptDataException("Assinatura mágica inválida: esperava 'RIFF'.");
            }

            header.RiffSize = ReadUInt32(reader, "tamanho do contêiner RIFF");
            if (header.RiffSize < 4)
            {
                throw new CorruptDataException("Tamanho RIFF inválido: o contêiner não comporta a assinatura 'WAVE'.");
            }
            if (header.RiffSize > SecurityLimits.DefaultMaxAudioFileSize)
            {
                throw new ResourceLimitException(string.Format(
                    "Tamanho declarado do container RIFF ({0:N0} bytes) excede o limite máximo permitido de {1:N0} bytes.",
                    header.RiffSize, SecurityLimits.DefaultMaxAudioFileSize));
            }

            long riffEnd = 8L + header.RiffSize;
            uint wave = ReadUInt32(reader, "assinatura WAVE");
            if (wave != FourCcWave)
            {
                throw new CorruptDataException("Formato RIFF inválido: esperava FourCC 'WAVE'.");
            }

            bool foundFmt = false;
            bool foundData = false;
            bool dataPadOutsideRiff = false;
            long totalAuxiliaryBytes = 0;
            int chunksProcessed = 0;

            // 2. Iteração de Chunks limitada ao contêiner RIFF declarado
            long streamPos = 12;
            while (!foundData)
            {
                if (riffEnd - streamPos < 8)
                {
                    break;
                }

                // Se o stream tiver tamanho conhecido e não houver mais 8 bytes para ler o próximo chunk
                if (streamLength >= 0 && streamPos + 8 > streamLength)
                {
                    break;
                }

                uint chunkId = ReadUInt32(reader, "identificador de chunk");
                uint chunkSize = ReadUInt32(reader, "tamanho de chunk");
                streamPos += 8;
                chunksProcessed++;
                if (chunksProcessed > SecurityLimits.MaxAudioChunkCount)
                {
                    throw new ResourceLimitException(string.Format(
                        "Quantidade de chunks RIFF excede o limite máximo permitido de {0:N0}.",
                        SecurityLimits.MaxAudioChunkCount));
                }

                string chunkIdStr = Encoding.ASCII.GetString(BitConverter.GetBytes(chunkId));

                if (chunkId != FourCcData && chunkSize > SecurityLimits.MaxSingleChunkSize)
                {
                    throw new ResourceLimitException(string.Format(
                        "Tamanho do chunk auxiliar '{0}' ({1:N0} bytes) excede o limite máximo permitido de {2:N0} bytes.",
                        chunkIdStr, chunkSize, SecurityLimits.MaxSingleChunkSize));
                }

                long chunkBytesRemaining = riffEnd - streamPos;
                if ((long)chunkSize > chunkBytesRemaining)
                {
                    throw new CorruptDataException(string.Format(
                        "Chunk '{0}' ultrapassa o limite do contêiner RIFF declarado.", chunkIdStr));
                }
                if ((chunkSize & 1u) != 0 && chunkBytesRemaining - (long)chunkSize < 1)
                {
                    if (chunkId == FourCcData)
                    {
                        dataPadOutsideRiff = true;
                    }
                    else
                    {
                        throw new CorruptDataException(string.Format(
                            "Chunk '{0}' ultrapassa o limite do contêiner RIFF declarado.", chunkIdStr));
                    }
                }

                if (chunkId == FourCcFmt)
                {
                    if (foundFmt)
                    {
                        throw new CorruptDataException("Múltiplos chunks 'fmt ' encontrados no arquivo RIFF/WAVE.");
                    }

                    if (chunkSize < 16)
                    {
                        throw new CorruptDataException(string.Format(
                            "Chunk 'fmt ' corrompido: tamanho de {0} bytes é menor que o mínimo padrão de 16 bytes.", chunkSize));
                    }

                    header.AudioFormat = ReadUInt16(reader, "wFormatTag do chunk 'fmt '");
                    header.Channels = ReadUInt16(reader, "contagem de canais do chunk 'fmt '");
                    header.SampleRate = ReadUInt32(reader, "taxa de amostragem do chunk 'fmt '");
                    header.ByteRate = ReadUInt32(reader, "byte rate do chunk 'fmt '");
                    header.BlockAlign = ReadUInt16(reader, "alinhamento de bloco do chunk 'fmt '");
                    header.BitsPerSample = ReadUInt16(reader, "profundidade de bits do chunk 'fmt '");
                    streamPos += 16;

                    long bytesLeftInChunk = chunkSize - 16;

                    // Suporte a WAVE_FORMAT_EXTENSIBLE se o SubFormat for PCM
                    if (header.AudioFormat == WaveFormatExtensible)
                    {
                        if (bytesLeftInChunk < 24)
                        {
                            throw new CorruptDataException("Cabeçalho WAVE_FORMAT_EXTENSIBLE truncado.");
                        }

                        ushort cbSize = ReadUInt16(reader, "cbSize do cabeçalho WAVE_FORMAT_EXTENSIBLE");
                        if (cbSize < 22)
                        {
                            throw new CorruptDataException(string.Format(
                                "Cabeçalho WAVE_FORMAT_EXTENSIBLE inválido: cbSize={0} (mínimo 22 bytes esperado).", cbSize));
                        }
                        if ((long)cbSize > bytesLeftInChunk - 2)
                        {
                            throw new CorruptDataException(string.Format(
                                "Cabeçalho WAVE_FORMAT_EXTENSIBLE inválido: cbSize={0}, mas o chunk 'fmt ' comporta apenas {1} bytes de extensão.",
                                cbSize, bytesLeftInChunk - 2));
                        }

                        ushort validBits = ReadUInt16(reader, "validBits do cabeçalho WAVE_FORMAT_EXTENSIBLE");
                        if (validBits != 0 && validBits > header.BitsPerSample)
                        {
                            throw new CorruptDataException(string.Format(
                                "WAVE_FORMAT_EXTENSIBLE inválido: validBits ({0}) maior que BitsPerSample ({1}).",
                                validBits, header.BitsPerSample));
                        }

                        uint channelMask = ReadUInt32(reader, "máscara de canais do cabeçalho WAVE_FORMAT_EXTENSIBLE");
                        byte[] subFormat = ReadBytesExact(reader, 16, "GUID do cabeçalho WAVE_FORMAT_EXTENSIBLE");
                        streamPos += 24;
                        bytesLeftInChunk -= 24;

                        bool isPcmGuid = true;
                        for (int i = 0; i < 16; i++)
                        {
                            if (subFormat[i] != PcmSubFormatGuid[i])
                            {
                                isPcmGuid = false;
                                break;
                            }
                        }

                        if (isPcmGuid)
                        {
                            header.AudioFormat = WaveFormatPcm; // Normaliza para PCM
                        }
                    }

                    // Pula qualquer byte remanescente do chunk 'fmt '
                    if (bytesLeftInChunk > 0)
                    {
                        SkipBytes(reader, bytesLeftInChunk);
                        streamPos += bytesLeftInChunk;
                    }

                    // Pad byte se chunkSize for ímpar
                    if (chunkSize % 2 != 0)
                    {
                        ReadByte(reader, "byte de preenchimento do chunk 'fmt '");
                        streamPos += 1;
                    }

                    foundFmt = true;
                }
                else if (chunkId == FourCcData)
                {
                    if (!foundFmt)
                    {
                        throw new CorruptDataException("Chunk 'data' encontrado antes do chunk 'fmt '.");
                    }

                    if (chunkSize > SecurityLimits.DefaultMaxAudioFileSize)
                    {
                        throw new ResourceLimitException(string.Format(
                            "Tamanho do chunk 'data' ({0:N0} bytes) excede o limite máximo permitido de {1:N0} bytes.",
                            chunkSize, SecurityLimits.DefaultMaxAudioFileSize));
                    }

                    header.DataSize = chunkSize;
                    header.DataOffset = streamPos;
                    foundData = true;
                    // Os dados de áudio começam imediatamente na posição atual do stream
                    break;
                }
                else
                {
                    // Chunk auxiliar (LIST, JUNK, bext, id3 , etc.)
                    totalAuxiliaryBytes += chunkSize;
                    if (totalAuxiliaryBytes > SecurityLimits.MaxAuxiliaryHeaderBytes)
                    {
                        throw new ResourceLimitException(string.Format(
                            "Tamanho acumulado de chunks auxiliares ({0:N0} bytes) excede o limite máximo de {1:N0} bytes.",
                            totalAuxiliaryBytes, SecurityLimits.MaxAuxiliaryHeaderBytes));
                    }

                    header.AuxiliaryChunks[chunkIdStr] = string.Format("{0} bytes", chunkSize);

                    // Pula o conteúdo do chunk
                    SkipBytes(reader, chunkSize);
                    streamPos += chunkSize;

                    // Pad byte se chunkSize for ímpar (regra RIFF word-alignment)
                    if (chunkSize % 2 != 0)
                    {
                        ReadByte(reader, "byte de preenchimento de chunk auxiliar");
                        streamPos += 1;
                    }
                }
            }

            if (!foundFmt)
            {
                throw new CorruptDataException("Chunk obrigatório 'fmt ' não encontrado no arquivo RIFF/WAVE.");
            }
            if (!foundData)
            {
                throw new CorruptDataException("Chunk obrigatório 'data' não encontrado no arquivo RIFF/WAVE.");
            }

            header.Validate(streamLength);
            if (dataPadOutsideRiff)
            {
                throw new CorruptDataException("Chunk 'data' ultrapassa o limite do contêiner RIFF declarado.");
            }
            if (streamLength >= 0 && riffEnd > streamLength)
            {
                throw new CorruptDataException(string.Format(
                    "Arquivo truncado: o contêiner RIFF declara terminar no offset {0}, mas o stream possui apenas {1} bytes.",
                    riffEnd, streamLength));
            }
            return header;
        }

        private static uint ReadUInt32(BinaryReader reader, string fieldName)
        {
            try
            {
                return reader.ReadUInt32();
            }
            catch (EndOfStreamException ex)
            {
                throw new CorruptDataException("Arquivo truncado durante a leitura de " + fieldName + ".", ex);
            }
        }

        private static ushort ReadUInt16(BinaryReader reader, string fieldName)
        {
            try
            {
                return reader.ReadUInt16();
            }
            catch (EndOfStreamException ex)
            {
                throw new CorruptDataException("Arquivo truncado durante a leitura de " + fieldName + ".", ex);
            }
        }

        private static byte ReadByte(BinaryReader reader, string fieldName)
        {
            try
            {
                return reader.ReadByte();
            }
            catch (EndOfStreamException ex)
            {
                throw new CorruptDataException("Arquivo truncado durante a leitura de " + fieldName + ".", ex);
            }
        }

        private static byte[] ReadBytesExact(BinaryReader reader, int count, string fieldName)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
            {
                throw new CorruptDataException(string.Format(
                    "Arquivo truncado durante a leitura de {0}: esperava {1} bytes, obtidos {2}.",
                    fieldName, count, bytes.Length));
            }
            return bytes;
        }

        private static void SkipBytes(BinaryReader reader, long count)
        {
            if (count <= 0) return;

            if (reader.BaseStream.CanSeek)
            {
                reader.BaseStream.Seek(count, SeekOrigin.Current);
            }
            else
            {
                byte[] buffer = new byte[Math.Min(8192, count)];
                long remaining = count;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(buffer.Length, remaining);
                    int read = reader.Read(buffer, 0, toRead);
                    if (read <= 0)
                    {
                        throw new CorruptDataException("Arquivo truncado durante o processamento de chunk auxiliar.");
                    }
                    remaining -= read;
                }
            }
        }

        public void Validate(long actualStreamLength = -1)
        {
            if (AudioFormat != WaveFormatPcm)
            {
                string formatName;
                switch (AudioFormat)
                {
                    case 2: formatName = "Microsoft ADPCM"; break;
                    case 3: formatName = "IEEE Float"; break;
                    case 6: formatName = "A-law"; break;
                    case 7: formatName = "Mu-law"; break;
                    case 0x0055: formatName = "MPEG Layer 3 (MP3)"; break;
                    case 0xFFFE: formatName = "WAVE_FORMAT_EXTENSIBLE (Não PCM)"; break;
                    default: formatName = string.Format("Código 0x{0:X4}", AudioFormat); break;
                }
                throw new UnsupportedFormatException(string.Format(
                    "Formato de áudio '{0}' não é suportado pelo motor nativo PCM (apenas PCM não-comprimido, wFormatTag = 1).",
                    formatName));
            }

            if (Channels <= 0 || Channels > SecurityLimits.MaxAudioChannels)
            {
                throw new CorruptDataException(string.Format(
                    "Contagem de canais inválida: {0} (suportado: 1 a {1}).",
                    Channels, SecurityLimits.MaxAudioChannels));
            }

            if (BitsPerSample != 8 && BitsPerSample != 16 && BitsPerSample != 24 && BitsPerSample != 32)
            {
                throw new UnsupportedFormatException(string.Format(
                    "Profundidade de bits de áudio {0} não é suportada (suportado: 8, 16, 24 ou 32 bits PCM).",
                    BitsPerSample));
            }

            int bytesPerSample = BitsPerSample / 8;
            int expectedBlockAlign = Channels * bytesPerSample;
            if (BlockAlign != expectedBlockAlign)
            {
                throw new CorruptDataException(string.Format(
                    "BlockAlign inválido no chunk fmt: {0} bytes (esperado: {1} canais * {2} bytes = {3} bytes).",
                    BlockAlign, Channels, bytesPerSample, expectedBlockAlign));
            }

            uint expectedByteRate = (uint)(SampleRate * BlockAlign);
            if (ByteRate != expectedByteRate)
            {
                // Em arquivos ligeiramente fora de padrão, podemos aceitar mas atualizar para o valor matematicamente correto
                ByteRate = expectedByteRate;
            }

            if (DataSize < 0)
            {
                throw new CorruptDataException("Tamanho do chunk 'data' é negativo.");
            }

            if (DataSize > SecurityLimits.DefaultMaxAudioFileSize)
            {
                throw new ResourceLimitException(string.Format(
                    "Tamanho do chunk 'data' ({0:N0} bytes) excede o limite máximo permitido de {1:N0} bytes.",
                    DataSize, SecurityLimits.DefaultMaxAudioFileSize));
            }

            if (BlockAlign > 0 && (DataSize % BlockAlign) != 0)
            {
                throw new CorruptDataException(string.Format(
                    "Tamanho do chunk 'data' ({0} bytes) não é múltiplo inteiro do alinhamento de bloco (BlockAlign = {1} bytes).",
                    DataSize, BlockAlign));
            }

            // Se o tamanho do stream for conhecido, validar se não ultrapassa o arquivo
            if (actualStreamLength > 0 && DataOffset + DataSize > actualStreamLength)
            {
                throw new CorruptDataException(string.Format(
                    "Arquivo truncado: o chunk 'data' requer {0} bytes, mas restam apenas {1} bytes no arquivo.",
                    DataSize, Math.Max(0, actualStreamLength - DataOffset)));
            }

            FrameCount = BlockAlign > 0 ? (DataSize / BlockAlign) : 0;
            SecurityLimits.AssertAudioParameters(Channels, (int)SampleRate, BitsPerSample, FrameCount);

            DurationSeconds = SampleRate > 0 ? (double)FrameCount / SampleRate : 0.0;
        }
    }
}
