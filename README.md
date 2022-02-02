# jSock
My C# WebSocket Server

# Example of a jSock Server
```csharp
using jSock;

class RTC : jSockServer
{
    public RTC(string address, int port) : base(address, port) {}

    //Runs when a client connects to the server.
    public override void OnConnect(int clientID)
    {
        Console.WriteLine("A client has connected!");
    }

    //Runs when a client disconnects from the server.
    public override void OnDisconnect()
    {
        Console.WriteLine("A client has disconnected!");
    }

    //Runs when a client sends a message to the server.
    public override void OnRecieve(int clientID, string text)
    {
        //Display the message.
        Console.WriteLine("A message from a client! " + text);

        //Send the message back to the client that sent it.
        Reply(clientID, "Thanks for your message!");

        //Send the message to all clients except the one that sent it.
        Broadcast("Hey guys, some client sent me a message!", clientID);
    }
}

class Program
{
    static void Main()
    {
        //Create a new jSock server instance and start it.
        RTC server = new RTC("127.0.0.1", 8080);
        server.Start();
    }
}
```
