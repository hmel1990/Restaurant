using System.Net;
using System.Net.Sockets;
using System.Text;

class UdpChatClient
{
    private const int serverPort = 9000;
    private UdpClient? client;
    private IPEndPoint? serverEndpoint;
    private string menu = "Выберите из списка, что хотетите заказать: \r\n\r\n" +
        "1.Утка по-французски с апельсиновым соусом (Canard à l'orange)\r\n\r\n" +
        "2.Паэлья с морепродуктами (Paella de Mariscos)\r\n\r\n" +
        "3.Бефстроганов по-русски (Boeuf Stroganoff)\r\n\r\n" +
        "4/Итальянская лазанья с мясом (Lasagna al Forno)\r\n\r\n" +
        "5.Французский луковый суп (Soupe à l'oignon)\r\n\r\n" +
        "6.Медальоны из телятины с грибным соусом\r\n\r\n" +
        "7.Ризотто с белыми грибами (Risotto ai Funghi Porcini)\r\n\r\n" +
        "8.Карпаччо из говядины с пармезаном\r\n\r\n" +
        "9.Запечённый камамбер с мёдом и орехами\r\n\r\n" +
        "10.Тапас ассорти (разные испанские закуски)";

    public async Task StartAsync()
    {
        Console.Title = "CLIENT SIDE";
        var serverIp = "127.0.0.1";
        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        client = new UdpClient(0);
        await ConnectToServerAsync();

        AppDomain.CurrentDomain.ProcessExit += async (s, e) => await SendDisconnectMessageAsync();

        _ = Task.Run(ReceiveMessagesAsync);
        await SendMessagesAsync();
    }

    private async Task ConnectToServerAsync()
    {
        while (true)
        {
            try
            {
                client.Connect(serverEndpoint);
                await SendInitialMessageAsync();
                Console.WriteLine("Подключено к серверу.");
                break;
            }
            catch (SocketException)
            {
                Console.WriteLine("Ожидание подключения к серверу...");
                await Task.Delay(1000);
            }
        }
    }

    private async Task SendInitialMessageAsync()
    {
        var initialMessage = "Клиент подключился";
        var data = Encoding.UTF8.GetBytes(initialMessage);
        await client.SendAsync(data, data.Length);
    }

    private async Task SendDisconnectMessageAsync()
    {
        var disconnectMessage = "off";
        var data = Encoding.UTF8.GetBytes(disconnectMessage);
        await client.SendAsync(data, data.Length);
    }

    private async Task ReceiveMessagesAsync()
    {
        while (true)
        {
            var result = await client.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);
            Console.WriteLine(message);
        }
    }

    private async Task SendMessagesAsync()
    {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(menu);
            Console.ResetColor();

        while (true)
        {
            var message = Console.ReadLine();
            var data = Encoding.UTF8.GetBytes(message);
            await client.SendAsync(data, data.Length);

            if (message == "off")
                break;
        }

        client.Close();
        Console.WriteLine("Отключено от сервера.");
    }

    static async Task Main() => await new UdpChatClient().StartAsync();
}