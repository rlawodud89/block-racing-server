using block_racing_common.Network;
using block_racing_common.Network.Packets;
using block_racing_server.Game;
using block_racing_server.Game.Players;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using System.IO;

namespace block_racing_server.Network;

public class PlayerSession
{
    public long Id { get; set; }
    public Player? Player { get; private set; }

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    private readonly PacketManager _packetManager;
    private readonly SessionManager _sessionManager;
    private readonly ReceiveBuffer _receiveBuffer;

    private readonly GameManager _gameManager;

    private readonly ILogger<PlayerSession> _logger;

    private readonly Channel<byte[]> _sendQueue;
    private Task? _sendLoopTask;

    private DateTime _lastHeartbeatTime;
    private DateTime _lastHeartbeatSendTime;

    private const int HeartbeatInterval = 1000;
    private const int HeartbeatTimeout = 5000;

    private int _isDisconnected;

    public bool IsDisconnected =>
        Volatile.Read(ref _isDisconnected) == 1;

    public PlayerSession(
        TcpClient client,
        PacketManager packetManager,
        SessionManager sessionManager,
        GameManager gameManager,
        ILoggerFactory loggerFactory)
    {
        _client = client;
        _stream = client.GetStream();

        _packetManager = packetManager;
        _sessionManager = sessionManager;
        _receiveBuffer = new ReceiveBuffer();
        _gameManager = gameManager;

        _logger = loggerFactory.CreateLogger<PlayerSession>();

        _sendQueue = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _lastHeartbeatTime = DateTime.UtcNow;
        _lastHeartbeatSendTime = DateTime.UtcNow;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation(
            "Player session started. SessionId={SessionId} RemoteEndPoint={RemoteEndPoint}",
            Id,
            _client.Client.RemoteEndPoint);

        _sendLoopTask = SendLoopAsync();

        await ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int received =
                    await _stream.ReadAsync(buffer);

                if (received == 0)
                {
                    _logger.LogInformation(
                        "Client closed connection. SessionId={SessionId}",
                        Id);

                    break;
                }

                _receiveBuffer.Append(buffer, received);

                while (_receiveBuffer.TryReadPacket(out byte[] packet))
                {
                    if (!ProcessPacket(packet))
                        return;
                }
            }
        }
        catch (IOException ex)
        {
            if (Volatile.Read(ref _isDisconnected) == 1)
                return;

            _logger.LogError(
                ex,
                "Error occurred while receiving data. SessionId={SessionId}",
                Id);
        }
        catch (ObjectDisposedException)
        {
            // 정상적인 연결 종료
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while receiving data. SessionId={SessionId}",
                Id);
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    private bool ProcessPacket(byte[] packet)
    {
        try
        {
            PacketReader reader = new(packet);

            // Length skip
            ushort length = reader.ReadUInt16();

            if (length != packet.Length)
                throw new InvalidDataException(
                    $"Packet length mismatch. Header={length}, Actual={packet.Length}");

            ushort packetId = reader.ReadUInt16();

            PacketId id = (PacketId)packetId;

            _packetManager.Process(this, id, reader);

            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while processing packet. " +
                "SessionId={SessionId} RemoteEndPoint={RemoteEndPoint}",
                Id,
                _client.Client.RemoteEndPoint);

            return false;
        }
    }

    /// <summary>
    /// 패킷을 송신 Queue에 등록한다.
    /// 실제 Socket 전송은 SendLoopAsync()에서 수행한다.
    /// </summary>
    public Task SendAsync(byte[] data)
    {
        if (IsDisconnected)
            return Task.CompletedTask;

        try
        {
            if (!_sendQueue.Writer.TryWrite(data))
            {
                _logger.LogWarning(
                    "Failed to enqueue data because send queue is closed. SessionId={SessionId}",
                    Id);

                _ = DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to enqueue data for sending. SessionId={SessionId}",
                Id);

            _ = DisconnectAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// IPacket을 직렬화한 뒤 송신 Queue에 등록한다.
    /// </summary>
    public Task SendAsync(IPacket packet)
    {
        try
        {
            PacketWriter writer = new((ushort)packet.PacketId);
            packet.Write(writer);

            byte[] data = writer.ToArray();

            return SendAsync(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to prepare packet for sending. SessionId={SessionId}",
                Id);

            _ = DisconnectAsync();

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 송신 Queue에서 패킷을 꺼내 실제 Socket으로 전송한다.
    /// 이 메서드만 Socket WriteAsync()를 수행한다.
    /// </summary>
    private async Task SendLoopAsync()
    {
        try
        {
            await foreach (byte[] data in _sendQueue.Reader.ReadAllAsync())
            {
                if (IsDisconnected)
                    break;

                var start = Stopwatch.GetTimestamp();

                try
                {
                    await _stream.WriteAsync(data);

                    var elapsed =
                        Stopwatch.GetElapsedTime(start);

                    if (elapsed > TimeSpan.FromMilliseconds(10))
                    {
                        _logger.LogWarning(
                            "Socket Send slow. " +
                            "SessionId={SessionId} " +
                            "Bytes={Bytes} " +
                            "ElapsedMs={ElapsedMs:F2}",
                            Id,
                            data.Length,
                            elapsed.TotalMilliseconds);
                    }
                }
                catch (IOException)
                {
                    if (!IsDisconnected)
                    {
                        _logger.LogWarning(
                            "Failed to send data because connection was lost. SessionId={SessionId}",
                            Id);

                        _ = DisconnectAsync();
                    }

                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 송신 Loop 종료
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred in send loop. SessionId={SessionId}",
                Id);

            _ = DisconnectAsync();
        }
    }

    public async Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _isDisconnected, 1) == 1)
            return;

        // 더 이상 새로운 송신 요청을 받지 않는다.
        _sendQueue.Writer.TryComplete();

        _logger.LogInformation(
            "Disconnecting session. SessionId={SessionId} PlayerId={PlayerId}",
            Id,
            Player?.Id);

        try
        {
            if (Player != null)
                await _gameManager.UnregisterPlayer(Player);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while unregistering player. SessionId={SessionId} PlayerId={PlayerId}",
                Id,
                Player?.Id);
        }
        finally
        {
            _sessionManager.Remove(this);

            try
            {
                _stream.Close();
                _client.Close();
            }
            catch
            {
                // 이미 종료된 연결
            }

            _logger.LogInformation(
                "Session disconnected. SessionId={SessionId}",
                Id);
        }
    }

    public void UpdateHeartbeat()
    {
        _lastHeartbeatTime = DateTime.UtcNow;
    }

    public bool IsHeartbeatTimeout()
    {
        return DateTime.UtcNow - _lastHeartbeatTime
            > TimeSpan.FromMilliseconds(HeartbeatTimeout);
    }

    public bool ShouldSendHeartbeat()
    {
        return DateTime.UtcNow - _lastHeartbeatSendTime
            > TimeSpan.FromMilliseconds(HeartbeatInterval);
    }

    public Task SendHeartbeatAsync()
    {
        _lastHeartbeatSendTime = DateTime.UtcNow;

        return SendAsync(new S_HeartbeatPacket());
    }

    public async Task OnLogin(string nickname)
    {
        Player = new Player(this, Id, nickname);

        _gameManager.RegisterPlayer(Player);

        _logger.LogInformation(
            "Player logged in. SessionId={SessionId} PlayerId={PlayerId} Nickname={Nickname}",
            Id,
            Player.Id,
            nickname);

        S_LoginPacket responsePacket = new()
        {
            PlayerId = Id,
            Nickname = nickname
        };

        await SendAsync(responsePacket);
    }

    public void OnMatchRequest(bool isMatch)
    {
        if (Player == null)
        {
            _logger.LogWarning(
                "Match request ignored because player is not logged in. SessionId={SessionId}",
                Id);

            return;
        }

        if (isMatch)
        {
            _gameManager.EnqueueMatch(Player);
        }
        else
        {
            _gameManager.CancelMatch(Player);
        }
    }

    public async Task OnCreatePrivateRoom()
    {
        if (Player == null)
        {
            _logger.LogWarning(
                "Create room request ignored because player is not logged in. SessionId={SessionId}",
                Id);

            return;
        }

        _logger.LogInformation(
            "Create private room request received. PlayerId={PlayerId}",
            Player.Id);

        await _gameManager.CreatePrivateRoom(Player);
    }

    public async Task OnJoinRoom(string roomCode)
    {
        if (Player == null)
        {
            _logger.LogWarning(
                "Join room request ignored because player is not logged in. SessionId={SessionId}",
                Id);

            return;
        }

        _logger.LogInformation(
            "Join room request received. PlayerId={PlayerId} RoomCode={RoomCode}",
            Player.Id,
            roomCode);

        await _gameManager.JoinRoom(Player, roomCode);
    }

    public async Task OnCloseRoom()
    {
        if (Player == null)
        {
            _logger.LogWarning(
                "Leave room request ignored because player is not logged in. SessionId={SessionId}",
                Id);

            return;
        }

        _logger.LogInformation(
            "Leave room request received. PlayerId={PlayerId}",
            Player.Id);

        await _gameManager.LeaveRoom(Player);
    }
}