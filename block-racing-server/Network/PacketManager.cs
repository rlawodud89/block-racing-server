using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using block_racing_server.Network.Packet;
using block_racing_server.Network.Packet.IPackets;
using block_racing_server.Network.Handler;

namespace block_racing_server.Network;

public class PacketManager
{
    private readonly Dictionary<PacketId, Func<IPacket>> _factory = new();

    private readonly PacketHandler _handler = new();

    public PacketManager()
    {
        Register(PacketId.C_Chat, () => new CChatPacket());
    }

    public void Register(PacketId id, Func<IPacket> creator)
    {
        _factory[id] = creator;
    }

    public void Process(PlayerSession session, PacketId id, PacketReader reader)
    {
        if (!_factory.TryGetValue(id, out var creator))
            return;

        IPacket packet = creator();

        packet.Read(reader);

        _handler.Handle(session, packet);
    }
}