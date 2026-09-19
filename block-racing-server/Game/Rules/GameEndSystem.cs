using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations;
using block_racing_common.Game.Enums;
using block_racing_server.Data;

namespace block_racing_server.Game.Rules;

public class GameEndSystem
{
    public GameEndResult? Update(GameState gameState)
    {
        Player? winner = null;
        Player? loser = null;
        int finishedCount = 0;

        foreach (Player player in gameState.PlayerDictionary.Values)
        {
            if (player.Car.Distance >= GameBalance.TargetDistance)
            {
                finishedCount++;
                winner = player;
            }
            else
            {
                loser = player;
            }
        }

        if (finishedCount == 0)
            return null;

        if (finishedCount == 2)
        {
            return new GameEndResult(
                winner: null,
                loser: null,
                reason: GameEndReason.Normal
            );
        }

        return new GameEndResult(
            winner!,
            loser!,
            GameEndReason.Normal
        );
    }
}