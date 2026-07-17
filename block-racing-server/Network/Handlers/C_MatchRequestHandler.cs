using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_MatchRequestHandler
{
    public static void Handle(PlayerSession session, C_MatchRequestPacket packet)
    {
        if(session.Player == null)
        {
            Console.WriteLine($"Player session {session.Id} has no player associated.");
            return;
        }

        session.OnMatchRequest(packet.IsMatch);
    }
}
