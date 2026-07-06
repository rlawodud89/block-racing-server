using block_racing_server.Game;
using block_racing_server.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network.Handlers;

public static class MatchRequestHandler
{
    public static void Handle(PlayerSession session, MatchRequestPacket packet)
    {
        if(session.Player == null)
        {
            Console.WriteLine($"Player session {session.Id} has no player associated.");
            return;
        }

        session.OnMatchRequest(packet.IsMatch);
    }
}
