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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Soketin
{
    public class SoketinClient {

        public int bufferSize
        {
            get { return m_buffer.Length; }
            set {
                if (value != m_buffer.Length) {
                    m_buffer = new byte[value];
                }
            }
        }
        public bool autoReconnect { get; set; }
        public bool useDispatcher { get; set; }
        public SoketinEvent onEvent {
            get { return m_event; }
            set {
                if (value == null)
                    m_event = new SoketinEventImpl();
                else
                    m_event = value;
            }
        }
        public SoketinUser server { get; private set; }

        private byte[] m_buffer;
        private Socket m_socket;
        
        private bool m_stopSignal;
        private IPAddress m_ip = null;
        private int m_port = 0;
        private ManualResetEventSlim m_signalRecieve;
        private SoketinEvent m_event;

        public SoketinClient() {
            m_buffer = new byte[8196];
            onEvent = new SoketinEventImpl();
        }
        ~SoketinClient() {
            Disconnect();
        }

        public async void Connect(string address, uint port) {
            m_stopSignal = false;
            try {
                var hostEntry = await Dns.GetHostEntryAsync(address);
                foreach (var addr in hostEntry.AddressList) {
                    if (addr.AddressFamily == AddressFamily.InterNetwork) {
                        m_ip = addr;
                        break;
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
                _execute((Action<Exception, object>)onEvent.OnError, e, null);
;            }
            if (m_ip != null) {
                m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                m_socket.DontFragment = true;

                m_signalRecieve = new ManualResetEventSlim(false);
                m_port = (int)port;
                m_socket.BeginConnect(m_ip, m_port, new AsyncCallback(_onBeginConnect), m_socket);
                _execute((Action)onEvent.OnServiceStart);
            }
        }
        public void Disconnect() {
            m_stopSignal = true;
            m_socket.BeginDisconnect(true, new AsyncCallback(_onBeginDisconnect), m_socket);
        }
        public void Send(byte[] data) {
            if (m_stopSignal)
                return;
            var packedData = SoketinUtility.PackRawData(data);
            m_socket.BeginSend(packedData, 0, packedData.Length, 0, new AsyncCallback(_onBeginSend), m_socket);
        }
        public void Send(SoketinData data) {
            Send(data.GetBytes());
        }

        private void _onBeginConnect(IAsyncResult ar) {
            if (m_stopSignal)
                return;
            var socket = (Socket)ar.AsyncState;
            try {
                socket.EndConnect(ar);
                server = new SoketinUser() {
                    _ipAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString(),
                    _port = ((IPEndPoint)socket.RemoteEndPoint).Port,
                    _socket = socket,
                };
                _execute((Action)onEvent.OnServerConnected);
                if(!m_stopSignal)
                    m_socket.BeginReceive(m_buffer, 0, m_buffer.Length, 0, new AsyncCallback(_onBeginRecieve), m_socket);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                _execute((Action<Exception, object>)onEvent.OnError, e, null);
                if (autoReconnect)
                    socket.BeginConnect(m_ip, m_port, new AsyncCallback(_onBeginConnect), socket);
            }
        }
        private void _onBeginDisconnect(IAsyncResult ar) {
            var socket = (Socket)ar.AsyncState;
            try
            {
                socket.EndDisconnect(ar);
                _execute((Action)onEvent.OnServerDisconnected);
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                _execute((Action<Exception, object>)onEvent.OnError, e, null);
            }
            finally {
                socket.Close();
                _execute((Action)onEvent.OnServiceStop);
                server = null;
            }
        }
        private void _onBeginRecieve(IAsyncResult ar) {
            if (m_stopSignal)
                return;
            var socket = (Socket)ar.AsyncState;
            try
            {
                int byteReaded = socket.EndReceive(ar);
                if (byteReaded > 0) {
                    var packs = SoketinUtility.SplitRawPacket(m_buffer, byteReaded);
                    foreach (var pack in packs) {
                        _execute((Action<SoketinUser, byte[]>)onEvent.OnDataRecieved, server, pack);
                    }
                }
                m_socket.BeginReceive(m_buffer, 0, m_buffer.Length, 0, new AsyncCallback(_onBeginRecieve), m_socket);
            }
            catch (Exception e)
            {
                _execute((Action<Exception, object>)onEvent.OnError, e, null);
                if (autoReconnect)
                    socket.BeginDisconnect(true, new AsyncCallback(_onReconnect), socket);
                else
                    Disconnect();
            }
        }
        private void _onReconnect(IAsyncResult ar) {
            var socket = (Socket)ar.AsyncState;
            socket.EndDisconnect(ar);
            socket.BeginConnect(m_ip, (int)m_port, new AsyncCallback(_onBeginConnect), socket);
        }
        private void _onBeginSend(IAsyncResult ar) {
            try
            {
                var socket = (Socket)ar.AsyncState;
                var byteTransfered = socket.EndSend(ar);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                _execute((Action<Exception, object>)onEvent.OnError, e);
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
