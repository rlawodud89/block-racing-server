using block_racing_server.Game.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Rules;

public class GameEndResult
{
    public Player? Winner { get; }

    public Player? Loser { get; }

    public GameEndResult(Player? winner, Player? loser)
    {
        Winner = winner;
        Loser = loser;
    }
}
