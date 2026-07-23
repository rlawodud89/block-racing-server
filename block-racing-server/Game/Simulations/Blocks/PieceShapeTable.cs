using block_racing_server.Game.Simulations.Lanes;
using block_racing_common.Game.Enums;

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
            {
                (PieceType.Single, Rotation.Right),
                new[]
                {
                    new CellPosition(0,0)
                }
            },
            {
                (PieceType.Single, Rotation.Down),
                new[]
                {
                    new CellPosition(0,0)
                }
            },
            {
                (PieceType.Single, Rotation.Left),
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
            {
                (PieceType.Line2, Rotation.Down),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0)
                }
            },
            {
                (PieceType.Line2, Rotation.Left),
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
            {
                (PieceType.Line3, Rotation.Down),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(2,0)
                }
            },
            {
                (PieceType.Line3, Rotation.Left),
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
            {
                (PieceType.Square, Rotation.Right),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            },
            {
                (PieceType.Square, Rotation.Down),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            },
            {
                (PieceType.Square, Rotation.Left),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            },

            // LShape
            {
                (PieceType.LShape, Rotation.Up),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(0,1),
                    new CellPosition(1,1)
                }
            },
            {
                (PieceType.LShape, Rotation.Right),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(0,1)
                }
            },
            {
                (PieceType.LShape, Rotation.Down),
                new[]
                {
                    new CellPosition(0,0),
                    new CellPosition(1,0),
                    new CellPosition(1,1)
                }
            },
            {
                (PieceType.LShape, Rotation.Left),
                new[]
                {
                    new CellPosition(1,0),
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