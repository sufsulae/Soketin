# Soketin
Soketin is a simple asynchronous non-blocking-thread Socket Library.

# Example
Server:
```csharp
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
```
Client:
```csharp
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
```

