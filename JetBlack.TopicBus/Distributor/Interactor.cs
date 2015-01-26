using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using JetBlack.TopicBus.IO;
using JetBlack.TopicBus.Messages;
using log4net;
using BufferManager = System.ServiceModel.Channels.BufferManager;

namespace JetBlack.TopicBus.Distributor
{
    class Interactor : IDisposable, IEquatable<Interactor>, IComparable<Interactor>
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Interactor));

        readonly Socket _socket;
        public readonly int Id;
        readonly BufferManager _bufferManager;
        readonly FrameReader _reader;
        readonly FrameWriter _writer;
        IObserver<Message> _observer;
        readonly ManualResetEvent _waitEvent;

        public Interactor(Socket socket, int id, BufferManager bufferManager)
        {
            _socket = socket;
            Id = id;
            _bufferManager = bufferManager;
            var stream = new NetworkStream(_socket, true);
            _reader = new FrameReader(stream, bufferManager);
            _writer = new FrameWriter(stream);
            _observer = new PendingObserver();
            _waitEvent = new ManualResetEvent(true);
        }

        public IObservable<Message> ToObservable()
        {
            return Observable.Create<Message>(
                observer =>
                {
                    _waitEvent.Reset();

                    var pendingObserver = (PendingObserver)Interlocked.Exchange(ref _observer, observer);

                    foreach (var message in pendingObserver.Messages)
                        _observer.OnNext(message);

                    if (pendingObserver.Error != null)
                        _observer.OnError(pendingObserver.Error);
                    else if (pendingObserver.IsCompleted)
                        _observer.OnCompleted();

                    _waitEvent.Set();


                    return Disposable.Create(_socket.Close);
                }
            );
        }

        public bool ReadMessage()
        {
            Log.DebugFormat("Reading {0}", this);

            try
            {
                _waitEvent.WaitOne();
                using (var frameContent = _reader.Read())
                {
                    if (frameContent != null)
                    {
                        using (var frameStream = new BufferedMemoryStream(frameContent.Buffer, frameContent.Length))
                        {
                            var message = Message.Read(frameStream);
                            Log.DebugFormat("Read {0} ({1})", this, message);
                            _observer.OnNext(message);
                        }
                    }
                }
                return true;
            }
            catch (EndOfStreamException)
            {
                _observer.OnCompleted();
            }
            catch (Exception ex)
            {
                _observer.OnError(ex);
            }

            return false;
        }

        public void SendMessage(Message message)
        {
            Log.DebugFormat("Sending {0} ({1})", this, message);

            using (var frameStream = new BufferedMemoryStream(_bufferManager, 256))
            {
                message.Write(frameStream);
                _writer.Write(new FrameContent(frameStream.GetBuffer(), (int)frameStream.Length));
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)_socket.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_socket.RemoteEndPoint; }
        }

        public Socket Socket
        {
            get { return _socket; }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Id, RemoteEndPoint);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Interactor);
        }

        public bool Equals(Interactor other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(Interactor other)
        {
            return (other == null ? 1 : Id - other.Id);
        }

        public void Dispose()
        {
            _socket.Close();
        }

        private class PendingObserver : IObserver<Message>
        {
            public List<Message> Messages = new List<Message>();
            public Exception Error;
            public bool IsCompleted;

            public void OnNext(Message value)
            {
                lock (this)
                {
                    Messages.Add(value);
                }
            }

            public void OnError(Exception error)
            {
                lock (this)
                {
                    Error = error;
                }
            }

            public void OnCompleted()
            {
                lock (this)
                {
                    IsCompleted = true;
                }
            }
        }
    }
}

