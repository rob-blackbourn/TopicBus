using System.IO;

namespace JetBlack.TopicBus.IO
{
    public class FrameWriter
    {
        readonly Stream _stream;

        public FrameWriter(Stream stream)
        {
            _stream = stream;
        }

        public void Write(FrameContent frameContent)
        {
            WriteLength(frameContent.Length);
            WriteBody(frameContent.Buffer, frameContent.Length);
        }

        void WriteLength(int length)
        {
            var lengthBytes = NetworkBitConverter.GetBytes(length);
            _stream.Write(lengthBytes, 0, lengthBytes.Length);
        }

        void WriteBody(byte[] buffer, int length)
        {
            _stream.Write(buffer, 0, length);
        }
    }
}

