using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_LoginHandler
{
    public static void Handle(PlayerSession session, C_LoginPacket packet)
    {
        Console.WriteLine($"Player {packet.Nickname} has logged in.");
        _ = session.OnLogin(packet.Nickname);
    }
}
