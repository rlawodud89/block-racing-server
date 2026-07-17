
namespace block_racing_common.Network
{
    public struct PacketHeader
    {
        public ushort Length;
        public ushort PacketId;

        public const int Size = 4;
    }
}