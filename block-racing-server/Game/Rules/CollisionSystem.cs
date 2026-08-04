using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;

public class CollisionSystem
{
    public void Update(
        IReadOnlyDictionary<int, Player> players)
    {
        foreach (Player player in players.Values)
        {
            CheckCarCollision(player);
        }
    }


    private void CheckCarCollision(Player player)
    {
        Car car = player.Car;
        Lane lane = player.Lane;

        if (car.IsInvincible)
            return;

        for (int y = 0; y < Car.Height; y++)
        {
            for (int x = 0; x < Car.Width; x++)
            {
                int gridX = car.X + x;
                int gridY = y;

                if (lane.HasBlock(gridX, gridY))
                {
                    car.OnCollision();
                    return;
                }
            }
        }
    }

}