using System.Net.Sockets;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "10.0.2.83";
        int port = 8080;
        using TcpClient client = new TcpClient();
        Console.WriteLine($"Клієнти будуть підключатись до ip: {host} по порту:{port}");
        Console.Write("Введіть своє ім'я: ");
        string? userName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "Анонім";
        }
        Console.WriteLine($"Ласкаво просимо, {userName}");
        
        StreamReader? Reader = null;
        StreamWriter? Writer = null;

        try
        {
            Console.WriteLine("Підключення до сервера...");
            await client.ConnectAsync(host, port);
            Console.WriteLine("Підключення встановлено!");
            
            Reader = new StreamReader(client.GetStream());
            Writer = new StreamWriter(client.GetStream());
            
            if (Writer is null || Reader is null) return;
            
            Task receiveTask = Task.Run(() => ReceiveMessageAsync(Reader));
            
            await SendMessageAsync(Writer, userName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
        finally
        {
            Writer?.Close();
            Reader?.Close();
            Console.WriteLine("З'єднання закрите");
        }
        
        Console.WriteLine("Натисніть любу клавішу для виходу...");
        Console.ReadKey();
    }

    static async Task SendMessageAsync(StreamWriter writer, string userName)
    {
        await writer.WriteLineAsync(userName);
        await writer.FlushAsync();
        Console.WriteLine("Щоб надіслати повідомлення, введіть текст і натисніть Enter");

        while (true)
        {
            string? message = Console.ReadLine();
            if (string.IsNullOrEmpty(message)) continue;
            
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();
        }
    }

    static async Task ReceiveMessageAsync(StreamReader reader)
    {
        while (true)
        {
            try
            {
                string? message = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message)) continue;
                Print(message);
            }
            catch
            {
                Console.WriteLine("З'єднання з сервером втрачено");
                break;
            }
        }
    }

    static void Print(string message)
    {
        if (OperatingSystem.IsWindows())
        {
            var position = Console.GetCursorPosition();
            int left = position.Left;
            int top = position.Top;
            Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
            Console.SetCursorPosition(0, top);
            Console.WriteLine(message);
            Console.SetCursorPosition(left, top + 1);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}