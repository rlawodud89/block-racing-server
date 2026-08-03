using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;

public class LineClearSystem
{
    public int ClearLines(Lane lane)
    {
        List<int> completedLines = FindCompletedLines(lane);


        if (completedLines.Count == 0)
            return 0;


        foreach (int line in completedLines)
        {
            lane.ClearLine(line);
        }


        return completedLines.Count;
    }



    private List<int> FindCompletedLines(Lane lane)
    {
        List<int> lines = new();

        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;

            for (int x = 0; x < Lane.Width; x++)
            {
                if (lane.Grid[y, x].Block == null)
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
            {
                lines.Add(y);
            }
        }

        return lines;
    }

    public void SettleBlocksCompletingLines(Lane lane)
    {
        List<FlyingBlock> activeBlocks =
            lane.FlyingBlocks
                .Where(b => !b.IsFinished)
                .ToList();

        if (activeBlocks.Count == 0)
            return;

        List<int> completedLines =
            FindCompletedLines(lane, activeBlocks);

        if (completedLines.Count == 0)
            return;

        foreach (FlyingBlock block in activeBlocks)
        {
            if (!ContributesToLine(block, completedLines))
                continue;

            lane.SettleBlock(block);
            block.Finish();
        }
    }

    private List<int> FindCompletedLines(Lane lane, IReadOnlyList<FlyingBlock> flyingBlocks)
    {
        List<int> lines = new();

        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;

            for (int x = 0; x < Lane.Width; x++)
            {
                if (!IsOccupied(lane, flyingBlocks, x, y))
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
                lines.Add(y);
        }

        return lines;
    }

    private bool IsOccupied(Lane lane, IReadOnlyList<FlyingBlock> flyingBlocks, int x, int y)
    {
        if (lane.HasBlock(x, y))
            return true;

        foreach (FlyingBlock block in flyingBlocks)
        {
            if (FlyingBlockOccupies(block, x, y))
                return true;
        }

        return false;
    }

    private bool FlyingBlockOccupies(FlyingBlock block, int x, int y)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int blockX = block.X + cell.X;
            int blockY = block.GridY + cell.Y;

            if (blockX == x && blockY == y)
                return true;
        }

        return false;
    }

    private bool ContributesToLine(FlyingBlock block, IReadOnlyList<int> completedLines)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int y = block.GridY + cell.Y;

            if (completedLines.Contains(y))
                return true;
        }

        return false;
    }
}
