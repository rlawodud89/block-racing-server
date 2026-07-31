using block_racing_common.Game.Snapshots;
using block_racing_server.Game.Players;

namespace block_racing_server.Game.Snapshots;

public static class PlayerSnapshotBuilder
{
    public static PlayerSnapshot Create(Player player)
    {
        return new PlayerSnapshot(
            player.Id,
            player.Car.X,
            player.Car.Distance,
            player.Car.Speed,
            player.Car.IsStunned,
            player.Mode,
            player.CurrentPiece?.Type,
            player.CurrentPiece?.Rotation,
            player.PieceCooldown,
            LaneSnapshotBuilder.Create(player.Lane)
        );
    }
}