using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations;

namespace block_racing_server.Game.Rules;

public class GameEndSystem
{
    public GameEndResult Update(GameState gameState)
    {
        foreach (Player player in gameState.Players.Values)
        {
            if (player.Car.Distance >= gameState.TargetDistance)
            {
                Player loser = gameState.Players.Values
                                .First(p => p.Id != player.Id);

                return new GameEndResult(
                    player,
                    loser);
            }
        }

        return null;
    }
}
