using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network;

public enum PacketId : ushort
{
    Login = 100,
    MatchRequest = 101,
}
