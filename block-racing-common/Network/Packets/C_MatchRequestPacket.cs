
namespace block_racing_common.Network.Packets
{
    public class C_MatchRequestPacket : IPacket
    {
        public PacketId PacketId => PacketId.C_MatchRequest;

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
}