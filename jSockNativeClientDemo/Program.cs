using jSock;

class Program
{
    static void Main()
    {
        jSockClient client = new jSockClient("ws://localhost:8080");

        client.Connect();

        client.SendMessage("Hello world!");

        client.OnRecieve += Client_OnRecieve;
        client.OnConnect += Client_OnConnect;
        client.OnDisconnect += Client_OnDisconnect;
        client.OnError += Client_OnError;
        
        while (true)
        {
            client.SendMessage(Console.ReadLine() ?? "Hello, world!");
        }
    }

    private static void Client_OnRecieve(string data)
    {
        Console.WriteLine("MSG:" + data);
    }

    private static void Client_OnConnect()
    {
        Console.WriteLine("Connected!");
    }

    private static void Client_OnDisconnect()
    {
        Console.WriteLine("Disconnected!");
    }

    private static void Client_OnError(string data)
    {
        Console.WriteLine("Error:" + data);
    }
}