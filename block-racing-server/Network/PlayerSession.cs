using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network;

public class PlayerSession
{
    public int Id { get; set; }

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    private readonly PacketManager _packetManager;
    private readonly SessionManager _sessionManager;
    private readonly ReceiveBuffer _receiveBuffer;
    
    public PlayerSession(TcpClient client, PacketManager packetManager, SessionManager sessionManager)
    {
        _client = client;
        _stream = client.GetStream();

        _packetManager = packetManager;
        _sessionManager = sessionManager;
        _receiveBuffer = new ReceiveBuffer();
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
            Disconnect();
        }
    }

    private void ProcessPacket(byte[] packet)
    {
        PacketReader reader = new(packet);

        // ❗ Length skip
        ushort length = reader.ReadUInt16();

        ushort packetId = reader.ReadUInt16();

        PacketId id = (PacketId)packetId;

        _packetManager.Process(this, id, reader);
    }

    public async Task SendAsync(byte[] data)
    {
        await _stream.WriteAsync(data);
    }

    private void Disconnect()
    {
        _sessionManager.Remove(this);

        _stream.Close();
        _client.Close();
    }
}


