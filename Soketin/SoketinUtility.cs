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
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Soketin
{
    public static class SoketinUtility
    {
        //Public Method
        public static byte[] PackRawData(byte[] rawData, int offset = 0)
        {
            var len = BitConverter.GetBytes(rawData.Length);
            var res = new byte[len.Length + rawData.Length];
            Buffer.BlockCopy(len, 0, res, 0, len.Length);
            Buffer.BlockCopy(rawData, offset, res, len.Length, rawData.Length - offset);
            return res;
        }
        public static byte[] UnpackRawData(byte[] packedData, int offset = 0)
        {

            if (packedData.Length < 4)
                throw new InvalidOperationException("Data is not in valid format");
            var countBytes = new byte[4];
            Buffer.BlockCopy(packedData, offset, countBytes, 0, 4);
            var len = BitConverter.ToInt32(countBytes, 0);
            if (len > packedData.Length - (offset + 4))
                throw new Exception("Data is Corrupt");
            var res = new byte[len];
            Buffer.BlockCopy(packedData, offset + 4, res, 0, len);
            return res;
        }
        public static List<byte[]> SplitRawPacket(byte[] packedData, int packedDataLength = 0)
        {
            var res = new List<byte[]>();
            var count = 0;
            if (packedDataLength <= 0)
                packedDataLength = packedData.Length;
            while (count < packedDataLength)
            {
                var data = UnpackRawData(packedData, count);
                count += data.Length + 4;
                res.Add(data);
            }
            return res;
        }

        public static byte[] ZipString(string str)
        {
            var raw = Encoding.UTF8.GetBytes(str);
            using (var dataIn = new MemoryStream(raw))
            {
                using (var dataOut = new MemoryStream())
                {
                    using (var zip = new GZipStream(dataOut, CompressionMode.Compress))
                    {
                        dataIn.CopyTo(zip);
                    }
                    return dataOut.ToArray();
                }
            }
        }
        public static string UnzipString(byte[] data)
        {
            using (var dataIn = new MemoryStream(data))
            {
                using (var dataOut = new MemoryStream())
                {
                    using (var zip = new GZipStream(dataIn, CompressionMode.Decompress))
                    {
                        zip.CopyTo(dataOut);
                    }
                    return Encoding.UTF8.GetString(dataOut.ToArray());
                }
            }
        }

        public static byte[] StringToBytes(string str) {
            return Encoding.UTF8.GetBytes(str);
        }
        public static string BytesToString(byte[] data) {
            return Encoding.UTF8.GetString(data);
        }
    }
}
