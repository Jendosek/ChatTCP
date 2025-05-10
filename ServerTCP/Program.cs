using System.Net;
using System.Net.Sockets;

class Program
{
    static async Task Main(string[] args)
    {
        ServerObject server = new ServerObject();
        await server.ListenAsync();
    }
}

class ServerObject
{
    TcpListener tcpListener = new TcpListener(IPAddress.Parse("10.0.2.83"), 8080);
    List<ClientObject> clients = new List<ClientObject>();

    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);
        if (client != null) clients.Remove(client);
        client?.Close();
    }

    protected internal async Task ListenAsync()
    {
        try
        {
            tcpListener.Start();
            Console.WriteLine("Сервер буде запущений на 10.0.2.83:8080");
            Console.WriteLine("Очікую підключень...");

            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                Console.WriteLine($"Підключений клієнт: {tcpClient.Client.RemoteEndPoint}");

                ClientObject clientObject = new ClientObject(tcpClient, this);
                clients.Add(clientObject);
                Task.Run(clientObject.ProcessAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка сервера: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    protected internal async Task BroadcastMessageAsync(string message, string id)
    {
        foreach (var client in clients)
        {
            if (client.Id != id)
            {
                await client.Writer.WriteLineAsync(message);
                await client.Writer.FlushAsync();
            }
        }
    }

    protected internal void Disconnect()
    {
        foreach (var client in clients)
        {
            client.Close();
        }
        tcpListener.Stop();
        Console.WriteLine("Сервер зупинено");
    }
}

class ClientObject
{
    protected internal string Id { get; } = Guid.NewGuid().ToString();
    protected internal StreamWriter Writer { get; }
    protected internal StreamReader Reader { get; }

    TcpClient client;
    ServerObject server;

    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
    {
        client = tcpClient;
        server = serverObject;
        var stream = client.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream);
    }

    public async Task ProcessAsync()
    {
        try
        {
            string? userName = await Reader.ReadLineAsync();
            string? message = $"{userName} увійшов в чат";
            await server.BroadcastMessageAsync(message, Id);
            Console.WriteLine(message);

            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    if (message == null) continue;
                    message = $"{userName}: {message}";
                    Console.WriteLine(message);
                    await server.BroadcastMessageAsync(message, Id);
                }
                catch
                {
                    message = $"{userName} вийшов з чату";
                    Console.WriteLine(message);
                    await server.BroadcastMessageAsync(message, Id);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Помилка: {e.Message}");
        }
        finally
        {
            server.RemoveConnection(Id);
        }
    }

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }
}