using block_racing_server.Game.Simulations.Lanes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Blocks;

public static class PieceShapeTable
{
    private static readonly Dictionary<(PieceType, Rotation), CellPosition[]> Shapes =
        new()
        {
            // Single
            {
                (PieceType.Single, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0)
                }
            },


            // Line2
            {
                (PieceType.Line2, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0)
                }
            },

            {
                (PieceType.Line2, Rotation.Right),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(0,1)
                }
            },


            // Line3
            {
                (PieceType.Line3, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(2,0)
                }
            },

            {
                (PieceType.Line3, Rotation.Right),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(0,1),
                    new CellPosition(0,2)
                }
            },


            // Square
            {
                (PieceType.Square, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            },


            // L
            {
                (PieceType.LShape, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            }
        };


    public static CellPosition[] GetShape(
        PieceType type,
        Rotation rotation)
    {
        return Shapes[(type, rotation)];
    }
}
