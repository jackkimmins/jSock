using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jSock;

public abstract class jSockClient : IDisposable
{
    private ClientWebSocket WS;
    private CancellationTokenSource CTS;
    public int ReceiveBufferSize { get; protected set; } = 8192;

    public string URL { get; private set; } = "";

    public async Task ConnectAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("URL is empty or null.");

        if (WS != null)
        {
            if (WS.State == WebSocketState.Open) return;
            else WS.Dispose();
        }

        WS = new ClientWebSocket();
        if (CTS != null) CTS.Dispose();
        CTS = new CancellationTokenSource();
        await WS.ConnectAsync(new Uri(url), CTS.Token);
        await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        if (WS.State == WebSocketState.Open) OnConnect();
    }

    public async Task DisconnectAsync()
    {
        if (WS is null) return;

        if (WS.State == WebSocketState.Open)
        {
            CTS.CancelAfter(TimeSpan.FromSeconds(2));
            await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
            await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        WS.Dispose();
        WS = null;
        CTS.Dispose();
        CTS = null;

        OnDisconnect();
    }

    private async Task ReceiveLoop()
    {
        var loopToken = CTS.Token;
        MemoryStream outputStream = null;
        WebSocketReceiveResult receiveResult = null;
        var buffer = new byte[ReceiveBufferSize];

        try
        {
            while (!loopToken.IsCancellationRequested)
            {
                outputStream = new MemoryStream(ReceiveBufferSize);
                do
                {
                    receiveResult = await WS.ReceiveAsync(buffer, CTS.Token);
                    if (receiveResult.MessageType != WebSocketMessageType.Close)
                    {
                        outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                }
                while (!receiveResult.EndOfMessage);

                if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                outputStream.Position = 0;
                ResponseReceived(outputStream);
            }
        }
        catch (TaskCanceledException)
        {
            OnError("Error");
        }
        finally
        {
            outputStream?.Dispose();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        await WS.SendAsync(buffer, WebSocketMessageType.Text, true, CTS.Token);
    }

    private void ResponseReceived(Stream inputStream)
    {
        var reader = new StreamReader(inputStream);
        var response = reader.ReadToEnd();

        OnRecieve(response);

        inputStream.Dispose();
    }

    public abstract void OnRecieve(string text);
    public virtual void OnConnect()
    {
        cText.WriteLine("Connected to jSock Server!", "jSockClient/INFO", ConsoleColor.Red);
    }

    public virtual void OnDisconnect()
    {
        cText.WriteLine("Disconnected from jSock Server!", "jSockClient/INFO", ConsoleColor.Red);
    }

    public virtual void OnError(string error)
    {
        cText.WriteLine(error, "jSockClient/Error", ConsoleColor.Red);
    }

    public void Dispose() => DisconnectAsync().Wait();
}