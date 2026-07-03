using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network;

public enum PacketId : ushort
{
    C_Chat = 1,

    S_Chat = 100,
}
