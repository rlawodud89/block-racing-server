using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Lanes;

public readonly struct CellPosition
{
    public readonly int X;
    public readonly int Y;

    public CellPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}