using Soketin;
using System;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create Service
            SoketinClient client = new SoketinClient(2112);

            //Set the Option
            client.AutoReconnect = true;

            //Assign Callback
            client.OnConnected += () => {
                Console.WriteLine("Connected to Server!");
            };
            client.OnDisconnected += () => {
                Console.WriteLine("Disconnected from Server!");
            };
            client.OnDataRecieved += (address, data) => {
                Console.WriteLine("Server: " + Encoding.UTF8.GetString(data));
            };

            //Connect to Server
            client.Connect("127.0.0.1", 2112);

            //Do Something while connected
            while (true)
            {
                if (client.isConnected)
                {
                    //Send Message to Server
                    var line = Console.ReadLine();
                    client.Send(Encoding.UTF8.GetBytes(line));
                }
                Thread.Sleep(1);
            }
        }
    }
}
