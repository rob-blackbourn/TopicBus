using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using log4net;
using JetBlack.TopicBus.Config;

namespace JetBlack.TopicBus.Distributor
{
    class Acceptor
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Acceptor));

        const int MaxBufferPoolSize = 100;
        const int MaxBufferSize = 100000;

        int _nextInteractorId;
        readonly Socket _listener;
        readonly DistributorConfig _config;
        readonly BufferManager _bufferManager;
        readonly IDictionary<Socket, Interactor> _interactors;

        public Acceptor(DistributorConfig config)
        {
            _bufferManager = BufferManager.CreateBufferManager(MaxBufferPoolSize, MaxBufferSize);
            _config = config;
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _interactors = new Dictionary<Socket, Interactor>();
        }

        public IObservable<Interactor> ToObservable()
        {
            return Observable.Create<Interactor>(observer =>
                {
                    var cancellationTokenSource = new CancellationTokenSource();

                    var endPoint = new IPEndPoint(IPAddress.Any, _config.Port);
                    _listener.Bind(endPoint);
                    _listener.Listen(10);

                    Start(observer, cancellationTokenSource.Token);

                    return Disposable.Create(cancellationTokenSource.Cancel);
                });
        }

        void Start(IObserver<Interactor> observer, CancellationToken cancellationToken)
        {
            Log.Info("Start listening");

            while (!cancellationToken.IsCancellationRequested)
            {
                var checkRead = new List<Socket>(_interactors.Keys) { _listener };

                Socket.Select(checkRead, null, null, _config.PollTimeout);

                foreach (var socket in checkRead.Where(socket => socket != null))
                {
                    if (socket != _listener)
                        Read(socket);
                    else if (!Accept(observer))
                        return;
                }
            }
        }

        bool Accept(IObserver<Interactor> observer)
        {
            Log.Info("Accept new connection");

            try
            {
                var newSocket = _listener.Accept();
                var interactor = new Interactor(newSocket, _nextInteractorId++, _bufferManager);
                _interactors.Add(newSocket, interactor);
                Log.DebugFormat("Accepted {0}", interactor);
                observer.OnNext(interactor);
                return true;
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

            return false;
        }

        void Read(Socket socket)
        {
            var interactor = _interactors[socket];
            Log.DebugFormat("Reading {0}", interactor);
            if (!interactor.ReadMessage())
                _interactors.Remove(socket);
        }
    }
}
