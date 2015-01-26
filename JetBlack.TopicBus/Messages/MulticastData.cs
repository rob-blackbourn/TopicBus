using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class MulticastData : Message
    {
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public MulticastData(string topic, bool isImage, byte[] data)
            : base(MessageType.MulticastData)
        {
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public MulticastData ReadBody(Stream stream)
        {
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();

            var nbytes = stream.ReadInt32();
            var data = new byte[nbytes];
            var offset = 0;
            while (nbytes > 0)
            {
                var bytesRead = stream.Read(data, offset, nbytes);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                nbytes -= bytesRead;
                offset += bytesRead;
            }

            return new MulticastData(topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Topic);
            stream.Write(IsImage);

            if (Data == null)
                stream.Write(0);
            else
            {
                stream.Write(Data.Length);
                stream.Write(Data, 0, Data.Length);
            }

            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} [{3}]", MessageType, Topic, IsImage, Data == null ? null : Data.Length + " bytes");
        }
    }
}
