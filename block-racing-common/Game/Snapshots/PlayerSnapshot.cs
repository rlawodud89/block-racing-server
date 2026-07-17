using block_racing_common.Game.Enums;

namespace block_racing_common.Game.Snapshots
{
    public class PlayerSnapshot
    {
        public int Id { get; }

        public int CarX { get; }

        public float Distance { get; }

        public float Speed { get; }

        public bool IsStunned { get; }

        public PlayMode Mode { get; }

        public LaneSnapshot Lane { get; }


        public PlayerSnapshot(int id, int carX, float distance,
            float speed, bool isStunned, PlayMode mode,
            LaneSnapshot lane)
        {
            Id = id;

            CarX = carX;

            Distance = distance;

            Speed = speed;

            IsStunned = isStunned;

            Mode = mode;

            Lane = lane;
        }
    }
}
