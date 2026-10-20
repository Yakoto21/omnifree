using System;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio
{
    /// <summary>
    /// Representação nativa e desacoplada de dados de áudio PCM multi-canal em memória.
    /// Armazena amostras no formato de ponto fixo planar Q31 (signed 32-bit int: [-2147483648, 2147483647]),
    /// garantindo preservação bit-perfect para 8, 16, 24 e 32 bits PCM em passthrough e round-trip,
    /// com suporte a conversão de canais (Mono/Stereo), reamostragem, ganho e normalização.
    /// </summary>
    public class AudioBuffer
    {
        private readonly int[][] _channelData;
        private readonly int _channels;
        private readonly int _sampleRate;
        private int _bitsPerSample;
        private readonly int _frameCount;

        public int Channels { get { return _channels; } }
        public int SampleRate { get { return _sampleRate; } }
        public int BitsPerSample 
        { 
            get { return _bitsPerSample; } 
            set 
            { 
                if (value != 8 && value != 16 && value != 24 && value != 32)
                {
                    throw new UnsupportedFormatException(string.Format("Profundidade de bits {0} não é suportada.", value));
                }
                _bitsPerSample = value; 
            } 
        }
        public int FrameCount { get { return _frameCount; } }
        public double DurationSeconds 
        { 
            get { return _sampleRate > 0 ? (double)_frameCount / _sampleRate : 0.0; } 
        }

        public AudioBuffer(int channels, int sampleRate, int bitsPerSample, int frameCount)
        {
            SecurityLimits.AssertAudioParameters(channels, sampleRate, bitsPerSample, frameCount);

            _channels = channels;
            _sampleRate = sampleRate;
            _bitsPerSample = bitsPerSample;
            _frameCount = frameCount;

            _channelData = new int[channels][];
            for (int c = 0; c < channels; c++)
            {
                _channelData[c] = new int[frameCount];
            }
        }

        public AudioBuffer(int channels, int sampleRate, int bitsPerSample, int[][] channelData)
        {
            if (channelData == null) throw new ArgumentNullException("channelData");
            if (channelData.Length != channels)
            {
                throw new ArgumentException("A contagem de canais não coincide com o array de dados.");
            }

            int frameCount = channels > 0 && channelData[0] != null ? channelData[0].Length : 0;
            for (int c = 1; c < channels; c++)
            {
                if (channelData[c] == null || channelData[c].Length != frameCount)
                {
                    throw new ArgumentException("Todos os canais devem ter o mesmo comprimento de quadros.");
                }
            }

            SecurityLimits.AssertAudioParameters(channels, sampleRate, bitsPerSample, frameCount);

            _channels = channels;
            _sampleRate = sampleRate;
            _bitsPerSample = bitsPerSample;
            _frameCount = frameCount;
            _channelData = channelData;
        }

        public AudioBuffer(int channels, int sampleRate, int bitsPerSample, float[][] channelData)
            : this(channels, sampleRate, bitsPerSample, channels > 0 && channelData != null && channelData[0] != null ? channelData[0].Length : 0)
        {
            if (channelData == null) throw new ArgumentNullException("channelData");
            if (channelData.Length != channels)
            {
                throw new ArgumentException("A contagem de canais não coincide com o array de dados.");
            }

            for (int c = 0; c < channels; c++)
            {
                if (channelData[c] == null || channelData[c].Length != _frameCount)
                {
                    throw new ArgumentException("Todos os canais devem ter o mesmo comprimento de quadros.");
                }
                for (int f = 0; f < _frameCount; f++)
                {
                    SetSample(c, f, channelData[c][f]);
                }
            }
        }

        public int GetSampleQ31(int channel, int frame)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");
            if (frame < 0 || frame >= _frameCount)
                throw new ArgumentOutOfRangeException("frame", "Índice de quadro inválido.");

            return _channelData[channel][frame];
        }

        public void SetSampleQ31(int channel, int frame, int sampleQ31)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");
            if (frame < 0 || frame >= _frameCount)
                throw new ArgumentOutOfRangeException("frame", "Índice de quadro inválido.");

            _channelData[channel][frame] = sampleQ31;
        }

        public int[] GetChannelQ31(int channel)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");

            return _channelData[channel];
        }

        public float GetSample(int channel, int frame)
        {
            int q = GetSampleQ31(channel, frame);
            return q < 0 ? (float)(q / 2147483648.0) : (float)(q / 2147483647.0);
        }

        public void SetSample(int channel, int frame, float value)
        {
            if (value > 1.0f) value = 1.0f;
            else if (value < -1.0f) value = -1.0f;

            double scaled = value < 0 ? (double)value * 2147483648.0 : (double)value * 2147483647.0;
            int q = (int)Math.Max(-2147483648.0, Math.Min(2147483647.0, Math.Round(scaled)));
            SetSampleQ31(channel, frame, q);
        }

        public float[] GetChannel(int channel)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");

            float[] floatArr = new float[_frameCount];
            int[] qArr = _channelData[channel];
            for (int i = 0; i < _frameCount; i++)
            {
                int q = qArr[i];
                floatArr[i] = q < 0 ? (float)(q / 2147483648.0) : (float)(q / 2147483647.0);
            }
            return floatArr;
        }

        /// <summary>
        /// Converte para canal mono (1 canal) usando média aritmética inteira de 64 bits.
        /// </summary>
        public AudioBuffer ToMono()
        {
            if (_channels == 1)
            {
                return Clone();
            }

            var mono = new AudioBuffer(1, _sampleRate, _bitsPerSample, _frameCount);
            int[] dst = mono.GetChannelQ31(0);

            if (_channels == 2)
            {
                int[] left = _channelData[0];
                int[] right = _channelData[1];
                for (int i = 0; i < _frameCount; i++)
                {
                    dst[i] = (int)(((long)left[i] + right[i]) / 2);
                }
            }
            else
            {
                for (int i = 0; i < _frameCount; i++)
                {
                    long sum = 0;
                    for (int c = 0; c < _channels; c++)
                    {
                        sum += _channelData[c][i];
                    }
                    dst[i] = (int)(sum / _channels);
                }
            }

            return mono;
        }

        /// <summary>
        /// Converte para estéreo (2 canais). Se mono, duplica o canal em L e R de forma bit-perfect.
        /// </summary>
        public AudioBuffer ToStereo()
        {
            if (_channels == 2)
            {
                return Clone();
            }

            var stereo = new AudioBuffer(2, _sampleRate, _bitsPerSample, _frameCount);
            int[] left = stereo.GetChannelQ31(0);
            int[] right = stereo.GetChannelQ31(1);

            if (_channels == 1)
            {
                int[] src = _channelData[0];
                Array.Copy(src, left, _frameCount);
                Array.Copy(src, right, _frameCount);
            }
            else
            {
                Array.Copy(_channelData[0], left, _frameCount);
                Array.Copy(_channelData.Length > 1 ? _channelData[1] : _channelData[0], right, _frameCount);
            }

            return stereo;
        }

        /// <summary>
        /// Converte para a quantidade de canais especificada (1 ou 2).
        /// </summary>
        public AudioBuffer ConvertChannels(int targetChannels)
        {
            if (targetChannels == _channels)
            {
                return Clone();
            }
            if (targetChannels == 1)
            {
                return ToMono();
            }
            if (targetChannels == 2)
            {
                return ToStereo();
            }

            throw new UnsupportedFormatException(string.Format(
                "Conversão para {0} canais não é suportada (apenas 1 ou 2 canais).", targetChannels));
        }

        /// <summary>
        /// Reamostra o áudio para uma nova taxa de amostragem usando interpolação linear em precisão double.
        /// </summary>
        public AudioBuffer Resample(int targetSampleRate)
        {
            if (targetSampleRate <= 0)
                throw new ArgumentOutOfRangeException("targetSampleRate", "Taxa de amostragem deve ser maior que zero.");

            if (targetSampleRate == _sampleRate || _frameCount == 0)
            {
                return Clone();
            }

            long targetFrameCountLong = checked((long)Math.Round((double)_frameCount * targetSampleRate / _sampleRate));
            if (targetFrameCountLong > int.MaxValue)
            {
                throw new ResourceLimitException("Tamanho do buffer reamostrado excede o limite máximo permitido.");
            }
            int targetFrameCount = (int)targetFrameCountLong;

            var resampled = new AudioBuffer(_channels, targetSampleRate, _bitsPerSample, targetFrameCount);
            if (targetFrameCount == 0) return resampled;

            double ratio = (double)_sampleRate / targetSampleRate;

            for (int c = 0; c < _channels; c++)
            {
                int[] src = _channelData[c];
                int[] dst = resampled.GetChannelQ31(c);

                for (int j = 0; j < targetFrameCount; j++)
                {
                    double srcPos = j * ratio;
                    int i = (int)srcPos;
                    if (i >= _frameCount - 1)
                    {
                        dst[j] = src[_frameCount - 1];
                    }
                    else
                    {
                        double alpha = srcPos - i;
                        double interpolated = (1.0 - alpha) * src[i] + alpha * src[i + 1];
                        dst[j] = (int)Math.Max(-2147483648.0, Math.Min(2147483647.0, Math.Round(interpolated)));
                    }
                }
            }

            return resampled;
        }

        /// <summary>
        /// Aplica ganho linear ao áudio.
        /// </summary>
        public void ApplyGain(float gain)
        {
            ApplyGain((double)gain);
        }

        /// <summary>
        /// Aplica ganho linear com precisão double e saturação Q31.
        /// </summary>
        public void ApplyGain(double gain)
        {
            if (gain == 1.0) return;

            for (int c = 0; c < _channels; c++)
            {
                int[] samples = _channelData[c];
                for (int i = 0; i < _frameCount; i++)
                {
                    double val = samples[i] * gain;
                    if (val > 2147483647.0) val = 2147483647.0;
                    else if (val < -2147483648.0) val = -2147483648.0;
                    samples[i] = (int)Math.Round(val);
                }
            }
        }

        /// <summary>
        /// Normaliza o sinal para o pico alvo (padrão 0.99f / -0.1 dBFS).
        /// </summary>
        public void Normalize(float targetPeak = 0.99f)
        {
            Normalize((double)targetPeak);
        }

        /// <summary>
        /// Normaliza o sinal para o pico alvo com precisão double.
        /// </summary>
        public void Normalize(double targetPeak = 0.99)
        {
            if (targetPeak <= 0.0 || targetPeak > 1.0)
                throw new ArgumentOutOfRangeException("targetPeak", "Pico alvo deve estar no intervalo (0.0, 1.0].");

            long maxAmp = 0;
            for (int c = 0; c < _channels; c++)
            {
                int[] samples = _channelData[c];
                for (int i = 0; i < _frameCount; i++)
                {
                    long abs = Math.Abs((long)samples[i]);
                    if (abs > maxAmp) maxAmp = abs;
                }
            }

            if (maxAmp > 0)
            {
                double maxAmpNormalized = (double)maxAmp / 2147483648.0;
                if (maxAmpNormalized > 0.00001 && Math.Abs(maxAmpNormalized - targetPeak) > 0.0001)
                {
                    double scale = (targetPeak * 2147483647.0) / maxAmp;
                    ApplyGain(scale);
                }
            }
        }

        public AudioBuffer Clone()
        {
            var clone = new AudioBuffer(_channels, _sampleRate, _bitsPerSample, _frameCount);
            for (int c = 0; c < _channels; c++)
            {
                Array.Copy(_channelData[c], clone.GetChannelQ31(c), _frameCount);
            }
            return clone;
        }
    }
}
