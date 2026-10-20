using System;
using System.IO;
using Lodino.Core.Errors;

namespace Lodino.Core.Formats.Image.Jpeg
{
    /// <summary>
    /// Leitor de bits orientado a MSB-first com suporte a byte-stuffing JPEG (0xFF 0x00)
    /// e detecção de marcadores de reinício (RST0..RST7).
    /// </summary>
    public class JpegBitReader
    {
        private readonly Stream _stream;
        private int _bitBuffer;
        private int _bitsInBuffer;
        private bool _hitEof;
        private int _unreadMarker;

        public bool HitEof { get { return _hitEof; } }
        public int UnreadMarker { get { return _unreadMarker; } }

        public JpegBitReader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            _stream = stream;
            _bitBuffer = 0;
            _bitsInBuffer = 0;
            _hitEof = false;
            _unreadMarker = 0;
        }

        /// <summary>
        /// Lê um único bit (0 ou 1). Retorna -1 se atingir o final do fluxo ou marcador não tratado.
        /// </summary>
        public int ReadBit()
        {
            if (_bitsInBuffer == 0)
            {
                if (!FillBuffer()) return -1;
            }

            _bitsInBuffer--;
            return (_bitBuffer >> _bitsInBuffer) & 1;
        }

        /// <summary>
        /// Lê 'count' bits (1..16). Lança CorruptDataException se não houver bits suficientes.
        /// </summary>
        public int ReadBits(int count)
        {
            if (count == 0) return 0;
            if (count < 0 || count > 16)
            {
                throw new ArgumentOutOfRangeException("count", "Contagem de bits deve estar entre 1 e 16.");
            }

            int result = 0;
            for (int i = 0; i < count; i++)
            {
                int b = ReadBit();
                if (b < 0)
                {
                    throw new CorruptDataException("Fim prematuro do fluxo durante a leitura de bits.");
                }
                result = (result << 1) | b;
            }
            return result;
        }

        /// <summary>
        /// Procedimento RECEIVE do Anexo F (Figura F.17).
        /// </summary>
        public int Receive(int category)
        {
            return ReadBits(category);
        }

        /// <summary>
        /// Procedimento EXTEND do Anexo F (Figura F.12).
        /// Converte magnitude unsigned de 'category' bits em valor signed.
        /// </summary>
        public static int Extend(int v, int category)
        {
            if (category == 0) return 0;
            int threshold = 1 << (category - 1);
            if (v < threshold)
            {
                v += (-1 << category) + 1;
            }
            return v;
        }

        /// <summary>
        /// Descarta bits residuais para alinhar à fronteira de byte (utilizado em marcadores RST).
        /// </summary>
        public void AlignToByte()
        {
            _bitBuffer = 0;
            _bitsInBuffer = 0;
        }

        /// <summary>
        /// Reinicia estado de predição após encontrar um marcador de reinício RSTm.
        /// </summary>
        public void ResetRestart()
        {
            AlignToByte();
            _unreadMarker = 0;
        }

        private bool FillBuffer()
        {
            int b = _stream.ReadByte();
            if (b < 0)
            {
                _hitEof = true;
                return false;
            }

            if (b == 0xFF)
            {
                // Descarta bytes 0xFF repetidos de preenchimento (padding bytes)
                int nextByte;
                do
                {
                    nextByte = _stream.ReadByte();
                } while (nextByte == 0xFF);

                if (nextByte < 0)
                {
                    _hitEof = true;
                    return false;
                }

                if (nextByte == 0x00)
                {
                    // Byte-stuffing: 0xFF seguido de 0x00 representa o byte de dado 0xFF literal
                    _bitBuffer = 0xFF;
                    _bitsInBuffer = 8;
                    return true;
                }

                // Encontrou um marcador (ex: RST0..RST7 ou EOI)
                _unreadMarker = (0xFF << 8) | nextByte;
                return false;
            }

            _bitBuffer = b;
            _bitsInBuffer = 8;
            return true;
        }
    }
}
