# Soketin
Soketin is a simple asynchronous non-blocking-thread Socket Library.

# Example
SERVER
```csharp
 //Create a service
SoketinServer server = new SoketinServer(2112, SoketinType.TCP);

//Assign Callback
server.OnUserConnected += (address) => {
    Console.WriteLine("Connected! " + address.ToString());
};
server.OnUserDisconnected += (address) =>
{
    Console.WriteLine("Disconnected! " + address.ToString());
};
server.OnReceivedData += (address, data) =>
{
    Console.WriteLine(address.ToString() + ": " + Encoding.UTF8.GetString(data));
};

//Start the Server
server.Start();

//Do Something while running
while (server.isRunning)
{
    //Broadcast message to all clients
    var line = Console.ReadLine();
    server.Send(Encoding.UTF8.GetBytes(line));
    Thread.Sleep(1);
}
```
Client:
```csharp
 //Create Service
SoketinClient client = new SoketinClient(2112, SoketinType.TCP);

//Set the Option
client.AutoReconnect = true;

//Assign Callback
client.OnConnected += (address) =>
{
    Console.WriteLine("Connected to Server: " + address.ToString());
};
client.OnDisconnected += (address) =>
{
    Console.WriteLine("Disconnected to Server: " + address.ToString());
};
client.OnReceivedData += (data) =>
{
    Console.WriteLine("Server: " + Encoding.UTF8.GetString(data));
};

//Connect to Server
client.Connect("192.168.8.101", 2112);

//Do Something while connected
while (client.isConnected)
{
    //Send Message to Server
    var line = Console.ReadLine();
    client.Send(Encoding.UTF8.GetBytes(line));
    Thread.Sleep(1);
}
```

