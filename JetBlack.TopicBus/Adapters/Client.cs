﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JetBlack.TopicBus.IO;
using JetBlack.TopicBus.Messages;

namespace JetBlack.TopicBus.Adapters
{
    public delegate void DataCallback(string topic, IDictionary<string, object> data, bool isImage);
    public delegate void ForwardedSubscriptionCallback(int clientId, string topic, bool isAdd);
    public delegate void ClosedCallback(bool isAbnormal);

    public class Client
    {
        public event DataCallback OnData;

        public event ForwardedSubscriptionCallback OnForwardedSubscription;

        public event ClosedCallback OnClosed;

        readonly ISerializer _serializer;
        TcpClient _tcpClient;
        bool _isConnected;
        readonly Thread _thread;
        NetworkStream _networkStream;
        readonly object _syncObject = new object();

        public Client(ISerializer serializer)
        {
            _serializer = serializer;
            _isConnected = false;
            _thread = new Thread(Dispatch);
        }

        public bool Connect(string host, int port)
        {
            _tcpClient = new TcpClient(host, port);
            _networkStream = _tcpClient.GetStream();

            _thread.Name = string.Format("interactor/{0}", _tcpClient.Client.RemoteEndPoint);
            _isConnected = true;
            _thread.Start();

            return true;
        }

        public void Close()
        {
            IsConnected = false;
            _tcpClient.Close();
            _thread.Join();
        }

        public void AddSubscription(string topic)
        {
            Write(new SubscriptionRequest(topic, true));
        }

        public void RemoveSubscription(string topic)
        {
            Write(new SubscriptionRequest(topic, false));
        }

        public void Send(int clientId, string topic, bool isImage, IDictionary<string, object> data)
        {
            Write(new UnicastDataMessage(clientId, topic, isImage, data));
        }

        public void Publish(string topic, bool isImage, IDictionary<string, object> data)
        {
            Write(new MulticastDataMessage(topic, isImage, data));
        }

        public virtual void AddNotification(string topicPattern)
        {
            Write(new NotificationRequest(topicPattern, true));
        }

        public virtual void RemoveNotification(string topicPattern)
        {
            Write(new NotificationRequest(topicPattern, false));
        }

        public bool IsConnected
        {
            get { lock (_syncObject) return _isConnected; }
            set { lock (_syncObject) _isConnected = value; }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return _tcpClient.Client.LocalEndPoint as IPEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return _tcpClient.Client.RemoteEndPoint as IPEndPoint; }
        }

        void Dispatch()
        {
            bool abnormalDisconnect = true;

            while (IsConnected)
            {
                try
                {
                    Message message = Message.Read(_networkStream, _serializer.Deserialize);

                    switch (message.MessageType)
                    {
                        case MessageType.MulticastDataMessage:
                            RaiseOnData(message as MulticastDataMessage);
                            break;
                        case MessageType.UnicastDataMessage:
                            RaiseOnData(message as UnicastDataMessage);
                            break;
                        case MessageType.ForwardedSubscriptionRequest:
                            RaiseOnForwardedSubscriptionRequest(message as ForwardedSubscriptionRequest);
                            break;
                        default:
                            throw new ArgumentException("invalid message type");
                    }
                }
                catch (EndOfStreamException)
                {
                    IsConnected = false;
                    abnormalDisconnect = false;
                    continue;
                }
                catch (IOException ex)
                {
                    var socketException = ex.InnerException as SocketException;
                    if (socketException != null)
                    {
                        if (socketException.ErrorCode == 10060)
                            continue;
                    }

                    if (IsConnected)
                    {
                        IsConnected = false;
                        abnormalDisconnect = true;
                    }
                    else
                        abnormalDisconnect = false;
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    if (IsConnected)
                    {
                        IsConnected = false;
                        abnormalDisconnect = true;
                    }
                    else
                        abnormalDisconnect = false;

                    continue;
                }
            }

            if (OnClosed != null)
                OnClosed(abnormalDisconnect);
        }

        void Write(Message message)
        {
            message.Write(_networkStream, _serializer.Serialize);
        }

        void RaiseOnForwardedSubscriptionRequest(ForwardedSubscriptionRequest message)
        {
            if (OnForwardedSubscription != null)
                OnForwardedSubscription(message.ClientId, message.Topic, message.IsAdd);
        }

        void RaiseOnData(MulticastDataMessage message)
        {
            RaiseOnData(message.Topic, (IDictionary<string,object>)message.Data, false);
        }

        void RaiseOnData(UnicastDataMessage message)
        {
            RaiseOnData(message.Topic, (IDictionary<string,object>)message.Data, true);
        }

        void RaiseOnData(string topic, IDictionary<string, object> data, bool isImage)
        {
            if (OnData != null)
                OnData(topic, data, isImage);
        }
    }
}
