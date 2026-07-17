using block_racing_common.Game.Snapshots;
using block_racing_server.Game.Simulations.Blocks;

namespace block_racing_server.Game.Snapshots;

public static class BlockSnapshotBuilder
{
    public static BlockSnapshot Create(FlyingBlock block)
    {
        return new BlockSnapshot(
            block.X,
            block.GridY,
            block.Piece.Type
        );
    }
}
