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
            for (int i = 0; i < nbytes; ++i)
            {
                var b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                data[i] = (byte)b;
            }
            return new MulticastData(topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Topic);
            stream.Write(IsImage);
            var data = Data ?? new byte[0];
            stream.Write(data.Length);
            for (int i = 0; i < data.Length; ++i)
                stream.Write(data[i]);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} [{3}]", MessageType, Topic, IsImage, Data);
        }
    }
}
