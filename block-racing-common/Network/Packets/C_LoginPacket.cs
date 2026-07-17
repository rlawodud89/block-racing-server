
namespace block_racing_common.Network.Packets
{
    public class C_LoginPacket : IPacket
    {
        public PacketId PacketId => PacketId.C_Login;

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
}