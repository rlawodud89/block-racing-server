using block_racing_server.Network;

namespace block_racing_server;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            TcpServer server = new();
            await server.StartAsync(7777);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 시작 에러: {ex.Message}");
            return;
        }
    }
}

