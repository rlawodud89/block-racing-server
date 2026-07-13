using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class Cell
{
    public Block? Block { get; set; }

    public bool IsEmpty => Block == null;
}
