using block_racing_common.Game.Enums;

namespace block_racing_common.Network.Packets
{
    public class C_InputPacket : IPacket
    {
        public PacketId PacketId => PacketId.C_Input;

        public InputType InputType { get; set; }


        public void Read(PacketReader reader)
        {
            InputType = (InputType)reader.ReadInt32();
        }


        public void Write(PacketWriter writer)
        {
            writer.Write((int)InputType);
        }
    }
}