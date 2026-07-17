using block_racing_server.Game.Simulations.Lanes;
using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Simulations.Blocks;

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

        Cells = PieceShapeTable.GetShape(type, Rotation).ToArray();
    }

    public int Width
    {
        get
        {
            return Cells.Max(cell => cell.X) + 1;
        }
    }

    public int Height
    {
        get
        {
            return Cells.Max(cell => cell.Y) + 1;
        }
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