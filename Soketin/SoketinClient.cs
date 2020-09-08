/* Copyright © 2020 Yusuf Sulaeman <ucupxh@gmail.com>
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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Soketin
{
    public class SoketinClient : SoketinBase
    {
        public bool AutoReconnect { get; set; }
        public bool isConnected { get; private set; }
        public Socket Server { get; private set; }

        public Action OnConnected;
        public Action OnDisconnected;

        private bool m_stopSignal;

        //Contructor
        public SoketinClient(int port)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.DontFragment = true;
        }

        //Public Method
        public void Connect(string address, int port) {
            if (isConnected)
                return;
            var addressEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
            SocketAsyncEventArgs recArg = new SocketAsyncEventArgs();
            recArg.SetBuffer(new byte[BufferSize], 0, (int)BufferSize);
            recArg.Completed += (obj, e) =>
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    var splittedData = SoketinUtility.SplitRawPacket(e.Buffer, e.BytesTransferred);
                    foreach (var data in splittedData)
                    {
                        OnDataRecieved?.Invoke((addressEndPoint).Address, data);
                    }
                }
                if (!m_stopSignal)
                    Socket.ReceiveAsync(e);
            };

            SocketAsyncEventArgs connArg = new SocketAsyncEventArgs();
            connArg.RemoteEndPoint = addressEndPoint;
            connArg.Completed += (obj, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    Server = e.ConnectSocket;
                    isConnected = true;
                    OnConnected?.Invoke();
                    Socket.ReceiveAsync(recArg);
                }
                else if (!m_stopSignal)
                    Socket.ConnectAsync(e);
            };
            Socket.ConnectAsync(connArg);

            _thread.Start(connArg);
        }
        public void Disconnect() {
            if (!isConnected)
                return;
            m_stopSignal = true;
            _thread.Join();
            Socket.BeginDisconnect(true, (e) => {
                isConnected = false;
                Socket.EndDisconnect(e);
            }, null);
        }
        public void Send(byte[] data, Action<int> OnSended = null) {
            if (Socket.Connected) {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.RemoteEndPoint = Server.RemoteEndPoint;
                sendArgs.Completed += (obj, e) => {
                    if (e.SocketError == SocketError.Success) {
                        OnDataSended?.Invoke(((IPEndPoint)Server.RemoteEndPoint).Address, e.BytesTransferred);
                    }
                };
                var packedData = SoketinUtility.PackRawData(data);
                sendArgs.SetBuffer(packedData, 0, packedData.Length);
                Socket.SendAsync(sendArgs);
            }
        }
        protected override void ThreadWorker(object userData)
        {
            while (!m_stopSignal) {
                if (Socket.Poll(100, SelectMode.SelectRead) && 
                    Socket.Poll(100, SelectMode.SelectWrite))
                {
                    if (Server != null)
                    {
                        Server = null;
                        Socket.Disconnect(true);
                        isConnected = false;
                        OnDisconnected?.Invoke();
                        if (AutoReconnect) {
                            Socket.ConnectAsync((SocketAsyncEventArgs)userData);
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }
    }
}
