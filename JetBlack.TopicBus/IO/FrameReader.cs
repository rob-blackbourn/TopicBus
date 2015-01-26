using System;
using System.IO;
using System.ServiceModel.Channels;

namespace JetBlack.TopicBus.IO
{
    public class FrameReader : IDisposable
    {
        readonly Stream _stream;
        readonly BufferManager _bufferManager;
        int _length;
        byte[] _buffer;
        int _totalBytesRead;

        bool NeedsLength { get; set; }

        public FrameReader(Stream stream, BufferManager bufferManager)
        {
            _stream = stream;
            _bufferManager = bufferManager;
            _length = sizeof(int);
            _totalBytesRead = 0;
            _buffer = null;
            NeedsLength = true;
        }

        public FrameContent Read()
        {
            if (NeedsLength)
                if (!ReadLength())
                    return null;
            return ReadBody();
        }

        bool ReadLength()
        {
            if (_buffer == null)
                _buffer = _bufferManager.TakeBuffer(_length);

            var bytesRead = _stream.Read(_buffer, _totalBytesRead, _length - _totalBytesRead);
            if (bytesRead == 0)
                throw new EndOfStreamException();

            _totalBytesRead += bytesRead;

            if (_totalBytesRead != _length)
                return false;

            _length = NetworkBitConverter.ToInt32(_buffer, 0);
            NeedsLength = false;

            _bufferManager.ReturnBuffer(_buffer);
            _buffer = null;
            _totalBytesRead = 0;

            return true;
        }

        FrameContent ReadBody()
        {
            if (_buffer == null)
                _buffer = _bufferManager.TakeBuffer(_length);

            var bytesRead = _stream.Read(_buffer, _totalBytesRead, _length - _totalBytesRead);
            if (bytesRead == 0)
                throw new EndOfStreamException();

            _totalBytesRead += bytesRead;

            if (_totalBytesRead != _length)
                return null;

            var buffer = _buffer;
            var length = _length;

            _length = sizeof(int);
            NeedsLength = true;
            _buffer = null;
            _totalBytesRead = 0;

            return new FrameContent(buffer, length, _bufferManager);
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                _bufferManager.ReturnBuffer(_buffer);
                _buffer = null;
            }
        }
    }
}

