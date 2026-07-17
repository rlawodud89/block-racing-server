using block_racing_common.Network.Packets;

namespace block_racing_server.Network.Handlers;

public static class C_InputHandler
{
    public static void Handle(PlayerSession session, C_InputPacket packet)
    {
        var player = session.Player;

        if (player?.Room == null)
            return;


        player.Room.EnqueueInput(player, packet.InputType);
    }
}
