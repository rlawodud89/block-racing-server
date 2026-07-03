using block_racing_server.Network;
using block_racing_server.Network.Packet;

namespace block_racing_server.Network.Handler;

public static class ChatHandler
{
    public static void Handle(PlayerSession session, CChatPacket packet)
    {
        Console.WriteLine(packet.Message);
    }
}