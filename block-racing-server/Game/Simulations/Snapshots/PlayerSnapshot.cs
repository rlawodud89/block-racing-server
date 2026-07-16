using block_racing_server.Game.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations.Snapshots;

public class PlayerSnapshot
{
    public int Id { get; }

    public int CarX { get; }

    public float Distance { get; }

    public float Speed { get; }

    public bool IsStunned { get; }

    public byte Mode { get; }

    public LaneSnapshot Lane { get; }

    public IReadOnlyList<FlyingBlockSnapshot> FlyingBlocks { get; }


    public PlayerSnapshot(Player player)
    {
        Id = player.Id;

        CarX = player.Car.X;

        Distance = player.Car.Distance;

        Speed = player.Car.Speed;

        IsStunned = player.Car.IsStunned;

        Mode = (byte)player.Mode;

        Lane = new LaneSnapshot(player.Lane);

        FlyingBlocks =
            player.Lane.FlyingBlocks
            .Select(
                b => new FlyingBlockSnapshot(b))
            .ToList();
    }

    public PlayerSnapshot(int id, int carX, float distance,
        float speed, bool isStunned, byte mode,
        LaneSnapshot lane, IReadOnlyList<FlyingBlockSnapshot> flyingBlocks)
    {
        Id = id;

        CarX = carX;

        Distance = distance;

        Speed = speed;

        IsStunned = isStunned;

        Mode = mode;

        Lane = lane;

        FlyingBlocks = flyingBlocks;
    }
}
