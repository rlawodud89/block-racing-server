using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_ReadyHandler
{
    public static void Handle(PlayerSession session, C_ReadyPacket packet)
    {
        session.Player?.Room?.SetReady(session.Player);
    }
}
