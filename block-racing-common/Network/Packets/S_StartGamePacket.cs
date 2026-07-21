
namespace block_racing_common.Network.Packets
{
    public class S_StartGamePacket : IPacket
    {
        public PacketId PacketId => PacketId.S_StartGame;

        public int RoomId { get; set; }
        public float CountdownSeconds { get; set; } = 3f;

        public void Read(PacketReader reader)
        {
            RoomId = reader.ReadInt32();
            CountdownSeconds = reader.ReadFloat();
        }

        public void Write(PacketWriter writer)
        {
            writer.Write(RoomId);
            writer.Write(CountdownSeconds);
        }
    }
}