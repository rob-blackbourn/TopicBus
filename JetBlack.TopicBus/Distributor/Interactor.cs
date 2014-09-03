using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JetBlack.TopicBus.IO;
using JetBlack.TopicBus.Messages;

namespace JetBlack.TopicBus.Distributor
{
    public class Interactor : IDisposable, IEquatable<Interactor>, IComparable<Interactor>
    {
        public readonly int Id;
        readonly TcpClient _tcpClient;
        readonly ISerializer _serializer;
        readonly Stream _stream;

        public Interactor(TcpClient tcpClient, int id, ISerializer serializer)
        {
            _tcpClient = tcpClient;
            Id = id;
            _serializer = serializer;

            _stream = tcpClient.GetStream();
        }

        public IObservable<Message> ToObservable()
        {
            return Observable.Create<Message>(
                observer =>
                {
                    Task.Factory.StartNew(() => Dispatch(observer));

                    return Disposable.Create(() =>
                        {
                            _stream.Close();
                            _tcpClient.Close();
                        });
                }
            );
        }

        void Dispatch(IObserver<Message> observer)
        {
            try
            {
                while (_tcpClient.Connected)
                {
                    var message = Message.Read(_stream, _serializer.Deserialize);
                    observer.OnNext(message);
                }
            }
            catch (EndOfStreamException)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public void SendMessage(Message message)
        {
            message.Write(_stream, _serializer.Serialize);
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)_tcpClient.Client.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_tcpClient.Client.RemoteEndPoint; }
        }

        public Socket Socket
        {
            get { return _tcpClient.Client; }
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
            _tcpClient.Close();
        }
    }
}

