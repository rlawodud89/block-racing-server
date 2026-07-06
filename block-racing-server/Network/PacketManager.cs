using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using block_racing_server.Network.Packets;
using block_racing_server.Network.Handlers;

namespace block_racing_server.Network;

public class PacketManager
{
    private readonly Dictionary<PacketId, Action<PlayerSession, PacketReader>> _handlers
        = new();

    public PacketManager()
    {
        Register<LoginPacket>(PacketId.Login, LoginHandler.Handle);
        Register<MatchRequestPacket>(PacketId.MatchRequest, MatchRequestHandler.Handle);
    }

    public void Register<T>(
        PacketId id,
        Action<PlayerSession, T> handler)
        where T : IPacket, new()
    {
        _handlers[id] = (session, reader) =>
        {
            T packet = new();

            packet.Read(reader);

            handler(session, packet);
        };
    }

    public void Process(PlayerSession session, PacketId id, PacketReader reader)
    {
        if (_handlers.TryGetValue(id, out var handler))
        {
            handler(session, reader);
        }
        else
        {
            Console.WriteLine($"Unknown Packet : {id}");
        }
    }
}