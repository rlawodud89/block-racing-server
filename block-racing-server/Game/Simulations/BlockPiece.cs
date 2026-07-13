using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class BlockPiece
{
    public PieceType Type { get; }

    public Rotation Rotation { get; private set; }

    // 현재 회전 상태의 블록 좌표
    public CellPosition[] Cells { get; private set; }


    public BlockPiece(PieceType type)
    {
        Type = type;
        Rotation = Rotation.Up;

        Cells = PieceShapeTable.GetShape(type, Rotation);
    }


    public void Rotate()
    {
        Rotation = Rotation switch
        {
            Rotation.Up => Rotation.Right,
            Rotation.Right => Rotation.Down,
            Rotation.Down => Rotation.Left,
            _ => Rotation.Up
        };


        Cells = PieceShapeTable.GetShape(Type, Rotation);
    }
}