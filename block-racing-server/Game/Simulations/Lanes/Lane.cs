using block_racing_common.Game.Enums;
using block_racing_server.Game.Simulations.Blocks;

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
            int x = block.X + cell.X;
            int y = block.GridY + cell.Y;

            if (y < 0 || y >= Height)
                continue;

            PlaceBlock(
                x,
                y,
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
            int y = Height - 1 + cell.Y;

            PlaceBlock(
                x,
                y,
                new Block(
                    BlockType.Normal,
                    attack.OwnerId));
        }
    }
}