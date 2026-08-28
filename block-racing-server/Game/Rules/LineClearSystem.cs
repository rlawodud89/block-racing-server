using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;
using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Rules;

public class LineClearSystem
{
    public int TryClearLines(Lane lane)
    {
        List<int> completedLines =
            FindCompletedLines(lane);

        if (completedLines.Count == 0)
            return 0;

        foreach (int line in completedLines)
        {
            lane.ClearLine(line);
        }

        return completedLines.Count;
    }

    public int TryClearLines(
        Lane lane,
        IReadOnlyList<FlyingBlockPosition> positions)
    {
        List<int> completedLines =
            FindCompletedLines(
                lane,
                positions);

        if (completedLines.Count == 0)
            return 0;

        foreach (FlyingBlockPosition position in positions)
        {
            FlyingBlock block = position.Block;

            if (block.IsFinished)
                continue;

            // 이 FlyingBlock이 Line Clear에 기여하지 않았다면
            // 계속 Flying 상태로 유지
            if (!ContributesToLine(
                    block,
                    position.GridY,
                    completedLines))
            {
                continue;
            }

            // Line Clear를 발생시킨 FlyingBlock은
            // 현재 위치에서 Flying을 멈추고
            // 제거되지 않은 Cell만 Grid에 정착
            SettleRemainingCells(
                lane,
                position,
                completedLines);

            block.Finish();
        }

        // 기존 Grid의 완성 Line 제거
        foreach (int line in completedLines)
        {
            lane.ClearLine(line);
        }

        return completedLines.Count;
    }

    private void SettleRemainingCells(
        Lane lane,
        FlyingBlockPosition position,
        IReadOnlyList<int> completedLines)
    {
        FlyingBlock block = position.Block;

        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;
            int y = position.GridY + cell.Y;

            if (x < 0 || x >= Lane.Width)
                continue;

            if (y < 0 || y >= Lane.Height)
                continue;

            // Line Clear된 Cell은 Grid에 정착시키지 않음
            if (completedLines.Contains(y))
                continue;

            lane.PlaceBlock(
                x,
                y,
                new Block(
                    BlockType.Normal,
                    block.OwnerId));
        }
    }

    private List<int> FindCompletedLines(Lane lane)
    {
        List<int> lines = new();

        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;

            for (int x = 0; x < Lane.Width; x++)
            {
                if (!lane.HasBlock(x, y))
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

    private List<int> FindCompletedLines(
        Lane lane,
        IReadOnlyList<FlyingBlockPosition> positions)
    {
        List<int> lines = new();

        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;

            for (int x = 0; x < Lane.Width; x++)
            {
                if (!IsOccupied(
                        lane,
                        positions,
                        x,
                        y))
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

    private bool IsOccupied(
        Lane lane,
        IReadOnlyList<FlyingBlockPosition> positions,
        int x,
        int y)
    {
        if (lane.HasBlock(x, y))
            return true;

        foreach (FlyingBlockPosition position in positions)
        {
            FlyingBlock block = position.Block;

            if (block.IsFinished)
                continue;

            foreach (var cell in block.Piece.Cells)
            {
                int blockX = block.X + cell.X;
                int blockY = position.GridY + cell.Y;

                if (blockX == x &&
                    blockY == y)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ContributesToLine(
        FlyingBlock block,
        int gridY,
        IReadOnlyList<int> completedLines)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int y = gridY + cell.Y;

            if (completedLines.Contains(y))
                return true;
        }

        return false;
    }
}