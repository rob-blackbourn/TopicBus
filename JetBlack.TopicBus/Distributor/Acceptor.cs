using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using log4net;
using JetBlack.TopicBus.IO;
using JetBlack.TopicBus.Config;

namespace JetBlack.TopicBus.Distributor
{
    public class Acceptor
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Acceptor));

        int _nextInteractorId;
        readonly TcpListener _listener;
        readonly ISerializer _serializer;
        readonly DistributorConfig _adapter;

        public Acceptor(DistributorConfig adapter)
        {
            _adapter = adapter;

            var endPoint = new IPEndPoint(IPAddress.Any, _adapter.Port);
            _listener = new TcpListener(endPoint);
            _serializer = _adapter.Serializer;
        }

        public IObservable<Interactor> ToObservable()
        {
            _listener.Start();

            return Observable.Create<Interactor>(observer =>
                {
                    Task.Factory.StartNew(() => Accept(observer));
                    return Disposable.Create(StopListener);
                });
        }

        void Accept(IObserver<Interactor> observer)
        {
            try
            {
                while (true)
                {
                    var tcpClient = _listener.AcceptTcpClient();
                    var interactor = new Interactor(tcpClient, _nextInteractorId++, _serializer);
                    Log.InfoFormat("Accepted new interactor: {0}", interactor);
                    observer.OnNext(interactor);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.Interrupted)
                    observer.OnCompleted();
                else
                    observer.OnError(ex);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        void StopListener()
        {
            Log.Info("Closing acceptor");
            _listener.Stop();
        }
    }
}
