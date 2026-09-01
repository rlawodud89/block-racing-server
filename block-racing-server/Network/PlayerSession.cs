using block_racing_common.Network;
using block_racing_common.Network.Packets;
using block_racing_server.Game;
using block_racing_server.Game.Players;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Net.Sockets;

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

    private DateTime _lastHeartbeatTime;
    private DateTime _lastHeartbeatSendTime;

    private const int HeartbeatInterval = 1000;
    private const int HeartbeatTimeout = 5000;

    private int _isDisconnected;

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

        _lastHeartbeatTime = DateTime.UtcNow;
        _lastHeartbeatSendTime = DateTime.UtcNow;
    }


    public async Task StartAsync()
    {
        _logger.LogInformation(
            "Player session started. SessionId={SessionId} RemoteEndPoint={RemoteEndPoint}",
            Id,
            _client.Client.RemoteEndPoint);

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
                    ProcessPacket(packet);
                }
            }
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

    private void ProcessPacket(byte[] packet)
    {
        try
        {
            PacketReader reader = new(packet);

            // Length skip
            ushort length = reader.ReadUInt16();

            ushort packetId = reader.ReadUInt16();

            PacketId id = (PacketId)packetId;

            _packetManager.Process(this, id, reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while processing packet. SessionId={SessionId}",
                Id);
        }
    }

    public async Task SendAsync(byte[] data)
    {
        try
        {
            await _stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while sending data. SessionId={SessionId}",
                Id);

            throw;
        }
    }

    public async Task SendAsync(IPacket packet)
    {
        try
        {
            PacketWriter writer = new((ushort)packet.PacketId);
            packet.Write(writer);

            await _stream.WriteAsync(writer.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while sending packet. SessionId={SessionId} PacketId={PacketId}",
                Id,
                packet.PacketId);

            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _isDisconnected, 1) == 1)
            return;

        _logger.LogInformation(
            "Disconnecting session. SessionId={SessionId} PlayerId={PlayerId}",
            Id,
            Player?.Id);

        try
        {
            if (Player != null)
                await _gameManager.UnregisterPlayer(Player);

            _sessionManager.Remove(this);

            _stream.Close();
            _client.Close();

            _logger.LogInformation(
                "Session disconnected. SessionId={SessionId}",
                Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while disconnecting session. SessionId={SessionId}",
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

    public async Task SendHeartbeatAsync()
    {
        _lastHeartbeatSendTime = DateTime.UtcNow;

        await SendAsync(new S_HeartbeatPacket());
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


