using Soketin;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace Broadcaster
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create Service
            SoketinBroadcaster broadcaster = new SoketinBroadcaster(2122);

            //Assign Callback
            broadcaster.OnDataRecieved += (address, data) => {
                Console.WriteLine("Received data from " + address + ":" + Encoding.ASCII.GetString(data));
            };
            //broadcaster.OnDataSended += (address, count) => {
            //    Console.WriteLine("Sended data to " + address + " - " + count + " bytes");
            //};

            //Start Service
            broadcaster.StartRecieving();

            //Start broadcast some data
            var ipList = new IPEndPoint[] {
                new IPEndPoint(IPAddress.Parse("192.168.8.104"), 2122),
                //new IPEndPoint(IPAddress.Parse("192.168.8.102"), 2122),
            };
            var load = Encoding.ASCII.GetBytes("Hello World!");
            while (true) {
                broadcaster.Send(load, ipList);
                Thread.Sleep(1);
            }
        }

        private static IPEndPoint[] GetIPListCandidate(int port) {
            var ipAddress = Dns.GetHostEntry(Dns.GetHostName());
            var result = new List<IPEndPoint>();
            foreach (var ip in ipAddress.AddressList) {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    var ipStr = ip.ToString();
                    var ipStart = ipStr.Substring(0, ipStr.LastIndexOf("."));
                    for (int i = 1; i < 256; i++) {
                        var testIp = ipStart + "." + i;
                        result.Add(new IPEndPoint(IPAddress.Parse(ipStart + "." + i), port));   
                    }
                }
            }
            return result.ToArray();
        }
    }
}
