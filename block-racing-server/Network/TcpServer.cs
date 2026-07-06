using block_racing_server.Game;
using block_racing_server.Game.Rooms;
using System.Net;
using System.Net.Sockets;

namespace block_racing_server.Network;

public class TcpServer
{
    private TcpListener? _listener;

    private readonly SessionManager _sessionManager = new();
    private readonly PacketManager _packetManager = new();

    private readonly GameManager _gameManager;

    private CancellationTokenSource _cts = new();

    public TcpServer()
    {
        var roomManager = new RoomManager();
        _gameManager = new GameManager(roomManager);
    }


    public async Task StartAsync(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        Console.WriteLine($"서버 시작 : {port}");

        try
        {
            _ = Task.Run(() => GameLoop(_cts.Token));

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                PlayerSession session =
                    new(client, _packetManager, _sessionManager, _gameManager);

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

    private async Task GameLoop(CancellationToken token)
    {
        var tick = TimeSpan.FromMilliseconds(50);

        while (!token.IsCancellationRequested)
        {
            var start = DateTime.UtcNow;

            await _gameManager.Update();

            var elapsed = DateTime.UtcNow - start;

            var delay = tick - elapsed;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay);
        }
    }
}