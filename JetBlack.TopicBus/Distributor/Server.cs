using System;
using log4net;
using JetBlack.TopicBus.Config;

namespace JetBlack.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SubscriptionManager));

        readonly Market _market;

        public Server(DistributorConfig adapter)
        {
            Log.Info("Starting server");

            _market = new Market(new Acceptor(adapter).ToObservable());
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}

