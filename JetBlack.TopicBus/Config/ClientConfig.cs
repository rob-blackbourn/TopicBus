using System;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Config
{
    public class ClientConfig
    {
        public string Name;
        public string Host;
        public int Port;
        public ISerializer Serializer;

        public ClientConfig(string name, string host, int port, ISerializer serializer)
        {
            Name = name;
            Host = host;
            Port = port;
            Serializer = serializer;
        }
    }
}

