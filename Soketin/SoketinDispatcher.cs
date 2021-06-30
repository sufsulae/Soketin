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
using System.Threading;

namespace Soketin
{
    internal class SoketinDelegate {
        public Delegate del;
        public object[] arg;
    }
    public static class SoketinDispatcher
    {
        internal static Queue<SoketinDelegate> m_queue;
        static SoketinDispatcher() {
            m_queue = new Queue<SoketinDelegate>();
        }

        internal static void AddExecution(SoketinDelegate del) {
            Monitor.Enter(m_queue);
            try {
                m_queue.Enqueue(del);
            }
            catch { }
            finally {
                Monitor.Exit(m_queue);
            }
        }

        public static void AddExecution(Delegate del, params object[] args) {
            var newDel = new SoketinDelegate() {  del = del,  arg = args };
            AddExecution(newDel);
        }
        public static void Execute() {
            Monitor.Enter(m_queue);
            SoketinDelegate delC = null;
            try { delC = m_queue.Dequeue(); } catch { }
            finally { Monitor.Exit(m_queue); }
            delC.del?.DynamicInvoke(delC.arg);

        }
        public static void Clear() {
            m_queue = new Queue<SoketinDelegate>();
        }
    }
}
