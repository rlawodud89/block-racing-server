using block_racing_server.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network.Handlers;

public static class C_ReadyHandler
{
    public static void Handle(PlayerSession session, C_ReadyPacket packet)
    {
        session.Player?.Room?.SetReady(session.Player);
    }
}
