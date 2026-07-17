using System.Collections.Generic;

namespace block_racing_common.Game.Snapshots
{
    public class LaneSnapshot
    {
        public byte[] Blocks { get; }

        public IReadOnlyList<FlyingBlockSnapshot> FlyingBlocks { get; }


        public LaneSnapshot(
            byte[] blocks,
            IReadOnlyList<FlyingBlockSnapshot> flyingBlocks)
        {
            Blocks = blocks;
            FlyingBlocks = flyingBlocks;
        }
    }
}