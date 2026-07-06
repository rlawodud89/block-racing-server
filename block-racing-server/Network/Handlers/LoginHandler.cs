using block_racing_server.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class LoginHandler
{
    public static void Handle(PlayerSession session, LoginPacket packet)
    {
        Console.WriteLine($"Player {packet.Nickname} has logged in.");
        session.OnLogin(packet.Nickname);
    }
}
