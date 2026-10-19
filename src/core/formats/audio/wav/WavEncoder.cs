using System;
using System.IO;
using System.Text;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio.Wav
{
    /// <summary>
    /// Codificador proprietário de arquivos RIFF/WAVE PCM.
    /// Serializa AudioBuffer em formato canônico padrão (8, 16, 24 ou 32 bits PCM little-endian).
    /// Garante o preenchimento de alinhamento por palavra (pad byte) em chunks ímpares.
    /// </summary>
    public class WavEncoder
    {
        public static void Encode(AudioBuffer buffer, Stream outputStream, Action<int> onProgress = null)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            int channels = buffer.Channels;
            int sampleRate = buffer.SampleRate;
            int bitsPerSample = buffer.BitsPerSample;
            int frameCount = buffer.FrameCount;

            SecurityLimits.AssertAudioParameters(channels, sampleRate, bitsPerSample, frameCount);

            int bytesPerSample = bitsPerSample / 8;
            int blockAlign = channels * bytesPerSample;
            uint byteRate = (uint)(sampleRate * blockAlign);

            long dataSizeLong = checked((long)frameCount * blockAlign);
            if (dataSizeLong > uint.MaxValue - 100)
            {
                throw new ResourceLimitException("Tamanho do fluxo WAV excede o limite do contêiner RIFF de 32 bits (4 GB).");
            }
            uint dataSize = (uint)dataSizeLong;

            bool oddData = (dataSize % 2 != 0);
            uint riffSize = 4 + (8 + 16) + (8 + dataSize + (oddData ? 1u : 0u));

            var writer = new BinaryWriter(outputStream, Encoding.ASCII, true);

            // 1. Cabeçalho RIFF
            writer.Write(WavHeader.FourCcRiff);
            writer.Write(riffSize);
            writer.Write(WavHeader.FourCcWave);

            // 2. Chunk "fmt " (16 bytes padrão para PCM)
            writer.Write(WavHeader.FourCcFmt);
            writer.Write((uint)16);
            writer.Write(WavHeader.WaveFormatPcm);
            writer.Write((ushort)channels);
            writer.Write((uint)sampleRate);
            writer.Write(byteRate);
            writer.Write((ushort)blockAlign);
            writer.Write((ushort)bitsPerSample);

            // 3. Chunk "data"
            writer.Write(WavHeader.FourCcData);
            writer.Write(dataSize);

            // 4. Amostras PCM em blocos
            if (frameCount > 0)
            {
                const int framesPerChunk = 4096;
                int chunkSizeBytes = framesPerChunk * blockAlign;
                byte[] rawBuffer = new byte[chunkSizeBytes];

                int framesEncoded = 0;
                while (framesEncoded < frameCount)
                {
                    int framesToWrite = Math.Min(framesPerChunk, frameCount - framesEncoded);
                    int byteIndex = 0;

                    for (int f = 0; f < framesToWrite; f++)
                    {
                        int currentFrame = framesEncoded + f;
                        for (int c = 0; c < channels; c++)
                        {
                            int sampleQ31 = buffer.GetSampleQ31(c, currentFrame);

                            switch (bitsPerSample)
                            {
                                case 8:
                                {
                                    // 8-bit unsigned [0..255], centro em 128
                                    int b = (sampleQ31 >> 24) + 128;
                                    if (b < 0) b = 0;
                                    else if (b > 255) b = 255;
                                    rawBuffer[byteIndex++] = (byte)b;
                                    break;
                                }
                                case 16:
                                {
                                    // 16-bit signed [-32768..32767]
                                    int s = sampleQ31 >> 16;
                                    if (s < -32768) s = -32768;
                                    else if (s > 32767) s = 32767;
                                    rawBuffer[byteIndex++] = (byte)(s & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((s >> 8) & 0xFF);
                                    break;
                                }
                                case 24:
                                {
                                    // 24-bit signed [-8388608..8388607]
                                    int v = sampleQ31 >> 8;
                                    if (v < -8388608) v = -8388608;
                                    else if (v > 8388607) v = 8388607;
                                    rawBuffer[byteIndex++] = (byte)(v & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((v >> 8) & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((v >> 16) & 0xFF);
                                    break;
                                }
                                case 32:
                                {
                                    // 32-bit signed [-2147483648..2147483647]
                                    int v = sampleQ31;
                                    rawBuffer[byteIndex++] = (byte)(v & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((v >> 8) & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((v >> 16) & 0xFF);
                                    rawBuffer[byteIndex++] = (byte)((v >> 24) & 0xFF);
                                    break;
                                }
                                default:
                                    throw new UnsupportedFormatException(string.Format("Profundidade de {0} bits não suportada.", bitsPerSample));
                            }
                        }
                    }

                    int totalBytesToWrite = framesToWrite * blockAlign;
                    outputStream.Write(rawBuffer, 0, totalBytesToWrite);
                    framesEncoded += framesToWrite;

                    if (onProgress != null && frameCount > 0)
                    {
                        int pct = (int)((long)framesEncoded * 100 / frameCount);
                        onProgress(pct);
                    }
                }
            }

            // Pad byte se dataSize for ímpar (regra RIFF)
            if (oddData)
            {
                outputStream.WriteByte(0);
            }

            outputStream.Flush();
        }
    }
}
