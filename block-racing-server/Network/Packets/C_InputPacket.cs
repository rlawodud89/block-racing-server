using block_racing_server.Game.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network.Packets;

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