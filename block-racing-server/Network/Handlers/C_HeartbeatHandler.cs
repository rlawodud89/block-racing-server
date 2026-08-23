using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_HeartbeatHandler
{
    public static void Handle(PlayerSession session, C_HeartbeatPacket packet)
    {
        session.UpdateHeartbeat();

        _ = session.SendAsync(new S_HeartbeatPacket());
    }
}
