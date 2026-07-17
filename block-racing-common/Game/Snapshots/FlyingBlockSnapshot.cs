using block_racing_common.Game.Enums;

namespace block_racing_common.Game.Snapshots
{
    public class FlyingBlockSnapshot
    {
        public int OwnerId { get; }

        public int X { get; }

        public int Y { get; }

        public PieceType Type { get; }

        public Rotation Rotation { get; }


        public FlyingBlockSnapshot(int ownerId, int x, int y,
            PieceType type, Rotation rotation)
        {
            OwnerId = ownerId;
            X = x;
            Y = y;
            Type = type;
            Rotation = rotation;
        }
    }
}
