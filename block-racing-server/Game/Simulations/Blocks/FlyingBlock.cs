using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Blocks;

public class FlyingBlock
{
    public BlockPiece Piece { get; }

    public int X { get; private set; }

    public float Y { get; private set; }

    public int GridY => (int)Math.Floor(Y);


    public float MoveSpeed { get; }

    public int OwnerId { get; }

    public bool IsFinished { get; private set; }



    public FlyingBlock(BlockPiece piece, int x, float y, int ownerId)
    {
        Piece = piece;

        X = x;

        Y = y;

        OwnerId = ownerId;

        // Lane Scroll보다 빨라야 함
        MoveSpeed = 8f;
    }


    public void MoveDown(float deltaTime)
    {
        Y += MoveSpeed * deltaTime;
    }


    public void Finish()
    {
        IsFinished = true;
    }
}
