using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class UnicastData : Message
    {
        public readonly int ClientId;
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public UnicastData(int clientId, string topic, bool isImage, byte[] data)
            : base(MessageType.UnicastData)
        {
            ClientId = clientId;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public UnicastData ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
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

            return new UnicastData(clientId, topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);

            stream.Write(ClientId);
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
            return string.Format("{0} {1} {2} {3} {4}", MessageType, ClientId, Topic, IsImage, Data == null ? null : Data.Length + " bytes");
        }
    }
}

