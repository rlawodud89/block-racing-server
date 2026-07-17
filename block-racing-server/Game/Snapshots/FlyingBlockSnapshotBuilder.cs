using block_racing_common.Game.Snapshots;
using block_racing_server.Game.Simulations.Blocks;

namespace block_racing_server.Game.Snapshots;

public static class FlyingBlockSnapshotBuilder
{
    public static FlyingBlockSnapshot Create(FlyingBlock block)
    {
        return new FlyingBlockSnapshot(
            block.OwnerId,
            block.X,
            block.GridY,
            block.Piece.Type,
            block.Piece.Rotation
        );
    }
}
