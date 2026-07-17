using block_racing_common.Game.Enums;

namespace block_racing_common.Game.Snapshots
{
    public class BlockSnapshot
    {
        public int X { get; }

        public int Y { get; }

        public PieceType Type { get; }


        public BlockSnapshot(int x, int y, PieceType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}