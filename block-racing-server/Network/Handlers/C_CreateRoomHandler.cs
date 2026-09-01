using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_CreateRoomHandler
{
    public static void Handle(PlayerSession session, C_CreateRoomPacket packet)
    {
        _ = session.OnCreatePrivateRoom();
    }
}
