using block_racing_server.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class CLoginHandler
{
    public static void Handle(PlayerSession session, C_LoginPacket packet)
    {
        Console.WriteLine($"Player {packet.Nickname} has logged in.");
        session.OnLogin(packet.Nickname);
    }
}
