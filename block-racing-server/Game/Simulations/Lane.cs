using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class Lane
{
    public const int Width = 5;
    public const int Height = 20;

    public Cell[,] Grid { get; }

    public Queue<AttackPiece> PendingAttacks { get; }

    public List<FlyingBlock> FlyingBlocks { get; }


    public Lane()
    {
        Grid = CreateGrid();

        PendingAttacks = new();
        FlyingBlocks = new();
    }


    private Cell[,] CreateGrid()
    {
        Cell[,] grid = new Cell[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                grid[y, x] = new Cell();
            }
        }

        return grid;
    }
}