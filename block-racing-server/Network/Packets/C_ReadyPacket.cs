
namespace block_racing_server.Network.Packets;

public class C_ReadyPacket : IPacket
{
    public PacketId PacketId => PacketId.C_Ready;

    public void Read(PacketReader reader) { }

    public void Write(PacketWriter writer) { }
}
