using block_racing_common.Network;
using block_racing_server.Game.Players;
using System.Net.Sockets;

using block_racing_server.Game;
using block_racing_common.Network.Packets;

namespace block_racing_server.Network;

public class PlayerSession
{
    public int Id { get; set; }
    public Player? Player { get; private set; }

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    private readonly PacketManager _packetManager;
    private readonly SessionManager _sessionManager;
    private readonly ReceiveBuffer _receiveBuffer;

    private readonly GameManager _gameManager;

    public PlayerSession(TcpClient client, PacketManager packetManager, SessionManager sessionManager, GameManager gameManager)
    {
        _client = client;
        _stream = client.GetStream();

        _packetManager = packetManager;
        _sessionManager = sessionManager;
        _receiveBuffer = new ReceiveBuffer();
        _gameManager = gameManager;
    }


    public async Task StartAsync()
    {
        Console.WriteLine($"PlayerSession 시작 : {_client.Client.RemoteEndPoint}");

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
                    break;

                _receiveBuffer.Append(buffer, received);

                while (_receiveBuffer.TryReadPacket(out byte[] packet))
                {
                    ProcessPacket(packet);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            await Disconnect();
        }
    }

    private void ProcessPacket(byte[] packet)
    {
        PacketReader reader = new(packet);

        // Length skip
        ushort length = reader.ReadUInt16();

        ushort packetId = reader.ReadUInt16();

        PacketId id = (PacketId)packetId;

        _packetManager.Process(this, id, reader);
    }

    public async Task SendAsync(byte[] data)
    {
        await _stream.WriteAsync(data);
    }

    public async Task SendAsync(IPacket packet)
    {
        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);

        await _stream.WriteAsync(writer.ToArray());
    }

    private async Task Disconnect()
    {
        if (Player != null)
            await _gameManager.UnregisterPlayer(Player);

        _sessionManager.Remove(this);

        _stream.Close();
        _client.Close();
    }


    public async Task OnLogin(string nickname)
    {
        Player = new Player(this, Id, nickname);

        _gameManager.RegisterPlayer(Player);

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
            return;

        if (isMatch)
        {
            _gameManager.EnqueueMatch(Player);
        }
        else
        {
            _gameManager.CancelMatch(Player);
        }
    }
}


