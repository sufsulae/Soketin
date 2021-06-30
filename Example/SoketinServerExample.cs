using System;
using Soketin;

public class SoketinServerExample
{
    static void Main(string[] args) {
        //Instancing Server
        SoketinServer server = new SoketinServer(4444);
        
        //Assign listener / event
        server.onEvent.onClientConnected = (client) => {
            Console.WriteLine("Client Connected: " + client.ipAddress + ":" + client.port);
        };
        server.onEvent.onClientDisconnected = (client) => {
            Console.WriteLine("Client Disconnected: " + client.ipAddress + ":" + client.port);
        };
        server.onEvent.onDataRecieved = (client, data) => {
            var packedData = new SoketinData(data);
            Console.WriteLine("Got Something from client: " + client.ipAddress + " : " + packedData.ReadString());
        };

        //Starting Server
        server.StartServer();

        //Sending Data
        var sendData = new SoketinData();
        sendData.WriteString("Something to Send");

        //Sending to specific Client
        server.Send(server.clients[0], sendData);

        //Sending to all connected Client
        server.Broadcast(sendData);

        //Stop the Server
        server.StopServer();
    }
}
