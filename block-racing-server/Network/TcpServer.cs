using System.Net;
using System.Net.Sockets;

namespace block_racing_server.Network;

public class TcpServer
{
    private TcpListener? _listener;

    public async Task StartAsync(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Console.WriteLine($"서버 시작 : {port}");

        try
        {
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                PlayerSession session = new PlayerSession(client);
                _ = session.StartAsync(); // 세션 시작
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 에러: {ex.Message}");
            return;
        }
        finally
        {
            _listener.Stop();
        }

    }
}