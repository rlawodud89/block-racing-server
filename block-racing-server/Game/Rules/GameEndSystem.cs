using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations;
using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Rules;

public class GameEndSystem
{
    public GameEndResult? Update(GameState gameState)
    {
        Player[] finishedPlayers = gameState.Players.Values
            .Where(player =>
                player.Car.Distance >= gameState.TargetDistance)
            .ToArray();

        // 아무도 결승선에 도달하지 않음
        if (finishedPlayers.Length == 0)
            return null;

        // 두 플레이어가 동시에 결승선 도달
        if (finishedPlayers.Length == 2)
            return new GameEndResult(
                winner: null,
                loser: null,
                reason: GameEndReason.Normal
            );

        // 한 명만 결승선 도달
        Player winner = finishedPlayers[0];

        Player loser = gameState.Players.Values
            .First(player => player.Id != winner.Id);

        return new GameEndResult(
            winner,
            loser,
            GameEndReason.Normal
        );
    }
}