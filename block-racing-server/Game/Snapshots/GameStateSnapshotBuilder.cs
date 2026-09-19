using block_racing_common.Game.Snapshots;
using block_racing_server.Data;
using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations;

namespace block_racing_server.Game.Snapshots;

public static class GameStateSnapshotBuilder
{
    public static GameStateSnapshot Create(GameState gameState)
    {
        var players =
            new List<PlayerSnapshot>(
                gameState.PlayerDictionary.Count);

        foreach (Player player in gameState.PlayerDictionary.Values)
        {
            players.Add(
                PlayerSnapshotBuilder.Create(player));
        }


        return new GameStateSnapshot(
            gameState.Tick,
            GameBalance.TargetDistance,
            players
        );
    }
}