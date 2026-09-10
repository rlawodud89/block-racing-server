using block_racing_common.Network;
using block_racing_server.Game;
using block_racing_server.Game.Rooms;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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

    private int _acceptedCount;

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
        _listener.Start(1000);

        _logger.LogInformation(
            "Server started. Port={Port}",
            port);

        try
        {
            _ = Task.Run(() => GameLoop(_cts.Token));

            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                TcpClient client =
                    await _listener.AcceptTcpClientAsync();

                stopwatch.Stop();

                int acceptedCount =
                    Interlocked.Increment(ref _acceptedCount);

                _logger.LogInformation(
                    "Client accepted. Count={Count} AcceptTime={AcceptTime}ms RemoteEndPoint={RemoteEndPoint}",
                    acceptedCount,
                    stopwatch.Elapsed.TotalMilliseconds,
                    client.Client.RemoteEndPoint);

                stopwatch.Restart();

                PlayerSession session =
                    new(
                        client,
                        _packetManager,
                        _sessionManager,
                        _gameManager,
                        _loggerFactory);

                _sessionManager.Add(session);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Session setup completed. Count={Count} SetupTime={SetupTime}ms",
                    acceptedCount,
                    stopwatch.Elapsed.TotalMilliseconds);

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