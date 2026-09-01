using block_racing_common.Network;
using block_racing_server.Game;
using block_racing_server.Game.Rooms;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace block_racing_server.Network;

public class TcpServer
{
    private TcpListener? _listener;

    private readonly SessionManager _sessionManager;
    private readonly PacketManager _packetManager;

    private readonly GameManager _gameManager;

    private CancellationTokenSource _cts = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TcpServer> _logger;

    public TcpServer(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TcpServer>();

        _sessionManager = new SessionManager(loggerFactory);
        _packetManager = new PacketManager(loggerFactory);

        var roomManager = new RoomManager(loggerFactory);
        _gameManager = new GameManager(roomManager, loggerFactory);
    }


    public async Task StartAsync(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        _logger.LogInformation(
            "Server started. Port={Port}",
            port);

        try
        {
            _ = Task.Run(() => GameLoop(_cts.Token));

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();

                _logger.LogInformation(
                    "Client accepted. RemoteEndPoint={RemoteEndPoint}",
                    client.Client.RemoteEndPoint);

                PlayerSession session =
                    new(
                        client,
                        _packetManager,
                        _sessionManager,
                        _gameManager,
                        _loggerFactory);

                _sessionManager.Add(session);

                _ = Task.Run(session.StartAsync);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Server error occurred.");
        }
        finally
        {
            _listener.Stop();

            _logger.LogInformation("Server stopped.");
        }

    }

    private async Task GameLoop(CancellationToken token)
    {
        var tick = TimeSpan.FromMilliseconds(50);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var start = DateTime.UtcNow;

                await _sessionManager.UpdateAsync();
                await _gameManager.Update();

                var elapsed = DateTime.UtcNow - start;
                var delay = tick - elapsed;

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            _logger.LogInformation("Game loop stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game loop crashed.");
        }
    }
}