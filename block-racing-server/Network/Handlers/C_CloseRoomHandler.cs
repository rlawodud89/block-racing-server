using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_CloseRoomHandler
{
    public static void Handle(PlayerSession session, C_CloseRoomPacket packet)
    {
        if (session.Player == null)
            return;

        _ = session.OnLeaveRoom();
    }
}
