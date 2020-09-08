using Soketin;
using System;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create a service
            SoketinServer server = new SoketinServer(2112);

            //Assign Callback
            server.OnUserConnected += (address) => {
                Console.WriteLine("Connected! " + address.ToString());
            };
            server.OnUserDisconnected += (address) =>
            {
                Console.WriteLine("Disconnected! " + address.ToString());
            };
            server.OnDataRecieved += (address, data) =>
            {
                Console.WriteLine(address.ToString() + ": " + Encoding.UTF8.GetString(data));
            };

            //Start the Server
            server.StartServer();

            //Do Something while Server is Running
            while (true)
            {
                if (server.isRunning) {
                    //Broadcast message to all clients
                    var line = Console.ReadLine();
                    server.Send(Encoding.UTF8.GetBytes(line));
                }
                Thread.Sleep(1);
            }
        }
    }
}
