using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace jSock;

public class SocketClient
{
    public string ClientID { get; set; } = "";

    public string? IP { get; set; } = null;
    public int Port { get; set; } = -1;

    public TcpClient Stream { get; internal set; } = new TcpClient();

    public SocketClient(string clientID, TcpClient stream)
    {
        ClientID = clientID;
        Stream = stream;
    }
}

public delegate void ServerOnRecieve(int clientID, string data);
public delegate void ServerOnConnect(int clientID);
public delegate void ServerOnDisconnect(int clientID);
public delegate void ServerOnError(string data);

//A class for real-time bidirectional communication between the AppServer and connected clients.
public class jSockServer
{
    public List<SocketClient> Clients = new List<SocketClient>();
    private Thread? rtcThread = null;
    public string Address { get; } = "";
    public int PORT { get; } = -1;

    public jSockServer(string address, int pORT)
    {
        Address = address;
        PORT = pORT;
    }

    public char[] ClientID_CharSet { get; private set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private string NewClientID()
    {
        var sb = new StringBuilder();
        var rnd = new Random();

        do { for (var i = 0; i < 5; i++) sb.Append(ClientID_CharSet[rnd.Next(ClientID_CharSet.Length)]);
        } while (Clients.Any(c => c.ClientID == sb.ToString()));

        return sb.ToString();
    }

    private void HandleClient(int index)
    {
        NetworkStream stream = Clients[index].Stream.GetStream();

        while (true)
        {
            //If no data is available from the stream, something is wrong with the server.
            try { while (!stream.DataAvailable); } catch { break; }

            //Check if the index is valid.
            if (index < 0 || index >= Clients.Count) break;

            //Checks for GET request.
            while (Clients[index].Stream.Available < 3);

            byte[] bytes = new byte[Clients[index].Stream.Available];
            stream.Read(bytes, 0, Clients[index].Stream.Available);
            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                //This upgrades the client to a persistent WebSocket connection.
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim(), swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

                //RFC6455: Base64 encoded SHA-1 of the value of the Sec-WebSocket-Key header field is used to complete the negotiation.
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                //Create the response headers and return the handshake.
                byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\n" +
                                                         "Connection: Upgrade\r\n" +
                                                         "Upgrade: websocket\r\n" +
                                                         "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");
                                                         
                stream.Write(response, 0, response.Length);

                //Invoke the OnConnect event.
                OnConnect(index);
            }
            else
            {
                bool mask = (bytes[1] & 0b10000000) != 0;

                //Ensures that it is a text message from the client.
                int opcode = bytes[0] & 0b00001111,
                    msglen = bytes[1] - 128,
                    offset = 2;

                if (msglen == 126)
                {
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }

                if (msglen == 0) cText.WriteLine("Message lenght was 0 chars.", "RTC", ConsoleColor.Red);
                else if (mask)
                {
                    byte[] decoded = new byte[msglen], masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };

                    //Increments the offset by 4 to get the next Nibble.
                    offset += 4;

                    for (int i = 0; i < msglen; ++i) decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                    if (decoded.Length == 2)
                    {
                        if (decoded[0] == 3 && decoded[1] == 233)
                        {
                            OnDisconnect(index);
                            return;
                        }
                    }

                    //Decode the message as UTF-8 text.
                    string text = Encoding.UTF8.GetString(decoded);

                    if (text == "") return;

                    OnRecieve?.Invoke(index, text);
                }
                else cText.WriteLine("No MASK bit was set!", "RTC", ConsoleColor.Red);
            }
        }
    }


    public event ServerOnConnect OnConnect;
    public event ServerOnDisconnect OnDisconnect;
    public event ServerOnRecieve OnRecieve;
    public event ServerOnError OnError;


    //To disconnect a client.
    public void Disconnect(int index) => Clients[index].Stream.Close();

    //Reply to specific client.
    public bool Reply(int clientID, string msg)
    {
        if (!Clients[clientID].Stream.Connected)
        {
            Disconnect(clientID);
            return false;
        }

        try
        {
            NetworkStream stream = Clients[clientID].Stream.GetStream();
            Queue<string> que = new Queue<string>(msg.SplitInGroups(125));
            int len = que.Count;

            while (que.Count > 0)
            {
                int header = que.Count > 1 ? 0 : 1;                         //fin: 0 = more frames, 1 = final frame
                    header = (header << 1) + 0;                             //rsv1
                    header = (header << 1) + 0;                             //rsv2
                    header = (header << 1) + 0;                             //rsv3
                    header = (header << 4) + (que.Count == len ? 1 : 0);    //opcode: 0 = continuation frame, 1 = text
                    header = (header << 1) + 0;                             //mask: server -> client = no mask

                byte[] list = Encoding.UTF8.GetBytes(que.Dequeue());
                header = (header << 7) + list.Length;

                stream.Write(IntToByteArray((ushort)header), 0, 2);
                stream.Write(list, 0, list.Length);
            }

        }
        catch
        {
            Disconnect(clientID);
            return false;
        }

        return true;
    }

    //Converts an ushort to a byte array
    protected byte[] IntToByteArray(ushort value)
    {
        byte[]? ary = BitConverter.GetBytes(value);

        //Reverse the array depending on the endianness of the system's architecture.
        if (BitConverter.IsLittleEndian) Array.Reverse(ary);

        return ary;
    }

    private void Run()
    {
        IPAddress? IP = null;

        try
        {
            IP = IPAddress.Parse(Address);
        }
        catch
        {
            cText.WriteLine("Invalid IP address!", "RTC", ConsoleColor.Red);
            return;
        }

        TcpListener server = new TcpListener(IP, PORT);

        server.Start();
        cText.WriteLine($"Server has started on {Address}:{PORT}. Waiting for a connection...", "RTC", ConsoleColor.Magenta);

        while (true)
        {
            SocketClient sc = new SocketClient(NewClientID(), server.AcceptTcpClient());

            EndPoint? address = sc.Stream.Client.RemoteEndPoint;

            if (address is not null)
            {
                var splitAddress = address.ToString().Split(':');

                sc.IP = splitAddress[0];
                sc.Port = int.Parse(splitAddress[1]);
            }

            //Add client to clients and get index
            int index = Clients.Count;
            Clients.Add(sc);

            // cText.WriteLine("Client Connected", "RTC", ConsoleColor.Magenta);

            Thread thread = new Thread(() => HandleClient(index));
            thread.Start();
        }
    }

    //Broadcast message to all connected clients.
    public void Broadcast(string msg = "Hello, World!", int ignoreClientID = -1)
    {
        for (int i = 0; i < Clients.Count; ++i)
        {
            if (i == ignoreClientID) continue;
            Reply(i, msg);
        }
    }

    //Starts the RTC server on a new thread.
    public void Start()
    {
        try
        {
            rtcThread = new Thread(Run);
            rtcThread.Start();
        }
        catch (Exception e)
        {
            cText.WriteLine(e.Message, "RTC", ConsoleColor.Red);
        }
    }

    //Stops the RTC server.
    public void Stop()
    {
        try
        {
            if (rtcThread != null)
            {
                rtcThread.Abort();
                rtcThread = null;
                cText.WriteLine("RTC server has stopped.", "RTC", ConsoleColor.Magenta);
            }
            else
                cText.WriteLine("RTC server is not running.", "RTC", ConsoleColor.Red);
        }
        catch
        {
            cText.WriteLine("RTC server could not be stopped.", "RTC", ConsoleColor.Red);
        }
    }
}