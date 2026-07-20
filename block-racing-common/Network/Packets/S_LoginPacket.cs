
namespace block_racing_common.Network.Packets
{
    public class S_LoginPacket : IPacket
    {
        public PacketId PacketId => PacketId.S_Login;

        public int PlayerId { get; set; }

        public string Nickname { get; set; }

        public void Read(PacketReader reader)
        {
            PlayerId = reader.ReadInt32();
            Nickname = reader.ReadString();
        }

        public void Write(PacketWriter writer)
        {
            writer.Write(PlayerId);
            writer.Write(Nickname);
        }
    }
}