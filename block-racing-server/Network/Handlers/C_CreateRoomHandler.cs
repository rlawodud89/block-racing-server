using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_CreateRoomHandler
{
    public static void Handle(PlayerSession session, C_CreateRoomPacket packet)
    {
        if (session.Player == null)
        {
            Console.WriteLine($"Player session {session.Id} has no player associated.");
            return;
        }

        session.OnCreateRoom();
    }
}
