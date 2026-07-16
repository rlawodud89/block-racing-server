using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Snapshots;

public class GameStateSnapshot
{
    public long Tick { get; }

    public List<PlayerSnapshot> Players { get; }


    public GameStateSnapshot(GameState state)
    {
        Tick = state.Tick;

        Players = state.Players.Values
            .Select(p => new PlayerSnapshot(p))
            .ToList();
    }

    public GameStateSnapshot(long tick, List<PlayerSnapshot> players)
    {
        Tick = tick;
        Players = players;
    }
}
