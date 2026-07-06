
namespace block_racing_server.Network.Packets;

public class MatchRequestPacket : IPacket
{
    public PacketId PacketId => PacketId.Login;

    public bool IsMatch { get; set; } = true; // 매치 시작하면 true, 매치 취소하면 false

    public void Read(PacketReader reader)
    {
        IsMatch = reader.ReadBool();
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(IsMatch);
    }
}