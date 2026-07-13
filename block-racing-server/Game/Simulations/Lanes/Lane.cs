using block_racing_server.Game.Rules;
using block_racing_server.Game.Simulations.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Lanes;

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

    public void RemoveLine(int removeY)
    {
        for (int x = 0; x < Width; x++)
        {
            Grid[removeY, x].Block = null;
        }
    }

    public void SettleBlock(FlyingBlock block)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;
            int y = block.Y + cell.Y;


            if (x < 0 || x >= Width)
                continue;


            if (y < 0 || y >= Height)
                continue;


            Grid[y, x].Block =
                new Block(
                    BlockType.Normal,
                    block.OwnerId
                );
        }
    }
}