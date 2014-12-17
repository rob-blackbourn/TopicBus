using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Config
{
    public class ClientConfig
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public ISerializer Serializer { get; set; }
    }
}

