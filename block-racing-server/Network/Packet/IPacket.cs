using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network.Packet;

public interface IPacket
{
    PacketId PacketId { get; }

    void Read(PacketReader reader);

    void Write(PacketWriter writer);
}
