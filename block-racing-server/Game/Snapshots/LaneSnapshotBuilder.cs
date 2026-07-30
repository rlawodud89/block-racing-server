using block_racing_common.Game.Snapshots;
using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Snapshots;

public static class LaneSnapshotBuilder
{
    public static LaneSnapshot Create(Lane lane)
    {
        return new LaneSnapshot(
            CreateBlocks(lane),
            CreateFlyingBlocks(lane)
        );
    }


    private static byte[] CreateBlocks(Lane lane)
    {
        byte[] blocks = new byte[
            Lane.Width * Lane.Height
        ];

        for (int y = 0; y < Lane.Height; y++)
        {
            for (int x = 0; x < Lane.Width; x++)
            {
                var block = lane.Grid[y, x].Block;

                int index = y * Lane.Width + x;

                blocks[index] =
                    block == null ? (byte)0 : (byte)1;
            }
        }

        return blocks;
    }


    private static List<FlyingBlockSnapshot> CreateFlyingBlocks(Lane lane)
    {
        foreach (var block in lane.FlyingBlocks)
        {
            Console.WriteLine(
                $"Snapshot FlyingBlock X:{block.X}, GridY:{block.GridY}"
            );
        }

        return lane.FlyingBlocks
            .Select(FlyingBlockSnapshotBuilder.Create)
            .ToList();
    }
}