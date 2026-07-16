using block_racing_server.Game.Simulations.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Snapshots;

public class FlyingBlockSnapshot
{
    public int OwnerId { get; }

    public int X { get; }

    public int Y { get; }

    public PieceType Type { get; }

    public Rotation Rotation { get; }


    public FlyingBlockSnapshot(FlyingBlock block)
    {
        OwnerId = block.OwnerId;

        X = block.X;

        Y = block.GridY;

        Type = block.Piece.Type;

        Rotation = block.Piece.Rotation;
    }

    public FlyingBlockSnapshot(int ownerId, int x, int y,
        PieceType type, Rotation rotation)
    {
        OwnerId = ownerId;
        X = x;
        Y = y;
        Type = type;
        Rotation = rotation;
    }
}
