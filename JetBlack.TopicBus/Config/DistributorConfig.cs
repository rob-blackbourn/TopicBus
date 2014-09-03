using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Config
{
    public class DistributorConfig
    {
        public string Name;

        public int Port;

        public ISerializer Serializer;

        public DistributorConfig(string name, int port, ISerializer serializer)
        {
            Name = name;
            Port = port;
            Serializer = serializer;
        }
    }
}

