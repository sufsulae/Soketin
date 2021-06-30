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

namespace Soketin
{
    public class SoketinBroadcaster
    {
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
        public bool useDispatcher { get; set; }
        public uint bufferSize {
            get { return m_bufferSize; }
            set {
                if (m_bufferSize != value) {
                    m_bufferSize = value;
                    m_buffer = new byte[m_bufferSize];
                }
            }
        }

        private Socket m_socket;
        private uint m_bufferSize = 8196;
        private byte[] m_buffer;
        private bool m_signalStop;
        private EndPoint m_endPoint;
        private SoketinEvent m_event;

        public SoketinBroadcaster(uint port) {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            m_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            m_socket.Bind(new IPEndPoint(IPAddress.Any, (int)port));
            m_buffer = new byte[m_bufferSize];
        }
        ~SoketinBroadcaster() {
            m_socket.Close();
        }

        public void StartListening() {
            m_signalStop = false;
            m_endPoint = new IPEndPoint(IPAddress.Any, 0);
            m_socket.BeginReceiveFrom(m_buffer, 0, m_buffer.Length, 0, ref m_endPoint, new AsyncCallback(_onBeginRecieve), m_socket);
        }
        public void StopListening() {
            m_signalStop = true;
            m_socket.Close();
        }
        public void Send(byte[] data, params SoketinUser[] addressess) {
            if (addressess != null && addressess.Length > 0) {
                var packedFile = SoketinUtility.PackRawData(data);
                foreach (var address in addressess) {
                    var endPoint = new IPEndPoint(IPAddress.Parse(address._ipAddress), address._port);
                    m_socket.BeginSendTo(packedFile, 0, packedFile.Length, 0, endPoint, new AsyncCallback(_onBeginSend), address);
                }
            }
        }

        private void _onBeginRecieve(IAsyncResult ar) {
            var socket = (Socket)ar.AsyncState;
            try {
                var readedBytes = socket.EndReceiveFrom(ar, ref m_endPoint);
                var splitted = SoketinUtility.SplitRawPacket(m_buffer, readedBytes);
                foreach (var data in splitted) {
                    _execute((Action<byte[]>)onEvent.OnDataRecieved, data);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally {
                if (!m_signalStop)
                    socket.BeginReceiveFrom(m_buffer, 0, m_buffer.Length, 0, ref m_endPoint, new AsyncCallback(_onBeginRecieve), socket);
            }
        }
        private void _onBeginSend(IAsyncResult ar) {
            try
            {
                var endPoint = (IPEndPoint)ar.AsyncState;
                var byteTransfered = m_socket.EndSendTo(ar);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                _execute((Action<Exception, object>)onEvent.OnError, e);
            }
        }
        private void _execute(Delegate del, params object[] args)
        {
            if (useDispatcher)
                SoketinDispatcher.AddExecution(del, args);
            else
                del?.DynamicInvoke(args);
        }
    }
}
