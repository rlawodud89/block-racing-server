
namespace block_racing_server.Network.Packet;

public struct PacketHeader
{
    public ushort Length;
    public ushort PacketId;

    public const int Size = 4;
}