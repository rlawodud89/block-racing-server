using block_racing_common.Network.Packets;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rooms;

namespace block_racing_server.Network.Handlers;

public static class C_RematchRequestHandler
{
    public static void Handle(PlayerSession session, C_RematchReqeustPacket packet)
    {
        Player? player = session.Player;

        if (player == null)
            return;

        Room? room = player.Room;

        if (room == null)
            return;

        room.RequestRematch(player);
    }
}
