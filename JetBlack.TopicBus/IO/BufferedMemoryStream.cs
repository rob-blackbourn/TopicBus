using System;
using System.IO;
using System.ServiceModel.Channels;

namespace JetBlack.TopicBus.IO
{
    public class BufferedMemoryStream : Stream
    {
        const int MemStreamMaxLength = Int32.MaxValue;

        readonly BufferManager _bufferManager;
        byte[] _buffer;
        int _position;
        int _length;
        int _capacity;
        readonly bool _isWriter;
        bool _isOpen;

        public BufferedMemoryStream(BufferManager bufferManager, int capacity)
        {
            if (bufferManager == null)
                throw new ArgumentNullException("bufferManager");
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", "Must be positive.");

            _bufferManager = bufferManager;
            _isWriter = true;

            _buffer = _bufferManager.TakeBuffer(capacity);
            _capacity = _buffer.Length;
            _length = 0;
            _isOpen = true;
        }

        public BufferedMemoryStream(byte[] buffer, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Must be positive.");
            if (length > buffer.Length)
                throw new ArgumentOutOfRangeException("length", "Must be less than the length of the buffer.");

            _bufferManager = null;
            _isWriter = false;

            _buffer = buffer;
            _length = length;
            _capacity = length;
            _isOpen = true;
        }

        public override bool CanRead { get { return _isOpen; } }

        public override bool CanSeek { get { return _isOpen; } }

        public override bool CanWrite { get { return _isOpen && _isWriter; } }

        void EnsureWriteable()
        {
            if (!CanWrite)
                throw new NotSupportedException("The stream is not writeable.");
        }

        void EnsureOpen()
        {
            if (!_isOpen)
                throw new InvalidOperationException("Stream closed.");
        }

        // Returns true if a new array was required.
        bool EnsureCapacity(int value)
        {
            // Check for overflow.
            if (value < 0)
                throw new IOException("Capacity overflow");

            if (value <= _capacity)
                return false;

            var newCapacity = value < 256 ? 256 : value;

            if (newCapacity < _capacity * 2)
                newCapacity = _capacity * 2;

            Capacity = newCapacity;

            return true;
        }

        public override void Flush()
        {
        }

        public virtual byte[] GetBuffer()
        {
            if (!_isWriter)
                throw new UnauthorizedAccessException("Access to the buffer is not allowed when reading.");

            return _buffer;
        }

        public virtual int Capacity
        {
            get
            {
                EnsureOpen();
                return _capacity;
            }
            set
            {
                if (value < Length)
                    throw new ArgumentOutOfRangeException(null, "Cannot shrink the stream.");
                if (!_isWriter)
                    throw new ArgumentException("Read streams are not expandable.");

                EnsureOpen();

                if (value == _capacity)
                    return;

                if (value <= 0)
                {
                    if (_length > 0)
                        _bufferManager.ReturnBuffer(_buffer);
                    _buffer = null;
                    _capacity = 0;
                }
                else
                {
                    var newBuffer = _bufferManager.TakeBuffer(value);
                    if (_length > 0)
                    {
                        Array.Copy(_buffer, 0, newBuffer, 0, _length);
                        _bufferManager.ReturnBuffer(_buffer);
                    }
                    _buffer = newBuffer;
                    _capacity = _buffer.Length;
                }
            }
        }

        public override long Length
        {
            get
            {
                EnsureOpen();
                return _length;
            }
        }

        public override long Position
        {
            get
            {
                EnsureOpen();
                return _position;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(null, "Position cannot be negative");
                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(null, "Position would exceed the maximum length");

                EnsureOpen();

                _position = (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Must be positive.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Must be positive.");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset.");

            EnsureOpen();

            var n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            if (n <= 8)
            {
                var byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                Array.Copy(_buffer, _position, buffer, offset, n);

            _position += n;

            return n;
        }

        public override int ReadByte()
        {
            EnsureOpen();
            if (_position >= _length)
                return -1;
            return _buffer[_position++];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureOpen();

            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException("offset", "Too large.");

            int absoluteOffset;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    absoluteOffset = (int)offset;
                    break;
                case SeekOrigin.Current:
                    absoluteOffset = unchecked(_position + (int)offset);
                    break;
                case SeekOrigin.End:
                    absoluteOffset = unchecked(_length + (int)offset);
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin", "origin");
            }

            if (absoluteOffset < 0)
                throw new IOException("Cannot seek before start of stream.");

            _position = absoluteOffset;

            return _position;
        }

        public override void SetLength(long value)
        {
            if (value < 0 || value > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException("value", "Too large.");

            EnsureWriteable();

            var newLength = (int)value;
            var isNewBuffer = EnsureCapacity(newLength);
            if (!isNewBuffer && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);

            _length = newLength;

            if (_position > newLength)
                _position = newLength;
        }

        public virtual byte[] ToArray()
        {
            var copy = new byte[_length];
            Array.Copy(_buffer, 0, copy, 0, _length);
            return copy;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Must be positive.");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Must be positive.");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset");

            EnsureOpen();
            EnsureWriteable();

            var newLength = _position + count;
            if (newLength < 0)
                throw new IOException("Overflow");

            if (newLength > _length)
            {
                var mustZero = _position > _length;

                if (newLength > _capacity)
                {
                    var isNewBuffer = EnsureCapacity(newLength);
                    if (isNewBuffer)
                        mustZero = false;
                }

                if (mustZero)
                    Array.Clear(_buffer, _length, newLength - _length);

                _length = newLength;
            }

            if (count > 8 || buffer == _buffer)
                Array.Copy(buffer, offset, _buffer, _position, count);
            else
            {
                var byteCount = count;
                while (--byteCount >= 0)
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
            }

            _position = newLength;
        }

        public override void WriteByte(byte value)
        {
            EnsureOpen();
            EnsureWriteable();

            if (_position >= _length)
            {
                var newLength = _position + 1;
                var mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    var isNewBuffer = EnsureCapacity(newLength);
                    if (isNewBuffer)
                        mustZero = false;
                }

                if (mustZero)
                    Array.Clear(_buffer, _length, _position - _length);

                _length = newLength;
            }

            _buffer[_position++] = value;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            EnsureOpen();

            stream.Write(_buffer, 0, _length);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_isWriter && _buffer != null)
                    {
                        _bufferManager.ReturnBuffer(_buffer);
                        _buffer = null;
                    }

                    _isOpen = false;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}

