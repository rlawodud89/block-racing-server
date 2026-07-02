using System.Net;
using System.Net.Sockets;

namespace block_racing_server.Network;

public class TcpServer
{
    private TcpListener? _listener;

    private readonly SessionManager _sessionManager = new();
    private readonly PacketManager _packetManager = new();

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

                PlayerSession session =
                    new(client, _packetManager, _sessionManager);

                _sessionManager.Add(session);

                _ = Task.Run(session.StartAsync);
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