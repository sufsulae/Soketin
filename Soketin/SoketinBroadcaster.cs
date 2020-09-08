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

namespace Soketin
{
    public class SoketinBroadcaster : SoketinBase
    {
        private bool m_stopSignal = true;
        private byte[] m_buffer;
        private IPEndPoint m_tgtEp;

        //Constructor
        public SoketinBroadcaster(int port) : base() {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            m_tgtEp = new IPEndPoint(IPAddress.Any, port);
            Socket.Bind(m_tgtEp);
            m_buffer = new byte[BufferSize];
        }

        //Public Method
        public void StartRecieving() {
            if (!m_stopSignal)
                return;
            m_stopSignal = false;
            SocketAsyncEventArgs recArg = new SocketAsyncEventArgs();
            recArg.SetBuffer(new byte[BufferSize], 0, (int)BufferSize);
            recArg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            recArg.Completed += (obj, e) => {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
                    var splittedData = SoketinUtility.SplitRawPacket(e.Buffer, e.BytesTransferred);
                    foreach (var data in splittedData) {
                        OnDataRecieved?.Invoke(e.ReceiveMessageFromPacketInfo.Address, data);
                    }
                }
                if (!m_stopSignal)
                    Socket.ReceiveMessageFromAsync(recArg);
            };
            Socket.ReceiveMessageFromAsync(recArg);
        }
        public void StopRecieving() {
            if (m_stopSignal)
                return;
            m_stopSignal = true;
        }
        public void Send(byte[] data, params IPEndPoint[] IpAddresses) {
            if (IpAddresses != null && IpAddresses.Length > 0)
            {
                data = SoketinUtility.PackRawData(data);
                foreach (var ip in IpAddresses) {
                    SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                    sendArg.SetBuffer(data, 0, data.Length);
                    sendArg.RemoteEndPoint = ip;
                    sendArg.Completed += (obj, e) => {
                        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) {
                            OnDataSended?.Invoke(((IPEndPoint)e.RemoteEndPoint).Address, e.BytesTransferred);
                        }
                        e.Dispose();
                    };
                    Socket.SendToAsync(sendArg);
                }
            }
        }
    }
}
