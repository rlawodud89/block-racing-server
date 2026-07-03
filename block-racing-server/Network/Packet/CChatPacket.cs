using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network.Packet;

public class CChatPacket : IPacket
{
    public PacketId PacketId => PacketId.C_Chat;

    public string Message = "";

    public void Read(PacketReader reader)
    {
        Message = reader.ReadString();
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(Message);
    }
}
