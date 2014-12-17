using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Config
{
    public class DistributorConfig
    {
        public string Name;

        public int Port;

        public DistributorConfig(string name, int port)
        {
            Name = name;
            Port = port;
        }
    }
}

