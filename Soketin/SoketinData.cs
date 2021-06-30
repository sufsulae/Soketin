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
using System.IO;

namespace Soketin
{
    public class SoketinData
    {
        private MemoryStream m_stream;

        public long Position
        {
            get { return m_stream.Position; }
            set { m_stream.Position = value; }
        }
        public long Length
        {
            get { return m_stream.Length; }
        }

        //Constructor
        public SoketinData() {
            m_stream = new MemoryStream();
        }
        public SoketinData(byte[] data) {
            m_stream = new MemoryStream(data);
        }
        ~SoketinData() {
            m_stream.Dispose();
        }

        //Public Function
        public void ClearAll()
        {
            m_stream.Flush();
            m_stream.Dispose();
            m_stream = new MemoryStream();
        }
        public byte[] GetBytes() {
            return m_stream.ToArray();
        }
        public void Dispose() {
            m_stream.Dispose();
        }

        //Write Function
        #region Write Function
        public void WriteString(string value, bool compressed = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("Value Cannot Be Null");
            }
            byte[] strData = compressed ? SoketinUtility.ZipString(value) : SoketinUtility.StringToBytes(value);
            byte[] packedData = SoketinUtility.PackRawData(strData);
            m_stream.Write(packedData, 0, packedData.Length);
        }
        public void WriteFloat(float value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteInt(int value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteByte(byte value)
        {
            m_stream.WriteByte(value);
        }
        public void WriteShort(short value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteDouble(double value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteLong(long value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteBool(bool value)
        {
            var data = BitConverter.GetBytes(value);
            m_stream.Write(data, 0, data.Length);
        }
        public void WriteRaw(byte[] value)
        {
            m_stream.Write(value, 0, value.Length);
        }
        #endregion

        //Read Function
        #region Read Function
        public string ReadString(bool isCompressed = false)
        {
            //Read 4 byte
            var len = ReadInt();
            var buffer = new byte[len];
            m_stream.Read(buffer, 0, len);
            return isCompressed ? SoketinUtility.UnzipString(buffer) : SoketinUtility.BytesToString(buffer);
        }
        public float ReadFloat()
        {
            var buffer = new byte[4];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToSingle(buffer, 0);
        }
        public int ReadInt()
        {
            var buffer = new byte[4];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }
        public byte ReadByte()
        {
            return (byte)m_stream.ReadByte();
        }
        public short ReadShort()
        {
            var buffer = new byte[2];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }
        public double ReadDouble()
        {
            var buffer = new byte[8];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToDouble(buffer, 0);
        }
        public long ReadLong()
        {
            var buffer = new byte[8];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt64(buffer, 0);
        }
        public bool ReadBool()
        {
            var buffer = new byte[2];
            m_stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToBoolean(buffer, 0);
        }
        public int ReadRaw(byte[] buffers)
        {
            return m_stream.Read(buffers, 0, buffers.Length);
        }
        #endregion
    }
}
