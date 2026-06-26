using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using block_racing_server.Network.Packet;
using block_racing_server.Network.Packet.IPackets;

namespace block_racing_server.Network.Handler;

public class PacketHandler
{
    public void Handle(PlayerSession session, IPacket packet)
    {
        Console.WriteLine($"Packet Received : {packet.PacketId} from Session : {session.Id}");

        switch (packet)
        {
            case CChatPacket chat:
                HandleChat(session, chat);
                break;

            default:
                Console.WriteLine($"Unknown Packet : {packet.PacketId}");
                break;
        }
    }

    private void HandleChat(PlayerSession session, CChatPacket packet)
    {
        Console.WriteLine($"{session.Id} : {packet.Message}");
    }
}
