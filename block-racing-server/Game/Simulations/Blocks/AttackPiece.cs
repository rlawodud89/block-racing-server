using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Blocks;

public class AttackPiece
{
    public BlockPiece Piece { get; }


    public long SpawnTick { get; }


    public AttackPiece(BlockPiece piece, long spawnTick)
    {
        Piece = piece;
        SpawnTick = spawnTick;
    }
}
