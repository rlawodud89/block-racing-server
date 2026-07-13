using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Blocks;

public class AttackPiece
{
    public BlockPiece Piece { get; }

    public int SpawnX { get; }

    public long SpawnTick { get; }

    public int OwnerId { get; }


    public AttackPiece(BlockPiece piece, int spawnX, long spawnTick, int ownerId)
    {
        Piece = piece;
        SpawnX = spawnX;
        SpawnTick = spawnTick;
        OwnerId = ownerId;
    }
}
