using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_LeaveRoomHandler
{
    public static void Handle(PlayerSession session, C_LeaveRoomPacket packet)
    {
        if (session.Player == null)
            return;

        _ = session.OnLeaveRoom();
    }
}
