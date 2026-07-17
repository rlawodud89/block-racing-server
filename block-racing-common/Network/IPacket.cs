
namespace block_racing_common.Network
{
    public interface IPacket
    {
        PacketId PacketId { get; }

        void Read(PacketReader reader);

        void Write(PacketWriter writer);
    }
}
