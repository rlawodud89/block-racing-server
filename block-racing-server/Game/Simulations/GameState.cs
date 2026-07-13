using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class GameState
{
    public int Tick { get; set; }

    public bool IsGameEnd { get; set; }

    public Dictionary<int, Lane> Lanes { get; } = new();
}