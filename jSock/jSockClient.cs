﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jSock;

public delegate void OnRecieve(string data);
public delegate void OnConnect();

public partial class jSockClient : IDisposable
{
    private ClientWebSocket WS;
    private CancellationTokenSource CTS;
    public int ReceiveBufferSize { get; protected set; } = 8192;

    public string URL { get; private set; } = "";

    public async Task ConnectAsync(string url = "")
    {
        if (url != "") URL = url;

        if (string.IsNullOrEmpty(URL)) throw new ArgumentNullException("URL is empty or null.");

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

        if (WS.State == WebSocketState.Open) OnConnect?.Invoke();
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

        //OnDisconnect();
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
            //OnError("Error");
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

    public event OnRecieve OnRecieve;
    public event OnConnect OnConnect;
    // public event OnDelegate OnDisconnect;
    // public event OnDelegateData OnError;

    public void Dispose() => DisconnectAsync().Wait();
}

public partial class jSockClient : IDisposable
{
    public void Connect(string url = "")
    {
        ConnectAsync(url).Wait();
    }

    public void Disconnect()
    {
        DisconnectAsync().Wait();
    }

    public void SendMessage(string message)
    {
        SendMessageAsync(message).Wait();
    }
}