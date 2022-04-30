using jSock;

class Program
{
    //Create a new jSock server instance and start it.
    private static jSockServer server = new jSockServer("127.0.0.1", 8080);

    static void Main()
    {
        server.OnConnect += Server_OnConnect;
        server.OnDisconnect += Server_OnDisconnect;
        server.OnRecieve += Server_OnRecieve;
        server.OnError += Server_OnError;

        server.Start();
    }

    private static void Server_OnConnect(int clientID)
    {
        Console.WriteLine("A client has connected!");
    }

    private static void Server_OnDisconnect(int clientID)
    {
        Console.WriteLine("A client has disconnected!");
    }

    private static void Server_OnRecieve(int clientID, string text)
    {
        //Display the message.
        Console.WriteLine("MSG: " + text);

        //Send the message back to the client that sent it.
        server.Reply(clientID, "You: " + text);

        //Send the message to all clients except the one that sent it.
        server.Broadcast("User:" + text, clientID);
    }

    private static void Server_OnError(string data)
    {
        Console.WriteLine("Error:" + data);
    }
}