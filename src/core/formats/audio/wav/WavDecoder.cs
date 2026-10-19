using System;
using System.IO;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio.Wav
{
    /// <summary>
    /// Decodificador proprietário de arquivos RIFF/WAVE PCM.
    /// Desempacota amostras inteiras de 8, 16, 24 e 32 bits para AudioBuffer normalizado.
    /// </summary>
    public class WavDecoder
    {
        public static AudioBuffer Decode(Stream stream, Action<int> onProgress = null)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            // Valida teto de arquivo
            if (stream.CanSeek)
            {
                SecurityLimits.AssertFileSize(stream.Length, SecurityLimits.DefaultMaxAudioFileSize);
            }

            WavHeader header = WavHeader.Read(stream);
            int channels = header.Channels;
            int sampleRate = (int)header.SampleRate;
            int bitsPerSample = header.BitsPerSample;
            int frameCount = (int)header.FrameCount;

            var buffer = new AudioBuffer(channels, sampleRate, bitsPerSample, frameCount);
            if (frameCount == 0)
            {
                if (onProgress != null) onProgress(100);
                return buffer;
            }

            int bytesPerSample = bitsPerSample / 8;
            int blockAlign = header.BlockAlign;

            // Buffer de leitura em blocos para alta performance
            const int framesPerChunk = 4096;
            int chunkSizeBytes = framesPerChunk * blockAlign;
            byte[] rawBuffer = new byte[chunkSizeBytes];

            int framesDecoded = 0;
            while (framesDecoded < frameCount)
            {
                int framesToRead = Math.Min(framesPerChunk, frameCount - framesDecoded);
                int bytesToRead = framesToRead * blockAlign;

                int offset = 0;
                while (offset < bytesToRead)
                {
                    int read = stream.Read(rawBuffer, offset, bytesToRead - offset);
                    if (read <= 0)
                    {
                        throw new CorruptDataException(string.Format(
                            "Dados de áudio PCM truncados: esperava {0} quadros, mas o arquivo terminou no quadro {1}.",
                            frameCount, framesDecoded));
                    }
                    offset += read;
                }

                // Desempacota as amostras do bloco para cada canal
                int byteIndex = 0;
                for (int f = 0; f < framesToRead; f++)
                {
                    int currentFrame = framesDecoded + f;
                    for (int c = 0; c < channels; c++)
                    {
                        int sampleQ31;
                        switch (bitsPerSample)
                        {
                            case 8:
                            {
                                byte b = rawBuffer[byteIndex++];
                                sampleQ31 = (b - 128) << 24;
                                break;
                            }
                            case 16:
                            {
                                short s = (short)(rawBuffer[byteIndex] | (rawBuffer[byteIndex + 1] << 8));
                                byteIndex += 2;
                                sampleQ31 = (int)s << 16;
                                break;
                            }
                            case 24:
                            {
                                int val = rawBuffer[byteIndex] |
                                          (rawBuffer[byteIndex + 1] << 8) |
                                          (rawBuffer[byteIndex + 2] << 16);
                                if ((val & 0x800000) != 0)
                                {
                                    val |= unchecked((int)0xFF000000);
                                }
                                byteIndex += 3;
                                sampleQ31 = val << 8;
                                break;
                            }
                            case 32:
                            {
                                int val = rawBuffer[byteIndex] |
                                          (rawBuffer[byteIndex + 1] << 8) |
                                          (rawBuffer[byteIndex + 2] << 16) |
                                          (rawBuffer[byteIndex + 3] << 24);
                                byteIndex += 4;
                                sampleQ31 = val;
                                break;
                            }
                            default:
                                throw new UnsupportedFormatException(string.Format("Profundidade de {0} bits não suportada.", bitsPerSample));
                        }

                        buffer.SetSampleQ31(c, currentFrame, sampleQ31);
                    }
                }

                framesDecoded += framesToRead;

                if (onProgress != null && frameCount > 0)
                {
                    int pct = (int)((long)framesDecoded * 100 / frameCount);
                    onProgress(pct);
                }
            }

            return buffer;
        }
    }
}
