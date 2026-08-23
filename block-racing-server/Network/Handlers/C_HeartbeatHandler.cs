using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_HeartbeatHandler
{
    public static void Handle(PlayerSession session, C_HeartbeatPacket packet)
    {
        Console.WriteLine($"C_Heartbeat 수신 : {session.Id}");

        session.UpdateHeartbeat();
    }
}
