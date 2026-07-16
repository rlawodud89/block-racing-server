using block_racing_server.Game.Simulations.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Snapshots;

public class BlockSnapshot
{
    public int X { get; }

    public int Y { get; }

    public int Type { get; }


    public BlockSnapshot(FlyingBlock block)
    {
        X = block.X;

        Y = block.GridY;

        Type = (int)block.Piece.Type;
    }
}