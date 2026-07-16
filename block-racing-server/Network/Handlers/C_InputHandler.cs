using block_racing_server.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
