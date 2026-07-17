
namespace block_racing_common.Network.Packets
{
    public class S_StartGamePacket : IPacket
    {
        public PacketId PacketId => PacketId.S_StartGame;

        public int RoomId { get; set; }

        public void Read(PacketReader reader)
        {
            RoomId = reader.ReadInt32();
        }

        public void Write(PacketWriter writer)
        {
            writer.Write(RoomId);
        }
    }
}