using System;
using Lodino.Core.Errors;
using Lodino.Core.Safety;

namespace Lodino.Core.Formats.Audio
{
    /// <summary>
    /// Representação nativa e desacoplada de dados de áudio PCM multi-canal em memória.
    /// Armazena amostras normalizadas em ponto flutuante [-1.0f, +1.0f] por canal planar,
    /// suportando conversão de canais (Mono/Stereo), reamostragem (resampling), ganho e normalização.
    /// </summary>
    public class AudioBuffer
    {
        private readonly float[][] _channelData;
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

            _channelData = new float[channels][];
            for (int c = 0; c < channels; c++)
            {
                _channelData[c] = new float[frameCount];
            }
        }

        public AudioBuffer(int channels, int sampleRate, int bitsPerSample, float[][] channelData)
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

        public float GetSample(int channel, int frame)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");
            if (frame < 0 || frame >= _frameCount)
                throw new ArgumentOutOfRangeException("frame", "Índice de quadro inválido.");

            return _channelData[channel][frame];
        }

        public void SetSample(int channel, int frame, float value)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");
            if (frame < 0 || frame >= _frameCount)
                throw new ArgumentOutOfRangeException("frame", "Índice de quadro inválido.");

            // Clamp [-1.0f, +1.0f]
            if (value > 1.0f) value = 1.0f;
            else if (value < -1.0f) value = -1.0f;

            _channelData[channel][frame] = value;
        }

        public float[] GetChannel(int channel)
        {
            if (channel < 0 || channel >= _channels)
                throw new ArgumentOutOfRangeException("channel", "Índice de canal inválido.");

            return _channelData[channel];
        }

        /// <summary>
        /// Converte para canal mono (1 canal). Se estéreo, faz a média (L + R) * 0.5f.
        /// </summary>
        public AudioBuffer ToMono()
        {
            if (_channels == 1)
            {
                return Clone();
            }

            var mono = new AudioBuffer(1, _sampleRate, _bitsPerSample, _frameCount);
            float[] dst = mono.GetChannel(0);

            if (_channels == 2)
            {
                float[] left = _channelData[0];
                float[] right = _channelData[1];
                for (int i = 0; i < _frameCount; i++)
                {
                    dst[i] = (left[i] + right[i]) * 0.5f;
                }
            }
            else
            {
                float factor = 1.0f / _channels;
                for (int i = 0; i < _frameCount; i++)
                {
                    float sum = 0f;
                    for (int c = 0; c < _channels; c++)
                    {
                        sum += _channelData[c][i];
                    }
                    dst[i] = sum * factor;
                }
            }

            return mono;
        }

        /// <summary>
        /// Converte para estéreo (2 canais). Se mono, duplica o canal em L e R.
        /// </summary>
        public AudioBuffer ToStereo()
        {
            if (_channels == 2)
            {
                return Clone();
            }

            var stereo = new AudioBuffer(2, _sampleRate, _bitsPerSample, _frameCount);
            float[] left = stereo.GetChannel(0);
            float[] right = stereo.GetChannel(1);

            if (_channels == 1)
            {
                float[] src = _channelData[0];
                Array.Copy(src, left, _frameCount);
                Array.Copy(src, right, _frameCount);
            }
            else
            {
                // Mapeia os dois primeiros canais ou duplica
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
        /// Reamostra o áudio para uma nova taxa de amostragem usando interpolação linear.
        /// </summary>
        public AudioBuffer Resample(int targetSampleRate)
        {
            if (targetSampleRate <= 0)
                throw new ArgumentOutOfRangeException("targetSampleRate", "Taxa de amostragem deve ser maior que zero.");

            if (targetSampleRate == _sampleRate || _frameCount == 0)
            {
                var copy = Clone();
                return copy;
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
                float[] src = _channelData[c];
                float[] dst = resampled.GetChannel(c);

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
                        float alpha = (float)(srcPos - i);
                        dst[j] = (1.0f - alpha) * src[i] + alpha * src[i + 1];
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
            if (gain == 1.0f) return;

            for (int c = 0; c < _channels; c++)
            {
                float[] samples = _channelData[c];
                for (int i = 0; i < _frameCount; i++)
                {
                    float val = samples[i] * gain;
                    if (val > 1.0f) val = 1.0f;
                    else if (val < -1.0f) val = -1.0f;
                    samples[i] = val;
                }
            }
        }

        /// <summary>
        /// Normaliza o sinal para o pico alvo (padrão 0.99f / -0.1 dBFS).
        /// </summary>
        public void Normalize(float targetPeak = 0.99f)
        {
            if (targetPeak <= 0f || targetPeak > 1.0f)
                throw new ArgumentOutOfRangeException("targetPeak", "Pico alvo deve estar no intervalo (0.0, 1.0].");

            float maxAmp = 0f;
            for (int c = 0; c < _channels; c++)
            {
                float[] samples = _channelData[c];
                for (int i = 0; i < _frameCount; i++)
                {
                    float abs = Math.Abs(samples[i]);
                    if (abs > maxAmp) maxAmp = abs;
                }
            }

            if (maxAmp > 0.00001f && Math.Abs(maxAmp - targetPeak) > 0.0001f)
            {
                float scale = targetPeak / maxAmp;
                ApplyGain(scale);
            }
        }

        public AudioBuffer Clone()
        {
            var clone = new AudioBuffer(_channels, _sampleRate, _bitsPerSample, _frameCount);
            for (int c = 0; c < _channels; c++)
            {
                Array.Copy(_channelData[c], clone.GetChannel(c), _frameCount);
            }
            return clone;
        }
    }
}
