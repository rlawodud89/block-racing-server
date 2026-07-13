using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class FlyingBlock
{
    public BlockPiece Piece { get; }


    // 현재 위치
    public int X { get; private set; }

    public int Y { get; private set; }


    // 이동 속도
    public int MoveSpeed { get; }


    public int OwnerId { get; }


    public bool IsFinished { get; private set; }



    public FlyingBlock(BlockPiece piece, int x, int y, int ownerId)
    {
        Piece = piece;

        X = x;

        Y = y;

        OwnerId = ownerId;


        // 차보다 빨라야 함
        MoveSpeed = 3;
    }



    public void MoveDown()
    {
        Y += MoveSpeed;
    }


    public void Finish()
    {
        IsFinished = true;
    }
}
