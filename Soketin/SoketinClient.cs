/* Copyright 2020 Yusuf Sulaeman <ucupxh@gmail.com>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the "Software"), to deal in 
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
 * of the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies 
 * or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Soketin
{
    public class SoketinClient
    {
        public uint BufferSize { get; set; }
        public bool AutoReconnect { get; set; }

        public Socket Socket { get; private set; }
        public bool isConnected { get; private set; }
        public Socket Server { get; private set; }

        public Action<IPAddress> OnConnected;
        public Action<IPAddress> OnDisconnected;
        public Action<byte[]> OnReceivedData;

        private Thread m_thread;
        private bool m_stopSignal;
        private Queue<List<object>> m_queueSend;

        public SoketinClient(int port, SoketinType type) {
            Socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, (ProtocolType)type);
            Socket.DontFragment = true;
            BufferSize = 8196; // Default 8k
            m_thread = new Thread(_worker);
            m_thread.IsBackground = true;
            m_queueSend = new Queue<List<object>>();
        }

        public void Connect(string address, int port) {
            if (isConnected)
                return;
            isConnected = true;
            m_thread.Start(new IPEndPoint(IPAddress.Parse(address), port));
        }

        public void DisConnect() {
            if (!isConnected)
                return;
            m_stopSignal = true;
            m_thread.Join();
            Socket.BeginDisconnect(true, (e) => {
                isConnected = false;
                Socket.EndDisconnect(e);
            }, null);
        }

        public void Send(byte[] data, Action OnSended = null) {
            if (Socket.Connected) {
                m_queueSend.Enqueue(new List<object>() { data, OnSended });
            }
        }

        private void _worker(object param)
        {
            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += (obj, e) => {
                if (e.SocketError == SocketError.Success && m_queueSend.Count > 0) {
                    var parameters = m_queueSend.Dequeue();
                    var data = SoketinUtility.PackRawData((byte[])parameters[0]);
                    ((Action)parameters[1])?.Invoke();
                    e.SetBuffer(data, 0, data.Length);
                    if (!m_stopSignal)
                        Socket.SendAsync(e);
                }
            };

            SocketAsyncEventArgs recArg = new SocketAsyncEventArgs();
            recArg.SetBuffer(new byte[BufferSize], 0, (int)BufferSize);
            recArg.Completed += (obj, e) => {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    var splittedData = SoketinUtility.SplitRawPacket(e.Buffer, e.BytesTransferred);
                    foreach (var data in splittedData)
                    {
                        OnReceivedData?.Invoke(data);
                    }
                }
                if (!m_stopSignal)
                    Socket.ReceiveAsync(e);
            };

            SocketAsyncEventArgs connArg = new SocketAsyncEventArgs();
            connArg.Completed += (obj, e) => {
                if (e.SocketError == SocketError.Success)
                {
                    Server = e.ConnectSocket;
                    OnConnected?.Invoke(((IPEndPoint)Server.RemoteEndPoint).Address);
                    Socket.ReceiveAsync(recArg);
                }
                else if (!m_stopSignal)
                    Socket.ConnectAsync(e);
            };
            connArg.RemoteEndPoint = (IPEndPoint)param;
            Socket.ConnectAsync(connArg);

            while (!m_stopSignal)
            {
                if (Socket.Poll(100, SelectMode.SelectRead) && Socket.Poll(100, SelectMode.SelectWrite))
                {
                    if (Server != null)
                    {
                        OnDisconnected?.Invoke(((IPEndPoint)Server.RemoteEndPoint).Address);
                        Server = null;
                        Socket.Disconnect(true);
                        if (AutoReconnect)
                        {
                            Console.WriteLine("Reconnecting...");
                            Socket.ConnectAsync(connArg);
                        }
                    }
                }
                if (Socket.Connected)
                {
                    if (m_queueSend.Count > 0)
                    {
                        var parameters = m_queueSend.Dequeue();
                        var data = SoketinUtility.PackRawData((byte[])parameters[0]);
                        sendArgs.SetBuffer(data, 0, data.Length);
                        Socket.SendAsync(sendArgs);
                    }
                }
                Thread.Sleep(1);
            }
        }
    }
}
