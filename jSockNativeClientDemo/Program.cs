using jSock;

class Program
{
    static void Main()
    {
        jSockClient client = new jSockClient();

        client.Connect("ws://localhost:8080");

        client.SendMessage("Hello world!");

        client.OnRecieve += Client_OnRecieve;
        
        while (true)
        {
            client.SendMessage(Console.ReadLine() ?? "Hello, world!");
        }
    }

    private static void Client_OnRecieve(string data)
    {
        Console.WriteLine("test:" + data);
    }
}