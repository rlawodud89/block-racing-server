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

    public float ScrollTimer { get; set; }


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

    public bool HasBlock(int x, int y)
    {
        if (x < 0 || x >= Width)
            return false;

        if (y < 0 || y >= Height)
            return false;

        return Grid[y, x].Block != null;
    }

    public void PlaceBlock(int x, int y, Block block)
    {
        if (x < 0 || x >= Width)
            return;

        if (y < 0 || y >= Height)
            return;

        Grid[y, x].Block = block;
    }

    public void RemoveBlock(int x, int y)
    {
        if (x < 0 || x >= Width)
            return;

        if (y < 0 || y >= Height)
            return;

        Grid[y, x].Block = null;
    }

    public void ClearLine(int removeY)
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
            PlaceBlock(
                block.X + cell.X,
                block.GridY + cell.Y,
                new Block(
                    BlockType.Normal,
                    block.OwnerId));
        }
    }


    public void SpawnAttack(AttackPiece attack)
    {
        foreach (var cell in attack.Piece.Cells)
        {
            int x = attack.SpawnX + cell.X;
            int y = Height - 1 - attack.Piece.Height;

            PlaceBlock(
                x,
                y,
                new Block(
                    BlockType.Normal,
                    attack.OwnerId));
        }
    }
}