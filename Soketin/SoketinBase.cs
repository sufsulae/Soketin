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
    public abstract class SoketinBase
    {
        public virtual uint BufferSize { get; set; }
        public virtual Socket Socket { get; protected set; }

        public Action<IPAddress, byte[]> OnDataRecieved;
        public Action<IPAddress, int> OnDataSended;

        protected Thread _thread;
        protected bool _stopSignal;

        public SoketinBase() {
            _thread = new Thread(ThreadWorker);
            BufferSize = 8196;
        }
        protected virtual void ThreadWorker(object obj) {
            while (!_stopSignal) {
                OnThreadRun(obj);
                Thread.Sleep(1);
            }
        }
        protected virtual void OnThreadRun(object obj) { }
    }
}
