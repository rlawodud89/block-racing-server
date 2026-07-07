using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network;

public enum PacketId : ushort
{
    C_Login = 0,
    C_MatchRequest = 1,
    C_Ready = 2,

    S_Login = 100,
    S_MatchFound = 101,
    S_StartGame = 102,
}
