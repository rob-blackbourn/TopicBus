using System;
using System.ServiceModel.Channels;

namespace JetBlack.TopicBus.IO
{
    public class FrameContent : IDisposable
    {
        readonly BufferManager _bufferManager;

        public FrameContent(byte[] buffer, int length, BufferManager bufferManager = null)
        {
            Buffer = buffer;
            Length = length;
            _bufferManager = bufferManager;
        }

        public byte[] Buffer { get; private set; }
        public int Length { get; private set; }

        public void Dispose()
        {
            if (_bufferManager != null && Buffer != null)
                _bufferManager.ReturnBuffer(Buffer);
        }
    }
}

