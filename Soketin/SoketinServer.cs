/* Copyright © 2021 Yusuf Sulaeman <sufsulae@gmail.com>
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
    public class SoketinServer {
        public bool useDispatcher { get; set; }
        public SoketinUser[] clients { get { return m_clients.ToArray(); } }
        public SoketinEvent onEvent
        {
            get { return m_event; }
            set
            {
                if (value == null)
                    m_event = new SoketinEventImpl();
                else
                    m_event = value;
            }
        }

        private Socket m_socket;
        private uint m_port;
        private bool m_signalStop;
        private ManualResetEventSlim m_signalAccept;
        private List<SoketinUser> m_clients;
        private Thread m_listenerThread;
        private byte[] m_buffer;
        private uint m_userID;
        private SoketinEvent m_event;

        public SoketinServer(uint port) {
            m_port = port;
        }
        ~SoketinServer() {
            StopServer();
        }

        public void StartServer() {
            m_signalStop = false;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            m_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_socket.DontFragment = true;

            m_socket.Bind(new IPEndPoint(IPAddress.Any, (int)m_port));
            m_socket.Listen(100);
            m_clients = new List<SoketinUser>();
            m_signalAccept = new ManualResetEventSlim(false);
            m_buffer = new byte[8196];

            m_listenerThread = new Thread(_listenerThread);
            m_listenerThread.Start();
            _execute((Action)onEvent.OnServiceStart);
        }
        public void StopServer() {
            m_signalStop = true;
            m_signalAccept.Reset();
            if (m_socket.Connected)
                m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
            m_signalAccept.Dispose();
            m_buffer = null;
            _execute((Action)onEvent.OnServiceStop);
        }
        public void Broadcast(byte[] data) {
            var packedData = SoketinUtility.PackRawData(data);
            foreach (var client in m_clients) {
                Send(client, packedData);
            }
        }
        public void Broadcast(SoketinData data) {
            Broadcast(data.GetBytes());
        }
        public void Send(SoketinUser client, byte[] data) {
            var user = m_clients.Find((c) => { return c._socket == client._socket; });
            if (user != null) {
                var packedData = SoketinUtility.PackRawData(data);
                client._socket.BeginSend(packedData, 0, packedData.Length, 0, new AsyncCallback(_onBeginSend), client);
            }
        }
        public void Send(SoketinUser client, SoketinData data) {
            Send(client, data.GetBytes());
        }
        public void Send(int clientId, byte[] data) {
            var client = m_clients.Find((c) => { return c.id == clientId; });
            Send(client, data);
        }
        public void Send(int clientId, SoketinData data) {
            Send(clientId, data.GetBytes());
        }

        private void _onBeginAccept(IAsyncResult ar) {
            m_signalAccept.Set();
            if (m_signalStop)
                return;
            var client = ((Socket)ar.AsyncState).EndAccept(ar);
            var newClient = new SoketinUser() {
                _id = m_userID,
                _ipAddress = ((IPEndPoint)client.RemoteEndPoint).Address.ToString(),
                _port = ((IPEndPoint)client.RemoteEndPoint).Port,
                _socket = client,
            };
            m_clients.Add(newClient);
            client.BeginReceive(m_buffer, 0, m_buffer.Length, 0, new AsyncCallback(_onBeginReceive), client);
            _execute((Action<SoketinUser>)onEvent.OnClientConnected,newClient);
            m_userID++;
        }
        private void _onBeginReceive(IAsyncResult ar) {
            var client = (Socket)ar.AsyncState;
            var user = m_clients.Find((e) => { return e._socket == client; });
            try
            {
                int byteReaded = client.EndReceive(ar);
                if (byteReaded > 0) {
                    var packs = SoketinUtility.SplitRawPacket(m_buffer, byteReaded);
                    foreach (var pack in packs)
                        _execute((Action<SoketinUser, byte[]>)onEvent.OnDataRecieved, client, pack);
                }
                if (!m_signalStop)
                    client.BeginReceive(m_buffer, 0, m_buffer.Length, 0, new AsyncCallback(_onBeginReceive), client);
            }
            catch(Exception e){
                _execute((Action<Exception, object>)onEvent.OnError, e, user);
                client.Close();
                m_clients.Remove(user);
                Console.WriteLine("Client Disconnected");
                _execute((Action<SoketinUser>)onEvent.OnClientDisconnected, user);
            }
        }
        private void _onBeginSend(IAsyncResult ar) {
            var client = (Socket)ar.AsyncState;
            var byteTransfered = client.EndSend(ar);
        }
        private void _listenerThread() {
            while (!m_signalStop) {
                m_signalAccept.Reset();
                m_socket.BeginAccept(new AsyncCallback(_onBeginAccept), m_socket);
                m_signalAccept.Wait();
            }
        }
        private void _execute(Delegate del, params object[] args) {
            if (useDispatcher)
                SoketinDispatcher.AddExecution(del, args);
            else
                del?.DynamicInvoke(args);
        }
    }
}
