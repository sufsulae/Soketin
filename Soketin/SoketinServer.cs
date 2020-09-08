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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Soketin
{
    public class SoketinServer : SoketinBase
    {
        public bool isRunning { get; private set; }
        public Socket[] Clients { get { return m_clientSockets.ToArray(); } }

        public Action<IPAddress> OnUserConnected;
        public Action<IPAddress> OnUserDisconnected;

        private List<Socket> m_clientSockets;
        private bool m_stopSignal;
        
        //Constructor
        public SoketinServer(int port) {
            m_clientSockets = new List<Socket>();

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            Socket.DontFragment = true;
        }

        //Public Method
        public void StartServer() {
            if (isRunning)
                return;
            Socket.Listen(100);
            m_stopSignal = false;
            isRunning = true;

            SocketAsyncEventArgs recieveArgs = new SocketAsyncEventArgs();
            recieveArgs.SetBuffer(new byte[BufferSize], 0, (int)BufferSize);
            recieveArgs.Completed += (obj, e) =>
            {
                var client = (Socket)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    var splittedData = SoketinUtility.SplitRawPacket(e.Buffer, e.BytesTransferred);
                    foreach (var data in splittedData)
                        OnDataRecieved?.Invoke(((IPEndPoint)client.RemoteEndPoint).Address, data);
                    if (!m_stopSignal)
                        client.ReceiveFromAsync(e);
                }
                else
                {
                    try
                    {
                        client.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception)
                    {
                        client.Close();
                    }

                }
            };

            SocketAsyncEventArgs accArgs = new SocketAsyncEventArgs();
            accArgs.Completed += (obj, e) =>
            {
                if (e.SocketError == SocketError.Success && e.AcceptSocket != null)
                {
                    var clientSock = e.AcceptSocket;
                    m_clientSockets.Add(clientSock);
                    var address = ((IPEndPoint)clientSock.RemoteEndPoint).Address;
                    OnUserConnected?.Invoke(address);
                    recieveArgs.RemoteEndPoint = clientSock.RemoteEndPoint;
                    recieveArgs.UserToken = clientSock;
                    clientSock.ReceiveFromAsync(recieveArgs);
                }
                e.AcceptSocket = null;
                if (!m_stopSignal)
                    Socket.AcceptAsync(e);
            };
            Socket.AcceptAsync(accArgs);

            _thread.Start(null);
        }
        public void StopServer()
        {
            if (!isRunning)
                return;
            m_stopSignal = true;
            isRunning = false;
            _thread.Join();
            Socket.Close();
        }
        public void Send(byte[] data, string ip = null)
        {
            if (data == null)
                return;
            data = SoketinUtility.PackRawData(data);
            if (ip == null)
            {
                foreach (var client in m_clientSockets)
                {
                    IPAddress address = ((IPEndPoint)client.RemoteEndPoint).Address;
                    client.BeginSend(data, 0, data.Length, SocketFlags.None, (e) => {
                        OnDataSended?.Invoke(address, client.EndSend(e));
                    }, null);
                }
            }
            else
            {
                foreach (var client in m_clientSockets)
                {
                    IPAddress address = ((IPEndPoint)client.RemoteEndPoint).Address;
                    if (address.ToString() == ip) {
                        client.BeginSend(data, 0, data.Length, SocketFlags.None, (e) => {
                            OnDataSended?.Invoke(address, client.EndSend(e)); 
                        }, null);
                        return;
                    }
                }
            }
        }
        protected override void ThreadWorker(object obj)
        {
            while (!m_stopSignal)
            {
                if (m_clientSockets.Count > 0)
                {
                    var clients = new List<Socket>();
                    clients.AddRange(m_clientSockets);
                    Socket.Select(clients, clients, null, 10);
                    foreach (var client in clients)
                    {
                        m_clientSockets.Remove(client);
                        OnUserDisconnected?.Invoke(((IPEndPoint)client.RemoteEndPoint).Address);
                    }
                }
                Thread.Sleep(1);
            };
        }
    }
}
