
namespace block_racing_server.Network.Packets;

public class LoginPacket : IPacket
{
    public PacketId PacketId => PacketId.Login;

    public string Nickname { get; set; } = string.Empty;

    public void Read(PacketReader reader)
    {
        Nickname = reader.ReadString();
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(Nickname);
    }
}