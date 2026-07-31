using block_racing_common.Game.Snapshots;
using block_racing_server.Game.Simulations;

namespace block_racing_server.Game.Snapshots;

public static class GameStateSnapshotBuilder
{
    public static GameStateSnapshot Create(GameState gameState)
    {
        var players = gameState.Players.Values
            .Select(PlayerSnapshotBuilder.Create)
            .ToList();


        return new GameStateSnapshot(
            gameState.Tick,
            gameState.TargetDistance,
            players
        );
    }
}