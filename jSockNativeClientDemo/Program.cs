using jSock;

class RTC : jSockClient
{
    public override void OnRecieve(string text)
    {
        throw new NotImplementedException();
    }
}

class Program
{
    static RTC client = new RTC();

    static async Task Start()
    {
        await client.ConnectAsync("ws://localhost:8080");

        await client.SendMessageAsync("Hello world!");
    }

    static void Main()
    {
        System.Threading.Tasks.Task.Run(() => Start());
        
        while (true)
        {
            
            Console.ReadKey();
            System.Threading.Tasks.Task.Run(() => client.SendMessageAsync("test123"));
        }
    }
}