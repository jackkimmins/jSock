using jSock;

class Program
{
    static void Main()
    {
        jSockClient client = new jSockClient();

        client.Connect("ws://localhost:8080");

        client.SendMessage("Hello world!");

        client.OnRecieve += Client_OnRecieve;
        client.OnConnect += Client_OnConnect;
        
        while (true)
        {
            client.SendMessage(Console.ReadLine() ?? "Hello, world!");
        }
    }

    private static void Client_OnConnect()
    {
        Console.WriteLine("Connected!");
    }

    private static void Client_OnDisconnect()
    {
        Console.WriteLine("Disconnected!");
    }

    private static void Client_OnRecieve(string data)
    {
        Console.WriteLine("test:" + data);
    }
}