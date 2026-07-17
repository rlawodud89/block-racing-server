using System.Collections.Generic;

namespace block_racing_common.Game.Snapshots
{
    public class GameStateSnapshot
    {
        public long Tick { get; }

        public IReadOnlyList<PlayerSnapshot> Players { get; }


        public GameStateSnapshot(long tick, IReadOnlyList<PlayerSnapshot> players)
        {
            Tick = tick;
            Players = players;
        }
    }
}