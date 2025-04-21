using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class UdpChatServer
{
    private const int port = 9000;
    private UdpClient? server;
    private ConcurrentDictionary<IPEndPoint, bool> clients = new();
    private ConcurrentDictionary<IPEndPoint, int> clientsWithId = new();
    private StringBuilder messageHistory = new();
    private int clientCounter = 0;

    private static readonly object fileLock = new object();

    private static readonly string logFilePath = "E:\\STEP\\WebProg\\Restaurant\\Server\\bin\\Debug\\net9.0\\server_log.txt";

    private static readonly ConcurrentQueue<string> logQueue = new();
    private static readonly CancellationTokenSource logCts = new();
    private static readonly Task logTask = Task.Run(() => ProcessLogQueue());
    private static async Task ProcessLogQueue()
    {
        while (!logCts.Token.IsCancellationRequested)
        {
            try
            {
                if (logQueue.TryDequeue(out string logEntry))
                {
                    lock (fileLock)
                    {
                        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                    }
                }
                else
                {
                    await Task.Delay(50, logCts.Token); // ждём немного, если очередь пуста
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи лога: {ex.Message}");
            }
        }
    }


    public async Task StartAsync()
    {
        Console.Title = "SERVER SIDE";
        await InitializeServerAsync();

        _ = Task.Run(ReceiveMessagesAsync);
        await HandleConsoleInputAsync();
    }

    private async Task InitializeServerAsync()
    {
        while (true)
        {
            try
            {
                server = new UdpClient(port);
                Console.WriteLine($"Сервер запущен на порту {port}.");
                break;
            }
            catch (SocketException)
            {
                Console.WriteLine("Не удалось запустить сервер. Ожидание повторного подключения...");
                await Task.Delay(1000);
            }
        }
    }

    private Dictionary<int, string> menuItems = new Dictionary<int, string>
{
    { 1, "Утка по-французски с апельсиновым соусом (Canard à l'orange)" },
    { 2, "Паэлья с морепродуктами (Paella de Mariscos)" },
    { 3, "Бефстроганов по-русски (Boeuf Stroganoff)" },
    { 4, "Итальянская лазанья с мясом (Lasagna al Forno)" },
    { 5, "Французский луковый суп (Soupe à l'oignon)" },
    { 6, "Медальоны из телятины с грибным соусом" },
    { 7, "Ризотто с белыми грибами (Risotto ai Funghi Porcini)" },
    { 8, "Карпаччо из говядины с пармезаном" },
    { 9, "Запечённый камамбер с мёдом и орехами" },
    { 10, "Тапас ассорти (разные испанские закуски)" }
};
    private async Task ForSwitchCase(int menuNumber, IPEndPoint client, int ID)
    {
        var formattedMessage = $"\n{client} (клиент № {ID}): заказал {menuItems[menuNumber]}";
        messageHistory.AppendLine(formattedMessage);
        var logEntry = $"{DateTime.Now}: {formattedMessage}";
        logQueue.Enqueue(logEntry);
        await BroadcastMessageAsync(formattedMessage, client);

        _ = Task.Run(async () =>
        {
            await Task.Delay(10000);
            var readyMessage = $"\n{client} {menuItems[menuNumber]} (клиента № {ID}) готова";
            messageHistory.AppendLine(readyMessage);
            logEntry = $"{DateTime.Now}: {readyMessage}";
            logQueue.Enqueue(logEntry);
            await BroadcastMessageAsync(readyMessage, client);
        });
    }

    private async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            var result = await server.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);

            if (!clients.ContainsKey(result.RemoteEndPoint))
            {
                clients[result.RemoteEndPoint] = true;
                clientCounter++;
                clientsWithId[result.RemoteEndPoint] = clientCounter;
                await SendHistoryAsync(result.RemoteEndPoint);
                Console.WriteLine($"\nКлиент подключился: {result.RemoteEndPoint} (Клиент #{clientCounter})");
                var logEntry = $"{DateTime.Now}: Клиент подключился: {result.RemoteEndPoint} (Клиент #{clientCounter})";
                logQueue.Enqueue(logEntry);

            }

            if (message == "off")
            {
                clients.TryRemove(result.RemoteEndPoint, out _);
                //clientsWithId.TryRemove(result.RemoteEndPoint, out _); // Не удаляем ID клиента, чтобы сохранить № заказа-клиента
                Console.WriteLine($"\nКлиент отключился: {result.RemoteEndPoint}");
                var logEntry = $"{DateTime.Now}: Клиент отключился: {result.RemoteEndPoint} (Клиент #{clientCounter})";
                logQueue.Enqueue(logEntry);
                continue;
            }

            var clienetID = clientsWithId[result.RemoteEndPoint];
            //var formattedMessage = $"\n{result.RemoteEndPoint} (клиент № {clienetID}): {message}";

            var formattedMessage="";
            switch (message)
            {
                case "1":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "2":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "3":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "4":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "5":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "6":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "7":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "8":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "9":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;

                case "10":
                    await ForSwitchCase(1, result.RemoteEndPoint, clienetID);
                    break;
            }

        }

    }

    private async Task SendHistoryAsync(IPEndPoint client)
    {
        var history = Encoding.UTF8.GetBytes(messageHistory.ToString());
        await server.SendAsync(history, history.Length, client);
    }

    private async Task BroadcastMessageAsync(string message, IPEndPoint? excludeClient = null)
    {
        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients.Keys)
        {
            //if (!client.Equals(excludeClient)) // Клиент тоже должен видеть себя в обще очереди
                await server.SendAsync(data, data.Length, client);
        }
    }

    private async Task HandleConsoleInputAsync()
    {
        while (true)
        {
            Console.Write("Отправьте сообщение клиентам: ");
            var input = Console.ReadLine();
            var formattedMessage = $"\nСервер: {input}";
            messageHistory.AppendLine(formattedMessage);
            await BroadcastMessageAsync(formattedMessage);
        }
    }

    static async Task Main() => await new UdpChatServer().StartAsync();
}