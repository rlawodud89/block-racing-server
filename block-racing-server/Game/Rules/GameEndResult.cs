using block_racing_common.Game.Enums;
using block_racing_server.Game.Players;

namespace block_racing_server.Game.Rules;

public class GameEndResult
{
    public Player? Winner { get; }
    public Player? Loser { get; }
    public GameEndReason Reason { get; }

    public GameEndResult(Player? winner, Player? loser, GameEndReason reason)
    {
        Winner = winner;
        Loser = loser;
        Reason = reason;
    }
}
